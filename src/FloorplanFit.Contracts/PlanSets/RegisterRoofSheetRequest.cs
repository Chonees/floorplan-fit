namespace FloorplanFit.Contracts.PlanSets;

public sealed record RegisterRoofSheetRequest(
    Guid PlanSetVersionId,
    Guid RoofSheetId,
    decimal Scale,
    decimal RotationDegrees,
    decimal TranslateX,
    decimal TranslateY,
    decimal Confidence,
    decimal OverhangInches,
    bool ConfirmRegistration,
    string? Warning = null);
