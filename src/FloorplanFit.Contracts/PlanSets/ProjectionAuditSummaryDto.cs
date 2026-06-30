namespace FloorplanFit.Contracts.PlanSets;

public sealed record ProjectionAuditSummaryDto(
    int TotalSheetCount,
    int AutomaticallyProjectedSheetCount,
    int ManualConfirmationRequiredSheetCount,
    decimal? LowestConfidence,
    bool CanExportPackageAutomatically);
