using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Contracts.PlanSets;

public sealed record ProjectRoofSheetAdjustmentRequest(
    Guid SheetRegistrationId,
    Guid CanonicalAdjustmentId,
    AdjustedSitePlanPlacementDto CanonicalPlacement);
