namespace FloorplanFit.Contracts.FloorPlans;

public sealed record FixedPlanComponentDto(
    Guid FixedPlanComponentId,
    string SourceEntityRef,
    string SourceLayer,
    string Kind,
    string SourceEntityKind,
    string? SourceBlockName,
    IReadOnlyList<Guid> GeometryPathIds,
    decimal Confidence,
    string? DetectionNotes,
    int SortOrder,
    string? ColorArgb = null);
