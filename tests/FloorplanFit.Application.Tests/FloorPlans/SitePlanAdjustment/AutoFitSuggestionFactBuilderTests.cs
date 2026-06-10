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
