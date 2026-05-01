namespace FloorplanFit.Contracts.FloorPlans;

public sealed record CuratedWallDto(
    Guid CuratedWallId,
    string StableWallId,
    Guid? SourceCandidateId,
    string WallRole,
    string MobilityLevel,
    string ProtectionLevel,
    decimal? ThicknessMm,
    string? AssemblyCode,
    decimal? HeightMm,
    bool IsExterior,
    bool IsStructuralHint,
    Guid? GeometryPathId,
    int SortOrder,
    string? Notes);
