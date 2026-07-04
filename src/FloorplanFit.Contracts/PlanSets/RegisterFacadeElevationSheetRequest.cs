namespace FloorplanFit.Contracts.PlanSets;

public sealed record RegisterFacadeElevationSheetRequest(
    Guid PlanSetVersionId,
    Guid FacadeElevationSheetId,
    decimal HorizontalScale,
    decimal HorizontalOffset,
    decimal Confidence,
    bool ConfirmRegistration,
    string? HorizontalReferenceName = null,
    string? Warning = null);
