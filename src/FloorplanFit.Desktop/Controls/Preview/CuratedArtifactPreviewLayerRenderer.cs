using Avalonia;
using Avalonia.Media;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class CuratedArtifactPreviewLayerRenderer
{
    public static void Render(
        DrawingContext context,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        Rect clipBounds,
        IReadOnlyList<GeometryPathDto> previewGeometry,
        IReadOnlyList<CuratedPlanArtifactDto>? curatedPlanArtifacts,
        Guid? highlightGeometryPathId)
    {
        if (curatedPlanArtifacts is not { Count: > 0 })
        {
            return;
        }

        var pathLookup = previewGeometry.ToDictionary(item => item.Id);
        foreach (var artifact in curatedPlanArtifacts)
        {
            foreach (var pathId in artifact.GeometryPathIds)
            {
                if (!pathLookup.TryGetValue(pathId, out var path))
                {
                    continue;
                }

                var pen = CreatePen(artifact, pathId == highlightGeometryPathId);
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

    internal static Pen CreatePen(CuratedPlanArtifactDto artifact, bool isHighlighted)
    {
        if (isHighlighted)
        {
            return new Pen(
                PreviewSemanticPalette.Brush(PreviewSemanticPalette.SelectionHighlight),
                3.4d);
        }

        return new Pen(
            PreviewSemanticPalette.Brush(Color.Parse(artifact.ResolvedColorArgb)),
            1.6d);
    }
}
