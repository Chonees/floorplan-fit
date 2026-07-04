namespace FloorplanFit.Contracts.PlanSets;

public sealed record CorrectPlanSheetTypeRequest(
    Guid PlanSetVersionId,
    Guid SheetId,
    string SheetType,
    string? Reason = null);
