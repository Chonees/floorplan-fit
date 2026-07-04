namespace FloorplanFit.Contracts.PlanSets;

public sealed record ExportProjectedPlanSheetRequest(
    Guid ProjectionId,
    string SourceFilePath,
    string OutputFilePath);
