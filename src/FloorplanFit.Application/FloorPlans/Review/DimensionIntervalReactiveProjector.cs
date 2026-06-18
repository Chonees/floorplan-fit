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
        Guid? previewPinchGroupId,
        IReadOnlyList<GeometryPathDto>? sourceGeometry = null)
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
        var previewGeometryLookup = previewGeometry.ToDictionary(item => item.Id);
        var sourceGeometryPaths = sourceGeometry is { Count: > 0 }
            ? sourceGeometry
            : null;
        var sourceGeometryLookup = sourceGeometryPaths is not null
            ? sourceGeometryPaths.ToDictionary(item => item.Id)
            : previewGeometryLookup;
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

                if (!nodeLookup.TryGetValue(binding.StartNodeId, out var startNode) ||
                    !nodeLookup.TryGetValue(binding.EndNodeId, out var endNode))
                {
                    return dimension;
                }

                var startPoint = TryResolveLivePoint(startNode, corridor.AxisTag, sourceGeometryLookup, previewGeometryLookup);
                var endPoint = TryResolveLivePoint(endNode, corridor.AxisTag, sourceGeometryLookup, previewGeometryLookup);
                if (startPoint is null || endPoint is null)
                {
                    return dimension;
                }

                var authoredStartPoint = sourceGeometryPaths is not null
                    ? TryResolveSourcePoint(startNode, corridor.AxisTag, sourceGeometryLookup) ??
                      ResolveAuthoredPoint(startNode, corridor.AxisTag)
                    : ResolveAuthoredPoint(startNode, corridor.AxisTag);
                var authoredEndPoint = sourceGeometryPaths is not null
                    ? TryResolveSourcePoint(endNode, corridor.AxisTag, sourceGeometryLookup) ??
                      ResolveAuthoredPoint(endNode, corridor.AxisTag)
                    : ResolveAuthoredPoint(endNode, corridor.AxisTag);

                if (!string.Equals(corridor.AxisTag, band.AxisTag, StringComparison.OrdinalIgnoreCase) ||
                    !OverlapsSelectedBand(binding, authoredStartPoint, authoredEndPoint, corridor.AxisTag, band))
                {
                    return DimensionGeometryProjector.TranslateAssociatedDimensionFromAnchorDeltas(
                        dimension,
                        authoredStartPoint.X,
                        authoredStartPoint.Y,
                        startPoint.Value.X,
                        startPoint.Value.Y,
                        authoredEndPoint.X,
                        authoredEndPoint.Y,
                        endPoint.Value.X,
                        endPoint.Value.Y,
                        band.AxisTag);
                }

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

    private static bool OverlapsSelectedBand(
        DimensionIntervalBindingDto binding,
        Point2 authoredStartPoint,
        Point2 authoredEndPoint,
        string axisTag,
        ArticulationBandDto band)
        => IntervalsOverlap(
               binding.IntervalStartCoordinate,
               binding.IntervalEndCoordinate,
               band.BandStartCoordinate,
               band.BandEndCoordinate) ||
           IntervalsOverlap(
               ResolveAxisCoordinate(authoredStartPoint, axisTag),
               ResolveAxisCoordinate(authoredEndPoint, axisTag),
               band.BandStartCoordinate,
               band.BandEndCoordinate);

    private static bool IntervalsOverlap(
        decimal firstStart,
        decimal firstEnd,
        decimal secondStart,
        decimal secondEnd)
    {
        var normalizedFirstStart = decimal.Min(firstStart, firstEnd);
        var normalizedFirstEnd = decimal.Max(firstStart, firstEnd);
        var normalizedSecondStart = decimal.Min(secondStart, secondEnd);
        var normalizedSecondEnd = decimal.Max(secondStart, secondEnd);

        return normalizedFirstStart <= normalizedSecondEnd &&
               normalizedSecondStart <= normalizedFirstEnd;
    }

    private static decimal ResolveAxisCoordinate(Point2 point, string axisTag)
    {
        return string.Equals(axisTag, "Height", StringComparison.OrdinalIgnoreCase)
            ? point.Y
            : point.X;
    }

    private static Point2 ResolveAuthoredPoint(MeasurementNodeDto node, string axisTag)
        => ApplyNodeOffsets(new Point2(node.AnchorX, node.AnchorY), node, axisTag);

    private static Point2? TryResolveSourcePoint(
        MeasurementNodeDto node,
        string axisTag,
        IReadOnlyDictionary<Guid, GeometryPathDto> sourceGeometryLookup)
    {
        if (!sourceGeometryLookup.TryGetValue(node.GeometryPathId, out var sourcePath))
        {
            return null;
        }

        var basePoint = GetPointAtRatio(sourcePath, node.PositionRatio);
        return basePoint is null
            ? null
            : ApplyNodeOffsets(basePoint.Value, node, axisTag);
    }

    private static Point2 ApplyNodeOffsets(Point2 basePoint, MeasurementNodeDto node, string axisTag)
    {
        if (string.Equals(axisTag, "Height", StringComparison.OrdinalIgnoreCase))
        {
            return new Point2(
                basePoint.X + node.OffsetNormal,
                basePoint.Y + node.OffsetAlongAxis);
        }

        return new Point2(
            basePoint.X + node.OffsetAlongAxis,
            basePoint.Y + node.OffsetNormal);
    }

    private static Point2? TryResolveLivePoint(
        MeasurementNodeDto node,
        string axisTag,
        IReadOnlyDictionary<Guid, GeometryPathDto> sourceGeometryLookup,
        IReadOnlyDictionary<Guid, GeometryPathDto> previewGeometryLookup)
    {
        if (!previewGeometryLookup.TryGetValue(node.GeometryPathId, out var previewPath))
        {
            return null;
        }

        var sourcePath = sourceGeometryLookup.TryGetValue(node.GeometryPathId, out var resolvedSourcePath)
            ? resolvedSourcePath
            : previewPath;
        var basePoint = GetPointAtMatchingSegmentLocation(sourcePath, previewPath, node.PositionRatio);
        if (basePoint is null)
        {
            return null;
        }

        return ApplyNodeOffsets(basePoint.Value, node, axisTag);
    }

    private static Point2? GetPointAtMatchingSegmentLocation(
        GeometryPathDto sourcePath,
        GeometryPathDto previewPath,
        decimal positionRatio)
    {
        var sourceLocation = TryResolveSegmentLocation(sourcePath, positionRatio);
        if (sourceLocation is null)
        {
            return GetPointAtRatio(previewPath, positionRatio);
        }

        var previewSegment = previewPath.Segments.FirstOrDefault(item => item.SortOrder == sourceLocation.Value.SortOrder);
        if (previewSegment is null)
        {
            return GetPointAtRatio(previewPath, positionRatio);
        }

        return GetPointAtSegmentLocalRatio(previewSegment, sourceLocation.Value.LocalRatio);
    }

    private static SegmentLocation? TryResolveSegmentLocation(GeometryPathDto path, decimal positionRatio)
    {
        if (path.Segments.Count == 0)
        {
            return null;
        }

        var clampedRatio = decimal.Clamp(positionRatio, 0m, 1m);
        var totalLength = path.Segments.Sum(GetSegmentLength);
        if (totalLength <= double.Epsilon)
        {
            return new SegmentLocation(path.Segments[0].SortOrder, 0d);
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

                return new SegmentLocation(segment.SortOrder, Math.Clamp(localRatio, 0d, 1d));
            }

            traversedLength += segmentLength;
        }

        return new SegmentLocation(path.Segments[^1].SortOrder, 1d);
    }

    private static Point2 GetPointAtSegmentLocalRatio(GeometrySegmentDto segment, double localRatio)
    {
        var clampedLocalRatio = Math.Clamp(localRatio, 0d, 1d);

        return new Point2(
            decimal.Round(segment.StartX + ((segment.EndX - segment.StartX) * decimal.CreateChecked(clampedLocalRatio)), 3, MidpointRounding.AwayFromZero),
            decimal.Round(segment.StartY + ((segment.EndY - segment.StartY) * decimal.CreateChecked(clampedLocalRatio)), 3, MidpointRounding.AwayFromZero));
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

    private readonly record struct SegmentLocation(int SortOrder, double LocalRatio);
}
