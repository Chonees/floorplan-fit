using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.SitePlanAdjustment;

public sealed record SitePlanAdjustmentFitContext(
    SitePlanBuildableAreaDto BuildableArea,
    decimal SitePlanToMillimetersFactor,
    IReadOnlyList<PinchGroupDto> PinchGroups,
    IReadOnlyList<ArticulationBandDto> ArticulationBands,
    IReadOnlyList<MeasurementCorridorDto> MeasurementCorridors,
    IReadOnlyList<DimensionIntervalBindingDto> DimensionIntervalBindings,
    IReadOnlySet<Guid>? PlacementGeometryPathIds = null);

public static class SitePlanAdjustmentFitAnalyzer
{
    private const decimal MillimetersPerInch = 25.4m;

    public static AutoFitSuggestionFacts BuildFacts(
        IReadOnlyList<GeometryPathDto> projectedFloorPlanGeometryPaths,
        SitePlanAdjustmentFitContext context)
    {
        ArgumentNullException.ThrowIfNull(projectedFloorPlanGeometryPaths);
        ArgumentNullException.ThrowIfNull(context);

        return AutoFitSuggestionFactBuilder.Build(
            ResolveFitGeometryPaths(projectedFloorPlanGeometryPaths, context.PlacementGeometryPathIds),
            context.BuildableArea,
            context.SitePlanToMillimetersFactor,
            context.PinchGroups,
            context.ArticulationBands,
            context.MeasurementCorridors,
            context.DimensionIntervalBindings);
    }

    public static IReadOnlyList<GeometryPathDto> ResolveFitGeometryPaths(
        IReadOnlyList<GeometryPathDto> projectedFloorPlanGeometryPaths,
        IReadOnlySet<Guid>? placementGeometryPathIds)
    {
        ArgumentNullException.ThrowIfNull(projectedFloorPlanGeometryPaths);

        if (placementGeometryPathIds is null || placementGeometryPathIds.Count == 0)
        {
            return projectedFloorPlanGeometryPaths;
        }

        var placementGeometryPaths = projectedFloorPlanGeometryPaths
            .Where(path => placementGeometryPathIds.Contains(path.Id))
            .ToArray();

        return placementGeometryPaths.Length > 0
            ? placementGeometryPaths
            : projectedFloorPlanGeometryPaths;
    }

    public static AutoFitSuggestionFacts TranslateSideOverflows(
        AutoFitSuggestionFacts facts,
        decimal deltaX,
        decimal deltaY,
        decimal sitePlanToMillimetersFactor)
    {
        ArgumentNullException.ThrowIfNull(facts);

        if (sitePlanToMillimetersFactor <= 0m)
        {
            return facts;
        }

        var deltaXInches = ConvertSourceUnitsToInches(deltaX, sitePlanToMillimetersFactor);
        var deltaYInches = ConvertSourceUnitsToInches(deltaY, sitePlanToMillimetersFactor);
        var deficit = facts.Deficit;

        return facts with
        {
            Deficit = deficit with
            {
                LeftInches = NonNegative(deficit.LeftInches - deltaXInches),
                RightInches = NonNegative(deficit.RightInches + deltaXInches),
                BottomInches = NonNegative(deficit.BottomInches - deltaYInches),
                TopInches = NonNegative(deficit.TopInches + deltaYInches)
            }
        };
    }

    private static decimal ConvertSourceUnitsToInches(decimal sourceUnits, decimal toMillimetersFactor)
        => decimal.Round((sourceUnits * toMillimetersFactor) / MillimetersPerInch, 3, MidpointRounding.AwayFromZero);

    private static decimal NonNegative(decimal value) => Math.Max(0m, value);
}
