namespace FloorplanFit.Application.FloorPlans.SitePlanAdjustment;

public sealed record AutoFitSuggestionFacts(
    AutoFitEnvelopeDeficitDto Deficit,
    IReadOnlyList<AutoFitCandidateGroupDto> CandidateGroups,
    IReadOnlyList<string> Warnings)
{
    private const decimal CapacityToleranceInches = 0.001m;

    public bool NeedsAdjustment =>
        Deficit.RequiresWidthAdjustment ||
        Deficit.RequiresHeightAdjustment;

    public bool HasRequiredCandidateCapacity =>
        HasCapacityForAxis("Width", Deficit.WidthInches) &&
        HasCapacityForAxis("Height", Deficit.HeightInches);

    private bool HasCapacityForAxis(string axisTag, decimal requiredInches)
    {
        if (requiredInches <= CapacityToleranceInches)
        {
            return true;
        }

        var capacityInches = CandidateGroups
            .Where(group => string.Equals(group.AxisTag, axisTag, StringComparison.OrdinalIgnoreCase))
            .Sum(group => group.CapacityInches);

        return capacityInches + CapacityToleranceInches >= requiredInches;
    }
}

/// <summary>
/// Two complementary readings of how the projected floor plan misses the buildable area.
/// <see cref="WidthInches"/>/<see cref="HeightInches"/> are the minimum trim per axis for the
/// footprint to fit at all, independent of where it currently sits — a plan that fits but is
/// shifted needs moving, not trimming, so these stay zero. The per-side values
/// (<see cref="LeftInches"/>, <see cref="RightInches"/>, <see cref="BottomInches"/>,
/// <see cref="TopInches"/>) are the actual overflow at the current projected position, so they
/// match what the preview shows and per-side sums only equal the axis trim when the plan is
/// centered in the buildable area.
/// </summary>
public sealed record AutoFitEnvelopeDeficitDto(
    decimal WidthInches,
    decimal HeightInches,
    decimal LeftInches,
    decimal RightInches,
    decimal BottomInches,
    decimal TopInches)
{
    public bool RequiresWidthAdjustment => WidthInches > 0m;

    public bool RequiresHeightAdjustment => HeightInches > 0m;

    public bool Fits => !RequiresWidthAdjustment && !RequiresHeightAdjustment;
}

public sealed record AutoFitCandidateGroupDto(
    Guid PinchGroupId,
    string Name,
    string AxisTag,
    decimal CapacityInches,
    decimal BandStartCoordinate,
    decimal BandEndCoordinate,
    int AffectedDimensionCount);

public sealed record AutoFitSuggestionPlan(
    string Summary,
    IReadOnlyList<AutoFitSuggestionStep> Steps,
    string Explanation);

public sealed record AutoFitSuggestionStep(
    string GroupName,
    string AxisTag,
    decimal ReductionInches,
    string Reason);

public sealed record AutoFitSuggestionValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors);

public sealed record AutoFitSuggestionPlanResponse(
    bool Succeeded,
    AutoFitSuggestionPlan? Plan,
    AutoFitSuggestionValidationResult Validation,
    string? ErrorMessage)
{
    public IReadOnlyList<AutoFitSuggestionPlan> Plans { get; init; } =
        Plan is null ? [] : [Plan];

    public IReadOnlyList<AutoFitSuggestionValidationResult> Validations { get; init; } =
        Plan is null ? [] : [Validation];
}
