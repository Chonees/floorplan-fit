namespace FloorplanFit.Contracts.FloorPlans;

public sealed record MeasurementNodeDto(
    Guid NodeId,
    Guid CorridorId,
    int SortOrder,
    string ReferenceKind,
    string SourceArtifactKind,
    Guid SourceArtifactId,
    Guid GeometryPathId,
    string SnapKind,
    decimal AnchorX,
    decimal AnchorY,
    decimal AxisCoordinate,
    decimal OffsetAlongAxis,
    decimal OffsetNormal,
    decimal PositionRatio);
