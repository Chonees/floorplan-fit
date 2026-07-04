namespace FloorplanFit.Contracts.PlanSets;

public sealed record PlanSetQualityReportDto(
    Guid PlanSetVersionId,
    int RegistrationEventCount,
    int ProjectionEventCount,
    decimal? LowestRegistrationConfidence,
    decimal? LowestProjectionConfidence,
    int ManualRegistrationCount,
    int ManualProjectionCount,
    IReadOnlyList<PlanSetQualitySignalDto> Signals);
