using Avalonia;
using Avalonia.Media;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class PreviewWorkspaceRenderer
{
    private const double DotSpacing = 24d;
    private const double DotRadius = 1.15d;
    private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.FromRgb(250, 251, 252));
    private static readonly IBrush DotBrush = new SolidColorBrush(Color.FromArgb(92, 148, 163, 184));

    public static void Render(DrawingContext context, Rect bounds)
    {
        context.FillRectangle(BackgroundBrush, bounds);

        foreach (var dot in GetDotCenters(bounds, DotSpacing))
        {
            context.DrawEllipse(DotBrush, null, dot, DotRadius, DotRadius);
        }
    }

    internal static IReadOnlyList<Point> GetDotCenters(Rect bounds, double spacing)
    {
        if (bounds.Width <= double.Epsilon || bounds.Height <= double.Epsilon || spacing <= double.Epsilon)
        {
            return [];
        }

        var points = new List<Point>();
        var startX = bounds.Left + (spacing / 2d);
        var startY = bounds.Top + (spacing / 2d);

        for (var y = startY; y < bounds.Bottom; y += spacing)
        {
            for (var x = startX; x < bounds.Right; x += spacing)
            {
                points.Add(new Point(x, y));
            }
        }

        return points;
    }
}
