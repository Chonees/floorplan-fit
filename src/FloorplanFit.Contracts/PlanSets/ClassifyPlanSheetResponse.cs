namespace FloorplanFit.Contracts.PlanSets;

public sealed record ClassifyPlanSheetResponse(
    string SheetType,
    decimal Confidence,
    bool RequiresManualConfirmation,
    string Reason);
