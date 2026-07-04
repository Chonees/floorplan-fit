using System.Text.Json;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Classification;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.Documents;
using FloorplanFit.Domain.Measurement;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.PlanSets.Import;

public sealed class ImportPlanSheetHandler
{
    private readonly IDxfGateway dxfGateway;
    private readonly IManagedFileStorage managedFileStorage;
    private readonly IImportedDocumentRepository importedDocumentRepository;
    private readonly IMeasurementContextRepository measurementContextRepository;
    private readonly IPlanSheetRepository planSheetRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IFileHashService fileHashService;
    private readonly IClock clock;
    private readonly ClassifyPlanSheetHandler? classifier;
    private readonly IPlanSetAuditEventRepository? planSetAuditEventRepository;

    public ImportPlanSheetHandler(
        IDxfGateway dxfGateway,
        IManagedFileStorage managedFileStorage,
        IImportedDocumentRepository importedDocumentRepository,
        IMeasurementContextRepository measurementContextRepository,
        IPlanSheetRepository planSheetRepository,
        IUnitOfWork unitOfWork,
        IFileHashService fileHashService,
        IClock clock,
        ClassifyPlanSheetHandler? classifier = null,
        IPlanSetAuditEventRepository? planSetAuditEventRepository = null)
    {
        this.dxfGateway = dxfGateway;
        this.managedFileStorage = managedFileStorage;
        this.importedDocumentRepository = importedDocumentRepository;
        this.measurementContextRepository = measurementContextRepository;
        this.planSheetRepository = planSheetRepository;
        this.unitOfWork = unitOfWork;
        this.fileHashService = fileHashService;
        this.clock = clock;
        this.classifier = classifier;
        this.planSetAuditEventRepository = planSetAuditEventRepository;
    }

    public async Task<ImportPlanSheetResponse> HandleAsync(
        ImportPlanSheetRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PlanSetVersionId == Guid.Empty)
        {
            throw new ArgumentException("Plan set version is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.FilePath))
        {
            throw new ArgumentException("A sheet file path is required.", nameof(request));
        }

        var sourceFileName = Path.GetFileName(request.FilePath);
        var sheetName = string.IsNullOrWhiteSpace(request.Name)
            ? Path.GetFileNameWithoutExtension(request.FilePath)
            : request.Name.Trim();
        var explicitSheetType = ResolveExplicitSheetType(request);
        if (explicitSheetType is PlanSheetType.FloorPlan)
        {
            throw new ArgumentException("Floor plans must use the canonical floor-plan import flow.", nameof(request));
        }

        var managedFilePath = await managedFileStorage.CopyIntoLibraryAsync(request.FilePath, cancellationToken);
        var detectedDocument = await dxfGateway.ReadFloorPlanAsync(managedFilePath, cancellationToken);
        var classification = explicitSheetType.HasValue
            ? new ResolvedSheetClassification(
                explicitSheetType.Value,
                "UserSelected",
                1m,
                "Sheet type selected by user.")
            : await ResolveClassifiedSheetTypeAsync(request, detectedDocument, cancellationToken);
        if (classification.SheetType is PlanSheetType.FloorPlan)
        {
            throw new ArgumentException("Floor plans must use the canonical floor-plan import flow.", nameof(request));
        }

        var importedAtUtc = clock.UtcNow;

        var measurementContext = new MeasurementContext(
            Guid.NewGuid(),
            detectedDocument.SourceUnit,
            detectedDocument.ToMillimetersFactor,
            1m,
            0.5m,
            importedAtUtc);

        var sha256 = await fileHashService.ComputeSha256Async(managedFilePath, cancellationToken);
        var importedDocument = new ImportedDocument(
            Guid.NewGuid(),
            ImportedDocumentType.PlanSheetDxf,
            sourceFileName,
            managedFilePath,
            sha256,
            detectedDocument.DxfVersion,
            measurementContext.Id,
            importedAtUtc);

        var sheet = new PlanSheet(
            Guid.NewGuid(),
            request.PlanSetVersionId,
            classification.SheetType,
            importedDocument.Id,
            measurementContext.Id,
            sheetName,
            PlanSheetStatus.Imported,
            importedAtUtc);

        await measurementContextRepository.AddAsync(measurementContext, cancellationToken);
        await importedDocumentRepository.AddAsync(importedDocument, cancellationToken);
        await planSheetRepository.AddAsync(sheet, cancellationToken);
        await TryRecordClassificationQualityEventAsync(sheet, classification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ImportPlanSheetResponse(
            sheet.Id,
            sheet.PlanSetVersionId,
            sheet.SheetType.ToString(),
            sheet.Name,
            sheet.ImportedDocumentId,
            sheet.MeasurementContextId,
            sheet.Status.ToString(),
            sheet.CreatedAtUtc);
    }

    private static PlanSheetType? ResolveExplicitSheetType(ImportPlanSheetRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SheetType))
        {
            return null;
        }

        if (Enum.TryParse<PlanSheetType>(request.SheetType, ignoreCase: true, out var explicitType) &&
            explicitType is not PlanSheetType.Unknown)
        {
            return explicitType;
        }

        throw new ArgumentException("A known dependent sheet type is required.", nameof(request));
    }

    private async Task<ResolvedSheetClassification> ResolveClassifiedSheetTypeAsync(
        ImportPlanSheetRequest request,
        DetectedFloorPlanDocument detectedDocument,
        CancellationToken cancellationToken)
    {
        if (classifier is null)
        {
            throw new ArgumentException("A known dependent sheet type is required.", nameof(request));
        }

        var classification = await classifier.HandleAsync(
            new ClassifyPlanSheetRequest(request.FilePath, request.Name, detectedDocument.LayerNames),
            cancellationToken);
        if (classification.RequiresManualConfirmation ||
            !Enum.TryParse<PlanSheetType>(classification.SheetType, out var classifiedType) ||
            classifiedType is PlanSheetType.Unknown)
        {
            throw new ArgumentException("A known dependent sheet type is required.", nameof(request));
        }

        return new ResolvedSheetClassification(
            classifiedType,
            "Classifier",
            classification.Confidence,
            classification.Reason);
    }

    private async Task TryRecordClassificationQualityEventAsync(
        PlanSheet sheet,
        ResolvedSheetClassification classification,
        CancellationToken cancellationToken)
    {
        if (planSetAuditEventRepository is null)
        {
            return;
        }

        try
        {
            await planSetAuditEventRepository.AddAsync(
                new PlanSetAuditEvent(
                    Guid.NewGuid(),
                    "PlanSheet",
                    sheet.Id,
                    "SheetClassificationQualityMeasured",
                    JsonSerializer.Serialize(new
                    {
                        planSetVersionId = sheet.PlanSetVersionId,
                        sheetType = sheet.SheetType.ToString(),
                        source = classification.Source,
                        confidence = classification.Confidence,
                        status = classification.Source == "Classifier" ? "AutoClassified" : "UserSelected",
                        warning = (string?)null,
                        ruleSummary = classification.Reason
                    }),
                    sheet.CreatedAtUtc),
                cancellationToken);
        }
        catch (Exception)
        {
            // ponytail: classification telemetry is best-effort; add retries only if import analytics becomes business-critical.
        }
    }

    private sealed record ResolvedSheetClassification(
        PlanSheetType SheetType,
        string Source,
        decimal? Confidence,
        string Reason);
}
