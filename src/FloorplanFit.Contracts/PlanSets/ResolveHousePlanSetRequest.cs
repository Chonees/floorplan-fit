namespace FloorplanFit.Contracts.PlanSets;

public sealed record ResolveHousePlanSetRequest(
    Guid SourceFloorPlanTemplateId,
    string Code,
    string Name);
