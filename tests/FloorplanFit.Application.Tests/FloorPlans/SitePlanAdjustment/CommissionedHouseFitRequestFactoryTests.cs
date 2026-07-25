using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.SitePlanAdjustment;

public sealed class CommissionedHouseFitRequestFactoryTests
{
    [Fact]
    public void Create_uses_the_structural_footprint_and_buildable_area_in_physical_inches()
    {
        var footprint = Rectangle(Guid.NewGuid(), 10m, 20m, 49m, 97.5m);
        var buildableArea = new SitePlanBuildableAreaDto(0m, 0m, 38.7m, 77.4m);

        var result = CommissionedHouseFitRequestFactory.Create(
            [footprint],
            buildableArea,
            sitePlanToMillimetersFactor: 25.4m);

        Assert.True(result.Succeeded, result.RejectionReason);
        Assert.NotNull(result.Request);
        Assert.Equal(39m, result.Request.OriginalWidthInches);
        Assert.Equal(77.5m, result.Request.OriginalDepthInches);
        Assert.Equal(38.7m, result.Request.BuildableWidthInches);
        Assert.Equal(77.4m, result.Request.BuildableDepthInches);
    }

    [Fact]
    public void Create_uses_only_the_commissioned_placement_paths_when_they_exist()
    {
        var house = Rectangle(Guid.NewGuid(), 0m, 0m, 39m, 77.5m);
        var unrelatedTitle = Rectangle(Guid.NewGuid(), 100m, 100m, 300m, 200m);

        var result = CommissionedHouseFitRequestFactory.Create(
            [house, unrelatedTitle],
            new SitePlanBuildableAreaDto(0m, 0m, 39m, 77.5m),
            25.4m,
            [house.Id]);

        Assert.True(result.Succeeded, result.RejectionReason);
        Assert.NotNull(result.Request);
        Assert.Equal(39m, result.Request.OriginalWidthInches);
        Assert.Equal(77.5m, result.Request.OriginalDepthInches);
    }

    [Fact]
    public void Create_fails_closed_without_structural_geometry()
    {
        var result = CommissionedHouseFitRequestFactory.Create(
            [],
            new SitePlanBuildableAreaDto(0m, 0m, 39m, 77.5m),
            25.4m);

        Assert.False(result.Succeeded);
        Assert.Null(result.Request);
        Assert.False(string.IsNullOrWhiteSpace(result.RejectionReason));
    }

    [Fact]
    public void Create_fails_closed_for_an_invalid_site_measurement_factor()
    {
        var result = CommissionedHouseFitRequestFactory.Create(
            [Rectangle(Guid.NewGuid(), 0m, 0m, 39m, 77.5m)],
            new SitePlanBuildableAreaDto(0m, 0m, 39m, 77.5m),
            0m);

        Assert.False(result.Succeeded);
        Assert.Null(result.Request);
    }

    [Fact]
    public void Create_fails_closed_when_buildable_dimensions_overflow()
    {
        var result = CommissionedHouseFitRequestFactory.Create(
            [Rectangle(Guid.NewGuid(), 0m, 0m, 1m, 1m)],
            new SitePlanBuildableAreaDto(decimal.MinValue, 0m, decimal.MaxValue, 1m),
            1m);

        Assert.False(result.Succeeded);
        Assert.Null(result.Request);
        Assert.False(string.IsNullOrWhiteSpace(result.RejectionReason));
    }

    private static GeometryPathDto Rectangle(
        Guid id,
        decimal minX,
        decimal minY,
        decimal maxX,
        decimal maxY)
        => new(
            id,
            true,
            [
                Segment(id, minX, minY, maxX, minY, 1),
                Segment(id, maxX, minY, maxX, maxY, 2),
                Segment(id, maxX, maxY, minX, maxY, 3),
                Segment(id, minX, maxY, minX, minY, 4)
            ]);

    private static GeometrySegmentDto Segment(
        Guid geometryPathId,
        decimal startX,
        decimal startY,
        decimal endX,
        decimal endY,
        int sortOrder)
        => new(geometryPathId, sortOrder, startX, startY, endX, endY);
}
