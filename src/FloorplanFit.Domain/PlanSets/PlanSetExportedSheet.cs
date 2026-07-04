namespace FloorplanFit.Domain.PlanSets;

public sealed class PlanSetExportedSheet
{
    public PlanSetExportedSheet(
        Guid id,
        Guid planSetExportId,
        Guid planSheetId,
        Guid? sheetProjectionId,
        string sheetKind,
        string? storagePath,
        PlanSetExportedSheetStatus status,
        string? projectionMethod,
        decimal? confidence,
        string? warning,
        string? ruleSummary,
        string? recipeHandlingSummary = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Exported sheet id is required.", nameof(id));
        }

        if (planSetExportId == Guid.Empty)
        {
            throw new ArgumentException("Plan-set export is required.", nameof(planSetExportId));
        }

        if (planSheetId == Guid.Empty)
        {
            throw new ArgumentException("Plan sheet is required.", nameof(planSheetId));
        }

        if (string.IsNullOrWhiteSpace(sheetKind))
        {
            throw new ArgumentException("Sheet kind is required.", nameof(sheetKind));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentException("Exported sheet status is required.", nameof(status));
        }

        if (confidence is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(confidence), "Sheet confidence must be between 0 and 1.");
        }

        if ((status is PlanSetExportedSheetStatus.ProjectedAutomatically or
            PlanSetExportedSheetStatus.RequiresManualConfirmation) &&
            !sheetProjectionId.HasValue)
        {
            throw new ArgumentException("Projected sheet status requires a projection id.", nameof(sheetProjectionId));
        }

        if ((status is PlanSetExportedSheetStatus.Exported or
            PlanSetExportedSheetStatus.ProjectedAutomatically) &&
            string.IsNullOrWhiteSpace(storagePath))
        {
            throw new ArgumentException("Exported sheet status requires a storage path.", nameof(storagePath));
        }

        if (status is PlanSetExportedSheetStatus.RequiresManualConfirmation &&
            !string.IsNullOrWhiteSpace(storagePath))
        {
            throw new ArgumentException("Manual confirmation sheets cannot include projection output.", nameof(storagePath));
        }

        if (status is PlanSetExportedSheetStatus.MissingProjection &&
            (sheetProjectionId.HasValue || !string.IsNullOrWhiteSpace(storagePath)))
        {
            throw new ArgumentException("Missing projection sheets cannot include projection output.", nameof(sheetProjectionId));
        }

        Id = id;
        PlanSetExportId = planSetExportId;
        PlanSheetId = planSheetId;
        SheetProjectionId = sheetProjectionId;
        SheetKind = sheetKind.Trim();
        StoragePath = string.IsNullOrWhiteSpace(storagePath) ? null : storagePath.Trim();
        Status = status;
        ProjectionMethod = string.IsNullOrWhiteSpace(projectionMethod) ? null : projectionMethod.Trim();
        Confidence = confidence;
        Warning = string.IsNullOrWhiteSpace(warning) ? null : warning.Trim();
        RuleSummary = string.IsNullOrWhiteSpace(ruleSummary) ? null : ruleSummary.Trim();
        RecipeHandlingSummary = string.IsNullOrWhiteSpace(recipeHandlingSummary) ? null : recipeHandlingSummary.Trim();
    }

    public Guid Id { get; }

    public Guid PlanSetExportId { get; }

    public Guid PlanSheetId { get; }

    public Guid? SheetProjectionId { get; }

    public string SheetKind { get; }

    public string? StoragePath { get; }

    public PlanSetExportedSheetStatus Status { get; }

    public string? ProjectionMethod { get; }

    public decimal? Confidence { get; }

    public string? Warning { get; }

    public string? RuleSummary { get; }

    public string? RecipeHandlingSummary { get; }
}
