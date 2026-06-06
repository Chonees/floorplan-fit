using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IDimensionIntervalBindingRepository
{
    Task<IReadOnlyList<DimensionIntervalBinding>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken);

    Task UpsertAsync(DimensionIntervalBinding binding, CancellationToken cancellationToken);

    Task DeleteAsync(Guid floorPlanCurationId, Guid dimensionId, CancellationToken cancellationToken);

    Task DeleteByCorridorAsync(Guid floorPlanCurationId, Guid corridorId, CancellationToken cancellationToken);

    Task DeleteByNodeAsync(Guid floorPlanCurationId, Guid nodeId, CancellationToken cancellationToken);
}
