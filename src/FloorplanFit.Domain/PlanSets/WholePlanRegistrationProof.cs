namespace FloorplanFit.Domain.PlanSets;

public sealed record WholePlanRegistrationProof
{
    public const int CurrentVersion = WholePlanRegistrationAcceptancePolicy.CurrentVersion;

    public WholePlanRegistrationProof(
        int Version,
        bool Passed,
        Guid CanonicalFloorPlanVersionId,
        Guid DependentSheetId,
        string CanonicalSourceSha256,
        string DependentSourceSha256,
        decimal HorizontalCoverage,
        decimal VerticalCoverage,
        decimal RootMeanSquareResidual,
        decimal MaximumResidual)
    {
        if (Version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Version), "Proof version must be positive.");
        }

        if (CanonicalFloorPlanVersionId == Guid.Empty)
        {
            throw new ArgumentException("Canonical floor-plan version is required.", nameof(CanonicalFloorPlanVersionId));
        }

        if (DependentSheetId == Guid.Empty)
        {
            throw new ArgumentException("Dependent sheet is required.", nameof(DependentSheetId));
        }

        if (!IsSha256(CanonicalSourceSha256))
        {
            throw new ArgumentException("Canonical source SHA-256 must be 64 hexadecimal characters.", nameof(CanonicalSourceSha256));
        }

        if (!IsSha256(DependentSourceSha256))
        {
            throw new ArgumentException("Dependent source SHA-256 must be 64 hexadecimal characters.", nameof(DependentSourceSha256));
        }

        if (HorizontalCoverage is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(HorizontalCoverage), "Horizontal coverage must be between 0 and 1.");
        }

        if (VerticalCoverage is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(VerticalCoverage), "Vertical coverage must be between 0 and 1.");
        }

        if (RootMeanSquareResidual < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(RootMeanSquareResidual), "RMS residual cannot be negative.");
        }

        if (MaximumResidual < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(MaximumResidual), "Maximum residual cannot be negative.");
        }

        this.Version = Version;
        this.Passed = Passed;
        this.CanonicalFloorPlanVersionId = CanonicalFloorPlanVersionId;
        this.DependentSheetId = DependentSheetId;
        this.CanonicalSourceSha256 = CanonicalSourceSha256.ToLowerInvariant();
        this.DependentSourceSha256 = DependentSourceSha256.ToLowerInvariant();
        this.HorizontalCoverage = HorizontalCoverage;
        this.VerticalCoverage = VerticalCoverage;
        this.RootMeanSquareResidual = RootMeanSquareResidual;
        this.MaximumResidual = MaximumResidual;
    }

    public int Version { get; init; }

    public bool Passed { get; init; }

    public Guid CanonicalFloorPlanVersionId { get; init; }

    public Guid DependentSheetId { get; init; }

    public string CanonicalSourceSha256 { get; init; }

    public string DependentSourceSha256 { get; init; }

    public decimal HorizontalCoverage { get; init; }

    public decimal VerticalCoverage { get; init; }

    public decimal RootMeanSquareResidual { get; init; }

    public decimal MaximumResidual { get; init; }

    public bool IsAuthoritative =>
        Passed &&
        CanonicalFloorPlanVersionId != Guid.Empty &&
        DependentSheetId != Guid.Empty &&
        IsSha256(CanonicalSourceSha256) &&
        IsSha256(DependentSourceSha256) &&
        WholePlanRegistrationAcceptancePolicy.IsAccepted(
            Version,
            HorizontalCoverage,
            VerticalCoverage,
            RootMeanSquareResidual,
            MaximumResidual);

    private static bool IsSha256(string? value)
        => value?.Length == 64 && value.All(Uri.IsHexDigit);
}
