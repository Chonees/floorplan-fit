namespace FloorplanFit.Contracts.FloorPlans;

public sealed record FloorPlanReviewSessionDto(
    Guid TemplateId,
    string Code,
    string Name,
    string Status,
    int ActiveVersionNumber,
    Guid? ActivePublishedCurationId,
    IReadOnlyList<GeometryPathDto> GeometryPaths,
    IReadOnlyList<RoomLabelDto> RoomLabels,
    IReadOnlyList<OpeningCandidateDto> OpeningCandidates,
    IReadOnlyList<OpeningLabelDto> OpeningLabels,
    IReadOnlyList<FixedPlanComponentDto> FixedPlanComponents,
    IReadOnlyList<ProtectedDetailAssemblyDto> ProtectedDetailAssemblies,
    IReadOnlyList<WallCandidateDto> WallCandidates,
    IReadOnlyList<PinchGroupDto> PinchGroups,
    IReadOnlyList<PinchMarkerDto> PinchMarkers);
