using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Extraction;

public sealed class ExtractWallCandidatesHandler
{
    private readonly IWallExtractor wallExtractor;
    private readonly IWallExtractionRunRepository wallExtractionRunRepository;
    private readonly IExtractedWallCandidateRepository extractedWallCandidateRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;

    public ExtractWallCandidatesHandler(
        IWallExtractor wallExtractor,
        IWallExtractionRunRepository wallExtractionRunRepository,
        IExtractedWallCandidateRepository extractedWallCandidateRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        this.wallExtractor = wallExtractor;
        this.wallExtractionRunRepository = wallExtractionRunRepository;
        this.extractedWallCandidateRepository = extractedWallCandidateRepository;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
    }

    public async Task<(Guid ExtractionRunId, string Status)> HandleAsync(
        Guid floorPlanVersionId,
        string managedFilePath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(managedFilePath);

        var startedAtUtc = clock.UtcNow;
        var run = new WallExtractionRun(
            Guid.NewGuid(),
            floorPlanVersionId,
            status: "Completed",
            startedAtUtc,
            finishedAtUtc: startedAtUtc,
            extractorVersion: "ixmilia-line-segments",
            errorMessage: null);

        var detectedCandidates = await wallExtractor.ExtractAsync(managedFilePath, cancellationToken);
        var domainCandidates = detectedCandidates
            .Select((item, index) => new ExtractedWallCandidate(
                Guid.NewGuid(),
                run.Id,
                item.SourceEntityRef,
                item.SourceLayer,
                geometryPathId: Guid.Empty,
                item.ThicknessMm,
                item.Confidence,
                item.DetectionNotes,
                ExtractedWallCandidateStatus.Pending,
                sortOrder: index + 1))
            .ToArray();

        await wallExtractionRunRepository.AddAsync(run, cancellationToken);
        await extractedWallCandidateRepository.AddRangeAsync(domainCandidates, detectedCandidates, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return (run.Id, "Extracted");
    }
}
