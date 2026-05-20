using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IExtractedWallCandidateRepository
{
    Task AddRangeAsync(
        IReadOnlyList<ExtractedWallCandidate> domainCandidates,
        IReadOnlyList<DetectedWallCandidate> detectedCandidates,
        CancellationToken cancellationToken);

    Task<ExtractedWallCandidate?> GetByIdAsync(Guid candidateId, CancellationToken cancellationToken);

    Task UpdateAsync(ExtractedWallCandidate candidate, CancellationToken cancellationToken);
}
