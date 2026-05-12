using Avalonia;
using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class NativeDimensionEditor
{
    public static DimensionDto ApplyHandleDelta(
        DimensionDto dimension,
        FloorPlanPreviewControl.DimensionHandleKind handleKind,
        decimal deltaX,
        decimal deltaY)
    {
        var updated = handleKind switch
        {
            FloorPlanPreviewControl.DimensionHandleKind.FirstDefinitionPoint => dimension with
            {
                DefPointX = RoundModelValue(dimension.DefPointX + deltaX),
                DefPointY = RoundModelValue(dimension.DefPointY + deltaY)
            },
            FloorPlanPreviewControl.DimensionHandleKind.SecondDefinitionPoint => dimension with
            {
                DefPoint2X = RoundModelValue(dimension.DefPoint2X + deltaX),
                DefPoint2Y = RoundModelValue(dimension.DefPoint2Y + deltaY)
            },
            FloorPlanPreviewControl.DimensionHandleKind.DimensionLinePoint => dimension with
            {
                DefPoint3X = RoundModelValue(dimension.DefPoint3X + deltaX),
                DefPoint3Y = RoundModelValue(dimension.DefPoint3Y + deltaY),
                RenderTextX = dimension.RenderTextX is null ? null : RoundModelValue(dimension.RenderTextX.Value + deltaX),
                RenderTextY = dimension.RenderTextY is null ? null : RoundModelValue(dimension.RenderTextY.Value + deltaY)
            },
            FloorPlanPreviewControl.DimensionHandleKind.TextAnchor => dimension with
            {
                RenderTextX = RoundModelValue((dimension.RenderTextX ?? dimension.DefPoint3X) + deltaX),
                RenderTextY = RoundModelValue((dimension.RenderTextY ?? dimension.DefPoint3Y) + deltaY)
            },
            _ => dimension
        };

        var recalculateMeasurement =
            handleKind == FloorPlanPreviewControl.DimensionHandleKind.FirstDefinitionPoint ||
            handleKind == FloorPlanPreviewControl.DimensionHandleKind.SecondDefinitionPoint;

        return DimensionGeometryProjector.RebuildEditedDimension(
            updated,
            dimension,
            recalculateMeasurement,
            markEdited: true,
            markDirty: true);
    }

    public static Point ResolveSnappedWorldPoint(
        Point worldPoint,
        CadViewportContext viewportContext,
        IReadOnlyList<Point> anchorPoints,
        bool enableGridSnap)
    {
        var snapTolerance = viewportContext.SnappingToleranceWorld;
        var gridSnapped = enableGridSnap
            ? new Point(
                SnapCoordinate(worldPoint.X, viewportContext.MajorGridSpacingWorld, snapTolerance),
                SnapCoordinate(worldPoint.Y, viewportContext.MajorGridSpacingWorld, snapTolerance))
            : worldPoint;
        if (Distance(gridSnapped, worldPoint) > double.Epsilon)
        {
            return gridSnapped;
        }

        var nearestAnchor = anchorPoints
            .Select(anchor => (Anchor: anchor, Distance: Distance(anchor, worldPoint)))
            .Where(item => item.Distance <= snapTolerance)
            .OrderBy(item => item.Distance)
            .Cast<(Point Anchor, double Distance)?>()
            .FirstOrDefault();
        return nearestAnchor is { } anchorMatch
            ? anchorMatch.Anchor
            : worldPoint;
    }

    public static IReadOnlyList<Point> ResolveSnapAnchors(
        DimensionDto dimension,
        FloorPlanPreviewControl.DimensionHandleKind activeHandleKind)
    {
        return ResolveHandles(dimension)
            .Where(item => item.HandleKind != activeHandleKind)
            .Select(item => item.WorldPoint)
            .ToArray();
    }

    public static IReadOnlyList<(FloorPlanPreviewControl.DimensionHandleKind HandleKind, Point WorldPoint)> ResolveHandles(DimensionDto dimension)
    {
        return
        [
            (FloorPlanPreviewControl.DimensionHandleKind.FirstDefinitionPoint, new Point((double)dimension.DefPointX, (double)dimension.DefPointY)),
            (FloorPlanPreviewControl.DimensionHandleKind.SecondDefinitionPoint, new Point((double)dimension.DefPoint2X, (double)dimension.DefPoint2Y)),
            (FloorPlanPreviewControl.DimensionHandleKind.DimensionLinePoint, new Point((double)dimension.DefPoint3X, (double)dimension.DefPoint3Y)),
            (FloorPlanPreviewControl.DimensionHandleKind.TextAnchor, ResolveHandlePoint(dimension, FloorPlanPreviewControl.DimensionHandleKind.TextAnchor))
        ];
    }

    public static Point ResolveHandlePoint(
        DimensionDto dimension,
        FloorPlanPreviewControl.DimensionHandleKind handleKind)
    {
        return handleKind switch
        {
            FloorPlanPreviewControl.DimensionHandleKind.FirstDefinitionPoint => new Point((double)dimension.DefPointX, (double)dimension.DefPointY),
            FloorPlanPreviewControl.DimensionHandleKind.SecondDefinitionPoint => new Point((double)dimension.DefPoint2X, (double)dimension.DefPoint2Y),
            FloorPlanPreviewControl.DimensionHandleKind.DimensionLinePoint => new Point((double)dimension.DefPoint3X, (double)dimension.DefPoint3Y),
            FloorPlanPreviewControl.DimensionHandleKind.TextAnchor => CadTextPreviewLayerRenderer.ResolveDimensionTextAnchor(dimension),
            _ => new Point((double)dimension.DefPoint3X, (double)dimension.DefPoint3Y)
        };
    }

    private static double SnapCoordinate(double coordinate, double spacing, double tolerance)
    {
        if (spacing <= double.Epsilon)
        {
            return coordinate;
        }

        var snapped = Math.Round(coordinate / spacing) * spacing;
        return Math.Abs(snapped - coordinate) <= tolerance
            ? snapped
            : coordinate;
    }

    private static double Distance(Point start, Point end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private static decimal RoundModelValue(decimal value)
    {
        return decimal.Round(value, 3, MidpointRounding.AwayFromZero);
    }
}
