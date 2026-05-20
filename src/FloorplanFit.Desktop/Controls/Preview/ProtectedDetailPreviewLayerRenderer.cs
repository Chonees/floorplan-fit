using Avalonia.Media;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class ProtectedDetailPreviewLayerRenderer
{
    public static void Render(
        DrawingContext context,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        IReadOnlyList<GeometryPathDto> previewGeometry,
        IReadOnlyList<ProtectedDetailAssemblyDto>? protectedDetailAssemblies,
        IReadOnlySet<Guid> protectedDetailGeometryPathIds,
        Guid? highlightGeometryPathId)
    {
        if (protectedDetailAssemblies is not { Count: > 0 } || protectedDetailGeometryPathIds.Count == 0)
        {
            return;
        }

        var pathLookup = previewGeometry.ToDictionary(item => item.Id);
        foreach (var assembly in protectedDetailAssemblies)
        {
            foreach (var pathId in assembly.GeometryPathIds)
            {
                if (!pathLookup.TryGetValue(pathId, out var path))
                {
                    continue;
                }

                var pen = CreatePen(assembly, pathId == highlightGeometryPathId);
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

    internal static Pen CreatePen(ProtectedDetailAssemblyDto assembly, bool isHighlighted)
    {
        if (isHighlighted)
        {
            return new Pen(
                PreviewSemanticPalette.Brush(PreviewSemanticPalette.ProtectedDetailHighlight),
                3.4d);
        }

        if (Color.TryParse(assembly.ColorArgb, out var originalColor))
        {
            return new Pen(PreviewSemanticPalette.Brush(originalColor), 1.6d);
        }

        return new Pen(
            PreviewSemanticPalette.Brush(PreviewSemanticPalette.FixedElement),
            1.6d);
    }
}
