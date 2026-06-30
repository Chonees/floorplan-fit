namespace FloorplanFit.Contracts.PlanSets;

public sealed record SheetRegistrationDto(
    Guid RegistrationId,
    Guid PlanSetVersionId,
    Guid DependentSheetId,
    Guid CanonicalFloorPlanVersionId,
    string Method,
    SheetRegistrationTransformDto Transform,
    decimal Confidence,
    string Status,
    string? Warning,
    DateTime CreatedAtUtc,
    DateTime? ConfirmedAtUtc,
    string? RuleSummary = null);
