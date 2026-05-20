namespace FloorplanFit.Contracts.FloorPlans;

public sealed record PinchMarkerDto(
    Guid PinchMarkerId,
    Guid PinchGroupId,
    string PinchGroupName,
    Guid SourceCandidateId,
    Guid GeometryPathId,
    string AxisTag,
    decimal PositionRatio,
    decimal MaxTrimMm,
    int SortOrder);
