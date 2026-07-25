using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;

namespace FloorplanFit.Application.Abstractions;

public interface ICommissionedHouseAdaptationProfileRepository
{
    Task UpsertAsync(
        CommissionedHouseAdaptationProfile profile,
        CancellationToken cancellationToken);

    Task<CommissionedHouseAdaptationProfile?> GetByFloorPlanVersionIdAsync(
        Guid floorPlanVersionId,
        Guid expectedPublishedCurationId,
        CancellationToken cancellationToken);

    Task RemoveByFloorPlanVersionIdAsync(
        Guid floorPlanVersionId,
        CancellationToken cancellationToken);
}
