using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Review;

public static class ReactiveDimensionProjector
{
    public static IReadOnlyList<DimensionDto> Project(
        IReadOnlyList<DimensionDto> dimensions,
        IReadOnlyList<DimensionAssociationDto> associations,
        IReadOnlyList<MeasurableEdgeDto> measurableEdges)
    {
        if (dimensions.Count == 0 || associations.Count == 0 || measurableEdges.Count == 0)
        {
            return dimensions;
        }

        var associationLookup = associations.ToDictionary(item => item.DimensionId);
        var edgeLookup = measurableEdges.ToDictionary(item => item.EdgeKey, StringComparer.Ordinal);

        return dimensions
            .Select(dimension =>
            {
                if (dimension.IsEdited ||
                    !associationLookup.TryGetValue(dimension.DimensionId, out var association) ||
                    !association.IsFullyResolved ||
                    association.StartAnchor is null ||
                    association.EndAnchor is null ||
                    !edgeLookup.TryGetValue(association.StartAnchor.EdgeKey, out var startEdge) ||
                    !edgeLookup.TryGetValue(association.EndAnchor.EdgeKey, out var endEdge))
                {
                    return dimension;
                }

                var startPoint = ResolveAnchorPoint(startEdge, association.StartAnchor);
                var endPoint = ResolveAnchorPoint(endEdge, association.EndAnchor);
                return DimensionGeometryProjector.RebuildAssociatedDimension(
                    dimension,
                    startPoint.X,
                    startPoint.Y,
                    endPoint.X,
                    endPoint.Y);
            })
            .ToArray();
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

    private static Point2 ResolveProjectedPoint(MeasurableEdgeDto edge, DimensionAnchorReferenceDto anchor)
    {
        var ratio = decimal.Clamp(anchor.SegmentRatio ?? 0m, 0m, 1m);
        return new Point2(
            edge.StartAnchorX + ((edge.EndAnchorX - edge.StartAnchorX) * ratio),
            edge.StartAnchorY + ((edge.EndAnchorY - edge.StartAnchorY) * ratio));
    }

    private readonly record struct Point2(decimal X, decimal Y);
}
