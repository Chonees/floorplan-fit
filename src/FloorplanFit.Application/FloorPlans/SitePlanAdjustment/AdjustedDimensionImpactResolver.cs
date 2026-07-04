using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.SitePlanAdjustment;

public static class AdjustedDimensionImpactResolver
{
    public static IReadOnlyList<Guid> ResolveAffectedDimensionIds(
        IReadOnlyList<AutoFitSuggestionStep> steps,
        IReadOnlyList<AutoFitCandidateGroupDto> candidateGroups,
        IReadOnlyList<MeasurementCorridorDto> measurementCorridors,
        IReadOnlyList<DimensionIntervalBindingDto> dimensionIntervalBindings)
    {
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(candidateGroups);
        ArgumentNullException.ThrowIfNull(measurementCorridors);
        ArgumentNullException.ThrowIfNull(dimensionIntervalBindings);

        if (steps.Count == 0 ||
            candidateGroups.Count == 0 ||
            measurementCorridors.Count == 0 ||
            dimensionIntervalBindings.Count == 0)
        {
            return [];
        }

        var impactedDimensionIds = new HashSet<Guid>();
        foreach (var step in steps)
        {
            var candidate = candidateGroups.FirstOrDefault(group =>
                string.Equals(group.Name, step.GroupName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(group.AxisTag, step.AxisTag, StringComparison.OrdinalIgnoreCase));
            if (candidate is null)
            {
                continue;
            }

            AddImpactedDimensions(candidate, measurementCorridors, dimensionIntervalBindings, impactedDimensionIds);
        }

        return impactedDimensionIds.ToArray();
    }

    private static void AddImpactedDimensions(
        AutoFitCandidateGroupDto candidate,
        IReadOnlyList<MeasurementCorridorDto> measurementCorridors,
        IReadOnlyList<DimensionIntervalBindingDto> dimensionIntervalBindings,
        HashSet<Guid> impactedDimensionIds)
    {
        var bandMin = Math.Min(candidate.BandStartCoordinate, candidate.BandEndCoordinate);
        var bandMax = Math.Max(candidate.BandStartCoordinate, candidate.BandEndCoordinate);
        var matchingCorridorIds = measurementCorridors
            .Where(corridor => IsAxis(corridor.AxisTag, candidate.AxisTag))
            .Select(corridor => corridor.CorridorId)
            .ToHashSet();

        if (matchingCorridorIds.Count == 0)
        {
            return;
        }

        foreach (var binding in dimensionIntervalBindings
                     .Where(binding => string.Equals(binding.BindingStatus, "ManualVerified", StringComparison.Ordinal))
                     .Where(binding => matchingCorridorIds.Contains(binding.CorridorId))
                     .Where(binding => Overlaps(bandMin, bandMax, binding.IntervalStartCoordinate, binding.IntervalEndCoordinate)))
        {
            impactedDimensionIds.Add(binding.DimensionId);
        }
    }

    private static bool IsAxis(string axisTag, string expected)
        => string.Equals(NormalizeAxis(axisTag), NormalizeAxis(expected), StringComparison.OrdinalIgnoreCase);

    private static string NormalizeAxis(string axisTag)
        => string.Equals(axisTag, "Height", StringComparison.OrdinalIgnoreCase)
            ? "Height"
            : "Width";

    private static bool Overlaps(decimal firstMin, decimal firstMax, decimal secondStart, decimal secondEnd)
    {
        var secondMin = Math.Min(secondStart, secondEnd);
        var secondMax = Math.Max(secondStart, secondEnd);
        return firstMin <= secondMax && secondMin <= firstMax;
    }
}
