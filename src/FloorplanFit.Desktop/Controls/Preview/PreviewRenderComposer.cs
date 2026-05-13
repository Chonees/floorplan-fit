using Avalonia.Media;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class PreviewRenderComposer
{
    internal enum PreviewArtifactLayerMode
    {
        Detected,
        Curated
    }

    public static void Render(DrawingContext context, PreviewRenderScene scene)
    {
        PreviewWorkspaceRenderer.Render(context, scene.Bounds, scene.Viewport);
        context.DrawRectangle(new Pen(Brushes.Gainsboro, 1d), scene.Bounds.Deflate(0.5));

        if (scene.AxisTag is { } axisTag)
        {
            CompressionHandlePreviewLayerRenderer.Render(
                context,
                scene.Bounds,
                axisTag,
                scene.IsPinchPlacementArmed);
        }

        if (!scene.HasGeometry || scene.Viewport is not { } viewport)
        {
            return;
        }

        foreach (var path in ResolveOrderedBasePaths(scene.PreviewGeometry, scene.ArtifactIndex, scene.HighlightGeometryPathId))
        {
            var style = FloorPlanPreviewGeometry.GetPathStyle(path.Id, scene.HighlightGeometryPathId);
            var pen = new Pen(new SolidColorBrush(style.Color), style.Thickness);

            foreach (var segment in path.Segments)
            {
                context.DrawLine(
                    pen,
                    viewport.Project(segment.StartX, segment.StartY),
                    viewport.Project(segment.EndX, segment.EndY));
            }
        }

        if (ResolveArtifactLayerMode(scene) == PreviewArtifactLayerMode.Curated)
        {
            CuratedArtifactPreviewLayerRenderer.Render(
                context,
                viewport,
                scene.PreviewGeometry,
                scene.CuratedPlanArtifacts,
                scene.HighlightGeometryPathId);
        }
        else
        {
            OpeningPreviewLayerRenderer.Render(
                context,
                viewport,
                scene.PreviewGeometry,
                scene.OpeningCandidates,
                scene.ArtifactIndex.OpeningGeometryPathIds,
                scene.HighlightGeometryPathId);
            FixedPlanComponentPreviewLayerRenderer.Render(
                context,
                viewport,
                scene.PreviewGeometry,
                scene.FixedPlanComponents,
                scene.ArtifactIndex.FixedPlanComponentGeometryPathIds,
                scene.HighlightGeometryPathId);
            ProtectedDetailPreviewLayerRenderer.Render(
                context,
                viewport,
                scene.PreviewGeometry,
                scene.ProtectedDetailAssemblies,
                scene.ArtifactIndex.ProtectedDetailGeometryPathIds,
                scene.HighlightGeometryPathId);
        }

        DimensionPreviewLayerRenderer.Render(context, viewport, scene.Dimensions, scene.HighlightDimensionId);
        CadTextPreviewLayerRenderer.RenderRoomLabels(context, viewport, scene.RoomLabels, scene.HighlightRoomLabelId);
        CadTextPreviewLayerRenderer.RenderOpeningLabels(context, viewport, scene.OpeningLabels, scene.HighlightOpeningLabelId);
        CadTextPreviewLayerRenderer.RenderDimensions(context, viewport, scene.Dimensions, scene.HighlightDimensionId);
        PinchMarkerPreviewLayerRenderer.Render(
            context,
            viewport,
            scene.PreviewGeometry,
            scene.PinchMarkers,
            scene.PreviewPinchGroupId,
            scene.PreviewAxisTag);
        DimensionPreviewLayerRenderer.RenderHandles(
            context,
            viewport,
            scene.Dimensions,
            scene.HighlightDimensionId,
            scene.ActiveDimensionHandleKind);
    }

    internal static IReadOnlyList<GeometryPathDto> ResolveOrderedBasePaths(
        IReadOnlyList<GeometryPathDto> previewGeometry,
        PreviewArtifactGeometryIndex artifactIndex,
        Guid? highlightGeometryPathId)
    {
        return previewGeometry
            .Where(path => !artifactIndex.Contains(path.Id))
            .OrderBy(path => FloorPlanPreviewGeometry.GetPathStyle(path.Id, highlightGeometryPathId).IsHighlighted)
            .ToArray();
    }

    internal static PreviewArtifactLayerMode ResolveArtifactLayerMode(PreviewRenderScene scene)
    {
        return scene.HasCuratedArtifacts
            ? PreviewArtifactLayerMode.Curated
            : PreviewArtifactLayerMode.Detected;
    }
}
