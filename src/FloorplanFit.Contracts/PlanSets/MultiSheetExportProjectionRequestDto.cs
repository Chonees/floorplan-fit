namespace FloorplanFit.Contracts.PlanSets;

public sealed record MultiSheetExportProjectionRequestDto(
    Guid ProjectionId,
    string? ExportPath = null);
