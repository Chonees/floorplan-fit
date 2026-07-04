namespace FloorplanFit.Application.Abstractions;

public sealed record DetectedOpeningCandidate(
    string SourceEntityRef,
    string SourceLayer,
    string Kind,
    string SourceEntityKind,
    IReadOnlyList<GeometryPoint> Points,
    decimal Confidence,
    string? DetectionNotes);
