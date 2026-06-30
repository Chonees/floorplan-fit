namespace FloorplanFit.Domain.PlanSets;

public sealed class SheetAdjustmentProjection
{
    public SheetAdjustmentProjection(
        Guid id,
        Guid planSetVersionId,
        Guid dependentSheetId,
        Guid sheetRegistrationId,
        Guid canonicalAdjustmentId,
        SheetAdjustmentProjectionMethod method,
        SheetAdjustmentProjectionTransform transform,
        decimal confidence,
        SheetAdjustmentProjectionStatus status,
        string? warning,
        int canonicalCompressionStepCount,
        DateTime createdAtUtc,
        string? ruleSummary = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Projection id is required.", nameof(id));
        }

        if (planSetVersionId == Guid.Empty)
        {
            throw new ArgumentException("Plan set version is required.", nameof(planSetVersionId));
        }

        if (dependentSheetId == Guid.Empty)
        {
            throw new ArgumentException("Dependent sheet is required.", nameof(dependentSheetId));
        }

        if (sheetRegistrationId == Guid.Empty)
        {
            throw new ArgumentException("Sheet registration is required.", nameof(sheetRegistrationId));
        }

        if (canonicalAdjustmentId == Guid.Empty)
        {
            throw new ArgumentException("Canonical adjustment is required.", nameof(canonicalAdjustmentId));
        }

        if (!Enum.IsDefined(method))
        {
            throw new ArgumentException("Projection method is required.", nameof(method));
        }

        if (confidence is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(confidence), "Projection confidence must be between 0 and 1.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentException("Projection status is required.", nameof(status));
        }

        if (canonicalCompressionStepCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(canonicalCompressionStepCount), "Compression step count cannot be negative.");
        }

        Id = id;
        PlanSetVersionId = planSetVersionId;
        DependentSheetId = dependentSheetId;
        SheetRegistrationId = sheetRegistrationId;
        CanonicalAdjustmentId = canonicalAdjustmentId;
        Method = method;
        Transform = transform ?? throw new ArgumentNullException(nameof(transform));
        Confidence = confidence;
        Status = status;
        Warning = string.IsNullOrWhiteSpace(warning) ? null : warning.Trim();
        CanonicalCompressionStepCount = canonicalCompressionStepCount;
        CreatedAtUtc = createdAtUtc;
        RuleSummary = string.IsNullOrWhiteSpace(ruleSummary) ? null : ruleSummary.Trim();
    }

    public Guid Id { get; }

    public Guid PlanSetVersionId { get; }

    public Guid DependentSheetId { get; }

    public Guid SheetRegistrationId { get; }

    public Guid CanonicalAdjustmentId { get; }

    public SheetAdjustmentProjectionMethod Method { get; }

    public SheetAdjustmentProjectionTransform Transform { get; }

    public decimal Confidence { get; }

    public SheetAdjustmentProjectionStatus Status { get; }

    public string? Warning { get; }

    public int CanonicalCompressionStepCount { get; }

    public DateTime CreatedAtUtc { get; }

    public string? RuleSummary { get; }
}
