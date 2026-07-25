using Avalonia;
using Avalonia.Media;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class PinchMarkerPreviewLayerRenderer
{
    public static void Render(
        DrawingContext context,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        IReadOnlyList<GeometryPathDto> previewGeometry,
        IReadOnlyList<PinchMarkerDto>? pinchMarkers,
        Guid? selectedPinchMarkerId,
        Guid? previewPinchGroupId,
        FloorPlanPreviewGeometry.PreviewCompressionEdge? activeDragEdge)
    {
        if (pinchMarkers is not { Count: > 0 })
        {
            return;
        }

        var lookup = previewGeometry.ToDictionary(item => item.Id);
        foreach (var marker in pinchMarkers)
        {
            if (!lookup.TryGetValue(marker.GeometryPathId, out var path))
            {
                continue;
            }

            var worldPoint = FloorPlanPreviewGeometry.GetPointAtRatio(path, marker.PositionRatio);
            if (worldPoint is null)
            {
                continue;
            }

            var projected = viewport.Project((decimal)worldPoint.Value.X, (decimal)worldPoint.Value.Y);
            var style = ResolveStyle(marker, selectedPinchMarkerId, previewPinchGroupId, activeDragEdge);
            context.DrawEllipse(new SolidColorBrush(style.Fill), null, projected, style.Radius, style.Radius);
            context.DrawEllipse(null, new Pen(Brushes.White, 1), projected, style.Radius, style.Radius);
        }
    }

    internal static IReadOnlyList<PinchMarkerDto>? FilterForPreviewGroup(
        IReadOnlyList<PinchMarkerDto>? pinchMarkers,
        Guid? previewPinchGroupId)
    {
        if (pinchMarkers is not { Count: > 0 } || previewPinchGroupId is null)
        {
            return [];
        }

        return pinchMarkers
            .Where(item => item.PinchGroupId == previewPinchGroupId)
            .ToArray();
    }

    internal static PinchMarkerVisualStyle ResolveStyle(
        PinchMarkerDto marker,
        Guid? selectedPinchMarkerId,
        Guid? previewPinchGroupId = null,
        FloorPlanPreviewGeometry.PreviewCompressionEdge? activeDragEdge = null)
    {
        var isActiveDragMarker = marker.PinchGroupId == previewPinchGroupId &&
                                 (activeDragEdge switch
                                 {
                                     FloorPlanPreviewGeometry.PreviewCompressionEdge.Left or
                                     FloorPlanPreviewGeometry.PreviewCompressionEdge.Right
                                         => string.Equals(marker.AxisTag, "Width", StringComparison.OrdinalIgnoreCase),
                                     FloorPlanPreviewGeometry.PreviewCompressionEdge.Top or
                                     FloorPlanPreviewGeometry.PreviewCompressionEdge.Bottom
                                         => string.Equals(marker.AxisTag, "Height", StringComparison.OrdinalIgnoreCase),
                                     _ => false
                                 });
        var isSelected = selectedPinchMarkerId == marker.PinchMarkerId;
        var isGreen = activeDragEdge is null ? isSelected : isActiveDragMarker;

        return new PinchMarkerVisualStyle(
            isGreen ? PreviewSemanticPalette.ActivePinchGroup : PreviewSemanticPalette.InactivePinch,
            isSelected ? 5d : 4d);
    }

    internal readonly record struct PinchMarkerVisualStyle(Color Fill, double Radius);
}
