namespace FloorplanFit.Contracts.PlanSets;

public sealed record ExportedPlanSheetDto(
    Guid SheetId,
    Guid? ProjectionId,
    string SheetKind,
    string Status,
    string? StoragePath,
    string? ProjectionMethod,
    decimal? Confidence,
    string? Warning,
    string? RuleSummary,
    string? RecipeHandlingSummary = null);
