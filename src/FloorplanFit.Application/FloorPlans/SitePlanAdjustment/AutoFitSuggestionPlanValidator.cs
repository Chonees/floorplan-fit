namespace FloorplanFit.Application.FloorPlans.SitePlanAdjustment;

public static class AutoFitSuggestionPlanValidator
{
    private const decimal ExactFitToleranceInches = 0.001m;

    public static AutoFitSuggestionValidationResult Validate(
        AutoFitSuggestionFacts facts,
        AutoFitSuggestionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(plan);

        var errors = new List<string>();
        var candidateGroups = facts.CandidateGroups
            .GroupBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);
        var resolvedSteps = new List<ResolvedStep>();

        foreach (var step in plan.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.GroupName))
            {
                errors.Add("Every suggested reduction must name an existing pinch group.");
                continue;
            }

            if (!candidateGroups.TryGetValue(step.GroupName, out var matches))
            {
                errors.Add($"Suggested pinch group '{step.GroupName}' does not exist in the fit facts.");
                continue;
            }

            if (matches.Length > 1)
            {
                errors.Add($"Suggested pinch group '{step.GroupName}' is ambiguous because multiple candidate groups share that name.");
                continue;
            }

            var candidate = matches[0];
            if (!IsAxis(candidate.AxisTag, step.AxisTag))
            {
                errors.Add(
                    $"Suggested pinch group '{step.GroupName}' has axis '{candidate.AxisTag}', but the plan tries to use it as '{step.AxisTag}'.");
                continue;
            }

            if (step.ReductionInches <= 0m)
            {
                errors.Add($"Suggested reduction for '{step.GroupName}' must be greater than zero.");
                continue;
            }

            resolvedSteps.Add(new ResolvedStep(candidate, step));
        }

        foreach (var group in resolvedSteps.GroupBy(item => item.Candidate.PinchGroupId))
        {
            var first = group.First();
            var total = group.Sum(item => item.Step.ReductionInches);
            if (total > first.Candidate.CapacityInches + ExactFitToleranceInches)
            {
                errors.Add(
                    $"Suggested reduction for '{first.Candidate.Name}' is {total:0.###} inches, exceeding its capacity {first.Candidate.CapacityInches:0.###} inches.");
            }
        }

        ValidateAxisTotal(errors, "Width", facts.Deficit.WidthInches, resolvedSteps);
        ValidateAxisTotal(errors, "Height", facts.Deficit.HeightInches, resolvedSteps);

        return new AutoFitSuggestionValidationResult(errors.Count == 0, errors);
    }

    private static void ValidateAxisTotal(
        List<string> errors,
        string axisTag,
        decimal requiredInches,
        IReadOnlyList<ResolvedStep> resolvedSteps)
    {
        var actualInches = resolvedSteps
            .Where(step => IsAxis(step.Candidate.AxisTag, axisTag))
            .Sum(step => step.Step.ReductionInches);

        if (requiredInches <= ExactFitToleranceInches)
        {
            if (actualInches > ExactFitToleranceInches)
            {
                errors.Add(
                    $"{axisTag} reduction must be exact: needed 0 inches, but the plan trims {actualInches:0.###} inches.");
            }

            return;
        }

        if (Math.Abs(actualInches - requiredInches) > ExactFitToleranceInches)
        {
            errors.Add(
                $"{axisTag} reduction must be exact: needed {requiredInches:0.###} inches, but the plan trims {actualInches:0.###} inches.");
        }
    }

    private static bool IsAxis(string left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private readonly record struct ResolvedStep(
        AutoFitCandidateGroupDto Candidate,
        AutoFitSuggestionStep Step);
}
