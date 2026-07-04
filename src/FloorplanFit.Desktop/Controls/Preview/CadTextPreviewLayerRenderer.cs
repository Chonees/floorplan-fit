using System.Globalization;
using Avalonia;
using Avalonia.Media;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class CadTextPreviewLayerRenderer
{
    private const double FallbackFontSize = 11d;
    internal const double MaxPreviewFontSize = 512d;
    private const double MaxRenderableCoordinate = 1_000_000d;

    public static void RenderRoomLabels(
        DrawingContext context,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        IReadOnlyList<RoomLabelDto>? roomLabels,
        Guid? highlightedRoomLabelId = null)
    {
        if (roomLabels is not { Count: > 0 })
        {
            return;
        }

        foreach (var roomLabel in roomLabels)
        {
            if (string.IsNullOrWhiteSpace(roomLabel.Text))
            {
                continue;
            }

            var plan = CreateRoomLabelRenderPlan(roomLabel, viewport, roomLabel.RoomLabelId == highlightedRoomLabelId);
            RenderText(context, plan, roomLabel.TextStyleName);
        }
    }

    public static void RenderOpeningLabels(
        DrawingContext context,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        IReadOnlyList<OpeningLabelDto>? openingLabels,
        Guid? highlightedOpeningLabelId = null)
    {
        if (openingLabels is not { Count: > 0 })
        {
            return;
        }

        foreach (var openingLabel in openingLabels)
        {
            if (string.IsNullOrWhiteSpace(openingLabel.Text))
            {
                continue;
            }

            var plan = CreateOpeningLabelRenderPlan(openingLabel, viewport, openingLabel.OpeningLabelId == highlightedOpeningLabelId);
            RenderText(context, plan, openingLabel.TextStyleName);
        }
    }

    public static void RenderDimensions(
        DrawingContext context,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
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
            if (string.IsNullOrWhiteSpace(dimension.DisplayText))
            {
                continue;
            }

            var plan = CreateDimensionRenderPlan(
                dimension,
                viewport,
                dimension.DimensionId == highlightedDimensionId,
                nodeBoundDimensionIds?.Contains(dimension.DimensionId) == true,
                changedNumberDimensionIds?.Contains(dimension.DimensionId) == true);
            RenderText(context, plan, dimension.RenderTextStyleName);
        }
    }

    internal static Point ProjectRoomLabel(RoomLabelDto roomLabel, FloorPlanPreviewGeometry.PreviewViewport viewport)
    {
        return viewport.Project(roomLabel.X, roomLabel.Y);
    }

    internal static Rect GetRoomLabelBounds(RoomLabelDto roomLabel, FloorPlanPreviewGeometry.PreviewViewport viewport)
    {
        var plan = CreateRoomLabelRenderPlan(roomLabel, viewport);
        return GetTextBounds(plan, roomLabel.TextStyleName);
    }

    internal static TextRenderPlan CreateRoomLabelRenderPlan(
        RoomLabelDto roomLabel,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        bool isSelected = false)
    {
        var fontSize = ResolvePreviewFontSize(roomLabel.TextHeight, viewport.Scale);

        return new TextRenderPlan(
            roomLabel.Text,
            ProjectRoomLabel(roomLabel, viewport),
            fontSize,
            (double)roomLabel.RotationDegrees,
            roomLabel.HorizontalAlignment,
            roomLabel.VerticalAlignment,
            roomLabel.AttachmentPoint,
            isSelected
                ? PreviewSemanticPalette.SelectionHighlightArgb
                : PreviewSemanticPalette.ReadablePreviewLabelColorArgb);
    }

    internal static Rect GetOpeningLabelBounds(OpeningLabelDto openingLabel, FloorPlanPreviewGeometry.PreviewViewport viewport)
    {
        var plan = CreateOpeningLabelRenderPlan(openingLabel, viewport);
        return GetTextBounds(plan, openingLabel.TextStyleName);
    }

    internal static TextRenderPlan CreateOpeningLabelRenderPlan(
        OpeningLabelDto openingLabel,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        bool isSelected = false)
    {
        var fontSize = ResolvePreviewFontSize(openingLabel.TextHeight, viewport.Scale);

        return new TextRenderPlan(
            openingLabel.Text,
            viewport.Project(openingLabel.X, openingLabel.Y),
            fontSize,
            (double)openingLabel.RotationDegrees,
            openingLabel.HorizontalAlignment,
            openingLabel.VerticalAlignment,
            openingLabel.AttachmentPoint,
            isSelected
                ? PreviewSemanticPalette.SelectionHighlightArgb
                : PreviewSemanticPalette.ReadablePreviewLabelColorArgb);
    }

    internal static Rect GetDimensionBounds(DimensionDto dimension, FloorPlanPreviewGeometry.PreviewViewport viewport)
    {
        var plan = CreateDimensionRenderPlan(dimension, viewport);
        return GetTextBounds(plan, dimension.RenderTextStyleName);
    }

    internal static TextRenderPlan CreateDimensionRenderPlan(
        DimensionDto dimension,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        bool isSelected = false,
        bool isNodeBound = false,
        bool hasChangedNumber = false)
    {
        var anchor = ResolveDimensionTextAnchor(dimension);
        var fontSize = ResolvePreviewFontSize(dimension.RenderTextHeight, viewport.Scale);
        return new TextRenderPlan(
            dimension.DisplayText,
            viewport.Project((decimal)anchor.X, (decimal)anchor.Y),
            fontSize,
            ResolveDimensionRotationDegrees(dimension),
            HorizontalAlignment: dimension.RenderTextHorizontalAlignment ?? "Center",
            VerticalAlignment: dimension.RenderTextVerticalAlignment ?? "Middle",
            AttachmentPoint: dimension.RenderTextAttachmentPoint,
            ResolveDimensionTextColorArgb(isSelected, isNodeBound, hasChangedNumber));
    }

    internal static string ResolveDimensionTextColorArgb(bool isSelected, bool isNodeBound, bool hasChangedNumber = false)
    {
        if (isSelected)
        {
            return PreviewSemanticPalette.SelectionHighlightArgb;
        }

        if (hasChangedNumber)
        {
            return PreviewSemanticPalette.DimensionChangedMeasurementArgb;
        }

        return isNodeBound
            ? PreviewSemanticPalette.DimensionNodeBoundArgb
            : PreviewSemanticPalette.ReadablePreviewLabelColorArgb;
    }

    internal static Point ResolveTextOriginForMetrics(
        TextRenderPlan plan,
        double textWidth,
        double textHeight,
        double textBaseline)
    {
        var horizontalOffset = ResolveHorizontalTextOffset(plan, textWidth);
        var verticalOffset = ResolveVerticalTextOffset(plan, textHeight, textBaseline);

        return new Point(plan.Anchor.X + horizontalOffset, plan.Anchor.Y + verticalOffset);
    }

    private static void RenderText(DrawingContext context, TextRenderPlan plan, string? textStyleName)
    {
        if (!CanRenderText(plan.Anchor, plan.FontSize))
        {
            return;
        }

        var textBrush = CreateBrush(plan.ColorArgb);
        var text = CreateFormattedText(plan, textStyleName, textBrush);
        var origin = ResolveTextOrigin(text, plan);

        using var _ = context.PushTransform(Matrix.CreateRotation(plan.RotationDegrees * Math.PI / 180d, plan.Anchor));
        context.DrawText(text, origin);
    }

    internal static double ResolvePreviewFontSize(decimal? textHeight, double viewportScale)
    {
        if (textHeight is not > 0m || !double.IsFinite(viewportScale) || viewportScale <= double.Epsilon)
        {
            return FallbackFontSize;
        }

        var requested = (double)textHeight.Value * viewportScale;
        if (!double.IsFinite(requested))
        {
            return MaxPreviewFontSize;
        }

        return Math.Clamp(requested, 1d, MaxPreviewFontSize);
    }

    internal static bool CanRenderText(Point anchor, double fontSize)
    {
        return double.IsFinite(anchor.X) &&
               double.IsFinite(anchor.Y) &&
               Math.Abs(anchor.X) <= MaxRenderableCoordinate &&
               Math.Abs(anchor.Y) <= MaxRenderableCoordinate &&
               double.IsFinite(fontSize) &&
               fontSize is > 0d and <= MaxPreviewFontSize;
    }

    internal static Rect GetTextBounds(TextRenderPlan plan, string? textStyleName)
    {
        if (string.IsNullOrWhiteSpace(plan.Text))
        {
            return new Rect(plan.Anchor, new Size(0d, 0d));
        }

        var metrics = TryMeasureText(plan, textStyleName);
        var origin = ResolveTextOriginForMetrics(plan, metrics.Width, metrics.Height, metrics.Baseline);
        var rotatedBounds = RotateBounds(new Rect(origin, new Size(metrics.Width, metrics.Height)), plan.Anchor, plan.RotationDegrees);
        return EnsureContains(rotatedBounds, plan.Anchor);
    }

    private static FormattedText CreateFormattedText(TextRenderPlan plan, string? textStyleName, IBrush textBrush)
    {
        return new FormattedText(
            plan.Text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            CreateTypeface(textStyleName),
            plan.FontSize,
            textBrush);
    }

    private static TextMetrics TryMeasureText(TextRenderPlan plan, string? textStyleName)
    {
        try
        {
            var text = CreateFormattedText(plan, textStyleName, Brushes.Black);
            return new TextMetrics(text.Width, text.Height, text.Baseline);
        }
        catch (InvalidOperationException)
        {
            var width = Math.Max(plan.FontSize * 0.62d * Math.Max(plan.Text.Length, 1), plan.FontSize);
            var height = Math.Max(plan.FontSize * 1.2d, 1d);
            var baseline = Math.Max(plan.FontSize * 0.9d, height * 0.7d);
            return new TextMetrics(width, height, baseline);
        }
    }

    private static Typeface CreateTypeface(string? textStyleName)
    {
        var family = string.IsNullOrWhiteSpace(textStyleName) || string.Equals(textStyleName, "STANDARD", StringComparison.OrdinalIgnoreCase)
            ? "Arial"
            : textStyleName;

        return new Typeface(family, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal);
    }

    private static IBrush CreateBrush(string? colorArgb)
    {
        return Color.TryParse(colorArgb, out var color)
            ? new SolidColorBrush(color)
            : Brushes.Black;
    }

    private static Point ResolveTextOrigin(FormattedText text, TextRenderPlan plan)
    {
        return ResolveTextOriginForMetrics(plan, text.Width, text.Height, text.Baseline);
    }

    private static Rect RotateBounds(Rect bounds, Point anchor, double rotationDegrees)
    {
        if (Math.Abs(rotationDegrees) <= double.Epsilon)
        {
            return bounds;
        }

        var rotation = Matrix.CreateRotation(rotationDegrees * Math.PI / 180d, anchor);
        var points =
            new[]
            {
                bounds.TopLeft,
                bounds.TopRight,
                bounds.BottomLeft,
                bounds.BottomRight
            }
            .Select(rotation.Transform)
            .ToArray();

        var minX = points.Min(point => point.X);
        var minY = points.Min(point => point.Y);
        var maxX = points.Max(point => point.X);
        var maxY = points.Max(point => point.Y);
        return new Rect(new Point(minX, minY), new Point(maxX, maxY));
    }

    private static Rect EnsureContains(Rect bounds, Point point)
    {
        if (bounds.Contains(point))
        {
            return bounds;
        }

        var minX = Math.Min(bounds.Left, point.X);
        var minY = Math.Min(bounds.Top, point.Y);
        var maxX = Math.Max(bounds.Right, point.X);
        var maxY = Math.Max(bounds.Bottom, point.Y);
        return new Rect(new Point(minX, minY), new Point(maxX, maxY));
    }

    private static double ResolveHorizontalTextOffset(TextRenderPlan plan, double textWidth)
    {
        if (ContainsToken(plan.AttachmentPoint, "Center") ||
            ContainsToken(plan.HorizontalAlignment, "Center") ||
            ContainsToken(plan.HorizontalAlignment, "Middle"))
        {
            return -textWidth / 2d;
        }

        if (ContainsToken(plan.AttachmentPoint, "Right") ||
            ContainsToken(plan.HorizontalAlignment, "Right"))
        {
            return -textWidth;
        }

        return 0d;
    }

    private static double ResolveVerticalTextOffset(TextRenderPlan plan, double textHeight, double textBaseline)
    {
        if (ContainsToken(plan.AttachmentPoint, "Middle") ||
            ContainsToken(plan.VerticalAlignment, "Middle"))
        {
            return -textHeight / 2d;
        }

        if (ContainsToken(plan.AttachmentPoint, "Top") ||
            ContainsToken(plan.VerticalAlignment, "Top"))
        {
            return 0d;
        }

        if (ContainsToken(plan.AttachmentPoint, "Bottom") ||
            ContainsToken(plan.VerticalAlignment, "Bottom"))
        {
            return -textHeight;
        }

        return -textBaseline;
    }

    private static bool ContainsToken(string? value, string token)
    {
        return value?.Contains(token, StringComparison.OrdinalIgnoreCase) == true;
    }

    internal static Point ResolveDimensionTextAnchor(DimensionDto dimension)
    {
        if (dimension.RenderTextX is not null && dimension.RenderTextY is not null)
        {
            return new Point((double)dimension.RenderTextX.Value, (double)dimension.RenderTextY.Value);
        }

        var p1 = new Point((double)dimension.DefPointX, (double)dimension.DefPointY);
        var p2 = new Point((double)dimension.DefPoint2X, (double)dimension.DefPoint2Y);
        var p3 = new Point((double)dimension.DefPoint3X, (double)dimension.DefPoint3Y);
        var axis = ResolveDimensionAxis(dimension, p1, p2);
        var normal = new Vector(-axis.Y, axis.X);
        var midpoint = new Point((p1.X + p2.X) / 2d, (p1.Y + p2.Y) / 2d);
        var offsetAlongNormal = ((p3.X - p1.X) * normal.X) + ((p3.Y - p1.Y) * normal.Y);
        return new Point(
            midpoint.X + (normal.X * offsetAlongNormal),
            midpoint.Y + (normal.Y * offsetAlongNormal));
    }

    private static Vector ResolveDimensionAxis(DimensionDto dimension, Point p1, Point p2)
    {
        var baseType = dimension.DimType & 0x7;
        if (baseType == 1)
        {
            var alignedVector = new Vector(p2.X - p1.X, p2.Y - p1.Y);
            if (alignedVector.Length > double.Epsilon)
            {
                return alignedVector / alignedVector.Length;
            }
        }

        var angleRadians = (double)dimension.Angle * Math.PI / 180d;
        var axis = new Vector(Math.Cos(angleRadians), Math.Sin(angleRadians));
        if (axis.Length > double.Epsilon)
        {
            return axis / axis.Length;
        }

        var fallback = new Vector(p2.X - p1.X, p2.Y - p1.Y);
        return fallback.Length > double.Epsilon
            ? fallback / fallback.Length
            : new Vector(1d, 0d);
    }

    internal static double ResolveDimensionRotationDegrees(DimensionDto dimension)
    {
        if (dimension.RenderTextRotationDegrees is not null)
        {
            return (double)dimension.RenderTextRotationDegrees.Value;
        }

        var baseType = dimension.DimType & 0x7;
        if (baseType == 1)
        {
            var dx = (double)(dimension.DefPoint2X - dimension.DefPointX);
            var dy = (double)(dimension.DefPoint2Y - dimension.DefPointY);
            if (Math.Abs(dx) > double.Epsilon || Math.Abs(dy) > double.Epsilon)
            {
                return Math.Atan2(dy, dx) * 180d / Math.PI;
            }
        }

        return (double)dimension.Angle;
    }

    private static double Dot(Point point, Vector vector)
    {
        return (point.X * vector.X) + (point.Y * vector.Y);
    }

    internal readonly record struct TextRenderPlan(
        string Text,
        Point Anchor,
        double FontSize,
        double RotationDegrees,
        string? HorizontalAlignment,
        string? VerticalAlignment,
        string? AttachmentPoint,
        string? ColorArgb);

    private readonly record struct TextMetrics(double Width, double Height, double Baseline);
}
