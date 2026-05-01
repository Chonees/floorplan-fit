using FloorplanFit.Application.Abstractions;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;

namespace FloorplanFit.Infrastructure.Dxf;

public sealed class IxMiliaWallExtractor : IWallExtractor
{
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

            if (!IsWallLayer(line.Layer))
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

            if (!IsWallLayer(polyline.Layer))
            {
                continue;
            }

            polylineIndex++;
            AddPolylineSegments(candidates, polyline, polylineIndex);
        }

        return Task.FromResult<IReadOnlyList<DetectedWallCandidate>>(candidates);
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

    private static bool IsWallLayer(string? layerName)
    {
        return !string.IsNullOrWhiteSpace(layerName)
            && layerName.Contains("WALL", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildSourceEntityRef(string entityType, int entityIndex, int? segmentIndex = null)
    {
        var stablePart = entityIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);

        return segmentIndex is null
            ? $"{entityType}:{stablePart}"
            : $"{entityType}:{stablePart}:{segmentIndex.Value}";
    }
}
