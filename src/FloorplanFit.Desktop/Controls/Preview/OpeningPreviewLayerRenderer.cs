using Avalonia;
using Avalonia.Media;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class OpeningPreviewLayerRenderer
{
    public static void Render(
        DrawingContext context,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        Rect clipBounds,
        IReadOnlyList<GeometryPathDto> previewGeometry,
        IReadOnlyList<OpeningCandidateDto>? openingCandidates,
        IReadOnlySet<Guid> openingGeometryPathIds,
        Guid? highlightGeometryPathId)
    {
        if (openingCandidates is not { Count: > 0 } || openingGeometryPathIds.Count == 0)
        {
            return;
        }

        var pathLookup = previewGeometry.ToDictionary(item => item.Id);
        foreach (var opening in openingCandidates)
        {
            if (opening.GeometryPathId is not { } pathId || !pathLookup.TryGetValue(pathId, out var path))
            {
                continue;
            }

            var pen = CreatePen(opening.Kind, pathId == highlightGeometryPathId);
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

    internal static Pen CreatePen(string kind, bool isHighlighted)
    {
        if (isHighlighted)
        {
            return new Pen(
                PreviewSemanticPalette.Brush(GetHighlightColor(kind)),
                3.2d);
        }

        return new Pen(
            PreviewSemanticPalette.Brush(GetBaseColor(kind)),
            1.6d);
    }

    private static Color GetBaseColor(string kind)
    {
        return string.Equals(kind, "Window", StringComparison.OrdinalIgnoreCase)
            ? PreviewSemanticPalette.Window
            : PreviewSemanticPalette.Door;
    }

    private static Color GetHighlightColor(string kind)
    {
        return string.Equals(kind, "Window", StringComparison.OrdinalIgnoreCase)
            ? PreviewSemanticPalette.WindowHighlight
            : PreviewSemanticPalette.DoorHighlight;
    }
}
