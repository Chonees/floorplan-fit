using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Contracts.PlanSets;

public sealed record ProjectFacadeElevationSheetAdjustmentRequest(
    Guid SheetRegistrationId,
    Guid CanonicalAdjustmentId,
    AdjustedSitePlanPlacementDto CanonicalPlacement,
    AdjustmentRecipeSummaryDto? CanonicalRecipe = null);
