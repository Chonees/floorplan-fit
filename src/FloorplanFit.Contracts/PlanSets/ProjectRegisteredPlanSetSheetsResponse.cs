namespace FloorplanFit.Contracts.PlanSets;

public sealed record ProjectRegisteredPlanSetSheetsResponse(
    int ProjectedSheetCount,
    IReadOnlyList<SheetAdjustmentProjectionDto> Projections);