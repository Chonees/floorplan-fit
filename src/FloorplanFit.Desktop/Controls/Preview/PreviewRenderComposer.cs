using Avalonia;
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
                scene.IsPinchPlacementArmed,
                scene.PinchMarkers,
                scene.PreviewPinchGroupId);
        }

        if (scene.Viewport is not { } viewport)
        {
            return;
        }

        using var previewContentClip = context.PushClip(scene.Bounds);

        SitePlanPreviewLayerRenderer.Render(
            context,
            viewport,
            scene.Bounds,
            scene.SitePlanRenderPaths,
            scene.SitePlanTexts);

        if (!scene.HasGeometry)
        {
            return;
        }

        foreach (var path in ResolveOrderedBasePaths(scene.PreviewGeometry, scene.ArtifactIndex, scene.HighlightGeometryPathId))
        {
            var style = FloorPlanPreviewGeometry.GetPathStyle(path.Id, scene.HighlightGeometryPathId);
            var pen = new Pen(new SolidColorBrush(style.Color), style.Thickness);

            foreach (var segment in path.Segments)
            {
                PreviewLineClipper.DrawLine(
                    context,
                    scene.Bounds,
                    pen,
                    viewport.Project(segment.StartX, segment.StartY),
                    viewport.Project(segment.EndX, segment.EndY));
            }
        }

        RenderChangePreviewGhost(context, viewport, scene.Bounds, scene.ChangePreviewGhostGeometry, scene.ChangePreviewGhostOpacity);
        ManualWallLinePreviewLayerRenderer.Render(context, viewport, scene.Bounds, scene.ManualWallLineDraft);

        if (ResolveArtifactLayerMode(scene) == PreviewArtifactLayerMode.Curated)
        {
            CuratedArtifactPreviewLayerRenderer.Render(
                context,
                viewport,
                scene.Bounds,
                scene.PreviewGeometry,
                scene.CuratedPlanArtifacts,
                scene.HighlightGeometryPathId);
        }
        else
        {
            OpeningPreviewLayerRenderer.Render(
                context,
                viewport,
                scene.Bounds,
                scene.PreviewGeometry,
                scene.OpeningCandidates,
                scene.ArtifactIndex.OpeningGeometryPathIds,
                scene.HighlightGeometryPathId);
            FixedPlanComponentPreviewLayerRenderer.Render(
                context,
                viewport,
                scene.Bounds,
                scene.PreviewGeometry,
                scene.FixedPlanComponents,
                scene.ArtifactIndex.FixedPlanComponentGeometryPathIds,
                scene.HighlightGeometryPathId);
            ProtectedDetailPreviewLayerRenderer.Render(
                context,
                viewport,
                scene.Bounds,
                scene.PreviewGeometry,
                scene.ProtectedDetailAssemblies,
                scene.ArtifactIndex.ProtectedDetailGeometryPathIds,
                scene.HighlightGeometryPathId);
        }

        MeasurementBindingPreviewLayerRenderer.Render(context, viewport, scene.Bounds, scene);
        var nodeBoundDimensionIds = ResolveNodeBoundDimensionIds(scene.DimensionIntervalBindings);
        var changedNumberDimensionIds = ResolveChangedNumberDimensionIds(scene.ChangedNumberDimensionIds);
        if (ShouldRenderDimensions(scene))
        {
            DimensionPreviewLayerRenderer.Render(
                context,
                viewport,
                scene.Bounds,
                scene.Dimensions,
                scene.HighlightDimensionId,
                nodeBoundDimensionIds,
                changedNumberDimensionIds);
        }
        CadTextPreviewLayerRenderer.RenderRoomLabels(context, viewport, scene.RoomLabels, scene.HighlightRoomLabelId);
        CadTextPreviewLayerRenderer.RenderOpeningLabels(context, viewport, scene.OpeningLabels, scene.HighlightOpeningLabelId);
        if (ShouldRenderDimensions(scene))
        {
            CadTextPreviewLayerRenderer.RenderDimensions(
                context,
                viewport,
                scene.Dimensions,
                scene.HighlightDimensionId,
                nodeBoundDimensionIds,
                changedNumberDimensionIds);
        }
        PinchMarkerPreviewLayerRenderer.Render(
            context,
            viewport,
            scene.PreviewGeometry,
            scene.PinchMarkers,
            scene.SelectedPinchMarkerId);
        if (ShouldRenderDimensions(scene))
        {
            DimensionPreviewLayerRenderer.RenderHandles(
                context,
                viewport,
                scene.Dimensions,
                scene.HighlightDimensionId,
                scene.ActiveDimensionHandleKind);
        }
    }

    internal static bool ShouldRenderDimensions(PreviewRenderScene scene)
        => scene.AreDimensionsVisible && scene.Dimensions.Count > 0;

    internal static void RenderChangePreviewGhost(
        DrawingContext context,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        Rect clipBounds,
        IReadOnlyList<GeometryPathDto>? ghostGeometry,
        double opacity)
    {
        if (ghostGeometry is not { Count: > 0 } || opacity <= 0d)
        {
            return;
        }

        var alpha = (byte)Math.Clamp(opacity * 180d, 0d, 180d);
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(alpha, 255, 176, 0)), 3d);
        foreach (var path in ghostGeometry)
        {
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

    internal static IReadOnlySet<Guid> ResolveNodeBoundDimensionIds(
        IReadOnlyList<DimensionIntervalBindingDto>? dimensionIntervalBindings)
    {
        if (dimensionIntervalBindings is not { Count: > 0 })
        {
            return new HashSet<Guid>();
        }

        return dimensionIntervalBindings
            .Select(binding => binding.DimensionId)
            .ToHashSet();
    }

    internal static IReadOnlySet<Guid> ResolveChangedNumberDimensionIds(
        IReadOnlyList<Guid>? changedNumberDimensionIds)
        => changedNumberDimensionIds is { Count: > 0 }
            ? changedNumberDimensionIds.ToHashSet()
            : new HashSet<Guid>();

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
