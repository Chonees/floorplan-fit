using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IFloorPlanDimensionOverrideRepository
{
    Task<IReadOnlyList<FloorPlanDimensionOverride>> ListByCurationAsync(Guid floorPlanCurationId, CancellationToken cancellationToken);

    Task UpsertAsync(FloorPlanDimensionOverride dimensionOverride, CancellationToken cancellationToken);

    Task DeleteAsync(Guid floorPlanCurationId, string sourceDimensionKey, CancellationToken cancellationToken);

    Task MarkExportedAsync(Guid floorPlanCurationId, IReadOnlyList<string> sourceDimensionKeys, DateTime exportedAtUtc, CancellationToken cancellationToken);
}
