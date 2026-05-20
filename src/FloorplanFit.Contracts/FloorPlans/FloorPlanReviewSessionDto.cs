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
    IReadOnlyList<PinchMarkerDto> PinchMarkers,
    IReadOnlyList<CuratedPlanArtifactDto> CuratedPlanArtifacts)
{
    public IReadOnlyList<DimensionDto> Dimensions { get; init; } = [];

    public MeasurementContextDto? MeasurementContext { get; init; }

    public IReadOnlyList<MeasurableEdgeDto> MeasurableEdges { get; init; } = [];

    public IReadOnlyList<DimensionBindingDto> DimensionBindings { get; init; } = [];

    public IReadOnlyList<DimensionAssociationDto> DimensionAssociations { get; init; } = [];

    public IReadOnlyList<MeasurementCorridorDto> MeasurementCorridors { get; init; } = [];

    public IReadOnlyList<MeasurementNodeDto> MeasurementNodes { get; init; } = [];

    public IReadOnlyList<DimensionIntervalBindingDto> DimensionIntervalBindings { get; init; } = [];

    public IReadOnlyList<ArticulationBandDto> ArticulationBands { get; init; } = [];

    public FloorPlanReviewSessionDto(
        Guid templateId,
        string code,
        string name,
        string status,
        int activeVersionNumber,
        Guid? activePublishedCurationId,
        IReadOnlyList<GeometryPathDto> geometryPaths,
        IReadOnlyList<RoomLabelDto> roomLabels,
        IReadOnlyList<OpeningCandidateDto> openingCandidates,
        IReadOnlyList<OpeningLabelDto> openingLabels,
        IReadOnlyList<FixedPlanComponentDto> fixedPlanComponents,
        IReadOnlyList<ProtectedDetailAssemblyDto> protectedDetailAssemblies,
        IReadOnlyList<WallCandidateDto> wallCandidates,
        IReadOnlyList<PinchGroupDto> pinchGroups,
        IReadOnlyList<PinchMarkerDto> pinchMarkers)
        : this(
            templateId,
            code,
            name,
            status,
            activeVersionNumber,
            activePublishedCurationId,
            geometryPaths,
            roomLabels,
            openingCandidates,
            openingLabels,
            fixedPlanComponents,
            protectedDetailAssemblies,
            wallCandidates,
            pinchGroups,
            pinchMarkers,
            [])
    {
    }
}
