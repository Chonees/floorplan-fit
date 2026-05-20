using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IFloorPlanDimensionBindingOverrideRepository
{
    Task<IReadOnlyList<FloorPlanDimensionBindingOverride>> ListByCurationAsync(Guid floorPlanCurationId, CancellationToken cancellationToken);

    Task UpsertAsync(FloorPlanDimensionBindingOverride bindingOverride, CancellationToken cancellationToken);

    Task DeleteAsync(Guid floorPlanCurationId, string sourceDimensionKey, CancellationToken cancellationToken);
}
