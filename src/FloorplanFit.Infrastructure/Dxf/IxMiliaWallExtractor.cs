using FloorplanFit.Application.Abstractions;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;
using System.Globalization;

namespace FloorplanFit.Infrastructure.Dxf;

public sealed class IxMiliaWallExtractor : IWallExtractor
{
    private const double AxisAlignmentTolerance = 0.01d;
    private const double MinimumWallFaceLength = 12d;
    private const double MinimumWallFaceOverlap = 18d;
    private const double NominalFourInchWallSpacing = 4d;
    private const double NominalSixInchWallSpacing = 6d;
    private const double WallSpacingTolerance = 0.35d;
    private readonly DxfExtractionProfile profile;

    public IxMiliaWallExtractor()
        : this(DxfExtractionProfile.PointeHomes)
    {
    }

    public IxMiliaWallExtractor(DxfExtractionProfile profile)
    {
        this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    public Task<IReadOnlyList<DetectedWallCandidate>> ExtractAsync(string managedFilePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(managedFilePath))
        {
            throw new ArgumentException("A DXF file path is required.", nameof(managedFilePath));
        }

        var dxf = DxfFile.Load(managedFilePath);
        var candidates = new List<DetectedWallCandidate>();
        var lineIndex = 0;
        var polylineIndex = 0;

        foreach (var line in dxf.Entities.OfType<DxfLine>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!profile.IsWallCandidateLayer(line.Layer))
            {
                continue;
            }

            lineIndex++;
            candidates.Add(new DetectedWallCandidate(
                BuildSourceEntityRef("LINE", lineIndex),
                line.Layer ?? string.Empty,
                [ToPoint(line.P1.X, line.P1.Y), ToPoint(line.P2.X, line.P2.Y)],
                null,
                0.95m,
                "Detected from WALL layer line entity."));
        }

        foreach (var polyline in dxf.Entities.OfType<DxfLwPolyline>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!profile.IsWallCandidateLayer(polyline.Layer))
            {
                continue;
            }

            polylineIndex++;
            AddPolylineSegments(candidates, polyline, polylineIndex);
        }

        return Task.FromResult<IReadOnlyList<DetectedWallCandidate>>(ApplyThicknessInferences(candidates));
    }

    private static void AddPolylineSegments(List<DetectedWallCandidate> candidates, DxfLwPolyline polyline, int polylineIndex)
    {
        var vertices = polyline.Vertices.ToArray();
        if (vertices.Length < 2)
        {
            return;
        }

        for (var index = 0; index < vertices.Length - 1; index++)
        {
            candidates.Add(CreatePolylineSegmentCandidate(polyline, polylineIndex, index, vertices[index], vertices[index + 1]));
        }

        if (polyline.IsClosed)
        {
            candidates.Add(CreatePolylineSegmentCandidate(polyline, polylineIndex, vertices.Length - 1, vertices[^1], vertices[0]));
        }
    }

    private static DetectedWallCandidate CreatePolylineSegmentCandidate(
        DxfLwPolyline polyline,
        int polylineIndex,
        int segmentIndex,
        DxfLwPolylineVertex start,
        DxfLwPolylineVertex end)
    {
        return new DetectedWallCandidate(
            BuildSourceEntityRef("LWPOLYLINE", polylineIndex, segmentIndex),
            polyline.Layer ?? string.Empty,
            [ToPoint(start.X, start.Y), ToPoint(end.X, end.Y)],
            null,
            0.90m,
            "Derived from WALL layer lightweight polyline segment.");
    }

    private static GeometryPoint ToPoint(double x, double y)
    {
        return new GeometryPoint((decimal)x, (decimal)y);
    }

    private IReadOnlyList<DetectedWallCandidate> ApplyThicknessInferences(IReadOnlyList<DetectedWallCandidate> candidates)
    {
        var wallFaces = candidates
            .Select((candidate, index) => TryCreateWallFace(candidate, index))
            .Where(face => face is not null)
            .Select(face => face!.Value)
            .ToArray();

        if (wallFaces.Length == 0)
        {
            return candidates;
        }

        var bestInferencesByCandidate = new Dictionary<int, WallThicknessInference>();

        for (var firstIndex = 0; firstIndex < wallFaces.Length; firstIndex++)
        {
            var first = wallFaces[firstIndex];

            for (var secondIndex = firstIndex + 1; secondIndex < wallFaces.Length; secondIndex++)
            {
                var second = wallFaces[secondIndex];
                if (first.Orientation != second.Orientation)
                {
                    continue;
                }

                var spacing = Math.Abs(first.Position - second.Position);
                var nominalSpacing = ClassifyNominalWallSpacing(spacing);
                if (nominalSpacing is null)
                {
                    continue;
                }

                var overlap = CalculateOverlap(first.Start, first.End, second.Start, second.End);
                if (overlap < MinimumWallFaceOverlap)
                {
                    continue;
                }

                var score = CalculateInferenceScore(overlap, spacing, nominalSpacing.Value);
                var inference = new WallThicknessInference(
                    InchesToMillimeters(nominalSpacing.Value),
                    nominalSpacing.Value,
                    spacing,
                    overlap,
                    score);

                RecordBestInference(bestInferencesByCandidate, first.CandidateIndex, inference);
                RecordBestInference(bestInferencesByCandidate, second.CandidateIndex, inference);
            }
        }

        return candidates
            .Select((candidate, index) => bestInferencesByCandidate.TryGetValue(index, out var inference)
                ? candidate with
                {
                    ThicknessMm = inference.ThicknessMm,
                    DetectionNotes = AppendThicknessInferenceNote(candidate.DetectionNotes, inference)
                }
                : candidate)
            .ToArray();
    }

    private WallFace? TryCreateWallFace(DetectedWallCandidate candidate, int candidateIndex)
    {
        if (!profile.IsPhysicalWallLayer(candidate.SourceLayer) || candidate.Points.Count < 2)
        {
            return null;
        }

        var start = candidate.Points[0];
        var end = candidate.Points[1];
        var startX = (double)start.X;
        var startY = (double)start.Y;
        var endX = (double)end.X;
        var endY = (double)end.Y;

        if (Math.Abs(startX - endX) <= AxisAlignmentTolerance)
        {
            var faceStart = Math.Min(startY, endY);
            var faceEnd = Math.Max(startY, endY);
            var length = faceEnd - faceStart;
            return length >= MinimumWallFaceLength
                ? new WallFace(candidateIndex, WallFaceOrientation.Vertical, (startX + endX) / 2d, faceStart, faceEnd)
                : null;
        }

        if (Math.Abs(startY - endY) <= AxisAlignmentTolerance)
        {
            var faceStart = Math.Min(startX, endX);
            var faceEnd = Math.Max(startX, endX);
            var length = faceEnd - faceStart;
            return length >= MinimumWallFaceLength
                ? new WallFace(candidateIndex, WallFaceOrientation.Horizontal, (startY + endY) / 2d, faceStart, faceEnd)
                : null;
        }

        return null;
    }

    private static double? ClassifyNominalWallSpacing(double spacing)
    {
        if (Math.Abs(spacing - NominalFourInchWallSpacing) <= WallSpacingTolerance)
        {
            return NominalFourInchWallSpacing;
        }

        if (Math.Abs(spacing - NominalSixInchWallSpacing) <= WallSpacingTolerance)
        {
            return NominalSixInchWallSpacing;
        }

        return null;
    }

    private static double CalculateOverlap(double firstStart, double firstEnd, double secondStart, double secondEnd)
    {
        return Math.Max(0d, Math.Min(firstEnd, secondEnd) - Math.Max(firstStart, secondStart));
    }

    private static double CalculateInferenceScore(double overlap, double measuredSpacing, double nominalSpacing)
    {
        return overlap - (Math.Abs(measuredSpacing - nominalSpacing) * 10d);
    }

    private static decimal InchesToMillimeters(double inches)
    {
        return Math.Round((decimal)inches * 25.4m, 1, MidpointRounding.AwayFromZero);
    }

    private static void RecordBestInference(Dictionary<int, WallThicknessInference> inferences, int candidateIndex, WallThicknessInference inference)
    {
        if (!inferences.TryGetValue(candidateIndex, out var current) || inference.Score > current.Score)
        {
            inferences[candidateIndex] = inference;
        }
    }

    private static string AppendThicknessInferenceNote(string? detectionNotes, WallThicknessInference inference)
    {
        var note = string.Format(
            CultureInfo.InvariantCulture,
            "Inferred {0:0.#}\" wall thickness from parallel wall faces ({1:0.##}\" measured spacing, {2:0.##}\" overlap).",
            inference.NominalSpacingInches,
            inference.MeasuredSpacingInches,
            inference.OverlapInches);

        return string.IsNullOrWhiteSpace(detectionNotes)
            ? note
            : $"{detectionNotes} {note}";
    }

    private static string BuildSourceEntityRef(string entityType, int entityIndex, int? segmentIndex = null)
    {
        var stablePart = entityIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);

        return segmentIndex is null
            ? $"{entityType}:{stablePart}"
            : $"{entityType}:{stablePart}:{segmentIndex.Value}";
    }

    private enum WallFaceOrientation
    {
        Horizontal,
        Vertical
    }

    private readonly record struct WallFace(
        int CandidateIndex,
        WallFaceOrientation Orientation,
        double Position,
        double Start,
        double End);

    private readonly record struct WallThicknessInference(
        decimal ThicknessMm,
        double NominalSpacingInches,
        double MeasuredSpacingInches,
        double OverlapInches,
        double Score);
}
