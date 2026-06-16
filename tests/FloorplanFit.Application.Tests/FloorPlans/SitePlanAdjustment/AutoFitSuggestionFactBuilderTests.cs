using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.SitePlanAdjustment;

public sealed class AutoFitSuggestionFactBuilderTests
{
    [Fact]
    public void Build_computes_width_and_height_deficits_from_projected_floorplan_against_buildable_area()
    {
        var widthGroupId = Guid.NewGuid();
        var heightGroupId = Guid.NewGuid();
        var floorPathId = Guid.NewGuid();

        var facts = AutoFitSuggestionFactBuilder.Build(
            floorPlanGeometryPaths: [RectanglePath(floorPathId, minX: -0.5m, minY: -1m, maxX: 100.75m, maxY: 201.25m)],
            buildableArea: new SitePlanBuildableAreaDto(0m, 0m, 100m, 200m),
            sitePlanToMillimetersFactor: 25.4m,
            pinchGroups:
            [
                new PinchGroupDto(widthGroupId, "Patio", "Width", 1),
                new PinchGroupDto(heightGroupId, "Garage depth", "Height", 2)
            ],
            articulationBands:
            [
                new ArticulationBandDto(widthGroupId, "Patio", "Width", 10m, 20m, 50.8m, "Suggested"),
                new ArticulationBandDto(heightGroupId, "Garage depth", "Height", 30m, 60m, 76.2m, "Suggested")
            ]);

        Assert.Equal(1.25m, facts.Deficit.WidthInches);
        Assert.Equal(2.25m, facts.Deficit.HeightInches);
        Assert.Equal(0.5m, facts.Deficit.LeftInches);
        Assert.Equal(0.75m, facts.Deficit.RightInches);
        Assert.Equal(1m, facts.Deficit.BottomInches);
        Assert.Equal(1.25m, facts.Deficit.TopInches);

        Assert.Collection(
            facts.CandidateGroups,
            group =>
            {
                Assert.Equal(widthGroupId, group.PinchGroupId);
                Assert.Equal("Patio", group.Name);
                Assert.Equal("Width", group.AxisTag);
                Assert.Equal(2m, group.CapacityInches);
            },
            group =>
            {
                Assert.Equal(heightGroupId, group.PinchGroupId);
                Assert.Equal("Garage depth", group.Name);
                Assert.Equal("Height", group.AxisTag);
                Assert.Equal(3m, group.CapacityInches);
            });
    }

    [Fact]
    public void Build_reports_when_matching_axis_has_less_capacity_than_required_deficit()
    {
        var widthGroupId = Guid.NewGuid();
        var floorPathId = Guid.NewGuid();

        var facts = AutoFitSuggestionFactBuilder.Build(
            floorPlanGeometryPaths: [RectanglePath(floorPathId, minX: -2m, minY: 0m, maxX: 101m, maxY: 200m)],
            buildableArea: new SitePlanBuildableAreaDto(0m, 0m, 100m, 200m),
            sitePlanToMillimetersFactor: 25.4m,
            pinchGroups: [new PinchGroupDto(widthGroupId, "Left corridor", "Width", 1)],
            articulationBands: [new ArticulationBandDto(widthGroupId, "Left corridor", "Width", 10m, 20m, 50.8m, "Suggested")]);

        Assert.Equal(3m, facts.Deficit.WidthInches);
        Assert.Contains(
            facts.Warnings,
            warning => warning.Contains("Width", StringComparison.OrdinalIgnoreCase) &&
                       warning.Contains("3", StringComparison.OrdinalIgnoreCase) &&
                       warning.Contains("2", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Build_does_not_report_width_deficit_when_plan_fits_by_total_width_but_is_shifted_right()
    {
        var floorPathId = Guid.NewGuid();

        var facts = AutoFitSuggestionFactBuilder.Build(
            floorPlanGeometryPaths: [RectanglePath(floorPathId, minX: 2m, minY: 0m, maxX: 102m, maxY: 200m)],
            buildableArea: new SitePlanBuildableAreaDto(0m, 0m, 100m, 200m),
            sitePlanToMillimetersFactor: 25.4m,
            pinchGroups: [],
            articulationBands: []);

        Assert.Equal(0m, facts.Deficit.WidthInches);
        Assert.Equal(0m, facts.Deficit.HeightInches);
        Assert.Equal(0m, facts.Deficit.LeftInches);
        Assert.Equal(2m, facts.Deficit.RightInches);
        Assert.False(facts.NeedsAdjustment);
    }

    [Fact]
    public void Build_does_not_report_height_deficit_when_plan_fits_by_total_height_but_is_shifted_up()
    {
        var floorPathId = Guid.NewGuid();

        var facts = AutoFitSuggestionFactBuilder.Build(
            floorPlanGeometryPaths: [RectanglePath(floorPathId, minX: 0m, minY: 2m, maxX: 100m, maxY: 202m)],
            buildableArea: new SitePlanBuildableAreaDto(0m, 0m, 100m, 200m),
            sitePlanToMillimetersFactor: 25.4m,
            pinchGroups: [],
            articulationBands: []);

        Assert.Equal(0m, facts.Deficit.WidthInches);
        Assert.Equal(0m, facts.Deficit.HeightInches);
        Assert.Equal(0m, facts.Deficit.BottomInches);
        Assert.Equal(2m, facts.Deficit.TopInches);
        Assert.False(facts.NeedsAdjustment);
    }

    [Fact]
    public void Build_reports_actual_per_side_overflow_when_projected_plan_is_off_center()
    {
        var floorPathId = Guid.NewGuid();

        var facts = AutoFitSuggestionFactBuilder.Build(
            floorPlanGeometryPaths: [RectanglePath(floorPathId, minX: 2m, minY: 0m, maxX: 103m, maxY: 200m)],
            buildableArea: new SitePlanBuildableAreaDto(0m, 0m, 100m, 200m),
            sitePlanToMillimetersFactor: 25.4m,
            pinchGroups: [],
            articulationBands: []);

        Assert.Equal(1m, facts.Deficit.WidthInches);
        Assert.Equal(0m, facts.Deficit.LeftInches);
        Assert.Equal(3m, facts.Deficit.RightInches);
        Assert.True(facts.NeedsAdjustment);
    }

    [Fact]
    public void Build_still_reports_height_deficit_from_current_vertical_overflow()
    {
        var floorPathId = Guid.NewGuid();

        var facts = AutoFitSuggestionFactBuilder.Build(
            floorPlanGeometryPaths: [RectanglePath(floorPathId, minX: 0m, minY: 0m, maxX: 100m, maxY: 202m)],
            buildableArea: new SitePlanBuildableAreaDto(0m, 0m, 100m, 200m),
            sitePlanToMillimetersFactor: 25.4m,
            pinchGroups: [],
            articulationBands: []);

        Assert.Equal(0m, facts.Deficit.WidthInches);
        Assert.Equal(2m, facts.Deficit.HeightInches);
        Assert.Equal(2m, facts.Deficit.TopInches);
    }

    [Fact]
    public void Build_measures_width_from_structural_wall_mass_ignoring_a_stray_appendage()
    {
        // Body occupies X[10..110]; a single stray wall-layer line juts to X=-20 with
        // negligible length. The width must be measured from the structural body
        // (100 wide, fits the 100-wide buildable), NOT the phantom 130 bounding box.
        var paths = new List<GeometryPathDto>();
        paths.Add(RectanglePath(Guid.NewGuid(), minX: 10m, minY: 0m, maxX: 110m, maxY: 200m));
        for (var index = 1; index < 80; index++)
        {
            var pathId = Guid.NewGuid();
            var x = 10m + (index * (100m / 80m));
            paths.Add(new GeometryPathDto(pathId, false, [new GeometrySegmentDto(pathId, 1, x, 0m, x, 200m)]));
        }

        var strayId = Guid.NewGuid();
        paths.Add(new GeometryPathDto(strayId, false, [new GeometrySegmentDto(strayId, 1, 10m, 100m, -20m, 100m)]));

        var facts = AutoFitSuggestionFactBuilder.Build(
            floorPlanGeometryPaths: paths,
            buildableArea: new SitePlanBuildableAreaDto(10m, 0m, 110m, 200m),
            sitePlanToMillimetersFactor: 25.4m,
            pinchGroups: [],
            articulationBands: []);

        Assert.Equal(0m, facts.Deficit.WidthInches);
        Assert.Equal(0m, facts.Deficit.LeftInches);
        Assert.Equal(0m, facts.Deficit.RightInches);
        Assert.False(facts.NeedsAdjustment);
    }

    private static GeometryPathDto RectanglePath(Guid pathId, decimal minX, decimal minY, decimal maxX, decimal maxY)
        => new(
            pathId,
            true,
            [
                new GeometrySegmentDto(pathId, 1, minX, minY, maxX, minY),
                new GeometrySegmentDto(pathId, 2, maxX, minY, maxX, maxY),
                new GeometrySegmentDto(pathId, 3, maxX, maxY, minX, maxY),
                new GeometrySegmentDto(pathId, 4, minX, maxY, minX, minY)
            ]);
}
