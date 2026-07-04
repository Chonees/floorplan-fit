using Avalonia;
using Avalonia.Media;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls;
using FloorplanFit.Domain.FloorPlans;
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
    public void CalculateViewport_ignores_degenerate_zero_length_segments_when_fitting()
    {
        var realPathId = Guid.NewGuid();
        var degeneratePathId = Guid.NewGuid();
        GeometryPathDto[] realPathsOnly =
        [
            new GeometryPathDto(
                realPathId,
                IsClosed: false,
                [
                    new GeometrySegmentDto(realPathId, 1, 0m, 0m, 100m, 0m),
                    new GeometrySegmentDto(realPathId, 2, 100m, 0m, 100m, 200m)
                ])
        ];
        GeometryPathDto[] pathsWithDegenerateOutlier =
        [
            realPathsOnly[0],
            new GeometryPathDto(
                degeneratePathId,
                IsClosed: true,
                [
                    new GeometrySegmentDto(degeneratePathId, 1, 1000m, 1000m, 1000m, 1000m),
                    new GeometrySegmentDto(degeneratePathId, 2, 1000m, 1000m, 1000m, 1000m)
                ])
        ];
        var bounds = new Rect(0, 0, 1000, 700);

        var reference = FloorPlanPreviewGeometry.CalculateViewport(realPathsOnly, bounds, 16d);
        var viewport = FloorPlanPreviewGeometry.CalculateViewport(pathsWithDegenerateOutlier, bounds, 16d);

        Assert.NotNull(reference);
        Assert.NotNull(viewport);
        Assert.Equal(reference.Value.Scale, viewport.Value.Scale);
        Assert.Equal(reference.Value.Project(0m, 0m), viewport.Value.Project(0m, 0m));
        Assert.Equal(reference.Value.Project(100m, 200m), viewport.Value.Project(100m, 200m));
    }

    [Fact]
    public void CalculateViewport_still_builds_viewport_when_every_segment_is_degenerate()
    {
        var pathId = Guid.NewGuid();
        GeometryPathDto[] geometryPaths =
        [
            new GeometryPathDto(
                pathId,
                IsClosed: false,
                [new GeometrySegmentDto(pathId, 1, 40m, 40m, 40m, 40m)])
        ];

        var viewport = FloorPlanPreviewGeometry.CalculateViewport(geometryPaths, new Rect(0, 0, 1000, 700), 16d);

        Assert.NotNull(viewport);
        var projected = viewport.Value.Project(40m, 40m);
        Assert.InRange(projected.X, 0d, 1000d);
        Assert.InRange(projected.Y, 0d, 700d);
    }

    [Fact]
    public void GetPathStyle_returns_selection_green_style_for_highlighted_path()
    {
        var highlightedPathId = Guid.NewGuid();

        var normal = FloorPlanPreviewGeometry.GetPathStyle(Guid.NewGuid(), highlightedPathId);
        var highlighted = FloorPlanPreviewGeometry.GetPathStyle(highlightedPathId, highlightedPathId);

        Assert.False(normal.IsHighlighted);
        Assert.Equal(Colors.SlateGray, normal.Color);
        Assert.Equal(1.25d, normal.Thickness);

        Assert.True(highlighted.IsHighlighted);
        Assert.Equal(Colors.SeaGreen, highlighted.Color);
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

    [Fact]
    public void GetCompressionHandleRects_returns_two_visible_handles_for_height_preview()
    {
        var bounds = new Rect(0, 0, 1000, 700);

        var handles = FloorPlanPreviewGeometry.GetCompressionHandleRects(bounds, Domain.FloorPlans.PinchAxisTag.Height);

        Assert.Equal(2, handles.Count);
        Assert.Contains(handles, item => item.Edge == FloorPlanPreviewGeometry.PreviewCompressionEdge.Top);
        Assert.Contains(handles, item => item.Edge == FloorPlanPreviewGeometry.PreviewCompressionEdge.Bottom);
        Assert.All(handles, item => Assert.True(bounds.Contains(item.Rect.TopLeft)));
        Assert.All(handles, item => Assert.True(bounds.Contains(item.Rect.BottomRight)));
    }

    [Fact]
    public void TryResolveCompressionHandle_returns_bottom_when_pointer_hits_bottom_height_handle()
    {
        var bounds = new Rect(0, 0, 1000, 700);
        var bottomHandle = FloorPlanPreviewGeometry
            .GetCompressionHandleRects(bounds, PinchAxisTag.Height)
            .Single(item => item.Edge == FloorPlanPreviewGeometry.PreviewCompressionEdge.Bottom);
        var pointer = bottomHandle.Rect.Center;

        var resolved = FloorPlanPreviewGeometry.TryResolveCompressionHandle(pointer, bounds, PinchAxisTag.Height);

        Assert.Equal(FloorPlanPreviewGeometry.PreviewCompressionEdge.Bottom, resolved);
    }

    [Fact]
    public void CreatePreviewGeometry_applies_height_compression_from_the_top_handle()
    {
        var markerPathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var topPathId = Guid.NewGuid();
        var middlePathId = Guid.NewGuid();
        var bottomPathId = Guid.NewGuid();

        GeometryPathDto[] geometryPaths =
        [
            new GeometryPathDto(markerPathId, false, [new GeometrySegmentDto(markerPathId, 1, 0m, 0m, 0m, 100m)]),
            new GeometryPathDto(topPathId, false, [new GeometrySegmentDto(topPathId, 1, 0m, 90m, 100m, 90m)]),
            new GeometryPathDto(middlePathId, false, [new GeometrySegmentDto(middlePathId, 1, 0m, 50m, 100m, 50m)]),
            new GeometryPathDto(bottomPathId, false, [new GeometrySegmentDto(bottomPathId, 1, 0m, 10m, 100m, 10m)])
        ];

        PinchMarkerDto[] pinchMarkers =
        [
            new(Guid.NewGuid(), pinchGroupId, "Height", Guid.NewGuid(), markerPathId, nameof(PinchAxisTag.Height), 0.2m, 40m, 1),
            new(Guid.NewGuid(), pinchGroupId, "Height", Guid.NewGuid(), markerPathId, nameof(PinchAxisTag.Height), 0.8m, 40m, 2)
        ];

        var preview = FloorPlanPreviewGeometry.CreatePreviewGeometry(
            geometryPaths,
            PinchAxisTag.Height,
            pinchMarkers,
            requestedTrimSourceUnits: 40m,
            FloorPlanPreviewGeometry.PreviewCompressionEdge.Top);

        Assert.Equal(50m, preview.Single(path => path.Id == topPathId).Segments.Single().StartY);
        Assert.Equal(30m, preview.Single(path => path.Id == middlePathId).Segments.Single().StartY);
        Assert.Equal(10m, preview.Single(path => path.Id == bottomPathId).Segments.Single().StartY);
    }

    [Fact]
    public void CreatePreviewGeometry_applies_height_compression_from_the_bottom_handle()
    {
        var markerPathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var topPathId = Guid.NewGuid();
        var middlePathId = Guid.NewGuid();
        var bottomPathId = Guid.NewGuid();

        GeometryPathDto[] geometryPaths =
        [
            new GeometryPathDto(markerPathId, false, [new GeometrySegmentDto(markerPathId, 1, 0m, 0m, 0m, 100m)]),
            new GeometryPathDto(topPathId, false, [new GeometrySegmentDto(topPathId, 1, 0m, 90m, 100m, 90m)]),
            new GeometryPathDto(middlePathId, false, [new GeometrySegmentDto(middlePathId, 1, 0m, 50m, 100m, 50m)]),
            new GeometryPathDto(bottomPathId, false, [new GeometrySegmentDto(bottomPathId, 1, 0m, 10m, 100m, 10m)])
        ];

        PinchMarkerDto[] pinchMarkers =
        [
            new(Guid.NewGuid(), pinchGroupId, "Height", Guid.NewGuid(), markerPathId, nameof(PinchAxisTag.Height), 0.2m, 40m, 1),
            new(Guid.NewGuid(), pinchGroupId, "Height", Guid.NewGuid(), markerPathId, nameof(PinchAxisTag.Height), 0.8m, 40m, 2)
        ];

        var preview = FloorPlanPreviewGeometry.CreatePreviewGeometry(
            geometryPaths,
            PinchAxisTag.Height,
            pinchMarkers,
            requestedTrimSourceUnits: 40m,
            FloorPlanPreviewGeometry.PreviewCompressionEdge.Bottom);

        Assert.Equal(90m, preview.Single(path => path.Id == topPathId).Segments.Single().StartY);
        Assert.Equal(70m, preview.Single(path => path.Id == middlePathId).Segments.Single().StartY);
        Assert.Equal(50m, preview.Single(path => path.Id == bottomPathId).Segments.Single().StartY);
    }

    [Fact]
    public void CreatePreviewGeometry_converts_marker_max_trim_mm_to_source_units()
    {
        var markerPathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var affectedPathId = Guid.NewGuid();

        GeometryPathDto[] geometryPaths =
        [
            new GeometryPathDto(markerPathId, false, [new GeometrySegmentDto(markerPathId, 1, 10m, 0m, 10m, 100m)]),
            new GeometryPathDto(affectedPathId, false, [new GeometrySegmentDto(affectedPathId, 1, 20m, 0m, 20m, 100m)])
        ];

        PinchMarkerDto[] pinchMarkers =
        [
            new(Guid.NewGuid(), pinchGroupId, "Width", Guid.NewGuid(), markerPathId, nameof(PinchAxisTag.Width), 0.5m, 25.4m, 1)
        ];

        var preview = FloorPlanPreviewGeometry.CreatePreviewGeometry(
            geometryPaths,
            PinchAxisTag.Width,
            pinchMarkers,
            requestedTrimSourceUnits: 10m,
            FloorPlanPreviewGeometry.PreviewCompressionEdge.Right,
            sourceToMillimetersFactor: 25.4m);

        Assert.Equal(19m, preview.Single(path => path.Id == affectedPathId).Segments.Single().StartX);
        Assert.Equal(19m, preview.Single(path => path.Id == affectedPathId).Segments.Single().EndX);
    }

    [Fact]
    public void GetGeometryViewportBounds_for_height_leaves_rendering_space_between_top_and_bottom_handles()
    {
        var bounds = new Rect(0, 0, 1000, 700);

        var geometryBounds = FloorPlanPreviewGeometry.GetGeometryViewportBounds(bounds, PinchAxisTag.Height);
        var handles = FloorPlanPreviewGeometry.GetCompressionHandleRects(bounds, PinchAxisTag.Height);
        var topHandle = handles.Single(item => item.Edge == FloorPlanPreviewGeometry.PreviewCompressionEdge.Top);
        var bottomHandle = handles.Single(item => item.Edge == FloorPlanPreviewGeometry.PreviewCompressionEdge.Bottom);

        Assert.True(geometryBounds.Top > topHandle.Rect.Bottom);
        Assert.True(geometryBounds.Bottom < bottomHandle.Rect.Top);
        Assert.True(geometryBounds.Width > 0d);
        Assert.True(geometryBounds.Height > 0d);
    }

    [Fact]
    public void CalculateViewport_with_non_zero_bounds_keeps_geometry_inside_the_target_rect()
    {
        var pathId = Guid.NewGuid();
        GeometryPathDto[] geometryPaths =
        [
            new GeometryPathDto(
                pathId,
                IsClosed: false,
                [new GeometrySegmentDto(pathId, 1, 0m, 0m, 100m, 200m)])
        ];

        var targetBounds = new Rect(40, 30, 920, 620);

        var viewport = FloorPlanPreviewGeometry.CalculateViewport(geometryPaths, targetBounds, 16d);

        Assert.NotNull(viewport);
        var start = viewport.Value.Project(0m, 0m);
        var end = viewport.Value.Project(100m, 200m);

        Assert.InRange(start.X, targetBounds.Left, targetBounds.Right);
        Assert.InRange(start.Y, targetBounds.Top, targetBounds.Bottom);
        Assert.InRange(end.X, targetBounds.Left, targetBounds.Right);
        Assert.InRange(end.Y, targetBounds.Top, targetBounds.Bottom);
    }

    [Fact]
    public void CalculateViewport_keeps_every_endpoint_inside_the_target_rect_with_requested_padding()
    {
        var firstPathId = Guid.NewGuid();
        var secondPathId = Guid.NewGuid();
        GeometryPathDto[] geometryPaths =
        [
            new GeometryPathDto(
                firstPathId,
                IsClosed: false,
                [
                    new GeometrySegmentDto(firstPathId, 1, -80m, -220m, 30m, 420m),
                    new GeometrySegmentDto(firstPathId, 2, 30m, 420m, 180m, 260m)
                ]),
            new GeometryPathDto(
                secondPathId,
                IsClosed: false,
                [
                    new GeometrySegmentDto(secondPathId, 1, 240m, -200m, 260m, 380m),
                    new GeometrySegmentDto(secondPathId, 2, -100m, -210m, 260m, -210m)
                ])
        ];
        var targetBounds = new Rect(50, 70, 900, 620);
        var padding = 48d;

        var viewport = FloorPlanPreviewGeometry.CalculateViewport(geometryPaths, targetBounds, padding);

        Assert.NotNull(viewport);
        foreach (var point in geometryPaths.SelectMany(path => path.Segments).SelectMany(segment => new[] { viewport.Value.Project(segment.StartX, segment.StartY), viewport.Value.Project(segment.EndX, segment.EndY) }))
        {
            Assert.InRange(point.X, targetBounds.Left + padding - 0.001d, targetBounds.Right - padding + 0.001d);
            Assert.InRange(point.Y, targetBounds.Top + padding - 0.001d, targetBounds.Bottom - padding + 0.001d);
        }
    }

    [Fact]
    public void WithUserTransform_scales_viewport_and_applies_screen_pan()
    {
        var viewport = new FloorPlanPreviewGeometry.PreviewViewport(
            new Rect(0, 0, 500, 400),
            MinX: 10d,
            MinY: 20d,
            Scale: 2d,
            OffsetX: 40d,
            OffsetY: 50d);

        var transformed = viewport.WithUserTransform(zoomFactor: 1.5d, panOffset: new Vector(12d, -8d));

        Assert.Equal(3d, transformed.Scale);
        Assert.Equal(52d, transformed.OffsetX);
        Assert.Equal(58d, transformed.OffsetY);
    }

    [Fact]
    public void HitTestPathDetail_with_explicit_viewport_uses_zoomed_coordinates()
    {
        var pathId = Guid.NewGuid();
        GeometryPathDto[] geometryPaths =
        [
            new GeometryPathDto(
                pathId,
                IsClosed: false,
                [new GeometrySegmentDto(pathId, 1, 0m, 0m, 100m, 0m)])
        ];
        var baseViewport = FloorPlanPreviewGeometry.CalculateViewport(geometryPaths, new Rect(0, 0, 500, 400), 16d);
        Assert.NotNull(baseViewport);
        var zoomedViewport = baseViewport.Value.WithUserTransform(2d, new Vector(40d, 25d));
        var clickPoint = zoomedViewport.Project(75m, 0m);

        var hit = FloorPlanPreviewGeometry.HitTestPathDetail(geometryPaths, zoomedViewport, clickPoint, tolerance: 8d);

        Assert.NotNull(hit);
        Assert.Equal(pathId, hit.Value.GeometryPathId);
    }

    [Fact]
    public void Preview_control_localizes_parent_bounds_before_rendering()
    {
        var parentOffsetBounds = new Rect(420, 260, 900, 620);

        var localBounds = FloorPlanPreviewControl.GetLocalRenderBounds(parentOffsetBounds);

        Assert.Equal(0d, localBounds.Left);
        Assert.Equal(0d, localBounds.Top);
        Assert.Equal(parentOffsetBounds.Width, localBounds.Width);
        Assert.Equal(parentOffsetBounds.Height, localBounds.Height);
    }
}
