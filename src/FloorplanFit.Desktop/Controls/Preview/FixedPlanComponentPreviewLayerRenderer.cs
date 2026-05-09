using Avalonia.Media;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class FixedPlanComponentPreviewLayerRenderer
{
    public static void Render(
        DrawingContext context,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        IReadOnlyList<GeometryPathDto> previewGeometry,
        IReadOnlyList<FixedPlanComponentDto>? fixedPlanComponents,
        IReadOnlySet<Guid> fixedPlanComponentGeometryPathIds,
        Guid? highlightGeometryPathId)
    {
        if (fixedPlanComponents is not { Count: > 0 } || fixedPlanComponentGeometryPathIds.Count == 0)
        {
            return;
        }

        var pathLookup = previewGeometry.ToDictionary(item => item.Id);
        foreach (var component in fixedPlanComponents)
        {
            foreach (var pathId in component.GeometryPathIds)
            {
                if (!pathLookup.TryGetValue(pathId, out var path))
                {
                    continue;
                }

                var pen = CreatePen(component, pathId == highlightGeometryPathId);
                foreach (var segment in path.Segments)
                {
                    context.DrawLine(
                        pen,
                        viewport.Project(segment.StartX, segment.StartY),
                        viewport.Project(segment.EndX, segment.EndY));
                }
            }
        }
    }

    internal static Pen CreatePen(FixedPlanComponentDto component, bool isHighlighted)
    {
        if (isHighlighted)
        {
            return new Pen(new SolidColorBrush(Color.FromRgb(255, 72, 24)), 3.2d);
        }

        if (Color.TryParse(component.ColorArgb, out var originalColor))
        {
            return new Pen(new SolidColorBrush(originalColor), 1.35d);
        }

        var color = component.Kind switch
        {
            "Toilet" => Color.FromRgb(111, 66, 193),
            "Appliance" => Color.FromRgb(25, 135, 84),
            "Cabinet" => Color.FromRgb(90, 98, 104),
            "Fixture" => Color.FromRgb(13, 110, 253),
            _ => Color.FromRgb(102, 16, 242)
        };

        return new Pen(new SolidColorBrush(color), 1.35d);
    }
}
