namespace FloorplanFit.Contracts.FloorPlans;

public sealed record WallCandidateDto(
    Guid CandidateId,
    string SourceEntityRef,
    string SourceLayer,
    string Status,
    decimal Confidence,
    decimal? ThicknessMm,
    string? DetectionNotes,
    Guid? GeometryPathId,
    int SortOrder);
