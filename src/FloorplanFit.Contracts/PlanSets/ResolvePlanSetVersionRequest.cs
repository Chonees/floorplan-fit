namespace FloorplanFit.Contracts.PlanSets;

public sealed record ResolvePlanSetVersionRequest(
    Guid HousePlanSetId,
    Guid CanonicalFloorPlanVersionId);
