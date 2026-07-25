using Avalonia;
using Avalonia.Media;
using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Desktop.Controls;

internal static class FloorPlanPreviewGeometry
{
    private const double HandleInset = 8d;
    private const double HandleThickness = 18d;
    private const double MinHandleLength = 120d;
    private const double MaxHandleLength = 220d;
    private const double GeometryViewportClearance = 16d;

    public static PreviewViewport? CalculateViewport(IReadOnlyList<GeometryPathDto>? geometryPaths, Rect bounds, double padding)
    {
        if (geometryPaths is not { Count: > 0 })
        {
            return null;
        }

        var allSegments = geometryPaths
            .SelectMany(path => path.Segments)
            .ToArray();

        if (allSegments.Length == 0)
        {
            return null;
        }

        // Fit to segments with real extent so collapsed leftovers (zero-length artifacts
        // from degenerate CAD entities) cannot stretch the envelope and shrink the zoom.
        var segments = allSegments.Where(segment => segment.HasExtent()).ToArray();
        if (segments.Length == 0)
        {
            segments = allSegments;
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
            ? new PathStyle(Preview.PreviewSemanticPalette.WallHighlight, 3.5d, true)
            : new PathStyle(Preview.PreviewSemanticPalette.Wall, 1.25d, false);
    }

    public static Guid? HitTestPath(
        IReadOnlyList<GeometryPathDto>? geometryPaths,
        Rect bounds,
        Point point,
        double padding,
        double tolerance)
    {
        return HitTestPathDetail(geometryPaths, bounds, point, padding, tolerance)?.GeometryPathId;
    }

    public static HitTestDetail? HitTestPathDetail(
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

        return HitTestPathDetail(geometryPaths, viewport.Value, point, tolerance);
    }

    public static HitTestDetail? HitTestPathDetail(
        IReadOnlyList<GeometryPathDto>? geometryPaths,
        PreviewViewport viewport,
        Point point,
        double tolerance)
    {
        if (geometryPaths is not { Count: > 0 })
        {
            return null;
        }

        var toleranceSquared = tolerance * tolerance;
        HitTestDetail? closest = null;
        var closestDistanceSquared = double.MaxValue;

        foreach (var path in geometryPaths)
        {
            var totalLength = GetPathLength(path);
            var traversedLength = 0d;

            foreach (var segment in path.Segments)
            {
                var projectedStart = viewport.Project(segment.StartX, segment.StartY);
                var projectedEnd = viewport.Project(segment.EndX, segment.EndY);
                var projection = ProjectToSegment(point, projectedStart, projectedEnd);

                if (projection.DistanceSquared > toleranceSquared || projection.DistanceSquared >= closestDistanceSquared)
                {
                    traversedLength += GetSegmentLength(segment);
                    continue;
                }

                var segmentLength = GetSegmentLength(segment);
                var pathRatio = totalLength <= double.Epsilon
                    ? 0m
                    : (decimal)((traversedLength + (segmentLength * projection.ProjectionRatio)) / totalLength);

                closestDistanceSquared = projection.DistanceSquared;
                closest = new HitTestDetail(path.Id, decimal.Clamp(pathRatio, 0m, 1m));
                traversedLength += segmentLength;
            }
        }

        return closest;
    }

    public static Point? GetPointAtRatio(GeometryPathDto path, decimal positionRatio)
    {
        if (path.Segments.Count == 0)
        {
            return null;
        }

        var clampedRatio = decimal.Clamp(positionRatio, 0m, 1m);
        var totalLength = path.Segments.Sum(GetSegmentLength);
        if (totalLength <= double.Epsilon)
        {
            var first = path.Segments[0];
            return new Point((double)first.StartX, (double)first.StartY);
        }

        var targetLength = totalLength * (double)clampedRatio;
        var traversedLength = 0d;

        foreach (var segment in path.Segments)
        {
            var segmentLength = GetSegmentLength(segment);
            if (traversedLength + segmentLength >= targetLength)
            {
                var localRatio = segmentLength <= double.Epsilon
                    ? 0d
                    : (targetLength - traversedLength) / segmentLength;

                var x = (double)segment.StartX + (((double)segment.EndX - (double)segment.StartX) * localRatio);
                var y = (double)segment.StartY + (((double)segment.EndY - (double)segment.StartY) * localRatio);
                return new Point(x, y);
            }

            traversedLength += segmentLength;
        }

        var last = path.Segments[^1];
        return new Point((double)last.EndX, (double)last.EndY);
    }

    public static IReadOnlyList<GeometryPathDto> CreatePreviewGeometry(
        IReadOnlyList<GeometryPathDto>? geometryPaths,
        PinchAxisTag axisTag,
        IReadOnlyList<PinchMarkerDto>? pinchMarkers,
        decimal requestedTrimSourceUnits,
        PreviewCompressionEdge edge,
        decimal sourceToMillimetersFactor = 1m)
    {
        if (geometryPaths is not { Count: > 0 } || pinchMarkers is not { Count: > 0 } || requestedTrimSourceUnits <= 0m)
        {
            return geometryPaths ?? [];
        }

        var effectiveSourceToMillimetersFactor = sourceToMillimetersFactor > 0m
            ? sourceToMillimetersFactor
            : 1m;
        var geometryLookup = geometryPaths.ToDictionary(item => item.Id);
        var axisMarkers = pinchMarkers
            .Where(item => string.Equals(item.AxisTag, axisTag.ToString(), StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var activeMarkers = new List<ResolvedMarker>();

        foreach (var marker in axisMarkers)
        {
            if (!geometryLookup.TryGetValue(marker.GeometryPathId, out var path))
            {
                continue;
            }

            var point = GetPointAtRatio(path, marker.PositionRatio);
            if (point is null)
            {
                continue;
            }

            var maxTrimSourceUnits = marker.MaxTrimMm / effectiveSourceToMillimetersFactor;
            activeMarkers.Add(new ResolvedMarker(
                axisTag == PinchAxisTag.Width ? (decimal)point.Value.X : (decimal)point.Value.Y,
                decimal.Min(maxTrimSourceUnits, requestedTrimSourceUnits / axisMarkers.Length)));
        }

        activeMarkers = activeMarkers
            .OrderBy(item => item.Coordinate)
            .ToList();

        if (activeMarkers.Count == 0)
        {
            return geometryPaths;
        }

        return geometryPaths
            .Select(path => new GeometryPathDto(
                path.Id,
                path.IsClosed,
                path.Segments.Select(segment =>
                {
                    var start = TransformPoint(segment.StartX, segment.StartY, axisTag, edge, activeMarkers);
                    var end = TransformPoint(segment.EndX, segment.EndY, axisTag, edge, activeMarkers);
                    return new GeometrySegmentDto(path.Id, segment.SortOrder, start.X, start.Y, end.X, end.Y);
                }).ToArray()))
            .ToArray();
    }

    public static IReadOnlyList<GeometryPathDto> CreatePreviewGeometry(
        IReadOnlyList<GeometryPathDto>? geometryPaths,
        IReadOnlyList<AdjustmentRecipeStretchActionDto>? stretchActions)
    {
        if (geometryPaths is not { Count: > 0 } || stretchActions is not { Count: > 0 })
        {
            return geometryPaths ?? [];
        }

        var entities = geometryPaths
            .SelectMany(path => path.Segments.Select(segment => new CadStretchEntity(
                SegmentEntityId(path.Id, segment.SortOrder),
                "LINE",
                [
                    new CadStretchPoint(segment.StartX, segment.StartY),
                    new CadStretchPoint(segment.EndX, segment.EndY)
                ])))
            .ToArray();
        var pathSegments = geometryPaths.ToDictionary(
            path => path.Id,
            path => path.Segments.ToDictionary(segment => segment.SortOrder));
        var accumulated = new Dictionary<(string EntityId, int VertexIndex), (decimal DeltaX, decimal DeltaY)>();
        foreach (var actionDto in stretchActions)
        {
            var roles = ExpandPreviewRoles(actionDto, pathSegments);
            var targetEntityIds = actionDto.TargetSpans
                .Select(span => SegmentEntityId(span.GeometryPathId, span.SegmentSortOrder))
                .ToArray();
            var result = CadStretchDeformationEngine.Apply(
                new CadStretchAction(
                    actionDto.ActionId,
                    actionDto.AxisTag,
                    actionDto.Edge,
                    actionDto.DeltaSourceUnits,
                    actionDto.MaxDeltaSourceUnits,
                    targetEntityIds,
                    actionDto.CoordinateTolerance),
                entities,
                roles);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"CAD stretch action '{actionDto.ActionId}' rejected preview deformation: {result.RejectionReason}");
            }

            foreach (var edit in result.Edits)
            {
                foreach (var vertex in edit.Vertices)
                {
                    var key = (edit.EntityId, vertex.VertexIndex);
                    accumulated.TryGetValue(key, out var previous);
                    accumulated[key] = (
                        previous.DeltaX + vertex.DeltaX,
                        previous.DeltaY + vertex.DeltaY);
                }
            }
        }

        return geometryPaths
            .Select(path => new GeometryPathDto(
                path.Id,
                path.IsClosed,
                path.Segments.Select(segment =>
                {
                    var entityId = SegmentEntityId(path.Id, segment.SortOrder);
                    accumulated.TryGetValue((entityId, 0), out var startDelta);
                    accumulated.TryGetValue((entityId, 1), out var endDelta);
                    return new GeometrySegmentDto(
                        path.Id,
                        segment.SortOrder,
                        segment.StartX + startDelta.DeltaX,
                        segment.StartY + startDelta.DeltaY,
                        segment.EndX + endDelta.DeltaX,
                        segment.EndY + endDelta.DeltaY);
                }).ToArray()))
            .ToArray();
    }

    private static IReadOnlyList<CadStretchEntityRole> ExpandPreviewRoles(
        AdjustmentRecipeStretchActionDto action,
        IReadOnlyDictionary<Guid, Dictionary<int, GeometrySegmentDto>> pathSegments)
    {
        var roles = new List<CadStretchEntityRole>();
        foreach (var roleDto in action.CanonicalEntityRoles)
        {
            var role = ParseRole(roleDto.Role);
            if (!roleDto.GeometryPathId.HasValue ||
                !pathSegments.TryGetValue(roleDto.GeometryPathId.Value, out var segments))
            {
                roles.Add(new CadStretchEntityRole(
                    roleDto.EntityRef,
                    CadStretchRole.Rejected,
                    [],
                    roleDto.Reason ?? $"Recipe role '{roleDto.EntityRef}' has no preview geometry path."));
                continue;
            }

            var selectedSegments = roleDto.SegmentSortOrder.HasValue
                ? segments.Where(pair => pair.Key == roleDto.SegmentSortOrder.Value).Select(pair => pair.Value).ToArray()
                : segments.Values.ToArray();
            if (selectedSegments.Length == 0)
            {
                roles.Add(new CadStretchEntityRole(
                    roleDto.EntityRef,
                    CadStretchRole.Rejected,
                    [],
                    roleDto.Reason ?? $"Recipe role '{roleDto.EntityRef}' has no matching preview segment."));
                continue;
            }

            foreach (var segment in selectedSegments)
            {
                roles.Add(new CadStretchEntityRole(
                    SegmentEntityId(roleDto.GeometryPathId.Value, segment.SortOrder),
                    role,
                    role == CadStretchRole.Stretch ? roleDto.VertexIndices : [],
                    roleDto.Reason));
            }
        }

        return roles;
    }

    private static CadStretchRole ParseRole(string role)
        => role.Trim() switch
        {
            var value when value.Equals("Stretch", StringComparison.OrdinalIgnoreCase) => CadStretchRole.Stretch,
            var value when value.Equals("RigidMove", StringComparison.OrdinalIgnoreCase) => CadStretchRole.RigidMove,
            var value when value.Equals("Fixed", StringComparison.OrdinalIgnoreCase) => CadStretchRole.Fixed,
            var value when value.Equals("Rejected", StringComparison.OrdinalIgnoreCase) => CadStretchRole.Rejected,
            _ => CadStretchRole.Rejected
        };

    private static string SegmentEntityId(Guid pathId, int segmentSortOrder)
        => $"{pathId:N}:{segmentSortOrder}";

    public static IReadOnlyList<CompressionHandle> GetCompressionHandleRects(Rect bounds, PinchAxisTag axisTag)
    {
        if (bounds.Width <= double.Epsilon || bounds.Height <= double.Epsilon)
        {
            return [];
        }

        if (axisTag == PinchAxisTag.Width)
        {
            var handleLength = ClampHandleLength(bounds.Height - ((HandleInset + HandleThickness) * 2d), bounds.Height * 0.28d);
            var top = bounds.Center.Y - (handleLength / 2d);

            return
            [
                new CompressionHandle(
                    PreviewCompressionEdge.Left,
                    new Rect(bounds.Left + HandleInset, top, HandleThickness, handleLength)),
                new CompressionHandle(
                    PreviewCompressionEdge.Right,
                    new Rect(bounds.Right - HandleInset - HandleThickness, top, HandleThickness, handleLength))
            ];
        }

        var horizontalLength = ClampHandleLength(bounds.Width - ((HandleInset + HandleThickness) * 2d), bounds.Width * 0.28d);
        var left = bounds.Center.X - (horizontalLength / 2d);

        return
        [
            new CompressionHandle(
                PreviewCompressionEdge.Top,
                new Rect(left, bounds.Top + HandleInset, horizontalLength, HandleThickness)),
            new CompressionHandle(
                PreviewCompressionEdge.Bottom,
                new Rect(left, bounds.Bottom - HandleInset - HandleThickness, horizontalLength, HandleThickness))
        ];
    }

    public static Rect GetGeometryViewportBounds(Rect bounds, PinchAxisTag axisTag)
    {
        var safeBounds = new Rect(
            bounds.Left + GeometryViewportClearance,
            bounds.Top + GeometryViewportClearance,
            Math.Max(1d, bounds.Width - (GeometryViewportClearance * 2d)),
            Math.Max(1d, bounds.Height - (GeometryViewportClearance * 2d)));

        var handles = GetCompressionHandleRects(bounds, axisTag);

        if (axisTag == PinchAxisTag.Width)
        {
            var leftHandle = handles.Single(item => item.Edge == PreviewCompressionEdge.Left);
            var rightHandle = handles.Single(item => item.Edge == PreviewCompressionEdge.Right);
            var left = leftHandle.Rect.Right + GeometryViewportClearance;
            var right = rightHandle.Rect.Left - GeometryViewportClearance;

            return new Rect(
                left,
                safeBounds.Top,
                Math.Max(1d, right - left),
                safeBounds.Height);
        }

        var topHandle = handles.Single(item => item.Edge == PreviewCompressionEdge.Top);
        var bottomHandle = handles.Single(item => item.Edge == PreviewCompressionEdge.Bottom);
        var top = topHandle.Rect.Bottom + GeometryViewportClearance;
        var bottom = bottomHandle.Rect.Top - GeometryViewportClearance;

        return new Rect(
            safeBounds.Left,
            top,
            safeBounds.Width,
            Math.Max(1d, bottom - top));
    }

    public static PreviewCompressionEdge? TryResolveCompressionHandle(Point pointerPosition, Rect bounds, PinchAxisTag axisTag)
    {
        foreach (var handle in GetCompressionHandleRects(bounds, axisTag))
        {
            if (handle.Rect.Contains(pointerPosition))
            {
                return handle.Edge;
            }
        }

        return null;
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
            return Project((double)x, (double)y);
        }

        public Point Project(double x, double y)
        {
            var projectedX = Bounds.Left + OffsetX + ((x - MinX) * Scale);
            var projectedY = Bounds.Bottom - (OffsetY + ((y - MinY) * Scale));
            return new Point(projectedX, projectedY);
        }

        public Point Unproject(Point point)
        {
            var worldX = MinX + ((point.X - Bounds.Left - OffsetX) / Scale);
            var worldY = MinY + ((Bounds.Bottom - point.Y - OffsetY) / Scale);
            return new Point(worldX, worldY);
        }

        public PreviewViewport WithUserTransform(double zoomFactor, Vector panOffset)
        {
            var safeZoomFactor = Math.Max(zoomFactor, double.Epsilon);
            return new PreviewViewport(
                Bounds,
                MinX,
                MinY,
                Scale * safeZoomFactor,
                OffsetX + panOffset.X,
                OffsetY - panOffset.Y);
        }
    }

    internal readonly record struct PathStyle(Color Color, double Thickness, bool IsHighlighted);

    internal readonly record struct HitTestDetail(Guid GeometryPathId, decimal PositionRatio);

    internal readonly record struct CompressionHandle(PreviewCompressionEdge Edge, Rect Rect);

    internal enum PreviewCompressionEdge
    {
        Left = 1,
        Right = 2,
        Top = 3,
        Bottom = 4
    }

    private static TransformedPoint TransformPoint(
        decimal x,
        decimal y,
        PinchAxisTag axisTag,
        PreviewCompressionEdge edge,
        IReadOnlyList<ResolvedMarker> markers)
    {
        if (axisTag == PinchAxisTag.Width)
        {
            var deltaX = 0m;
            foreach (var marker in markers)
            {
                if (edge == PreviewCompressionEdge.Right && x >= marker.Coordinate)
                {
                    deltaX -= marker.TrimSourceUnits;
                }
                else if (edge == PreviewCompressionEdge.Left && x <= marker.Coordinate)
                {
                    deltaX += marker.TrimSourceUnits;
                }
            }

            return new TransformedPoint(x + deltaX, y);
        }

        var deltaY = 0m;
        foreach (var marker in markers)
        {
            if (edge == PreviewCompressionEdge.Top && y >= marker.Coordinate)
            {
                deltaY -= marker.TrimSourceUnits;
            }
            else if (edge == PreviewCompressionEdge.Bottom && y <= marker.Coordinate)
            {
                deltaY += marker.TrimSourceUnits;
            }
        }

        return new TransformedPoint(x, y + deltaY);
    }

    private static SegmentProjection ProjectToSegment(Point point, Point start, Point end)
    {
        var deltaX = end.X - start.X;
        var deltaY = end.Y - start.Y;
        var segmentLengthSquared = (deltaX * deltaX) + (deltaY * deltaY);

        if (segmentLengthSquared <= double.Epsilon)
        {
            return new SegmentProjection(DistanceSquared(point, start), 0d);
        }

        var projection = (((point.X - start.X) * deltaX) + ((point.Y - start.Y) * deltaY)) / segmentLengthSquared;
        var clampedProjection = Math.Clamp(projection, 0d, 1d);
        var closestPoint = new Point(start.X + (deltaX * clampedProjection), start.Y + (deltaY * clampedProjection));
        return new SegmentProjection(DistanceSquared(point, closestPoint), clampedProjection);
    }

    private static double DistanceSquared(Point point, Point other)
    {
        var deltaX = point.X - other.X;
        var deltaY = point.Y - other.Y;
        return (deltaX * deltaX) + (deltaY * deltaY);
    }

    private static double GetPathLength(GeometryPathDto path)
    {
        return path.Segments.Sum(GetSegmentLength);
    }

    private static double GetSegmentLength(GeometrySegmentDto segment)
    {
        var deltaX = (double)(segment.EndX - segment.StartX);
        var deltaY = (double)(segment.EndY - segment.StartY);
        return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
    }

    private static double ClampHandleLength(double availableLength, double desiredLength)
    {
        var maxAllowed = Math.Max(40d, availableLength);
        return Math.Clamp(desiredLength, Math.Min(MinHandleLength, maxAllowed), Math.Min(MaxHandleLength, maxAllowed));
    }

    private readonly record struct SegmentProjection(double DistanceSquared, double ProjectionRatio);

    private readonly record struct ResolvedMarker(decimal Coordinate, decimal TrimSourceUnits);

    private readonly record struct TransformedPoint(decimal X, decimal Y);
}
