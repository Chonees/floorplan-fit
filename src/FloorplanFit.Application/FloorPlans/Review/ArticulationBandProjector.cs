using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Review;

public static class ArticulationBandProjector
{
    public static IReadOnlyList<ArticulationBandDto> Build(
        IReadOnlyList<PinchGroupDto> pinchGroups,
        IReadOnlyList<PinchMarkerDto> pinchMarkers,
        IReadOnlyList<GeometryPathDto> geometryPaths)
    {
        if (pinchGroups.Count == 0 || pinchMarkers.Count == 0 || geometryPaths.Count == 0)
        {
            return [];
        }

        var geometryLookup = geometryPaths.ToDictionary(path => path.Id);
        var items = new List<ArticulationBandDto>();

        foreach (var group in pinchGroups)
        {
            var markers = pinchMarkers
                .Where(marker => marker.PinchGroupId == group.PinchGroupId)
                .Select<PinchMarkerDto, ResolvedMarker?>(marker =>
                {
                    if (!geometryLookup.TryGetValue(marker.GeometryPathId, out var path))
                    {
                        return null;
                    }

                    var point = GetPointAtRatio(path, marker.PositionRatio);
                    if (point is null)
                    {
                        return null;
                    }

                    var coordinate = string.Equals(group.AxisTag, "Height", StringComparison.OrdinalIgnoreCase)
                        ? decimal.Round((decimal)point.Value.Y, 3, MidpointRounding.AwayFromZero)
                        : decimal.Round((decimal)point.Value.X, 3, MidpointRounding.AwayFromZero);

                    return new ResolvedMarker(coordinate, marker.MaxTrimMm);
                })
                .Where(item => item.HasValue)
                .Select(item => item!.Value)
                .OrderBy(item => item.Coordinate)
                .ToArray();

            if (markers.Length == 0)
            {
                continue;
            }

            items.Add(new ArticulationBandDto(
                group.PinchGroupId,
                group.Name,
                group.AxisTag,
                markers[0].Coordinate,
                markers[^1].Coordinate,
                markers.Min(item => item.MaxTrimMm),
                "Suggested"));
        }

        return items;
    }

    private static (decimal X, decimal Y)? GetPointAtRatio(GeometryPathDto path, decimal positionRatio)
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
            return (first.StartX, first.StartY);
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

                return (
                    decimal.Round(segment.StartX + ((segment.EndX - segment.StartX) * decimal.CreateChecked(localRatio)), 3, MidpointRounding.AwayFromZero),
                    decimal.Round(segment.StartY + ((segment.EndY - segment.StartY) * decimal.CreateChecked(localRatio)), 3, MidpointRounding.AwayFromZero));
            }

            traversedLength += segmentLength;
        }

        var last = path.Segments[^1];
        return (last.EndX, last.EndY);
    }

    private static double GetSegmentLength(GeometrySegmentDto segment)
    {
        var dx = (double)(segment.EndX - segment.StartX);
        var dy = (double)(segment.EndY - segment.StartY);
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private readonly record struct ResolvedMarker(decimal Coordinate, decimal MaxTrimMm);
}
