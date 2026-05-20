using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Review;

public static class DimensionAssociationProjector
{
    private const string AssociationKind = "MeasuredEndpointAnchors";
    private const decimal MinimumAssociationToleranceSourceUnits = 0.5m;
    private const decimal ExactMatchEpsilonSourceUnits = 0.001m;

    public static IReadOnlyList<DimensionAssociationDto> Build(
        IReadOnlyList<DimensionDto> dimensions,
        IReadOnlyList<MeasurableEdgeDto> measurableEdges,
        MeasurementContextDto? measurementContext)
    {
        if (dimensions.Count == 0 || measurableEdges.Count == 0)
        {
            return [];
        }

        var toleranceSourceUnits = ResolveAssociationToleranceSourceUnits(measurementContext);

        return dimensions
            .Select(dimension => BuildAssociation(dimension, measurableEdges, toleranceSourceUnits))
            .ToArray();
    }

    private static DimensionAssociationDto BuildAssociation(
        DimensionDto dimension,
        IReadOnlyList<MeasurableEdgeDto> measurableEdges,
        decimal toleranceSourceUnits)
    {
        var startMatches = ResolveMatches(
            new PointInfo(dimension.DefPointX, dimension.DefPointY),
            measurableEdges,
            toleranceSourceUnits);
        var endMatches = ResolveMatches(
            new PointInfo(dimension.DefPoint2X, dimension.DefPoint2Y),
            measurableEdges,
            toleranceSourceUnits);

        var startSelection = startMatches.Count > 0 ? startMatches[0] : null;
        var endSelection = endMatches.Count > 0 ? endMatches[0] : null;
        if (startSelection is not null &&
            endSelection is not null &&
            string.Equals(startSelection.EdgeKey, endSelection.EdgeKey, StringComparison.Ordinal) &&
            string.Equals(startSelection.EdgeAnchorKind, endSelection.EdgeAnchorKind, StringComparison.Ordinal) &&
            Nullable.Equals(startSelection.SegmentRatio, endSelection.SegmentRatio) &&
            endMatches.Count > 1)
        {
            endSelection = endMatches[1];
        }

        var startAnchor = startSelection is null ? null : CreateAnchorReference(startSelection);
        var endAnchor = endSelection is null ? null : CreateAnchorReference(endSelection);
        var startConfidence = ResolveAnchorConfidence(startSelection, startMatches.Count, toleranceSourceUnits);
        var endConfidence = ResolveAnchorConfidence(endSelection, endMatches.Count, toleranceSourceUnits);
        var resolvedCount = (startAnchor is null ? 0 : 1) + (endAnchor is null ? 0 : 1);
        var confidence = decimal.Round((startConfidence + endConfidence) / 2m, 3, MidpointRounding.AwayFromZero);
        var notes = BuildNotes(startMatches.Count, endMatches.Count, toleranceSourceUnits, resolvedCount);

        return new DimensionAssociationDto(
            dimension.DimensionId,
            AssociationKind,
            resolvedCount == 2,
            confidence,
            notes)
        {
            StartAnchor = startAnchor,
            EndAnchor = endAnchor
        };
    }

    private static IReadOnlyList<AnchorCandidateMatch> ResolveMatches(
        PointInfo point,
        IReadOnlyList<MeasurableEdgeDto> measurableEdges,
        decimal toleranceSourceUnits)
    {
        return measurableEdges
            .Select(edge => ResolveBestMatch(point, edge))
            .Where(match => match.DistanceSourceUnits <= toleranceSourceUnits)
            .OrderBy(match => match.DistanceSourceUnits)
            .ThenBy(match => match.SourceArtifactKind, StringComparer.Ordinal)
            .ThenBy(match => match.EdgeKey, StringComparer.Ordinal)
            .ThenBy(match => match.EdgeAnchorKind, StringComparer.Ordinal)
            .ToArray();
    }

    private static AnchorCandidateMatch ResolveBestMatch(PointInfo point, MeasurableEdgeDto edge)
    {
        var dx = edge.EndAnchorX - edge.StartAnchorX;
        var dy = edge.EndAnchorY - edge.StartAnchorY;
        var lengthSquared = (dx * dx) + (dy * dy);

        if (lengthSquared <= ExactMatchEpsilonSourceUnits * ExactMatchEpsilonSourceUnits)
        {
            return new AnchorCandidateMatch(
                edge.EdgeKey,
                edge.SourceArtifactKind,
                edge.SourceArtifactId,
                edge.GeometryPathId,
                "Start",
                edge.StartAnchorX,
                edge.StartAnchorY,
                null,
                Distance(point.X, point.Y, edge.StartAnchorX, edge.StartAnchorY));
        }

        var projectionRatio = (((point.X - edge.StartAnchorX) * dx) + ((point.Y - edge.StartAnchorY) * dy)) / lengthSquared;
        var clampedRatio = decimal.Clamp(projectionRatio, 0m, 1m);
        var projectedX = edge.StartAnchorX + (dx * clampedRatio);
        var projectedY = edge.StartAnchorY + (dy * clampedRatio);
        var anchorKind = clampedRatio <= ExactMatchEpsilonSourceUnits
            ? "Start"
            : clampedRatio >= 1m - ExactMatchEpsilonSourceUnits
                ? "End"
                : "Projected";

        return new AnchorCandidateMatch(
            edge.EdgeKey,
            edge.SourceArtifactKind,
            edge.SourceArtifactId,
            edge.GeometryPathId,
            anchorKind,
            projectedX,
            projectedY,
            anchorKind == "Projected" ? decimal.Round(clampedRatio, 6, MidpointRounding.AwayFromZero) : null,
            Distance(point.X, point.Y, projectedX, projectedY));
    }

    private static DimensionAnchorReferenceDto CreateAnchorReference(AnchorCandidateMatch match)
        => new(
            match.EdgeKey,
            match.SourceArtifactKind,
            match.SourceArtifactId,
            match.GeometryPathId,
            match.EdgeAnchorKind,
            match.X,
            match.Y,
            decimal.Round(match.DistanceSourceUnits, 3, MidpointRounding.AwayFromZero))
        {
            SegmentRatio = match.SegmentRatio
        };

    private static decimal ResolveAnchorConfidence(
        AnchorCandidateMatch? match,
        int candidateCount,
        decimal toleranceSourceUnits)
    {
        if (match is null)
        {
            return 0m;
        }

        decimal confidence;
        if (match.DistanceSourceUnits <= ExactMatchEpsilonSourceUnits)
        {
            confidence = 1m;
        }
        else if (toleranceSourceUnits <= 0m)
        {
            confidence = 0.5m;
        }
        else
        {
            var normalized = 1m - (match.DistanceSourceUnits / toleranceSourceUnits);
            confidence = decimal.Max(0.25m, normalized);
        }

        if (candidateCount > 1)
        {
            confidence *= 0.75m;
        }

        return decimal.Round(confidence, 3, MidpointRounding.AwayFromZero);
    }

    private static string BuildNotes(int startCandidateCount, int endCandidateCount, decimal toleranceSourceUnits, int resolvedCount)
    {
        var parts = new List<string>();
        if (startCandidateCount == 0)
        {
            parts.Add($"start unresolved within {toleranceSourceUnits:0.###} source units");
        }
        else if (startCandidateCount > 1)
        {
            parts.Add($"start ambiguous across {startCandidateCount} anchors");
        }

        if (endCandidateCount == 0)
        {
            parts.Add($"end unresolved within {toleranceSourceUnits:0.###} source units");
        }
        else if (endCandidateCount > 1)
        {
            parts.Add($"end ambiguous across {endCandidateCount} anchors");
        }

        if (parts.Count == 0)
        {
            return resolvedCount == 2
                ? "Resolved both dimension endpoints to measurable-edge anchors."
                : "No measurable-edge anchors matched.";
        }

        return string.Join("; ", parts) + ".";
    }

    private static decimal ResolveAssociationToleranceSourceUnits(MeasurementContextDto? measurementContext)
    {
        if (measurementContext is null ||
            measurementContext.ToMillimetersFactor <= 0m ||
            measurementContext.LinearToleranceMm <= 0m)
        {
            return MinimumAssociationToleranceSourceUnits;
        }

        var convertedTolerance = measurementContext.LinearToleranceMm / measurementContext.ToMillimetersFactor;
        return decimal.Max(MinimumAssociationToleranceSourceUnits, decimal.Round(convertedTolerance, 3, MidpointRounding.AwayFromZero));
    }

    private static decimal Distance(decimal x1, decimal y1, decimal x2, decimal y2)
    {
        var dx = x2 - x1;
        var dy = y2 - y1;
        return decimal.CreateChecked(Math.Sqrt((double)((dx * dx) + (dy * dy))));
    }

    private readonly record struct PointInfo(decimal X, decimal Y);

    private sealed record AnchorCandidateMatch(
        string EdgeKey,
        string SourceArtifactKind,
        Guid SourceArtifactId,
        Guid GeometryPathId,
        string EdgeAnchorKind,
        decimal X,
        decimal Y,
        decimal? SegmentRatio,
        decimal DistanceSourceUnits);
}
