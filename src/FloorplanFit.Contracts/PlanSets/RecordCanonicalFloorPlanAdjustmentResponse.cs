using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Contracts.PlanSets;

public sealed record RecordCanonicalFloorPlanAdjustmentResponse(
    Guid AdjustmentId,
    Guid PlanSetVersionId,
    Guid CanonicalFloorPlanVersionId,
    string CanonicalFloorPlanExportPath,
    AdjustmentRecipeSummaryDto AdjustmentRecipe,
    DateTime CreatedAtUtc);
