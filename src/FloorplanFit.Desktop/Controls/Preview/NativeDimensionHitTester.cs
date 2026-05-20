using Avalonia;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class NativeDimensionHitTester
{
    public static bool TryResolveDimensionHit(
        IReadOnlyList<DimensionDto>? dimensions,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        Point pointerPosition,
        double hitTolerancePixels,
        out DimensionDto dimension,
        out FloorPlanPreviewControl.DimensionHandleKind suggestedHandle,
        out FloorPlanPreviewControl.DimensionHitArea hitArea,
        out Point referenceWorldPoint)
    {
        if (dimensions is not { Count: > 0 })
        {
            dimension = null!;
            suggestedHandle = default;
            hitArea = default;
            referenceWorldPoint = default;
            return false;
        }

        foreach (var item in dimensions.Reverse())
        {
            if (!string.IsNullOrWhiteSpace(item.DisplayText) &&
                CadTextPreviewLayerRenderer.GetDimensionBounds(item, viewport).Contains(pointerPosition))
            {
                dimension = item;
                suggestedHandle = FloorPlanPreviewControl.DimensionHandleKind.DimensionLinePoint;
                hitArea = FloorPlanPreviewControl.DimensionHitArea.BodyText;
                referenceWorldPoint = NativeDimensionEditor.ResolveImplicitBodyControl(item);
                return true;
            }

            foreach (var segment in EnumerateWorldSegments(item))
            {
                var projectedStart = viewport.Project(segment.StartX, segment.StartY);
                var projectedEnd = viewport.Project(segment.EndX, segment.EndY);
                if (DistanceSquared(pointerPosition, projectedStart, projectedEnd) <= hitTolerancePixels * hitTolerancePixels)
                {
                    dimension = item;
                    suggestedHandle = FloorPlanPreviewControl.DimensionHandleKind.DimensionLinePoint;
                    hitArea = FloorPlanPreviewControl.DimensionHitArea.BodyLine;
                    referenceWorldPoint = ResolveNearestWorldPoint(segment, projectedStart, projectedEnd, pointerPosition);
                    return true;
                }
            }
        }

        dimension = null!;
        suggestedHandle = default;
        hitArea = default;
        referenceWorldPoint = default;
        return false;
    }

    public static bool TryResolveHandleHit(
        IReadOnlyList<DimensionDto>? dimensions,
        Guid? highlightedDimensionId,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        Point pointerPosition,
        double handleTolerancePixels,
        out DimensionDto dimension,
        out FloorPlanPreviewControl.DimensionHandleKind handleKind,
        out Point referenceWorldPoint)
    {
        var resolvedDimension = dimensions?.FirstOrDefault(item => item.DimensionId == highlightedDimensionId);
        if (resolvedDimension is null)
        {
            dimension = null!;
            handleKind = default;
            referenceWorldPoint = default;
            return false;
        }

        foreach (var (candidateHandleKind, worldPoint) in NativeDimensionEditor.ResolveHandles(resolvedDimension))
        {
            var projected = viewport.Project(worldPoint.X, worldPoint.Y);
            if (DistanceSquared(pointerPosition, projected) <= handleTolerancePixels * handleTolerancePixels)
            {
                dimension = resolvedDimension;
                handleKind = candidateHandleKind;
                referenceWorldPoint = worldPoint;
                return true;
            }
        }

        handleKind = default;
        dimension = null!;
        referenceWorldPoint = default;
        return false;
    }

    private static IReadOnlyList<DimensionLineSegmentDto> EnumerateWorldSegments(DimensionDto dimension)
    {
        return dimension.LinePrimitives.Count > 0
            ? dimension.LinePrimitives
                .Select(item => new DimensionLineSegmentDto(item.StartX, item.StartY, item.EndX, item.EndY))
                .ToArray()
            : dimension.LineSegments;
    }

    private static Point ResolveNearestWorldPoint(
        DimensionLineSegmentDto segment,
        Point projectedStart,
        Point projectedEnd,
        Point pointerPosition)
    {
        var deltaX = projectedEnd.X - projectedStart.X;
        var deltaY = projectedEnd.Y - projectedStart.Y;
        var segmentLengthSquared = (deltaX * deltaX) + (deltaY * deltaY);
        if (segmentLengthSquared <= double.Epsilon)
        {
            return new Point((double)segment.StartX, (double)segment.StartY);
        }

        var projection = (((pointerPosition.X - projectedStart.X) * deltaX) + ((pointerPosition.Y - projectedStart.Y) * deltaY)) / segmentLengthSquared;
        var clamped = Math.Clamp(projection, 0d, 1d);
        var startX = (double)segment.StartX;
        var startY = (double)segment.StartY;
        var endX = (double)segment.EndX;
        var endY = (double)segment.EndY;
        return new Point(
            startX + ((endX - startX) * clamped),
            startY + ((endY - startY) * clamped));
    }

    private static double DistanceSquared(Point start, Point end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        return (dx * dx) + (dy * dy);
    }

    private static double DistanceSquared(Point point, Point segmentStart, Point segmentEnd)
    {
        var deltaX = segmentEnd.X - segmentStart.X;
        var deltaY = segmentEnd.Y - segmentStart.Y;
        var segmentLengthSquared = (deltaX * deltaX) + (deltaY * deltaY);
        if (segmentLengthSquared <= double.Epsilon)
        {
            return DistanceSquared(point, segmentStart);
        }

        var projection = (((point.X - segmentStart.X) * deltaX) + ((point.Y - segmentStart.Y) * deltaY)) / segmentLengthSquared;
        var clamped = Math.Clamp(projection, 0d, 1d);
        var projected = new Point(segmentStart.X + (deltaX * clamped), segmentStart.Y + (deltaY * clamped));
        return DistanceSquared(point, projected);
    }
}
