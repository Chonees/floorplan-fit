namespace FloorplanFit.Application.Abstractions;

public sealed record DetectedFixedPlanComponent(
    string SourceEntityRef,
    string SourceLayer,
    string Kind,
    string SourceEntityKind,
    string? SourceBlockName,
    IReadOnlyList<IReadOnlyList<GeometryPoint>> GeometryPaths,
    decimal Confidence,
    string? DetectionNotes,
    string? ColorArgb = null);
