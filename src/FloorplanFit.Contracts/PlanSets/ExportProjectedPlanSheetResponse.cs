namespace FloorplanFit.Contracts.PlanSets;

public sealed record ExportProjectedPlanSheetResponse(
    Guid ProjectionId,
    string OutputFilePath,
    ProjectedPlanSheetExportAuditDto? ExportAudit = null);
