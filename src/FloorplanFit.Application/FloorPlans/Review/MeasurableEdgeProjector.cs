using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Review;

public static class MeasurableEdgeProjector
{
    public static IReadOnlyList<MeasurableEdgeDto> Build(
        IReadOnlyList<GeometryPathDto> geometryPaths,
        IReadOnlyList<WallCandidateDto> wallCandidates,
        IReadOnlyList<OpeningCandidateDto> openingCandidates,
        MeasurementContextDto? measurementContext)
    {
        var geometryLookup = geometryPaths.ToDictionary(item => item.Id);
        var factor = measurementContext?.ToMillimetersFactor ?? 1m;
        var items = new List<MeasurableEdgeDto>();

        foreach (var candidate in wallCandidates)
        {
            if (candidate.GeometryPathId is Guid geometryPathId && geometryLookup.TryGetValue(geometryPathId, out var path))
            {
                items.AddRange(BuildForPath(
                    FloorPlanArtifactSourceKinds.WallCandidate,
                    candidate.CandidateId,
                    path,
                    factor));
            }
        }

        foreach (var opening in openingCandidates)
        {
            if (opening.GeometryPathId is Guid geometryPathId && geometryLookup.TryGetValue(geometryPathId, out var path))
            {
                items.AddRange(BuildForPath(
                    FloorPlanArtifactSourceKinds.OpeningCandidate,
                    opening.OpeningCandidateId,
                    path,
                    factor));
            }
        }

        return items;
    }

    private static IReadOnlyList<MeasurableEdgeDto> BuildForPath(
        string sourceArtifactKind,
        Guid sourceArtifactId,
        GeometryPathDto path,
        decimal toMillimetersFactor)
    {
        return path.Segments
            .Select(segment =>
            {
                var dx = segment.EndX - segment.StartX;
                var dy = segment.EndY - segment.StartY;
                var lengthSourceUnits = decimal.CreateChecked(Math.Sqrt((double)((dx * dx) + (dy * dy))));
                var orientationDegrees = decimal.CreateChecked(Math.Atan2((double)dy, (double)dx) * 180d / Math.PI);
                return new MeasurableEdgeDto(
                    $"{sourceArtifactKind}:{sourceArtifactId:N}:{path.Id:N}:{segment.SortOrder}",
                    sourceArtifactKind,
                    sourceArtifactId,
                    path.Id,
                    lengthSourceUnits,
                    decimal.Round(lengthSourceUnits * toMillimetersFactor, 3, MidpointRounding.AwayFromZero),
                    decimal.Round(orientationDegrees, 3, MidpointRounding.AwayFromZero),
                    segment.StartX,
                    segment.StartY,
                    segment.EndX,
                    segment.EndY);
            })
            .ToArray();
    }
}
