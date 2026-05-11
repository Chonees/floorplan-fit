using System.Globalization;
using Avalonia;
using Avalonia.Media;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class CadTextPreviewLayerRenderer
{
    private const double FallbackFontSize = 11d;

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

    internal static Point ProjectRoomLabel(RoomLabelDto roomLabel, FloorPlanPreviewGeometry.PreviewViewport viewport)
    {
        return viewport.Project(roomLabel.X, roomLabel.Y);
    }

    internal static TextRenderPlan CreateRoomLabelRenderPlan(
        RoomLabelDto roomLabel,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        bool isSelected = false)
    {
        var fontSize = roomLabel.TextHeight is > 0m
            ? Math.Max(1d, (double)roomLabel.TextHeight.Value * viewport.Scale)
            : FallbackFontSize;

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

    internal static TextRenderPlan CreateOpeningLabelRenderPlan(
        OpeningLabelDto openingLabel,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        bool isSelected = false)
    {
        var fontSize = openingLabel.TextHeight is > 0m
            ? Math.Max(1d, (double)openingLabel.TextHeight.Value * viewport.Scale)
            : FallbackFontSize;

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
        var textBrush = CreateBrush(plan.ColorArgb);
        var text = new FormattedText(
            plan.Text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            CreateTypeface(textStyleName),
            plan.FontSize,
            textBrush);
        var origin = ResolveTextOrigin(text, plan);

        using var _ = context.PushTransform(Matrix.CreateRotation(plan.RotationDegrees * Math.PI / 180d, plan.Anchor));
        context.DrawText(text, origin);
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

    internal readonly record struct TextRenderPlan(
        string Text,
        Point Anchor,
        double FontSize,
        double RotationDegrees,
        string? HorizontalAlignment,
        string? VerticalAlignment,
        string? AttachmentPoint,
        string? ColorArgb);
}
