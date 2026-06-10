using Avalonia;
using Avalonia.Media;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class DimensionPreviewLayerRenderer
{
    private const double DefaultTerminalRadius = 4d;
    private const double HandleRadius = 5d;

    public static void Render(
        DrawingContext context,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        Rect clipBounds,
        IReadOnlyList<DimensionDto>? dimensions,
        Guid? highlightedDimensionId = null,
        IReadOnlySet<Guid>? nodeBoundDimensionIds = null,
        IReadOnlySet<Guid>? changedNumberDimensionIds = null)
    {
        if (dimensions is not { Count: > 0 })
        {
            return;
        }

        foreach (var dimension in dimensions)
        {
            var isHighlighted = dimension.DimensionId == highlightedDimensionId;
            var isNodeBound = nodeBoundDimensionIds?.Contains(dimension.DimensionId) == true;
            var hasChangedNumber = changedNumberDimensionIds?.Contains(dimension.DimensionId) == true;
            var pen = CreatePen(isHighlighted, isNodeBound, hasChangedNumber);
            var brush = PreviewSemanticPalette.Brush(ResolveDimensionStrokeColor(isHighlighted, isNodeBound, hasChangedNumber));

            foreach (var segment in CreateProjectedSegments(dimension, viewport))
            {
                PreviewLineClipper.DrawLine(context, clipBounds, pen, segment.Start, segment.End);
            }

            RenderCircles(context, viewport, dimension, pen);
            RenderArcs(context, viewport, dimension, pen);
            RenderSolids(context, viewport, dimension, brush);
            RenderTerminalInserts(context, viewport, dimension, brush);
        }
    }

    public static void RenderHandles(
        DrawingContext context,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        IReadOnlyList<DimensionDto>? dimensions,
        Guid? highlightedDimensionId,
        FloorPlanPreviewControl.DimensionHandleKind? activeHandleKind)
    {
        if (dimensions is not { Count: > 0 } || highlightedDimensionId is null)
        {
            return;
        }

        var dimension = dimensions.FirstOrDefault(item => item.DimensionId == highlightedDimensionId.Value);
        if (dimension is null)
        {
            return;
        }

        foreach (var (kind, worldPoint) in NativeDimensionEditor.ResolveHandles(dimension))
        {
            var projected = viewport.Project(worldPoint.X, worldPoint.Y);
            IBrush fill = kind == activeHandleKind
                ? (IBrush)PreviewSemanticPalette.Brush(PreviewSemanticPalette.SelectionHighlight)
                : Brushes.White;
            context.DrawEllipse(fill, new Pen(PreviewSemanticPalette.Brush(PreviewSemanticPalette.SelectionHighlight), 1.25d), projected, HandleRadius, HandleRadius);
        }
    }

    internal static IReadOnlyList<ProjectedDimensionSegment> CreateProjectedSegments(
        DimensionDto dimension,
        FloorPlanPreviewGeometry.PreviewViewport viewport)
    {
        var lineSource = dimension.LinePrimitives.Count > 0
            ? dimension.LinePrimitives.Select(item => new DimensionLineSegmentDto(item.StartX, item.StartY, item.EndX, item.EndY)).ToArray()
            : dimension.LineSegments;
        if (lineSource.Count == 0)
        {
            return [];
        }

        return lineSource
            .Select(segment => new ProjectedDimensionSegment(
                viewport.Project(segment.StartX, segment.StartY),
                viewport.Project(segment.EndX, segment.EndY)))
            .ToArray();
    }

    private static void RenderTerminalInserts(
        DrawingContext context,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        DimensionDto dimension,
        IBrush brush)
    {
        if (NativeDimensionShape.TryResolve(dimension) is { } layout &&
            layout.StartInsertIndex is { } startInsertIndex &&
            layout.EndInsertIndex is { } endInsertIndex &&
            dimension.InsertPrimitives.Count > Math.Max(startInsertIndex, endInsertIndex))
        {
            DrawTerminalAt(context, viewport, dimension.InsertPrimitives[startInsertIndex], brush);
            if (endInsertIndex != startInsertIndex)
            {
                DrawTerminalAt(context, viewport, dimension.InsertPrimitives[endInsertIndex], brush);
            }
            return;
        }

        foreach (var insert in dimension.InsertPrimitives
                     .DistinctBy(item => (RoundKey(item.X), RoundKey(item.Y)))
                     .Take(2))
        {
            DrawTerminalAt(context, viewport, insert, brush);
        }
    }

    private static void DrawTerminalAt(
        DrawingContext context,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        DimensionInsertPrimitiveDto insert,
        IBrush brush)
    {
        var center = viewport.Project(insert.X, insert.Y);
        var radius = Math.Max(DefaultTerminalRadius, (double)Math.Max(insert.ScaleX, insert.ScaleY) * 0.75d);
        context.DrawEllipse(brush, null, center, radius, radius);
    }

    private static void RenderCircles(DrawingContext context, FloorPlanPreviewGeometry.PreviewViewport viewport, DimensionDto dimension, Pen pen)
    {
        foreach (var circle in dimension.CirclePrimitives)
        {
            var center = viewport.Project(circle.CenterX, circle.CenterY);
            var radius = Math.Max(1d, (double)circle.Radius * viewport.Scale);
            context.DrawEllipse(null, pen, center, radius, radius);
        }
    }

    private static void RenderArcs(DrawingContext context, FloorPlanPreviewGeometry.PreviewViewport viewport, DimensionDto dimension, Pen pen)
    {
        foreach (var arc in dimension.ArcPrimitives)
        {
            var center = viewport.Project(arc.CenterX, arc.CenterY);
            var radius = Math.Max(1d, (double)arc.Radius * viewport.Scale);
            var geometry = new StreamGeometry();
            using var stream = geometry.Open();
            var startRadians = (double)arc.StartAngleDegrees * Math.PI / 180d;
            var endRadians = (double)arc.EndAngleDegrees * Math.PI / 180d;
            var start = new Point(center.X + (Math.Cos(startRadians) * radius), center.Y - (Math.Sin(startRadians) * radius));
            var end = new Point(center.X + (Math.Cos(endRadians) * radius), center.Y - (Math.Sin(endRadians) * radius));
            stream.BeginFigure(start, isFilled: false);
            stream.ArcTo(end, new Size(radius, radius), 0d, Math.Abs(endRadians - startRadians) > Math.PI, SweepDirection.CounterClockwise);
            context.DrawGeometry(null, pen, geometry);
        }
    }

    private static void RenderSolids(
        DrawingContext context,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        DimensionDto dimension,
        IBrush brush)
    {
        foreach (var solid in dimension.SolidPrimitives)
        {
            var geometry = new StreamGeometry();
            using var stream = geometry.Open();
            stream.BeginFigure(viewport.Project(solid.Point1X, solid.Point1Y), isFilled: true);
            stream.LineTo(viewport.Project(solid.Point2X, solid.Point2Y));
            stream.LineTo(viewport.Project(solid.Point3X, solid.Point3Y));
            stream.LineTo(viewport.Project(solid.Point4X, solid.Point4Y));
            stream.EndFigure(isClosed: true);
            context.DrawGeometry(brush, null, geometry);
        }
    }

    private static Pen CreatePen(bool isHighlighted, bool isNodeBound, bool hasChangedNumber)
    {
        return new Pen(
            PreviewSemanticPalette.Brush(ResolveDimensionStrokeColor(isHighlighted, isNodeBound, hasChangedNumber)),
            isHighlighted ? 1.6d : 1.1d);
    }

    internal static Color ResolveDimensionStrokeColor(bool isHighlighted, bool isNodeBound, bool hasChangedNumber = false)
    {
        if (isHighlighted)
        {
            return PreviewSemanticPalette.SelectionHighlight;
        }

        if (hasChangedNumber)
        {
            return PreviewSemanticPalette.DimensionChangedMeasurement;
        }

        return isNodeBound
            ? PreviewSemanticPalette.DimensionNodeBound
            : Colors.Black;
    }

    private static decimal RoundKey(decimal value) => decimal.Round(value, 3, MidpointRounding.AwayFromZero);

    internal readonly record struct ProjectedDimensionSegment(Point Start, Point End);
}
