using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IExtractedFixedPlanComponentRepository
{
    Task AddRangeAsync(
        IReadOnlyList<ExtractedFixedPlanComponent> domainComponents,
        IReadOnlyList<DetectedFixedPlanComponent> detectedComponents,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ExtractedFixedPlanComponent>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken);

    Task RemoveAsync(Guid fixedPlanComponentId, CancellationToken cancellationToken);
}
