namespace FloorplanFit.Domain.PlanSets;

public sealed class CanonicalFloorPlanAdjustment
{
    public CanonicalFloorPlanAdjustment(
        Guid id,
        Guid planSetVersionId,
        Guid canonicalFloorPlanVersionId,
        string sitePlanSourcePath,
        string canonicalFloorPlanExportPath,
        string placementJson,
        string adjustmentRecipeJson,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Canonical adjustment id is required.", nameof(id));
        }

        if (planSetVersionId == Guid.Empty)
        {
            throw new ArgumentException("Plan set version is required.", nameof(planSetVersionId));
        }

        if (canonicalFloorPlanVersionId == Guid.Empty)
        {
            throw new ArgumentException("Canonical floor-plan version is required.", nameof(canonicalFloorPlanVersionId));
        }

        if (string.IsNullOrWhiteSpace(sitePlanSourcePath))
        {
            throw new ArgumentException("Site plan source path is required.", nameof(sitePlanSourcePath));
        }

        if (string.IsNullOrWhiteSpace(canonicalFloorPlanExportPath))
        {
            throw new ArgumentException("Canonical floor-plan export path is required.", nameof(canonicalFloorPlanExportPath));
        }

        if (string.IsNullOrWhiteSpace(placementJson))
        {
            throw new ArgumentException("Placement json is required.", nameof(placementJson));
        }

        if (string.IsNullOrWhiteSpace(adjustmentRecipeJson))
        {
            throw new ArgumentException("Adjustment recipe json is required.", nameof(adjustmentRecipeJson));
        }

        Id = id;
        PlanSetVersionId = planSetVersionId;
        CanonicalFloorPlanVersionId = canonicalFloorPlanVersionId;
        SitePlanSourcePath = sitePlanSourcePath.Trim();
        CanonicalFloorPlanExportPath = canonicalFloorPlanExportPath.Trim();
        PlacementJson = placementJson;
        AdjustmentRecipeJson = adjustmentRecipeJson;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public Guid PlanSetVersionId { get; }

    public Guid CanonicalFloorPlanVersionId { get; }

    public string SitePlanSourcePath { get; }

    public string CanonicalFloorPlanExportPath { get; }

    public string PlacementJson { get; }

    public string AdjustmentRecipeJson { get; }

    public DateTime CreatedAtUtc { get; }
}
