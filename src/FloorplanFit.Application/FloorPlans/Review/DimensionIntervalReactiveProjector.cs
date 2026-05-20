using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Review;

public static class DimensionIntervalReactiveProjector
{
    public static IReadOnlyList<DimensionDto> Project(
        IReadOnlyList<DimensionDto> dimensions,
        IReadOnlyList<GeometryPathDto> previewGeometry,
        IReadOnlyList<MeasurementCorridorDto> measurementCorridors,
        IReadOnlyList<MeasurementNodeDto> measurementNodes,
        IReadOnlyList<DimensionIntervalBindingDto> dimensionIntervalBindings,
        IReadOnlyList<ArticulationBandDto> articulationBands,
        Guid? previewPinchGroupId)
    {
        if (dimensions.Count == 0 ||
            previewGeometry.Count == 0 ||
            measurementCorridors.Count == 0 ||
            measurementNodes.Count == 0 ||
            dimensionIntervalBindings.Count == 0 ||
            articulationBands.Count == 0 ||
            previewPinchGroupId is null)
        {
            return dimensions;
        }

        var band = articulationBands.FirstOrDefault(item => item.PinchGroupId == previewPinchGroupId.Value);
        if (band is null)
        {
            return dimensions;
        }

        var corridorLookup = measurementCorridors.ToDictionary(item => item.CorridorId);
        var nodeLookup = measurementNodes.ToDictionary(item => item.NodeId);
        var geometryLookup = previewGeometry.ToDictionary(item => item.Id);
        var bindingLookup = dimensionIntervalBindings
            .Where(item => string.Equals(item.BindingStatus, "ManualVerified", StringComparison.Ordinal))
            .ToDictionary(item => item.DimensionId);

        return dimensions
            .Select(dimension =>
            {
                if (!bindingLookup.TryGetValue(dimension.DimensionId, out var binding))
                {
                    return dimension;
                }

                if (!corridorLookup.TryGetValue(binding.CorridorId, out var corridor))
                {
                    return dimension;
                }

                if (!string.Equals(corridor.AxisTag, band.AxisTag, StringComparison.OrdinalIgnoreCase))
                {
                    return dimension;
                }

                if (!nodeLookup.TryGetValue(binding.StartNodeId, out var startNode) ||
                    !nodeLookup.TryGetValue(binding.EndNodeId, out var endNode))
                {
                    return dimension;
                }

                var startPoint = TryResolveLivePoint(startNode, corridor.AxisTag, geometryLookup);
                var endPoint = TryResolveLivePoint(endNode, corridor.AxisTag, geometryLookup);
                if (startPoint is null || endPoint is null)
                {
                    return dimension;
                }

                var authoredStartPoint = ResolveAuthoredPoint(startNode, corridor.AxisTag);
                var authoredEndPoint = ResolveAuthoredPoint(endNode, corridor.AxisTag);

                return DimensionGeometryProjector.RebuildAssociatedDimensionFromAnchorDeltas(
                    dimension,
                    authoredStartPoint.X,
                    authoredStartPoint.Y,
                    startPoint.Value.X,
                    startPoint.Value.Y,
                    authoredEndPoint.X,
                    authoredEndPoint.Y,
                    endPoint.Value.X,
                    endPoint.Value.Y);
            })
            .ToArray();
    }

    private static Point2 ResolveAuthoredPoint(MeasurementNodeDto node, string axisTag)
    {
        if (string.Equals(axisTag, "Height", StringComparison.OrdinalIgnoreCase))
        {
            return new Point2(
                node.AnchorX + node.OffsetNormal,
                node.AnchorY + node.OffsetAlongAxis);
        }

        return new Point2(
            node.AnchorX + node.OffsetAlongAxis,
            node.AnchorY + node.OffsetNormal);
    }

    private static Point2? TryResolveLivePoint(
        MeasurementNodeDto node,
        string axisTag,
        IReadOnlyDictionary<Guid, GeometryPathDto> geometryLookup)
    {
        if (!geometryLookup.TryGetValue(node.GeometryPathId, out var path))
        {
            return null;
        }

        var basePoint = GetPointAtRatio(path, node.PositionRatio);
        if (basePoint is null)
        {
            return null;
        }

        var x = basePoint.Value.X;
        var y = basePoint.Value.Y;

        if (string.Equals(axisTag, "Height", StringComparison.OrdinalIgnoreCase))
        {
            return new Point2(
                x + node.OffsetNormal,
                y + node.OffsetAlongAxis);
        }

        return new Point2(
            x + node.OffsetAlongAxis,
            y + node.OffsetNormal);
    }

    private static Point2? GetPointAtRatio(GeometryPathDto path, decimal positionRatio)
    {
        if (path.Segments.Count == 0)
        {
            return null;
        }

        var clampedRatio = decimal.Clamp(positionRatio, 0m, 1m);
        var totalLength = path.Segments.Sum(GetSegmentLength);
        if (totalLength <= double.Epsilon)
        {
            var first = path.Segments[0];
            return new Point2(first.StartX, first.StartY);
        }

        var targetLength = totalLength * (double)clampedRatio;
        var traversedLength = 0d;

        foreach (var segment in path.Segments)
        {
            var segmentLength = GetSegmentLength(segment);
            if (traversedLength + segmentLength >= targetLength)
            {
                var localRatio = segmentLength <= double.Epsilon
                    ? 0d
                    : (targetLength - traversedLength) / segmentLength;

                return new Point2(
                    decimal.Round(segment.StartX + ((segment.EndX - segment.StartX) * decimal.CreateChecked(localRatio)), 3, MidpointRounding.AwayFromZero),
                    decimal.Round(segment.StartY + ((segment.EndY - segment.StartY) * decimal.CreateChecked(localRatio)), 3, MidpointRounding.AwayFromZero));
            }

            traversedLength += segmentLength;
        }

        var last = path.Segments[^1];
        return new Point2(last.EndX, last.EndY);
    }

    private static double GetSegmentLength(GeometrySegmentDto segment)
    {
        var dx = (double)(segment.EndX - segment.StartX);
        var dy = (double)(segment.EndY - segment.StartY);
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private readonly record struct Point2(decimal X, decimal Y);
}
