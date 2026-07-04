namespace FloorplanFit.Contracts.PlanSets;

public sealed record CreateMultiSheetExportAuditRequest(
    Guid PlanSetVersionId,
    Guid CanonicalFloorPlanVersionId,
    Guid CanonicalAdjustmentId,
    string CanonicalFloorPlanExportPath,
    IReadOnlyList<MultiSheetExportProjectionRequestDto> DependentProjections)
{
    public bool DiscoverAllDependentSheets { get; init; }
}
