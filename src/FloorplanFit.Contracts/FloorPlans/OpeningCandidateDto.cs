namespace FloorplanFit.Contracts.FloorPlans;

public sealed record OpeningCandidateDto(
    Guid OpeningCandidateId,
    string SourceEntityRef,
    string SourceLayer,
    string Kind,
    string SourceEntityKind,
    Guid? GeometryPathId,
    decimal Confidence,
    string? DetectionNotes,
    int SortOrder);
