using Avalonia;
using Avalonia.Media;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.Controls;

internal static class FloorPlanPreviewGeometry
{
    public static PreviewViewport? CalculateViewport(IReadOnlyList<GeometryPathDto>? geometryPaths, Rect bounds, double padding)
    {
        if (geometryPaths is not { Count: > 0 })
        {
            return null;
        }

        var segments = geometryPaths
            .SelectMany(path => path.Segments)
            .ToArray();

        if (segments.Length == 0)
        {
            return null;
        }

        var minX = segments.Min(segment => Math.Min((double)segment.StartX, (double)segment.EndX));
        var minY = segments.Min(segment => Math.Min((double)segment.StartY, (double)segment.EndY));
        var maxX = segments.Max(segment => Math.Max((double)segment.StartX, (double)segment.EndX));
        var maxY = segments.Max(segment => Math.Max((double)segment.StartY, (double)segment.EndY));

        var width = Math.Max(maxX - minX, 1d);
        var height = Math.Max(maxY - minY, 1d);
        var availableWidth = Math.Max(bounds.Width - (padding * 2d), 1d);
        var availableHeight = Math.Max(bounds.Height - (padding * 2d), 1d);
        var scale = Math.Min(availableWidth / width, availableHeight / height);
        var offsetX = padding + ((availableWidth - (width * scale)) / 2d);
        var offsetY = padding + ((availableHeight - (height * scale)) / 2d);

        return new PreviewViewport(bounds, minX, minY, scale, offsetX, offsetY);
    }

    public static PathStyle GetPathStyle(Guid pathId, Guid? highlightGeometryPathId)
    {
        var isHighlighted = highlightGeometryPathId == pathId;
        return isHighlighted
            ? new PathStyle(Colors.OrangeRed, 3.5d, true)
            : new PathStyle(Colors.SlateGray, 1.25d, false);
    }

    public static Guid? HitTestPath(
        IReadOnlyList<GeometryPathDto>? geometryPaths,
        Rect bounds,
        Point point,
        double padding,
        double tolerance)
    {
        var viewport = CalculateViewport(geometryPaths, bounds, padding);
        if (viewport is null || geometryPaths is not { Count: > 0 })
        {
            return null;
        }

        var toleranceSquared = tolerance * tolerance;
        Guid? closestPathId = null;
        var closestDistanceSquared = double.MaxValue;

        foreach (var path in geometryPaths)
        {
            foreach (var segment in path.Segments)
            {
                var projectedStart = viewport.Value.Project(segment.StartX, segment.StartY);
                var projectedEnd = viewport.Value.Project(segment.EndX, segment.EndY);
                var distanceSquared = DistanceSquaredToSegment(point, projectedStart, projectedEnd);

                if (distanceSquared > toleranceSquared || distanceSquared >= closestDistanceSquared)
                {
                    continue;
                }

                closestDistanceSquared = distanceSquared;
                closestPathId = path.Id;
            }
        }

        return closestPathId;
    }

    internal readonly record struct PreviewViewport(
        Rect Bounds,
        double MinX,
        double MinY,
        double Scale,
        double OffsetX,
        double OffsetY)
    {
        public Point Project(decimal x, decimal y)
        {
            var projectedX = OffsetX + (((double)x - MinX) * Scale);
            var projectedY = Bounds.Height - (OffsetY + (((double)y - MinY) * Scale));
            return new Point(projectedX, projectedY);
        }
    }

    private static double DistanceSquaredToSegment(Point point, Point start, Point end)
    {
        var deltaX = end.X - start.X;
        var deltaY = end.Y - start.Y;
        var segmentLengthSquared = (deltaX * deltaX) + (deltaY * deltaY);

        if (segmentLengthSquared <= double.Epsilon)
        {
            return DistanceSquared(point, start);
        }

        var projection = (((point.X - start.X) * deltaX) + ((point.Y - start.Y) * deltaY)) / segmentLengthSquared;
        var clampedProjection = Math.Clamp(projection, 0d, 1d);
        var closestPoint = new Point(start.X + (deltaX * clampedProjection), start.Y + (deltaY * clampedProjection));
        return DistanceSquared(point, closestPoint);
    }

    private static double DistanceSquared(Point point, Point other)
    {
        var deltaX = point.X - other.X;
        var deltaY = point.Y - other.Y;
        return (deltaX * deltaX) + (deltaY * deltaY);
    }

    internal readonly record struct PathStyle(Color Color, double Thickness, bool IsHighlighted);
}
