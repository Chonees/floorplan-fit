using Avalonia.Media;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class OpeningPreviewLayerRenderer
{
    public static void Render(
        DrawingContext context,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
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
                context.DrawLine(
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
            return new Pen(new SolidColorBrush(Color.FromRgb(255, 72, 24)), 3.2d);
        }

        var color = string.Equals(kind, "Window", StringComparison.OrdinalIgnoreCase)
            ? Color.FromRgb(0, 147, 197)
            : Color.FromRgb(150, 83, 13);

        return new Pen(new SolidColorBrush(color), 1.6d);
    }
}
