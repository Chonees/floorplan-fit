using Avalonia;
using Avalonia.Media;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class FixedPlanComponentPreviewLayerRenderer
{
    public static void Render(
        DrawingContext context,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        Rect clipBounds,
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
                    PreviewLineClipper.DrawLine(
                        context,
                        clipBounds,
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
            return new Pen(
                PreviewSemanticPalette.Brush(PreviewSemanticPalette.FixedElementHighlight),
                3.2d);
        }

        return new Pen(
            PreviewSemanticPalette.Brush(ResolveBaseColor(component)),
            1.35d);
    }

    private static Color ResolveBaseColor(FixedPlanComponentDto component)
    {
        return string.Equals(component.Kind, "Cabinet", StringComparison.OrdinalIgnoreCase)
            ? PreviewSemanticPalette.Cabinet
            : PreviewSemanticPalette.FixedElement;
    }
}
