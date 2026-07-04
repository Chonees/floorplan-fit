namespace FloorplanFit.Contracts.PlanSets;

public sealed record PlanSetQualitySignalDto(
    string Kind,
    Guid AggregateId,
    string Method,
    decimal? Confidence,
    string Status,
    string? Warning,
    string? RuleSummary,
    DateTime OccurredAtUtc);
