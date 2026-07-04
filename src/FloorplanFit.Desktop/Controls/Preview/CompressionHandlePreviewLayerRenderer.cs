using Avalonia;
using Avalonia.Media;
using FloorplanFit.Contracts.FloorPlans;
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
        bool isPinchPlacementArmed,
        IReadOnlyList<PinchMarkerDto>? pinchMarkers,
        Guid? previewPinchGroupId)
    {
        foreach (var handle in GetVisibleHandles(bounds, axisTag, isPinchPlacementArmed, pinchMarkers, previewPinchGroupId))
        {
            context.DrawRectangle(HandleBrush, HandlePen, handle.Rect, CornerRadius, CornerRadius);
        }
    }

    internal static IReadOnlyList<FloorPlanPreviewGeometry.CompressionHandle> GetVisibleHandles(
        Rect bounds,
        PinchAxisTag axisTag,
        bool isPinchPlacementArmed,
        IReadOnlyList<PinchMarkerDto>? pinchMarkers,
        Guid? previewPinchGroupId)
    {
        return isPinchPlacementArmed || !HasPreviewDriver(pinchMarkers, previewPinchGroupId, axisTag)
            ? []
            : FloorPlanPreviewGeometry.GetCompressionHandleRects(bounds, axisTag);
    }

    internal static bool HasPreviewDriver(
        IReadOnlyList<PinchMarkerDto>? pinchMarkers,
        Guid? previewPinchGroupId,
        PinchAxisTag axisTag)
    {
        return previewPinchGroupId is Guid groupId &&
               pinchMarkers is { Count: > 0 } &&
               pinchMarkers.Any(item =>
                   item.PinchGroupId == groupId &&
                   string.Equals(item.AxisTag, axisTag.ToString(), StringComparison.OrdinalIgnoreCase));
    }
}
