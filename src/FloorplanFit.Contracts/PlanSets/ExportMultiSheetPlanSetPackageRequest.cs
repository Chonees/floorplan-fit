namespace FloorplanFit.Contracts.PlanSets;

public sealed record ExportMultiSheetPlanSetPackageRequest(
    Guid PlanSetVersionId,
    Guid CanonicalFloorPlanVersionId,
    Guid CanonicalAdjustmentId,
    string CanonicalFloorPlanExportPath,
    string PackageDirectory,
    IReadOnlyList<Guid> DependentProjectionIds);
