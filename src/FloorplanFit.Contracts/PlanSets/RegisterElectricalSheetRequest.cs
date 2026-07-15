namespace FloorplanFit.Contracts.PlanSets;

public sealed record RegisterElectricalSheetRequest(
    Guid PlanSetVersionId,
    Guid ElectricalSheetId);
