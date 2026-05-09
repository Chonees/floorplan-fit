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
        Guid? previewPinchGroupId,
        string? previewAxisTag)
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
            var style = ResolveStyle(marker, previewPinchGroupId, previewAxisTag);
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
            return pinchMarkers;
        }

        return pinchMarkers
            .Where(item => item.PinchGroupId == previewPinchGroupId)
            .ToArray();
    }

    internal static PinchMarkerVisualStyle ResolveStyle(
        PinchMarkerDto marker,
        Guid? previewPinchGroupId,
        string? previewAxisTag)
    {
        var isActiveGroup = previewPinchGroupId is not null && marker.PinchGroupId == previewPinchGroupId;
        if (isActiveGroup)
        {
            return new PinchMarkerVisualStyle(Colors.SeaGreen, 5d);
        }

        var isActiveAxis = string.Equals(marker.AxisTag, previewAxisTag, StringComparison.OrdinalIgnoreCase);
        return isActiveAxis
            ? new PinchMarkerVisualStyle(Colors.DodgerBlue, 4d)
            : new PinchMarkerVisualStyle(Colors.SlateGray, 4d);
    }

    internal readonly record struct PinchMarkerVisualStyle(Color Fill, double Radius);
}
