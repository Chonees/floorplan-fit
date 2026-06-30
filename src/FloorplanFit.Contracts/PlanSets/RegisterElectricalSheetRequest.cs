namespace FloorplanFit.Contracts.PlanSets;

public sealed record RegisterElectricalSheetRequest(
    Guid PlanSetVersionId,
    Guid ElectricalSheetId,
    decimal Scale,
    decimal RotationDegrees,
    decimal TranslateX,
    decimal TranslateY,
    decimal Confidence,
    bool ConfirmRegistration,
    string? Warning = null);
