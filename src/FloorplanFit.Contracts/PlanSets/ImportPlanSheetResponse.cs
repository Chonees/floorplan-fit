namespace FloorplanFit.Contracts.PlanSets;

public sealed record ImportPlanSheetResponse(
    Guid SheetId,
    Guid PlanSetVersionId,
    string SheetType,
    string Name,
    Guid ImportedDocumentId,
    Guid MeasurementContextId,
    string Status,
    DateTime ImportedAtUtc);
