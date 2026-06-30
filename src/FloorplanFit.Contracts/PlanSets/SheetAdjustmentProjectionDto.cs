namespace FloorplanFit.Contracts.PlanSets;

public sealed record SheetAdjustmentProjectionDto(
    Guid ProjectionId,
    Guid PlanSetVersionId,
    Guid DependentSheetId,
    Guid SheetRegistrationId,
    Guid CanonicalAdjustmentId,
    string Method,
    SheetAdjustmentProjectionTransformDto Transform,
    decimal Confidence,
    string Status,
    string? Warning,
    int CanonicalCompressionStepCount,
    DateTime CreatedAtUtc,
    string? RuleSummary = null);
