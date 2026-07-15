namespace FloorplanFit.Domain.PlanSets;

public sealed class SheetRegistration
{
    public SheetRegistration(
        Guid id,
        Guid planSetVersionId,
        Guid dependentSheetId,
        Guid canonicalFloorPlanVersionId,
        SheetRegistrationMethod method,
        SheetRegistrationTransform transform,
        decimal confidence,
        SheetRegistrationStatus status,
        DateTime createdAtUtc,
        DateTime? confirmedAtUtc,
        string? warning,
        string? ruleSummary = null,
        WholePlanRegistrationProof? wholePlanRegistrationProof = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Registration id is required.", nameof(id));
        }

        if (planSetVersionId == Guid.Empty)
        {
            throw new ArgumentException("Plan set version is required.", nameof(planSetVersionId));
        }

        if (dependentSheetId == Guid.Empty)
        {
            throw new ArgumentException("Dependent sheet is required.", nameof(dependentSheetId));
        }

        if (canonicalFloorPlanVersionId == Guid.Empty)
        {
            throw new ArgumentException("Canonical floor-plan version is required.", nameof(canonicalFloorPlanVersionId));
        }

        if (!Enum.IsDefined(method))
        {
            throw new ArgumentException("Registration method is required.", nameof(method));
        }

        if (confidence is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(confidence), "Registration confidence must be between 0 and 1.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentException("Registration status is required.", nameof(status));
        }

        if (status is SheetRegistrationStatus.Confirmed && confirmedAtUtc is null)
        {
            throw new ArgumentException("Confirmed registration requires a confirmation timestamp.", nameof(confirmedAtUtc));
        }

        if (status is not SheetRegistrationStatus.Confirmed && confirmedAtUtc is not null)
        {
            throw new ArgumentException("Only confirmed registration can have a confirmation timestamp.", nameof(confirmedAtUtc));
        }

        Id = id;
        PlanSetVersionId = planSetVersionId;
        DependentSheetId = dependentSheetId;
        CanonicalFloorPlanVersionId = canonicalFloorPlanVersionId;
        Method = method;
        Transform = transform ?? throw new ArgumentNullException(nameof(transform));
        Confidence = confidence;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        ConfirmedAtUtc = confirmedAtUtc;
        Warning = string.IsNullOrWhiteSpace(warning) ? null : warning.Trim();
        RuleSummary = string.IsNullOrWhiteSpace(ruleSummary) ? null : ruleSummary.Trim();
        WholePlanRegistrationProof = wholePlanRegistrationProof;
    }

    public Guid Id { get; }

    public Guid PlanSetVersionId { get; }

    public Guid DependentSheetId { get; }

    public Guid CanonicalFloorPlanVersionId { get; }

    public SheetRegistrationMethod Method { get; }

    public SheetRegistrationTransform Transform { get; }

    public decimal Confidence { get; }

    public SheetRegistrationStatus Status { get; }

    public DateTime CreatedAtUtc { get; }

    public DateTime? ConfirmedAtUtc { get; }

    public string? Warning { get; }

    public string? RuleSummary { get; }

    public WholePlanRegistrationProof? WholePlanRegistrationProof { get; }
}
