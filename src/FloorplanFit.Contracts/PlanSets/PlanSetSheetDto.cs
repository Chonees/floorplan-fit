namespace FloorplanFit.Contracts.PlanSets;

public sealed record PlanSetSheetDto(
    Guid SheetId,
    string SheetType,
    string Name,
    Guid ImportedDocumentId,
    Guid? SourceFloorPlanVersionId,
    bool IsCanonical,
    string RegistrationStatus,
    string ProjectionStatus);
