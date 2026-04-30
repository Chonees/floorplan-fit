using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.Documents;
using FloorplanFit.Domain.FloorPlans;
using FloorplanFit.Domain.Measurement;

namespace FloorplanFit.Application.FloorPlans.Import;

public sealed class ImportFloorPlanHandler
{
    private readonly IDxfGateway dxfGateway;
    private readonly IManagedFileStorage managedFileStorage;
    private readonly IFloorPlanTemplateRepository floorPlanTemplateRepository;
    private readonly IFloorPlanVersionRepository floorPlanVersionRepository;
    private readonly IImportedDocumentRepository importedDocumentRepository;
    private readonly IMeasurementContextRepository measurementContextRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IFileHashService fileHashService;
    private readonly IClock clock;
    private readonly ImportFloorPlanResultFactory resultFactory;

    public ImportFloorPlanHandler(
        IDxfGateway dxfGateway,
        IManagedFileStorage managedFileStorage,
        IFloorPlanTemplateRepository floorPlanTemplateRepository,
        IFloorPlanVersionRepository floorPlanVersionRepository,
        IImportedDocumentRepository importedDocumentRepository,
        IMeasurementContextRepository measurementContextRepository,
        IUnitOfWork unitOfWork,
        IFileHashService fileHashService,
        IClock clock,
        ImportFloorPlanResultFactory resultFactory)
    {
        this.dxfGateway = dxfGateway;
        this.managedFileStorage = managedFileStorage;
        this.floorPlanTemplateRepository = floorPlanTemplateRepository;
        this.floorPlanVersionRepository = floorPlanVersionRepository;
        this.importedDocumentRepository = importedDocumentRepository;
        this.measurementContextRepository = measurementContextRepository;
        this.unitOfWork = unitOfWork;
        this.fileHashService = fileHashService;
        this.clock = clock;
        this.resultFactory = resultFactory;
    }

    public async Task<ImportFloorPlanResponse> HandleAsync(
        ImportFloorPlanRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.FilePath))
        {
            throw new ArgumentException("A floor plan file path is required.", nameof(request));
        }

        var sourceFileName = Path.GetFileName(request.FilePath);
        var sourceSuggestedName = Path.GetFileNameWithoutExtension(request.FilePath);
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
            ImportedDocumentType.FloorPlanDxf,
            sourceFileName,
            managedFilePath,
            sha256,
            detectedDocument.DxfVersion,
            measurementContext.Id,
            importedAtUtc);

        var normalizedCode = FloorPlanCodeNormalizer.Normalize(sourceSuggestedName);
        var existingTemplate = await floorPlanTemplateRepository.GetByCodeAsync(normalizedCode, cancellationToken);

        var template = existingTemplate ?? new FloorPlanTemplate(
            Guid.NewGuid(),
            normalizedCode,
            sourceSuggestedName,
            isActive: true);

        var versionNumber = existingTemplate is null
            ? 1
            : await floorPlanVersionRepository.GetNextVersionNumberAsync(template.Id, cancellationToken);

        var version = new FloorPlanVersion(
            Guid.NewGuid(),
            template.Id,
            importedDocument.Id,
            detectedDocument.GeometryFingerprint,
            versionNumber,
            importedAtUtc);

        template.SetCurrentVersion(version.Id);

        await measurementContextRepository.AddAsync(measurementContext, cancellationToken);
        await importedDocumentRepository.AddAsync(importedDocument, cancellationToken);

        if (existingTemplate is null)
        {
            await floorPlanTemplateRepository.AddAsync(template, cancellationToken);
        }
        else
        {
            await floorPlanTemplateRepository.UpdateAsync(template, cancellationToken);
        }

        await floorPlanVersionRepository.AddAsync(version, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return resultFactory.Create(template, version, measurementContext);
    }
}
