using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.SitePlanAdjustment;

public sealed class StructuralFootprintTests
{
    [Fact]
    public void Resolve_returns_absolute_bounds_for_a_clean_rectangle()
    {
        var footprint = StructuralFootprint.Resolve([Rectangle(minX: 0m, minY: 0m, maxX: 100m, maxY: 200m)]);

        Assert.NotNull(footprint);
        Assert.Equal(0m, footprint.Value.MinX);
        Assert.Equal(100m, footprint.Value.MaxX);
        Assert.Equal(0m, footprint.Value.MinY);
        Assert.Equal(200m, footprint.Value.MaxY);
    }

    [Fact]
    public void Resolve_ignores_a_negligible_mass_appendage_that_juts_past_the_body()
    {
        // A real-shaped body in X[0..100] (perimeter + interior walls) plus one stray
        // line jutting to X=-30 with negligible wall length — the SEMINOLE "phantom
        // width" shape, where a single thin segment defines the raw bounding box.
        var paths = DenseBody(minX: 0m, minY: 0m, maxX: 100m, maxY: 100m, interiorWalls: 80);
        paths.Add(HorizontalWall(y: 50m, minX: -30m, maxX: 0m));

        var footprint = StructuralFootprint.Resolve(paths);

        Assert.NotNull(footprint);
        Assert.Equal(0m, footprint.Value.MinX);
        Assert.Equal(100m, footprint.Value.MaxX);
        Assert.Equal(0m, footprint.Value.MinY);
        Assert.Equal(100m, footprint.Value.MaxY);
    }

    [Fact]
    public void Resolve_keeps_a_substantial_wing_that_carries_real_wall_mass()
    {
        // An L-shaped plan: the left wing is real structure (its own perimeter + walls),
        // so it must NOT be trimmed even though it extends the bounding box.
        var paths = DenseBody(minX: 0m, minY: 0m, maxX: 100m, maxY: 100m, interiorWalls: 40);
        foreach (var wingWall in DenseBody(minX: -40m, minY: 0m, maxX: 0m, maxY: 60m, interiorWalls: 20))
        {
            paths.Add(wingWall);
        }

        var footprint = StructuralFootprint.Resolve(paths);

        Assert.NotNull(footprint);
        Assert.Equal(-40m, footprint.Value.MinX);
        Assert.Equal(100m, footprint.Value.MaxX);
    }

    [Fact]
    public void Resolve_returns_null_when_there_is_no_geometry()
    {
        var footprint = StructuralFootprint.Resolve([]);

        Assert.Null(footprint);
    }

    private static List<GeometryPathDto> DenseBody(decimal minX, decimal minY, decimal maxX, decimal maxY, int interiorWalls)
    {
        var paths = new List<GeometryPathDto>
        {
            HorizontalWall(minY, minX, maxX),
            HorizontalWall(maxY, minX, maxX),
            VerticalWall(minX, minY, maxY),
            VerticalWall(maxX, minY, maxY)
        };

        for (var index = 1; index < interiorWalls; index++)
        {
            var x = minX + ((maxX - minX) * index / interiorWalls);
            paths.Add(VerticalWall(x, minY, maxY));
        }

        return paths;
    }

    private static GeometryPathDto VerticalWall(decimal x, decimal minY, decimal maxY)
    {
        var pathId = Guid.NewGuid();
        return new GeometryPathDto(pathId, false, [new GeometrySegmentDto(pathId, 1, x, minY, x, maxY)]);
    }

    private static GeometryPathDto HorizontalWall(decimal y, decimal minX, decimal maxX)
    {
        var pathId = Guid.NewGuid();
        return new GeometryPathDto(pathId, false, [new GeometrySegmentDto(pathId, 1, minX, y, maxX, y)]);
    }

    private static GeometryPathDto Rectangle(decimal minX, decimal minY, decimal maxX, decimal maxY)
    {
        var pathId = Guid.NewGuid();
        return new GeometryPathDto(
            pathId,
            true,
            [
                new GeometrySegmentDto(pathId, 1, minX, minY, maxX, minY),
                new GeometrySegmentDto(pathId, 2, maxX, minY, maxX, maxY),
                new GeometrySegmentDto(pathId, 3, maxX, maxY, minX, maxY),
                new GeometrySegmentDto(pathId, 4, minX, maxY, minX, minY)
            ]);
    }
}
