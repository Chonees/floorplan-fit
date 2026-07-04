namespace FloorplanFit.Contracts.PlanSets;

public sealed record PlanSheetSourceDto(
    Guid SheetId,
    string SheetType,
    string Name,
    Guid ImportedDocumentId,
    string SourceFilePath);
