namespace FloorplanFit.Contracts.FloorPlans;

public sealed record ProtectedDetailAssemblyDto(
    Guid ProtectedDetailAssemblyId,
    string SourceEntityRef,
    string SourceLayer,
    string Kind,
    string SourceEntityKind,
    IReadOnlyList<Guid> GeometryPathIds,
    decimal Confidence,
    string? DetectionNotes,
    int SortOrder,
    string? ColorArgb = null);
