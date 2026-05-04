using Avalonia;
using Avalonia.Media;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls;
using Xunit;

namespace FloorplanFit.Desktop.Tests.Controls;

public sealed class FloorPlanPreviewGeometryTests
{
    [Fact]
    public void CalculateViewport_centers_geometry_within_available_bounds()
    {
        var pathId = Guid.NewGuid();
        GeometryPathDto[] geometryPaths =
        [
            new GeometryPathDto(
                pathId,
                IsClosed: false,
                [
                    new GeometrySegmentDto(pathId, 1, 0m, 0m, 100m, 0m),
                    new GeometrySegmentDto(pathId, 2, 100m, 0m, 100m, 200m)
                ])
        ];

        var viewport = FloorPlanPreviewGeometry.CalculateViewport(geometryPaths, new Rect(0, 0, 1000, 700), 16d);

        Assert.NotNull(viewport);

        Point[] points =
        [
            viewport.Value.Project(0m, 0m),
            viewport.Value.Project(100m, 0m),
            viewport.Value.Project(100m, 200m)
        ];

        var minX = points.Min(point => point.X);
        var maxX = points.Max(point => point.X);
        var minY = points.Min(point => point.Y);
        var maxY = points.Max(point => point.Y);

        var leftMargin = minX;
        var rightMargin = 1000d - maxX;
        var topMargin = minY;
        var bottomMargin = 700d - maxY;

        Assert.InRange(Math.Abs(leftMargin - rightMargin), 0d, 0.001d);
        Assert.InRange(Math.Abs(topMargin - bottomMargin), 0d, 0.001d);
    }

    [Fact]
    public void GetPathStyle_returns_emphasized_red_style_for_highlighted_path()
    {
        var highlightedPathId = Guid.NewGuid();

        var normal = FloorPlanPreviewGeometry.GetPathStyle(Guid.NewGuid(), highlightedPathId);
        var highlighted = FloorPlanPreviewGeometry.GetPathStyle(highlightedPathId, highlightedPathId);

        Assert.False(normal.IsHighlighted);
        Assert.Equal(Colors.SlateGray, normal.Color);
        Assert.Equal(1.25d, normal.Thickness);

        Assert.True(highlighted.IsHighlighted);
        Assert.Equal(Colors.OrangeRed, highlighted.Color);
        Assert.True(highlighted.Thickness > normal.Thickness);
    }

    [Fact]
    public void HitTestPath_returns_the_closest_path_within_tolerance()
    {
        var horizontalPathId = Guid.NewGuid();
        var verticalPathId = Guid.NewGuid();
        GeometryPathDto[] geometryPaths =
        [
            new GeometryPathDto(
                horizontalPathId,
                IsClosed: false,
                [new GeometrySegmentDto(horizontalPathId, 1, 0m, 0m, 100m, 0m)]),
            new GeometryPathDto(
                verticalPathId,
                IsClosed: false,
                [new GeometrySegmentDto(verticalPathId, 1, 100m, 0m, 100m, 100m)])
        ];

        var bounds = new Rect(0, 0, 1000, 700);
        var viewport = FloorPlanPreviewGeometry.CalculateViewport(geometryPaths, bounds, 16d);
        Assert.NotNull(viewport);

        var clickPoint = viewport.Value.Project(100m, 45m);

        var hit = FloorPlanPreviewGeometry.HitTestPath(geometryPaths, bounds, clickPoint, padding: 16d, tolerance: 10d);

        Assert.Equal(verticalPathId, hit);
    }

    [Fact]
    public void HitTestPath_returns_null_when_click_is_outside_tolerance()
    {
        var pathId = Guid.NewGuid();
        GeometryPathDto[] geometryPaths =
        [
            new GeometryPathDto(
                pathId,
                IsClosed: false,
                [new GeometrySegmentDto(pathId, 1, 0m, 0m, 100m, 0m)])
        ];

        var hit = FloorPlanPreviewGeometry.HitTestPath(
            geometryPaths,
            new Rect(0, 0, 1000, 700),
            new Point(30, 30),
            padding: 16d,
            tolerance: 6d);

        Assert.Null(hit);
    }
}
