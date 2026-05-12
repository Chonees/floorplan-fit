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
        out FloorPlanPreviewControl.DimensionHandleKind suggestedHandle)
    {
        if (dimensions is not { Count: > 0 })
        {
            dimension = null!;
            suggestedHandle = default;
            return false;
        }

        foreach (var item in dimensions.Reverse())
        {
            if (!string.IsNullOrWhiteSpace(item.DisplayText) &&
                CadTextPreviewLayerRenderer.GetDimensionBounds(item, viewport).Contains(pointerPosition))
            {
                dimension = item;
                suggestedHandle = FloorPlanPreviewControl.DimensionHandleKind.TextAnchor;
                return true;
            }

            foreach (var projected in DimensionPreviewLayerRenderer.CreateProjectedSegments(item, viewport))
            {
                if (DistanceSquared(pointerPosition, projected.Start, projected.End) <= hitTolerancePixels * hitTolerancePixels)
                {
                    dimension = item;
                    suggestedHandle = GuessSuggestedHandle(item, viewport, pointerPosition);
                    return true;
                }
            }
        }

        dimension = null!;
        suggestedHandle = default;
        return false;
    }

    public static bool TryResolveHandleHit(
        IReadOnlyList<DimensionDto>? dimensions,
        Guid? highlightedDimensionId,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        Point pointerPosition,
        double handleTolerancePixels,
        out DimensionDto dimension,
        out FloorPlanPreviewControl.DimensionHandleKind handleKind)
    {
        var resolvedDimension = dimensions?.FirstOrDefault(item => item.DimensionId == highlightedDimensionId);
        if (resolvedDimension is null)
        {
            dimension = null!;
            handleKind = default;
            return false;
        }

        foreach (var (candidateHandleKind, worldPoint) in NativeDimensionEditor.ResolveHandles(resolvedDimension))
        {
            var projected = viewport.Project(worldPoint.X, worldPoint.Y);
            if (DistanceSquared(pointerPosition, projected) <= handleTolerancePixels * handleTolerancePixels)
            {
                dimension = resolvedDimension;
                handleKind = candidateHandleKind;
                return true;
            }
        }

        handleKind = default;
        dimension = null!;
        return false;
    }

    private static FloorPlanPreviewControl.DimensionHandleKind GuessSuggestedHandle(
        DimensionDto dimension,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        Point pointerPosition)
    {
        return NativeDimensionEditor.ResolveHandles(dimension)
            .Select(item => (item.HandleKind, Distance: Distance(viewport.Project(item.WorldPoint.X, item.WorldPoint.Y), pointerPosition)))
            .OrderBy(item => item.Distance)
            .First()
            .HandleKind;
    }

    private static double Distance(Point start, Point end)
    {
        return Math.Sqrt(DistanceSquared(start, end));
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
