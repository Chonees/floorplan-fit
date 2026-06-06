using Avalonia;
using Avalonia.Media;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class MeasurementBindingPreviewLayerRenderer
{
    public static void Render(DrawingContext context, FloorPlanPreviewGeometry.PreviewViewport viewport, PreviewRenderScene scene)
    {
        var overlay = ResolveOverlay(scene);
        RenderCorridorGuide(context, viewport, scene.PreviewGeometry, overlay.ActiveCorridor);
        RenderInterval(context, viewport, overlay.ActiveInterval);
        RenderNodes(context, viewport, overlay.CorridorNodes);
    }

    internal static MeasurementBindingOverlaySnapshot ResolveOverlay(PreviewRenderScene scene)
    {
        var activeBand = scene.PreviewPinchGroupId is Guid pinchGroupId
            ? scene.ArticulationBands?.FirstOrDefault(item => item.PinchGroupId == pinchGroupId)
            : null;

        var corridorLookup = (scene.MeasurementCorridors ?? []).ToDictionary(item => item.CorridorId);
        var nodeLookup = (scene.MeasurementNodes ?? []).ToDictionary(item => item.NodeId);
        var geometryLookup = scene.PreviewGeometry.ToDictionary(item => item.Id);
        var bindingLookup = (scene.DimensionIntervalBindings ?? [])
            .Where(item => string.Equals(item.BindingStatus, "ManualVerified", StringComparison.Ordinal))
            .ToDictionary(item => item.DimensionId);

        MeasurementCorridorDto? activeCorridor = null;
        if (scene.SelectedMeasurementCorridorId is Guid selectedCorridorId)
        {
            corridorLookup.TryGetValue(selectedCorridorId, out activeCorridor);
        }
        else if (scene.HighlightDimensionId is Guid highlightDimensionId &&
                 bindingLookup.TryGetValue(highlightDimensionId, out var highlightedBinding))
        {
            corridorLookup.TryGetValue(highlightedBinding.CorridorId, out activeCorridor);
        }

        if (activeCorridor is null)
        {
            return new MeasurementBindingOverlaySnapshot(activeBand, null, [], null);
        }

        var corridorNodes = (scene.MeasurementNodes ?? [])
            .Where(item => item.CorridorId == activeCorridor.CorridorId)
            .Select(node =>
            {
                var worldPoint = ResolveNodeWorldPoint(node, activeCorridor.AxisTag, geometryLookup);
                return worldPoint is null
                    ? null
                    : new MeasurementNodeOverlay(
                        node.NodeId,
                        worldPoint.Value,
                        node.NodeId == scene.SelectedMeasurementNodeId,
                        node.NodeId == scene.SelectedMeasurementStartNodeId,
                        node.NodeId == scene.SelectedMeasurementEndNodeId);
            })
            .Where(item => item is not null)
            .Select(item => item!)
            .OrderBy(item => item.IsSelected ? 1 : 0)
            .ThenBy(item => item.IsStart ? 1 : 0)
            .ThenBy(item => item.IsEnd ? 1 : 0)
            .ToArray();

        var corridorNodeLookup = corridorNodes.ToDictionary(item => item.NodeId);
        MeasurementIntervalOverlay? activeInterval = null;
        if (scene.SelectedMeasurementStartNodeId is Guid selectedStartNodeId &&
            scene.SelectedMeasurementEndNodeId is Guid selectedEndNodeId &&
            corridorNodeLookup.TryGetValue(selectedStartNodeId, out var selectedStartNode) &&
            corridorNodeLookup.TryGetValue(selectedEndNodeId, out var selectedEndNode))
        {
            activeInterval = new MeasurementIntervalOverlay(
                activeCorridor.CorridorId,
                selectedStartNodeId,
                selectedEndNodeId,
                selectedStartNode.WorldPoint,
                selectedEndNode.WorldPoint);
        }
        else if (scene.HighlightDimensionId is Guid dimensionId &&
                 bindingLookup.TryGetValue(dimensionId, out var binding) &&
                 binding.CorridorId == activeCorridor.CorridorId &&
                 corridorNodeLookup.TryGetValue(binding.StartNodeId, out var startNode) &&
                 corridorNodeLookup.TryGetValue(binding.EndNodeId, out var endNode))
        {
            activeInterval = new MeasurementIntervalOverlay(
                activeCorridor.CorridorId,
                binding.StartNodeId,
                binding.EndNodeId,
                startNode.WorldPoint,
                endNode.WorldPoint);
        }

        return new MeasurementBindingOverlaySnapshot(activeBand, activeCorridor, corridorNodes, activeInterval);
    }

    private static void RenderArticulationBand(
        DrawingContext context,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        IReadOnlyList<GeometryPathDto> previewGeometry,
        ArticulationBandDto? activeBand)
    {
        if (activeBand is null || previewGeometry.Count == 0)
        {
            return;
        }

        var bounds = ResolveGeometryBounds(previewGeometry);
        if (bounds is null)
        {
            return;
        }

        Rect screenRect;
        if (string.Equals(activeBand.AxisTag, nameof(Domain.FloorPlans.PinchAxisTag.Height), StringComparison.OrdinalIgnoreCase))
        {
            var topLeft = viewport.Project(bounds.Value.MinX, activeBand.BandStartCoordinate);
            var bottomRight = viewport.Project(bounds.Value.MaxX, activeBand.BandEndCoordinate);
            screenRect = NormalizeRect(topLeft, bottomRight);
        }
        else
        {
            var topLeft = viewport.Project(activeBand.BandStartCoordinate, bounds.Value.MinY);
            var bottomRight = viewport.Project(activeBand.BandEndCoordinate, bounds.Value.MaxY);
            screenRect = NormalizeRect(topLeft, bottomRight);
        }

        context.DrawRectangle(
            new SolidColorBrush(PreviewSemanticPalette.MeasurementBandFill),
            new Pen(new SolidColorBrush(PreviewSemanticPalette.MeasurementBandStroke), 1.5d),
            screenRect);
    }

    private static void RenderCorridorGuide(
        DrawingContext context,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        IReadOnlyList<GeometryPathDto> previewGeometry,
        MeasurementCorridorDto? activeCorridor)
    {
        if (activeCorridor is null)
        {
            return;
        }

        var path = previewGeometry.FirstOrDefault(item => item.Id == activeCorridor.GuideGeometryPathId);
        if (path is null)
        {
            return;
        }

        var pen = new Pen(new SolidColorBrush(PreviewSemanticPalette.MeasurementCorridorGuide), 2d)
        {
            DashStyle = DashStyle.Dash
        };

        foreach (var segment in path.Segments)
        {
            context.DrawLine(
                pen,
                viewport.Project(segment.StartX, segment.StartY),
                viewport.Project(segment.EndX, segment.EndY));
        }
    }

    private static void RenderInterval(
        DrawingContext context,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        MeasurementIntervalOverlay? activeInterval)
    {
        if (activeInterval is null)
        {
            return;
        }

        context.DrawLine(
            new Pen(new SolidColorBrush(PreviewSemanticPalette.MeasurementInterval), 3d),
            viewport.Project((decimal)activeInterval.StartPoint.X, (decimal)activeInterval.StartPoint.Y),
            viewport.Project((decimal)activeInterval.EndPoint.X, (decimal)activeInterval.EndPoint.Y));
    }

    private static void RenderNodes(
        DrawingContext context,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        IReadOnlyList<MeasurementNodeOverlay> nodes)
    {
        foreach (var node in nodes)
        {
            var projected = viewport.Project((decimal)node.WorldPoint.X, (decimal)node.WorldPoint.Y);
            var radius = node.IsSelected || node.IsStart || node.IsEnd ? 5d : 4d;
            var fill = node.IsSelected || node.IsStart || node.IsEnd
                ? PreviewSemanticPalette.MeasurementNodeSelected
                : PreviewSemanticPalette.MeasurementNode;
            context.DrawEllipse(
                new SolidColorBrush(fill),
                new Pen(Brushes.Black, 1d),
                projected,
                radius,
                radius);
        }
    }

    private static Point? ResolveNodeWorldPoint(
        MeasurementNodeDto node,
        string axisTag,
        IReadOnlyDictionary<Guid, GeometryPathDto> geometryLookup)
    {
        if (!geometryLookup.TryGetValue(node.GeometryPathId, out var path))
        {
            return null;
        }

        var basePoint = FloorPlanPreviewGeometry.GetPointAtRatio(path, node.PositionRatio);
        if (basePoint is null)
        {
            return null;
        }

        var x = basePoint.Value.X;
        var y = basePoint.Value.Y;

        return string.Equals(axisTag, nameof(Domain.FloorPlans.PinchAxisTag.Height), StringComparison.OrdinalIgnoreCase)
            ? new Point(x + (double)node.OffsetNormal, y + (double)node.OffsetAlongAxis)
            : new Point(x + (double)node.OffsetAlongAxis, y + (double)node.OffsetNormal);
    }

    private static (decimal MinX, decimal MaxX, decimal MinY, decimal MaxY)? ResolveGeometryBounds(IReadOnlyList<GeometryPathDto> previewGeometry)
    {
        if (previewGeometry.Count == 0)
        {
            return null;
        }

        var xs = previewGeometry.SelectMany(path => path.Segments.SelectMany(segment => new[] { segment.StartX, segment.EndX })).ToArray();
        var ys = previewGeometry.SelectMany(path => path.Segments.SelectMany(segment => new[] { segment.StartY, segment.EndY })).ToArray();
        return xs.Length == 0 || ys.Length == 0
            ? null
            : (xs.Min(), xs.Max(), ys.Min(), ys.Max());
    }

    private static Rect NormalizeRect(Point a, Point b)
    {
        var left = Math.Min(a.X, b.X);
        var top = Math.Min(a.Y, b.Y);
        var right = Math.Max(a.X, b.X);
        var bottom = Math.Max(a.Y, b.Y);
        return new Rect(left, top, right - left, bottom - top);
    }
}

internal sealed record MeasurementBindingOverlaySnapshot(
    ArticulationBandDto? ActiveArticulationBand,
    MeasurementCorridorDto? ActiveCorridor,
    IReadOnlyList<MeasurementNodeOverlay> CorridorNodes,
    MeasurementIntervalOverlay? ActiveInterval);

internal sealed record MeasurementNodeOverlay(
    Guid NodeId,
    Point WorldPoint,
    bool IsSelected,
    bool IsStart,
    bool IsEnd);

internal sealed record MeasurementIntervalOverlay(
    Guid CorridorId,
    Guid StartNodeId,
    Guid EndNodeId,
    Point StartPoint,
    Point EndPoint);
