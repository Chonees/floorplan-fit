namespace FloorplanFit.Contracts.PlanSets;

public sealed record ResolveHousePlanSetResponse(
    Guid HousePlanSetId,
    Guid SourceFloorPlanTemplateId,
    string Code,
    string Name,
    bool Created);
