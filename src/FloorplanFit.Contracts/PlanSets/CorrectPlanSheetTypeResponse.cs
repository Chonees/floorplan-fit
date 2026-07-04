namespace FloorplanFit.Contracts.PlanSets;

public sealed record CorrectPlanSheetTypeResponse(
    Guid SheetId,
    Guid PlanSetVersionId,
    string SheetType,
    string PreviousSheetType,
    string Status,
    DateTime CorrectedAtUtc);
