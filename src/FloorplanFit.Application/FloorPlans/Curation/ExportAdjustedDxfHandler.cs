using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.Documents;

namespace FloorplanFit.Application.FloorPlans.Curation;

public class ExportAdjustedDxfHandler
{
    private readonly IFloorPlanExtractionSourceReader extractionSourceReader;
    private readonly IFloorPlanReviewSessionReader reviewSessionReader;
    private readonly IFloorPlanDimensionOverrideRepository dimensionOverrideRepository;
    private readonly IImportedDocumentRepository importedDocumentRepository;
    private readonly IManagedFileStorage managedFileStorage;
    private readonly IAdjustedDxfExporter adjustedDxfExporter;
    private readonly IFileHashService fileHashService;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;

    public ExportAdjustedDxfHandler(
        IFloorPlanExtractionSourceReader extractionSourceReader,
        IFloorPlanReviewSessionReader reviewSessionReader,
        IFloorPlanDimensionOverrideRepository dimensionOverrideRepository,
        IImportedDocumentRepository importedDocumentRepository,
        IManagedFileStorage managedFileStorage,
        IAdjustedDxfExporter adjustedDxfExporter,
        IFileHashService fileHashService,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        this.extractionSourceReader = extractionSourceReader;
        this.reviewSessionReader = reviewSessionReader;
        this.dimensionOverrideRepository = dimensionOverrideRepository;
        this.importedDocumentRepository = importedDocumentRepository;
        this.managedFileStorage = managedFileStorage;
        this.adjustedDxfExporter = adjustedDxfExporter;
        this.fileHashService = fileHashService;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
    }

    public virtual async Task<ExportAdjustedDxfResponse> HandleAsync(
        Guid templateId,
        Guid? floorPlanVersionId,
        Guid curationId,
        CancellationToken cancellationToken)
    {
        var source = floorPlanVersionId is Guid versionId
            ? await extractionSourceReader.GetByVersionAsync(versionId, cancellationToken)
            : await extractionSourceReader.GetCurrentSourceAsync(templateId, cancellationToken);
        if (source is null)
        {
            throw new InvalidOperationException("Floor plan extraction source was not found.");
        }

        var session = floorPlanVersionId is Guid selectedVersionId
            ? await reviewSessionReader.GetByVersionAsync(templateId, selectedVersionId, cancellationToken)
            : await reviewSessionReader.GetByTemplateAsync(templateId, cancellationToken);
        if (session is null)
        {
            throw new InvalidOperationException("Floor plan review session was not found.");
        }

        var exportDimensions = session.Dimensions
            .Where(item => item.IsEdited)
            .ToArray();
        if (exportDimensions.Length == 0)
        {
            throw new InvalidOperationException("No edited native dimensions are available to export.");
        }

        var fileName = source.OriginalFileName ?? Path.GetFileName(source.ManagedFilePath);
        var outputPath = await managedFileStorage.ReserveAdjustedDxfPathAsync(fileName, cancellationToken);
        await adjustedDxfExporter.ExportAsync(source.ManagedFilePath, outputPath, exportDimensions, cancellationToken);

        var importedDocumentId = Guid.NewGuid();
        var measurementContextId = source.MeasurementContextId
            ?? throw new InvalidOperationException("Measurement context is required to persist an adjusted DXF.");
        var sha256 = await fileHashService.ComputeSha256Async(outputPath, cancellationToken);
        var importedDocument = new ImportedDocument(
            importedDocumentId,
            ImportedDocumentType.ExportedAdjustedDxf,
            Path.GetFileName(outputPath),
            outputPath,
            sha256,
            source.DxfVersion,
            measurementContextId,
            clock.UtcNow);
        await importedDocumentRepository.AddAsync(importedDocument, cancellationToken);
        await dimensionOverrideRepository.MarkExportedAsync(
            curationId,
            exportDimensions
                .Select(SaveFloorPlanDimensionOverrideHandler.ResolveSourceDimensionKey)
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            clock.UtcNow,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ExportAdjustedDxfResponse(importedDocumentId, outputPath, exportDimensions.Length);
    }
}
