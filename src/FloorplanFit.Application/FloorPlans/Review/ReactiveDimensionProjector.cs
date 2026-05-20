using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Review;

public static class ReactiveDimensionProjector
{
    private const string LinearSpanBindingKind = "LinearSpan";
    private const string OrdinateXBindingKind = "OrdinateX";
    private const string OrdinateYBindingKind = "OrdinateY";
    private const string RadiusBindingKind = "Radius";
    private const string DiameterBindingKind = "Diameter";
    private const decimal CoordinateEpsilon = 0.001m;

    public static IReadOnlyList<DimensionDto> Project(
        IReadOnlyList<DimensionDto> dimensions,
        IReadOnlyList<DimensionAssociationDto> associations,
        IReadOnlyList<MeasurableEdgeDto> measurableEdges)
        => Project(dimensions, associations, measurableEdges, null);

    public static IReadOnlyList<DimensionDto> Project(
        IReadOnlyList<DimensionDto> dimensions,
        IReadOnlyList<DimensionAssociationDto> associations,
        IReadOnlyList<MeasurableEdgeDto> measurableEdges,
        IReadOnlyList<DimensionBindingDto>? dimensionBindings)
    {
        if (dimensions.Count == 0 || associations.Count == 0 || measurableEdges.Count == 0)
        {
            return dimensions;
        }

        var associationLookup = associations.ToDictionary(item => item.DimensionId);
        var edgeLookup = measurableEdges.ToDictionary(item => item.EdgeKey, StringComparer.Ordinal);
        var bindingLookup = dimensionBindings?.ToDictionary(item => item.DimensionId);

        return dimensions
            .Select(dimension =>
            {
                DimensionBindingDto? binding = null;
                bindingLookup?.TryGetValue(dimension.DimensionId, out binding);
                var bindingKind = binding?.BindingKind ?? ResolveImplicitBindingKind(dimension);

                if (binding is not null && !binding.IsResolved)
                {
                    return dimension;
                }

                if (!IsSupportedReactiveBindingKind(bindingKind))
                {
                    return dimension;
                }

                if (!associationLookup.TryGetValue(dimension.DimensionId, out var association) ||
                    !association.IsFullyResolved ||
                    association.StartAnchor is null ||
                    association.EndAnchor is null)
                {
                    return dimension;
                }

                var startPoint = TryResolveLiveAnchorPoint(association.StartAnchor, edgeLookup, measurableEdges);
                var endPoint = TryResolveLiveAnchorPoint(association.EndAnchor, edgeLookup, measurableEdges);
                if (startPoint is null || endPoint is null)
                {
                    return dimension;
                }

                return string.Equals(bindingKind, LinearSpanBindingKind, StringComparison.Ordinal)
                    ? DimensionGeometryProjector.RebuildAssociatedDimension(
                        dimension,
                        startPoint.Value.X,
                        startPoint.Value.Y,
                        endPoint.Value.X,
                        endPoint.Value.Y)
                    : string.Equals(bindingKind, OrdinateXBindingKind, StringComparison.Ordinal) ||
                      string.Equals(bindingKind, OrdinateYBindingKind, StringComparison.Ordinal)
                        ? DimensionGeometryProjector.RebuildAssociatedOrdinateDimension(
                            dimension,
                            startPoint.Value.X,
                            startPoint.Value.Y,
                            endPoint.Value.X,
                            endPoint.Value.Y,
                            bindingKind)
                        : DimensionGeometryProjector.RebuildAssociatedRadialDimension(
                            dimension,
                            startPoint.Value.X,
                            startPoint.Value.Y,
                            endPoint.Value.X,
                            endPoint.Value.Y);
            })
            .ToArray();
    }

    private static bool IsSupportedReactiveBindingKind(string bindingKind)
        => string.Equals(bindingKind, LinearSpanBindingKind, StringComparison.Ordinal) ||
           string.Equals(bindingKind, OrdinateXBindingKind, StringComparison.Ordinal) ||
           string.Equals(bindingKind, OrdinateYBindingKind, StringComparison.Ordinal) ||
           string.Equals(bindingKind, RadiusBindingKind, StringComparison.Ordinal) ||
           string.Equals(bindingKind, DiameterBindingKind, StringComparison.Ordinal);

    private static string ResolveImplicitBindingKind(DimensionDto dimension)
    {
        var baseType = dimension.DimType & 0x7;
        return baseType switch
        {
            3 => DiameterBindingKind,
            4 => RadiusBindingKind,
            6 => ResolveOrdinateBindingKind(dimension),
            _ => LinearSpanBindingKind
        };
    }

    private static string ResolveOrdinateBindingKind(DimensionDto dimension)
    {
        var dx = Math.Abs((double)(dimension.DefPoint2X - dimension.DefPointX));
        var dy = Math.Abs((double)(dimension.DefPoint2Y - dimension.DefPointY));
        return dx >= dy - (double)CoordinateEpsilon
            ? OrdinateXBindingKind
            : OrdinateYBindingKind;
    }

    private static Point2? TryResolveLiveAnchorPoint(
        DimensionAnchorReferenceDto anchor,
        IReadOnlyDictionary<string, MeasurableEdgeDto> edgeLookup,
        IReadOnlyList<MeasurableEdgeDto> measurableEdges)
    {
        edgeLookup.TryGetValue(anchor.EdgeKey, out var directEdge);

        var bestTopologyCandidate = FindBestTopologyCandidate(anchor, measurableEdges);
        if (directEdge is null)
        {
            return bestTopologyCandidate?.Point;
        }

        var directPoint = ResolveAnchorPoint(directEdge, anchor);
        if (bestTopologyCandidate is null)
        {
            return directPoint;
        }

        var directDistance = DistanceToStoredAnchor(anchor, directPoint);
        return bestTopologyCandidate.Value.DistanceToStoredAnchor + CoordinateEpsilon < directDistance
            ? bestTopologyCandidate.Value.Point
            : directPoint;
    }

    private static AnchorResolutionCandidate? FindBestTopologyCandidate(
        DimensionAnchorReferenceDto anchor,
        IReadOnlyList<MeasurableEdgeDto> measurableEdges)
    {
        var preferredCandidates = measurableEdges
            .Where(edge =>
                edge.GeometryPathId == anchor.GeometryPathId &&
                edge.SourceArtifactId == anchor.SourceArtifactId &&
                string.Equals(edge.SourceArtifactKind, anchor.SourceArtifactKind, StringComparison.Ordinal))
            .ToArray();
        var candidates = preferredCandidates.Length > 0
            ? preferredCandidates
            : measurableEdges
                .Where(edge => edge.GeometryPathId == anchor.GeometryPathId)
                .ToArray();

        if (candidates.Length == 0)
        {
            return null;
        }

        return candidates
            .Select(edge =>
            {
                var point = ResolveTopologyAwareAnchorPoint(edge, anchor);
                return new AnchorResolutionCandidate(edge, point, DistanceToStoredAnchor(anchor, point));
            })
            .OrderBy(item => item.DistanceToStoredAnchor)
            .ThenBy(item => item.Edge.SortOrderSafe())
            .First();
    }

    private static Point2 ResolveAnchorPoint(MeasurableEdgeDto edge, DimensionAnchorReferenceDto anchor)
    {
        if (anchor.SegmentRatio.HasValue)
        {
            return ResolveProjectedPoint(edge, anchor);
        }

        return string.Equals(anchor.EdgeAnchorKind, "End", StringComparison.Ordinal)
            ? new Point2(edge.EndAnchorX, edge.EndAnchorY)
            : new Point2(edge.StartAnchorX, edge.StartAnchorY);
    }

    private static Point2 ResolveTopologyAwareAnchorPoint(MeasurableEdgeDto edge, DimensionAnchorReferenceDto anchor)
    {
        if (anchor.SegmentRatio.HasValue || string.Equals(anchor.EdgeAnchorKind, "Projected", StringComparison.Ordinal))
        {
            return ResolveProjectedPointFromStoredAnchor(edge, anchor);
        }

        return ResolveAnchorPoint(edge, anchor);
    }

    private static Point2 ResolveProjectedPoint(MeasurableEdgeDto edge, DimensionAnchorReferenceDto anchor)
    {
        var ratio = decimal.Clamp(anchor.SegmentRatio ?? 0m, 0m, 1m);
        return new Point2(
            edge.StartAnchorX + ((edge.EndAnchorX - edge.StartAnchorX) * ratio),
            edge.StartAnchorY + ((edge.EndAnchorY - edge.StartAnchorY) * ratio));
    }

    private static Point2 ResolveProjectedPointFromStoredAnchor(MeasurableEdgeDto edge, DimensionAnchorReferenceDto anchor)
    {
        var dx = edge.EndAnchorX - edge.StartAnchorX;
        var dy = edge.EndAnchorY - edge.StartAnchorY;
        var lengthSquared = (dx * dx) + (dy * dy);
        if (lengthSquared <= CoordinateEpsilon * CoordinateEpsilon)
        {
            return new Point2(edge.StartAnchorX, edge.StartAnchorY);
        }

        var projectionRatio = (((anchor.AnchorX - edge.StartAnchorX) * dx) + ((anchor.AnchorY - edge.StartAnchorY) * dy)) / lengthSquared;
        var clampedRatio = decimal.Clamp(projectionRatio, 0m, 1m);
        return new Point2(
            edge.StartAnchorX + (dx * clampedRatio),
            edge.StartAnchorY + (dy * clampedRatio));
    }

    private static decimal DistanceToStoredAnchor(DimensionAnchorReferenceDto anchor, Point2 point)
    {
        var dx = point.X - anchor.AnchorX;
        var dy = point.Y - anchor.AnchorY;
        return decimal.CreateChecked(Math.Sqrt((double)((dx * dx) + (dy * dy))));
    }

    private static int SortOrderSafe(this MeasurableEdgeDto edge)
    {
        var lastColon = edge.EdgeKey.LastIndexOf(':');
        if (lastColon < 0)
        {
            return int.MaxValue;
        }

        return int.TryParse(edge.EdgeKey[(lastColon + 1)..], out var parsed)
            ? parsed
            : int.MaxValue;
    }

    private readonly record struct Point2(decimal X, decimal Y);

    private readonly record struct AnchorResolutionCandidate(
        MeasurableEdgeDto Edge,
        Point2 Point,
        decimal DistanceToStoredAnchor);
}
