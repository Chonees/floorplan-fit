namespace FloorplanFit.Contracts.PlanSets;

public sealed record MultiSheetExportAuditDto(
    Guid ExportId,
    Guid PlanSetVersionId,
    Guid CanonicalAdjustmentId,
    string Status,
    ProjectionAuditSummaryDto Summary,
    IReadOnlyList<ExportedPlanSheetDto> Sheets,
    string? PackageManifestPath,
    DateTime CreatedAtUtc);
