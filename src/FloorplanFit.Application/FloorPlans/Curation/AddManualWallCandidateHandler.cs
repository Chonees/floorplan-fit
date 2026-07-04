using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class AddManualWallCandidateHandler
{
    private const decimal MinimumLineLength = 0.001m;

    private readonly IFloorPlanTemplateRepository floorPlanTemplateRepository;
    private readonly IWallExtractionRunRepository wallExtractionRunRepository;
    private readonly IExtractedWallCandidateRepository extractedWallCandidateRepository;
    private readonly IUnitOfWork unitOfWork;

    public AddManualWallCandidateHandler(
        IFloorPlanTemplateRepository floorPlanTemplateRepository,
        IWallExtractionRunRepository wallExtractionRunRepository,
        IExtractedWallCandidateRepository extractedWallCandidateRepository,
        IUnitOfWork unitOfWork)
    {
        this.floorPlanTemplateRepository = floorPlanTemplateRepository;
        this.wallExtractionRunRepository = wallExtractionRunRepository;
        this.extractedWallCandidateRepository = extractedWallCandidateRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task<Guid> HandleAsync(
        Guid templateId,
        Guid? floorPlanVersionId,
        decimal startX,
        decimal startY,
        decimal endX,
        decimal endY,
        CancellationToken cancellationToken)
    {
        var resolvedVersionId = await ResolveFloorPlanVersionIdAsync(templateId, floorPlanVersionId, cancellationToken);
        if (!HasMeasurableLength(startX, startY, endX, endY))
        {
            throw new InvalidOperationException("Manual wall line must have a measurable length.");
        }

        var extractionRun = await wallExtractionRunRepository.GetLatestByVersionAsync(resolvedVersionId, cancellationToken)
            ?? throw new InvalidOperationException("A wall extraction run is required before adding manual wall lines.");
        var candidateId = Guid.NewGuid();
        var geometryPathId = Guid.NewGuid();
        var nextSortOrder = await extractedWallCandidateRepository.GetNextSortOrderAsync(extractionRun.Id, cancellationToken);
        var candidate = new ExtractedWallCandidate(
            candidateId,
            extractionRun.Id,
            $"MANUAL-WALL:{candidateId:N}",
            "MANUAL-WALLS",
            geometryPathId,
            thicknessMm: null,
            confidence: 1m,
            detectionNotes: "Manual wall line added in review.",
            ExtractedWallCandidateStatus.Accepted,
            nextSortOrder);
        var detectedCandidate = new DetectedWallCandidate(
            candidate.SourceEntityRef,
            candidate.SourceLayer ?? "MANUAL-WALLS",
            [new GeometryPoint(startX, startY), new GeometryPoint(endX, endY)],
            ThicknessMm: null,
            Confidence: 1m,
            DetectionNotes: candidate.DetectionNotes);

        await extractedWallCandidateRepository.AddAsync(candidate, detectedCandidate, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return candidateId;
    }

    private async Task<Guid> ResolveFloorPlanVersionIdAsync(
        Guid templateId,
        Guid? floorPlanVersionId,
        CancellationToken cancellationToken)
    {
        if (floorPlanVersionId is { } explicitVersionId)
        {
            return explicitVersionId;
        }

        var template = await floorPlanTemplateRepository.GetByIdAsync(templateId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan template was not found.");
        return template.CurrentVersionId
            ?? throw new InvalidOperationException("Floor plan template does not have an active version.");
    }

    private static bool HasMeasurableLength(decimal startX, decimal startY, decimal endX, decimal endY)
    {
        var deltaX = endX - startX;
        var deltaY = endY - startY;
        return Math.Sqrt(((double)deltaX * (double)deltaX) + ((double)deltaY * (double)deltaY)) >= (double)MinimumLineLength;
    }
}
