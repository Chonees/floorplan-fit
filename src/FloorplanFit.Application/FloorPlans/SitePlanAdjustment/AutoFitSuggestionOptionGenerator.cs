namespace FloorplanFit.Application.FloorPlans.SitePlanAdjustment;

public static class AutoFitSuggestionOptionGenerator
{
    private const decimal ExactFitToleranceInches = 0.001m;
    private const int DefaultMaxPlans = 6;

    public static IReadOnlyList<AutoFitSuggestionPlan> Generate(
        AutoFitSuggestionFacts facts,
        int maxPlans = DefaultMaxPlans)
    {
        ArgumentNullException.ThrowIfNull(facts);

        if (maxPlans <= 0 || !facts.NeedsAdjustment || !facts.HasRequiredCandidateCapacity)
        {
            return [];
        }

        var widthStepSets = BuildAxisStepSets(facts, "Width", facts.Deficit.WidthInches);
        var heightStepSets = BuildAxisStepSets(facts, "Height", facts.Deficit.HeightInches);
        var plans = new List<AutoFitSuggestionPlan>();
        var signatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var widthSteps in widthStepSets)
        {
            foreach (var heightSteps in heightStepSets)
            {
                var steps = widthSteps.Concat(heightSteps).ToArray();
                if (steps.Length == 0)
                {
                    continue;
                }

                var plan = BuildPlan(plans.Count + 1, facts, steps);
                var validation = AutoFitSuggestionPlanValidator.Validate(facts, plan);
                var signature = BuildSignature(plan.Steps);
                if (!validation.IsValid || !signatures.Add(signature))
                {
                    continue;
                }

                plans.Add(plan);
                if (plans.Count >= maxPlans)
                {
                    return plans;
                }
            }
        }

        return plans;
    }

    private static IReadOnlyList<IReadOnlyList<AutoFitSuggestionStep>> BuildAxisStepSets(
        AutoFitSuggestionFacts facts,
        string axisTag,
        decimal requiredInches)
    {
        if (requiredInches <= ExactFitToleranceInches)
        {
            return [[]];
        }

        var candidates = facts.CandidateGroups
            .Where(candidate => IsAxis(candidate.AxisTag, axisTag))
            .OrderBy(candidate => candidate.AffectedDimensionCount)
            .ThenBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (candidates.Length == 0 || candidates.Sum(candidate => candidate.CapacityInches) + ExactFitToleranceInches < requiredInches)
        {
            return [];
        }

        var stepSets = new List<IReadOnlyList<AutoFitSuggestionStep>>();
        var signatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates.Where(candidate => candidate.CapacityInches + ExactFitToleranceInches >= requiredInches))
        {
            AddUnique(
                stepSets,
                signatures,
                [BuildStep(candidate, requiredInches, "Single group can absorb the exact required reduction.")]);
        }

        if (candidates.Length > 1)
        {
            var split = TryBuildEvenSplit(candidates, requiredInches);
            if (split is not null)
            {
                AddUnique(stepSets, signatures, split);
            }
        }

        var balanced = TryBuildCapacityBalanced(candidates, requiredInches);
        if (balanced is not null)
        {
            AddUnique(stepSets, signatures, balanced);
        }

        return stepSets;
    }

    private static IReadOnlyList<AutoFitSuggestionStep>? TryBuildEvenSplit(
        IReadOnlyList<AutoFitCandidateGroupDto> candidates,
        decimal requiredInches)
    {
        var selected = candidates
            .Take(Math.Min(candidates.Count, 3))
            .ToArray();
        if (selected.Length < 2)
        {
            return null;
        }

        var share = Round(requiredInches / selected.Length);
        var steps = new List<AutoFitSuggestionStep>();
        var assigned = 0m;

        for (var index = 0; index < selected.Length; index++)
        {
            var reduction = index == selected.Length - 1
                ? Round(requiredInches - assigned)
                : share;

            if (reduction <= ExactFitToleranceInches || reduction > selected[index].CapacityInches + ExactFitToleranceInches)
            {
                return null;
            }

            steps.Add(BuildStep(selected[index], reduction, "Distributed option shares the reduction across compatible groups."));
            assigned += reduction;
        }

        return Math.Abs(assigned - requiredInches) <= ExactFitToleranceInches
            ? steps
            : null;
    }

    private static IReadOnlyList<AutoFitSuggestionStep>? TryBuildCapacityBalanced(
        IReadOnlyList<AutoFitCandidateGroupDto> candidates,
        decimal requiredInches)
    {
        var remaining = requiredInches;
        var steps = new List<AutoFitSuggestionStep>();

        foreach (var candidate in candidates)
        {
            if (remaining <= ExactFitToleranceInches)
            {
                break;
            }

            var reduction = Round(decimal.Min(candidate.CapacityInches, remaining));
            if (reduction <= ExactFitToleranceInches)
            {
                continue;
            }

            steps.Add(BuildStep(candidate, reduction, "Capacity-balanced option fills compatible groups until the exact deficit is absorbed."));
            remaining = Round(remaining - reduction);
        }

        return remaining <= ExactFitToleranceInches && steps.Count > 0
            ? steps
            : null;
    }

    private static AutoFitSuggestionStep BuildStep(
        AutoFitCandidateGroupDto candidate,
        decimal reductionInches,
        string reason)
        => new(candidate.Name, candidate.AxisTag, Round(reductionInches), reason);

    private static AutoFitSuggestionPlan BuildPlan(
        int optionNumber,
        AutoFitSuggestionFacts facts,
        IReadOnlyList<AutoFitSuggestionStep> steps)
    {
        var parts = new List<string>();
        if (facts.Deficit.WidthInches > ExactFitToleranceInches)
        {
            parts.Add($"width by {facts.Deficit.WidthInches:0.###} inches");
        }

        if (facts.Deficit.HeightInches > ExactFitToleranceInches)
        {
            parts.Add($"height by {facts.Deficit.HeightInches:0.###} inches");
        }

        var groupList = string.Join(
            "; ",
            steps.Select(step => $"{step.GroupName} {step.ReductionInches:0.###}\""));

        return new AutoFitSuggestionPlan(
            $"Option {optionNumber}: reduce {string.Join(" and ", parts)}.",
            steps,
            $"Uses {groupList}. Every axis total is exact and stays within group capacity.");
    }

    private static void AddUnique(
        List<IReadOnlyList<AutoFitSuggestionStep>> stepSets,
        HashSet<string> signatures,
        IReadOnlyList<AutoFitSuggestionStep> steps)
    {
        var signature = BuildSignature(steps);
        if (signatures.Add(signature))
        {
            stepSets.Add(steps);
        }
    }

    private static string BuildSignature(IEnumerable<AutoFitSuggestionStep> steps)
        => string.Join(
            "|",
            steps
                .OrderBy(step => step.AxisTag, StringComparer.OrdinalIgnoreCase)
                .ThenBy(step => step.GroupName, StringComparer.OrdinalIgnoreCase)
                .Select(step => $"{step.AxisTag}:{step.GroupName}:{step.ReductionInches:0.###}"));

    private static bool IsAxis(string left, string right)
        => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static decimal Round(decimal value)
        => decimal.Round(value, 3, MidpointRounding.AwayFromZero);
}
