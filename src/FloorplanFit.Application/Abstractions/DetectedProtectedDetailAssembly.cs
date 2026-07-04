namespace FloorplanFit.Application.Abstractions;

public sealed record DetectedProtectedDetailAssembly(
    string SourceEntityRef,
    string SourceLayer,
    string Kind,
    string SourceEntityKind,
    IReadOnlyList<IReadOnlyList<GeometryPoint>> GeometryPaths,
    decimal Confidence,
    string? DetectionNotes,
    string? ColorArgb = null);
