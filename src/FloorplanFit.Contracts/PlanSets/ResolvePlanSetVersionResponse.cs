namespace FloorplanFit.Contracts.PlanSets;

public sealed record ResolvePlanSetVersionResponse(
    Guid PlanSetVersionId,
    Guid HousePlanSetId,
    Guid CanonicalFloorPlanVersionId,
    int VersionNumber,
    bool Created);
