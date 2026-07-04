using Avalonia;
using Avalonia.Media;
using FloorplanFit.Desktop.Controls;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class ManualWallLinePreviewLayerRenderer
{
    private static readonly Pen DraftLinePen = new(
        new SolidColorBrush(PreviewSemanticPalette.ActivePinchAxis),
        2.5d)
    {
        DashStyle = DashStyle.Dash
    };

    private static readonly IBrush EndpointBrush = new SolidColorBrush(PreviewSemanticPalette.SelectionHighlight);

    public static void Render(
        DrawingContext context,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        Rect bounds,
        ManualWallLineDraft? draft)
    {
        if (draft is null ||
            draft.StartX == draft.CurrentX && draft.StartY == draft.CurrentY)
        {
            return;
        }

        var start = viewport.Project((double)draft.StartX, (double)draft.StartY);
        var end = viewport.Project((double)draft.CurrentX, (double)draft.CurrentY);
        PreviewLineClipper.DrawLine(context, bounds, DraftLinePen, start, end);

        DrawEndpoint(context, bounds, start);
        DrawEndpoint(context, bounds, end);
    }

    private static void DrawEndpoint(DrawingContext context, Rect bounds, Point point)
    {
        if (!bounds.Contains(point))
        {
            return;
        }

        context.DrawEllipse(EndpointBrush, null, point, 4d, 4d);
    }
}
