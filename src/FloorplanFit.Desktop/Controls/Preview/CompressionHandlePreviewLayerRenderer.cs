using Avalonia;
using Avalonia.Media;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class CompressionHandlePreviewLayerRenderer
{
    private const double CornerRadius = 6d;
    private static readonly IBrush HandleBrush = PreviewSemanticPalette.Brush(PreviewSemanticPalette.HandleFill);
    private static readonly Pen HandlePen = new(PreviewSemanticPalette.Brush(PreviewSemanticPalette.HandleStroke), 1.5);

    public static void Render(
        DrawingContext context,
        Rect bounds,
        PinchAxisTag axisTag,
        bool isPinchPlacementArmed)
    {
        foreach (var handle in GetVisibleHandles(bounds, axisTag, isPinchPlacementArmed))
        {
            context.DrawRectangle(HandleBrush, HandlePen, handle.Rect, CornerRadius, CornerRadius);
        }
    }

    internal static IReadOnlyList<FloorPlanPreviewGeometry.CompressionHandle> GetVisibleHandles(
        Rect bounds,
        PinchAxisTag axisTag,
        bool isPinchPlacementArmed)
    {
        return isPinchPlacementArmed
            ? []
            : FloorPlanPreviewGeometry.GetCompressionHandleRects(bounds, axisTag);
    }
}
