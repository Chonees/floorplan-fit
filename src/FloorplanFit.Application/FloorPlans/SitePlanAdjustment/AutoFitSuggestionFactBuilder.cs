using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.SitePlanAdjustment;

public static class AutoFitSuggestionFactBuilder
{
    private const decimal MillimetersPerInch = 25.4m;
    private const decimal CapacityToleranceInches = 0.001m;

    public static AutoFitSuggestionFacts Build(
        IReadOnlyList<GeometryPathDto> floorPlanGeometryPaths,
        SitePlanBuildableAreaDto buildableArea,
        decimal sitePlanToMillimetersFactor,
        IReadOnlyList<PinchGroupDto> pinchGroups,
        IReadOnlyList<ArticulationBandDto> articulationBands,
        IReadOnlyList<MeasurementCorridorDto>? measurementCorridors = null,
        IReadOnlyList<DimensionIntervalBindingDto>? dimensionIntervalBindings = null)
    {
        ArgumentNullException.ThrowIfNull(floorPlanGeometryPaths);
        ArgumentNullException.ThrowIfNull(buildableArea);
        ArgumentNullException.ThrowIfNull(pinchGroups);
        ArgumentNullException.ThrowIfNull(articulationBands);

        if (sitePlanToMillimetersFactor <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sitePlanToMillimetersFactor),
                sitePlanToMillimetersFactor,
                "Site plan unit conversion factor must be greater than zero.");
        }

        var warnings = new List<string>();
        var bounds = ResolveBounds(floorPlanGeometryPaths);
        if (bounds is null)
        {
            warnings.Add("No projected floor plan geometry was provided, so no fit deficit can be computed.");
            return new AutoFitSuggestionFacts(
                new AutoFitEnvelopeDeficitDto(0m, 0m, 0m, 0m, 0m, 0m),
                [],
                warnings);
        }

        var deficit = BuildDeficit(bounds.Value, buildableArea, sitePlanToMillimetersFactor);
        var candidates = BuildCandidates(
            deficit,
            pinchGroups,
            articulationBands,
            measurementCorridors ?? [],
            dimensionIntervalBindings ?? []);

        AddCapacityWarning(warnings, "Width", deficit.WidthInches, candidates);
        AddCapacityWarning(warnings, "Height", deficit.HeightInches, candidates);

        return new AutoFitSuggestionFacts(deficit, candidates, warnings);
    }

    private static AutoFitEnvelopeDeficitDto BuildDeficit(
        Bounds bounds,
        SitePlanBuildableAreaDto buildableArea,
        decimal sitePlanToMillimetersFactor)
    {
        var left = Math.Max(0m, buildableArea.MinX - bounds.MinX);
        var right = Math.Max(0m, bounds.MaxX - buildableArea.MaxX);
        var bottom = Math.Max(0m, buildableArea.MinY - bounds.MinY);
        var top = Math.Max(0m, bounds.MaxY - buildableArea.MaxY);

        return new AutoFitEnvelopeDeficitDto(
            WidthInches: ConvertSourceUnitsToInches(left + right, sitePlanToMillimetersFactor),
            HeightInches: ConvertSourceUnitsToInches(bottom + top, sitePlanToMillimetersFactor),
            LeftInches: ConvertSourceUnitsToInches(left, sitePlanToMillimetersFactor),
            RightInches: ConvertSourceUnitsToInches(right, sitePlanToMillimetersFactor),
            BottomInches: ConvertSourceUnitsToInches(bottom, sitePlanToMillimetersFactor),
            TopInches: ConvertSourceUnitsToInches(top, sitePlanToMillimetersFactor));
    }

    private static IReadOnlyList<AutoFitCandidateGroupDto> BuildCandidates(
        AutoFitEnvelopeDeficitDto deficit,
        IReadOnlyList<PinchGroupDto> pinchGroups,
        IReadOnlyList<ArticulationBandDto> articulationBands,
        IReadOnlyList<MeasurementCorridorDto> measurementCorridors,
        IReadOnlyList<DimensionIntervalBindingDto> dimensionIntervalBindings)
    {
        var needsWidth = deficit.RequiresWidthAdjustment;
        var needsHeight = deficit.RequiresHeightAdjustment;
        if (!needsWidth && !needsHeight)
        {
            return [];
        }

        var groupLookup = pinchGroups.ToDictionary(group => group.PinchGroupId);
        return articulationBands
            .Select(band =>
            {
                groupLookup.TryGetValue(band.PinchGroupId, out var group);
                var axisTag = group?.AxisTag ?? band.AxisTag;
                return new
                {
                    Band = band,
                    Group = group,
                    AxisTag = axisTag,
                    SortOrder = group?.SortOrder ?? int.MaxValue
                };
            })
            .Where(item =>
                (needsWidth && IsAxis(item.AxisTag, "Width")) ||
                (needsHeight && IsAxis(item.AxisTag, "Height")))
            .Where(item => item.Band.MaxTrimMm > 0m)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Group?.Name ?? item.Band.PinchGroupName, StringComparer.OrdinalIgnoreCase)
            .Select(item => new AutoFitCandidateGroupDto(
                item.Band.PinchGroupId,
                item.Group?.Name ?? item.Band.PinchGroupName,
                NormalizeAxis(item.AxisTag),
                ConvertMillimetersToInches(item.Band.MaxTrimMm),
                item.Band.BandStartCoordinate,
                item.Band.BandEndCoordinate,
                CountAffectedDimensions(
                    item.AxisTag,
                    item.Band.BandStartCoordinate,
                    item.Band.BandEndCoordinate,
                    measurementCorridors,
                    dimensionIntervalBindings)))
            .ToArray();
    }

    private static int CountAffectedDimensions(
        string axisTag,
        decimal bandStartCoordinate,
        decimal bandEndCoordinate,
        IReadOnlyList<MeasurementCorridorDto> measurementCorridors,
        IReadOnlyList<DimensionIntervalBindingDto> dimensionIntervalBindings)
    {
        if (measurementCorridors.Count == 0 || dimensionIntervalBindings.Count == 0)
        {
            return 0;
        }

        var min = Math.Min(bandStartCoordinate, bandEndCoordinate);
        var max = Math.Max(bandStartCoordinate, bandEndCoordinate);
        var matchingCorridorIds = measurementCorridors
            .Where(corridor => IsAxis(corridor.AxisTag, axisTag))
            .Where(corridor => Overlaps(min, max, corridor.BandMinCoordinate, corridor.BandMaxCoordinate))
            .Select(corridor => corridor.CorridorId)
            .ToHashSet();

        if (matchingCorridorIds.Count == 0)
        {
            return 0;
        }

        return dimensionIntervalBindings
            .Where(binding => string.Equals(binding.BindingStatus, "ManualVerified", StringComparison.Ordinal))
            .Where(binding => matchingCorridorIds.Contains(binding.CorridorId))
            .Where(binding => Overlaps(min, max, binding.IntervalStartCoordinate, binding.IntervalEndCoordinate))
            .Select(binding => binding.DimensionId)
            .Distinct()
            .Count();
    }

    private static Bounds? ResolveBounds(IReadOnlyList<GeometryPathDto> geometryPaths)
    {
        decimal? minX = null;
        decimal? minY = null;
        decimal? maxX = null;
        decimal? maxY = null;

        foreach (var segment in geometryPaths.SelectMany(path => path.Segments))
        {
            Include(segment.StartX, segment.StartY);
            Include(segment.EndX, segment.EndY);
        }

        return minX.HasValue && minY.HasValue && maxX.HasValue && maxY.HasValue
            ? new Bounds(minX.Value, minY.Value, maxX.Value, maxY.Value)
            : null;

        void Include(decimal x, decimal y)
        {
            minX = minX.HasValue ? Math.Min(minX.Value, x) : x;
            minY = minY.HasValue ? Math.Min(minY.Value, y) : y;
            maxX = maxX.HasValue ? Math.Max(maxX.Value, x) : x;
            maxY = maxY.HasValue ? Math.Max(maxY.Value, y) : y;
        }
    }

    private static void AddCapacityWarning(
        List<string> warnings,
        string axisTag,
        decimal requiredInches,
        IReadOnlyList<AutoFitCandidateGroupDto> candidates)
    {
        if (requiredInches <= 0m)
        {
            return;
        }

        var capacity = candidates
            .Where(candidate => IsAxis(candidate.AxisTag, axisTag))
            .Sum(candidate => candidate.CapacityInches);

        if (capacity + CapacityToleranceInches < requiredInches)
        {
            warnings.Add($"{axisTag} deficit {requiredInches:0.###} inches exceeds available capacity {capacity:0.###} inches.");
        }
    }

    private static bool Overlaps(decimal firstMin, decimal firstMax, decimal secondStart, decimal secondEnd)
    {
        var secondMin = Math.Min(secondStart, secondEnd);
        var secondMax = Math.Max(secondStart, secondEnd);
        return firstMin <= secondMax && secondMin <= firstMax;
    }

    private static bool IsAxis(string axisTag, string expected) =>
        string.Equals(NormalizeAxis(axisTag), expected, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeAxis(string axisTag) =>
        string.Equals(axisTag, "Height", StringComparison.OrdinalIgnoreCase)
            ? "Height"
            : "Width";

    private static decimal ConvertSourceUnitsToInches(decimal sourceUnits, decimal toMillimetersFactor) =>
        decimal.Round((sourceUnits * toMillimetersFactor) / MillimetersPerInch, 3, MidpointRounding.AwayFromZero);

    private static decimal ConvertMillimetersToInches(decimal millimeters) =>
        decimal.Round(millimeters / MillimetersPerInch, 3, MidpointRounding.AwayFromZero);

    private readonly record struct Bounds(decimal MinX, decimal MinY, decimal MaxX, decimal MaxY);
}
