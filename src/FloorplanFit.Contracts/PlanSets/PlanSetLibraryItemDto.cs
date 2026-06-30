namespace FloorplanFit.Contracts.PlanSets;

public sealed record PlanSetLibraryItemDto(
    Guid HousePlanSetId,
    string Code,
    string Name,
    Guid? ActivePlanSetVersionId,
    Guid? CanonicalFloorPlanVersionId,
    Guid? ActivePublishedCurationId,
    IReadOnlyList<PlanSetSheetDto> Sheets)
{
    public bool HasCanonicalFloorPlan => CanonicalFloorPlanVersionId.HasValue;

    public bool CanProduceCanonicalAdjustment =>
        CanonicalFloorPlanVersionId.HasValue && ActivePublishedCurationId.HasValue;
}
