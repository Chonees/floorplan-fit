namespace FloorplanFit.Contracts.PlanSets;

public sealed record ImportPlanSheetRequest(
    Guid PlanSetVersionId,
    string SheetType,
    string FilePath,
    string? Name = null);
