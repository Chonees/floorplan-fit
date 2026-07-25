using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Contracts.PlanSets;

public sealed record ExportMultiSheetPlanSetPackageRequest(
    Guid PlanSetVersionId,
    Guid CanonicalFloorPlanVersionId,
    Guid CanonicalAdjustmentId,
    string CanonicalFloorPlanExportPath,
    string PackageDirectory,
    IReadOnlyList<Guid> DependentProjectionIds)
{
    public AdjustedSitePlanPlacementDto? CanonicalPlacement { get; init; }

    public AdjustmentRecipeSummaryDto? CanonicalRecipe { get; init; }

    public bool DeleteCanonicalSourceAfterSuccess { get; init; }

    /// <summary>
    /// When true, automatic discovery requires the latest ElectricalPlan projection to be
    /// ReadyForExport before any package staging. Missing or non-ready Electrical fails closed
    /// instead of publishing a FloorPlan-only package. Explicit projection-ID requests are unaffected.
    /// </summary>
    public bool RequireReadyElectricalPlan { get; init; }
}
