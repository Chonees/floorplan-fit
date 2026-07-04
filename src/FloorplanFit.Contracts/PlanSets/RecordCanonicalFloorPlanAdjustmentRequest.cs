using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Contracts.PlanSets;

public sealed record RecordCanonicalFloorPlanAdjustmentRequest(
    Guid PlanSetVersionId,
    Guid CanonicalFloorPlanVersionId,
    string SitePlanSourcePath,
    string CanonicalFloorPlanExportPath,
    AdjustedSitePlanPlacementDto Placement);
