using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Contracts.PlanSets;

public sealed record ProjectRegisteredPlanSetSheetsRequest(
    Guid PlanSetVersionId,
    Guid CanonicalAdjustmentId,
    AdjustedSitePlanPlacementDto CanonicalPlacement,
    AdjustmentRecipeSummaryDto? CanonicalRecipe = null);
