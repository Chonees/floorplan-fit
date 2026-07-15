using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Contracts.PlanSets;

public sealed record CreateMultiSheetExportAuditRequest(
    Guid PlanSetVersionId,
    Guid CanonicalFloorPlanVersionId,
    Guid CanonicalAdjustmentId,
    string CanonicalFloorPlanExportPath,
    IReadOnlyList<MultiSheetExportProjectionRequestDto> DependentProjections)
{
    public bool DiscoverAllDependentSheets { get; init; }

    public AdjustedSitePlanPlacementDto? CanonicalPlacement { get; init; }

    public AdjustmentRecipeSummaryDto? CanonicalRecipe { get; init; }

    public string? CanonicalFloorPlanVerificationPath { get; init; }

    public IReadOnlyList<PlanSetPackageArtifactDto> PackageArtifacts { get; init; } = [];
}
