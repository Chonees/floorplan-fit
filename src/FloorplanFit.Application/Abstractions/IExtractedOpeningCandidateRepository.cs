using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IExtractedOpeningCandidateRepository
{
    Task AddRangeAsync(
        IReadOnlyList<ExtractedOpeningCandidate> domainCandidates,
        IReadOnlyList<DetectedOpeningCandidate> detectedCandidates,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ExtractedOpeningCandidate>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken);

    Task RemoveAsync(Guid openingCandidateId, CancellationToken cancellationToken);
}
