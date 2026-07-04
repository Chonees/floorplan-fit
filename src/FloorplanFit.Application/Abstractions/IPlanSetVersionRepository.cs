using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Abstractions;

public interface IPlanSetVersionRepository
{
    Task<PlanSetVersion?> GetByCanonicalFloorPlanVersionAsync(
        Guid canonicalFloorPlanVersionId,
        CancellationToken cancellationToken);

    Task AddAsync(PlanSetVersion version, CancellationToken cancellationToken);
}
