namespace FloorplanFit.Application.Abstractions;

public sealed record DetectedWallCandidate(
    string SourceEntityRef,
    string SourceLayer,
    IReadOnlyList<GeometryPoint> Points,
    decimal? ThicknessMm,
    decimal Confidence,
    string? DetectionNotes);
