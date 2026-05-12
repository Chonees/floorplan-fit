using Avalonia;
using Avalonia.Media;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class PreviewWorkspaceRenderer
{
    private const double DotSpacing = 24d;
    private const double DotRadius = 1.15d;
    private static readonly IBrush BackgroundBrush = PreviewSemanticPalette.Brush(PreviewSemanticPalette.WorkspaceBackground);
    private static readonly IBrush DotBrush = PreviewSemanticPalette.Brush(PreviewSemanticPalette.WorkspaceDot);
    private static readonly Pen MinorGridPen = new(PreviewSemanticPalette.Brush(PreviewSemanticPalette.MinorGrid), 1d);
    private static readonly Pen MajorGridPen = new(PreviewSemanticPalette.Brush(PreviewSemanticPalette.MajorGrid), 1.05d);

    public static void Render(
        DrawingContext context,
        Rect bounds,
        FloorPlanPreviewGeometry.PreviewViewport? viewport = null)
    {
        context.FillRectangle(BackgroundBrush, bounds);

        if (viewport is not null)
        {
            RenderCadGrid(context, bounds, viewport.Value);
            return;
        }

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

    internal static IReadOnlyList<double> GetWorldLinePositions(double minWorld, double maxWorld, double spacingWorld)
    {
        if (spacingWorld <= double.Epsilon || maxWorld < minWorld)
        {
            return [];
        }

        var positions = new List<double>();
        var first = Math.Ceiling(minWorld / spacingWorld) * spacingWorld;
        for (var value = first; value <= maxWorld + 0.000001d; value += spacingWorld)
        {
            positions.Add(Math.Round(value, 12));
        }

        return positions;
    }

    private static void RenderCadGrid(
        DrawingContext context,
        Rect bounds,
        FloorPlanPreviewGeometry.PreviewViewport viewport)
    {
        var cadViewport = CadViewportContext.Create(viewport);
        if (cadViewport.MinorGridSpacingWorld <= double.Epsilon)
        {
            return;
        }

        var topLeft = viewport.Unproject(bounds.TopLeft);
        var bottomRight = viewport.Unproject(bounds.BottomRight);
        var minX = Math.Min(topLeft.X, bottomRight.X);
        var maxX = Math.Max(topLeft.X, bottomRight.X);
        var minY = Math.Min(topLeft.Y, bottomRight.Y);
        var maxY = Math.Max(topLeft.Y, bottomRight.Y);

        var minorVerticals = GetWorldLinePositions(minX, maxX, cadViewport.MinorGridSpacingWorld);
        var minorHorizontals = GetWorldLinePositions(minY, maxY, cadViewport.MinorGridSpacingWorld);
        var majorVerticals = new HashSet<double>(GetWorldLinePositions(minX, maxX, cadViewport.MajorGridSpacingWorld));
        var majorHorizontals = new HashSet<double>(GetWorldLinePositions(minY, maxY, cadViewport.MajorGridSpacingWorld));

        foreach (var worldX in minorVerticals)
        {
            var screenX = viewport.Project(worldX, minY).X;
            context.DrawLine(
                majorVerticals.Contains(worldX) ? MajorGridPen : MinorGridPen,
                new Point(screenX, bounds.Top),
                new Point(screenX, bounds.Bottom));
        }

        foreach (var worldY in minorHorizontals)
        {
            var screenY = viewport.Project(minX, worldY).Y;
            context.DrawLine(
                majorHorizontals.Contains(worldY) ? MajorGridPen : MinorGridPen,
                new Point(bounds.Left, screenY),
                new Point(bounds.Right, screenY));
        }
    }
}
