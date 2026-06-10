using System.Globalization;
using Avalonia;
using Avalonia.Media;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class SitePlanPreviewLayerRenderer
{
    private static readonly Color FallbackSitePlanColor = Color.FromArgb(210, 148, 163, 184);
    private static readonly Color SetbackHighlightColor = Color.Parse("#FFFFB000");

    public static void Render(
        DrawingContext context,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        Rect clipBounds,
        IReadOnlyList<SitePlanRenderPathDto>? paths,
        IReadOnlyList<SitePlanTextDto>? texts)
    {
        RenderPaths(context, viewport, clipBounds, paths);
        RenderTexts(context, viewport, texts);
    }

    internal static Pen CreatePen(SitePlanRenderPathDto path)
    {
        var color = ResolveColor(path.ColorArgb, path.IsSetback);
        return new Pen(new SolidColorBrush(color), path.IsSetback ? 1.6d : 1.1d);
    }

    internal static Color ResolveColor(string? colorArgb, bool isSetback)
    {
        return isSetback
            ? SetbackHighlightColor
            : FallbackSitePlanColor;
    }

    private static void RenderPaths(
        DrawingContext context,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        Rect clipBounds,
        IReadOnlyList<SitePlanRenderPathDto>? paths)
    {
        if (paths is not { Count: > 0 })
        {
            return;
        }

        foreach (var path in paths)
        {
            var pen = CreatePen(path);
            foreach (var segment in path.Segments)
            {
                PreviewLineClipper.DrawLine(
                    context,
                    clipBounds,
                    pen,
                    viewport.Project(segment.StartX, segment.StartY),
                    viewport.Project(segment.EndX, segment.EndY));
            }
        }
    }

    private static void RenderTexts(
        DrawingContext context,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        IReadOnlyList<SitePlanTextDto>? texts)
    {
        if (texts is not { Count: > 0 })
        {
            return;
        }

        foreach (var text in texts.Where(item => !string.IsNullOrWhiteSpace(item.Text)))
        {
            RenderText(context, viewport, text);
        }
    }

    private static void RenderText(
        DrawingContext context,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        SitePlanTextDto sitePlanText)
    {
        var anchor = viewport.Project(sitePlanText.X, sitePlanText.Y);
        var fontSize = Math.Max(1d, (double)sitePlanText.Height * viewport.Scale);
        var brush = new SolidColorBrush(ResolveColor(sitePlanText.ColorArgb, sitePlanText.IsSetback));
        var formatted = new FormattedText(
            sitePlanText.Text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Arial"),
            fontSize,
            brush);
        var origin = new Point(anchor.X, anchor.Y - formatted.Baseline);

        using var _ = context.PushTransform(Matrix.CreateRotation((double)sitePlanText.RotationDegrees * Math.PI / 180d, anchor));
        context.DrawText(formatted, origin);
    }
}
