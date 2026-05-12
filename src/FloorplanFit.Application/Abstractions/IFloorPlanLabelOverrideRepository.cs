using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IFloorPlanLabelOverrideRepository
{
    Task<IReadOnlyList<FloorPlanLabelOverride>> ListByCurationAsync(Guid floorPlanCurationId, CancellationToken cancellationToken);

    Task UpsertAsync(FloorPlanLabelOverride labelOverride, CancellationToken cancellationToken);
}
