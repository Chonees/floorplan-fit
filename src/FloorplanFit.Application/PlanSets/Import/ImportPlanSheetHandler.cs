using FloorplanFit.Application.Abstractions;
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

    public ImportPlanSheetHandler(
        IDxfGateway dxfGateway,
        IManagedFileStorage managedFileStorage,
        IImportedDocumentRepository importedDocumentRepository,
        IMeasurementContextRepository measurementContextRepository,
        IPlanSheetRepository planSheetRepository,
        IUnitOfWork unitOfWork,
        IFileHashService fileHashService,
        IClock clock)
    {
        this.dxfGateway = dxfGateway;
        this.managedFileStorage = managedFileStorage;
        this.importedDocumentRepository = importedDocumentRepository;
        this.measurementContextRepository = measurementContextRepository;
        this.planSheetRepository = planSheetRepository;
        this.unitOfWork = unitOfWork;
        this.fileHashService = fileHashService;
        this.clock = clock;
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

        if (!Enum.TryParse<PlanSheetType>(request.SheetType, ignoreCase: true, out var sheetType) ||
            sheetType is PlanSheetType.Unknown)
        {
            throw new ArgumentException("A known dependent sheet type is required.", nameof(request));
        }

        if (sheetType is PlanSheetType.FloorPlan)
        {
            throw new ArgumentException("Floor plans must use the canonical floor-plan import flow.", nameof(request));
        }

        var sourceFileName = Path.GetFileName(request.FilePath);
        var sheetName = string.IsNullOrWhiteSpace(request.Name)
            ? Path.GetFileNameWithoutExtension(request.FilePath)
            : request.Name.Trim();
        var managedFilePath = await managedFileStorage.CopyIntoLibraryAsync(request.FilePath, cancellationToken);
        var detectedDocument = await dxfGateway.ReadFloorPlanAsync(managedFilePath, cancellationToken);
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
            sheetType,
            importedDocument.Id,
            measurementContext.Id,
            sheetName,
            PlanSheetStatus.Imported,
            importedAtUtc);

        await measurementContextRepository.AddAsync(measurementContext, cancellationToken);
        await importedDocumentRepository.AddAsync(importedDocument, cancellationToken);
        await planSheetRepository.AddAsync(sheet, cancellationToken);
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
}
