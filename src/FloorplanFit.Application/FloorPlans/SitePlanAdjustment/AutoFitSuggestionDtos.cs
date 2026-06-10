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
