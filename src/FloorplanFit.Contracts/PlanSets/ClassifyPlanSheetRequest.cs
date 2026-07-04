namespace FloorplanFit.Contracts.PlanSets;

public sealed record ClassifyPlanSheetRequest(
    string FileName,
    string? SheetTitle = null,
    IReadOnlyList<string>? LayerHints = null);
