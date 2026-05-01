namespace FloorplanFit.Contracts.FloorPlans;

public sealed record FloorPlanReviewSessionDto(
    Guid TemplateId,
    string Code,
    string Name,
    string Status,
    int ActiveVersionNumber,
    Guid? ActivePublishedCurationId,
    IReadOnlyList<GeometryPathDto> GeometryPaths,
    IReadOnlyList<WallCandidateDto> WallCandidates,
    IReadOnlyList<CuratedWallDto> CuratedWalls);
