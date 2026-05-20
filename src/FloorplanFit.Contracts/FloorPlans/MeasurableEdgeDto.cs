namespace FloorplanFit.Contracts.FloorPlans;

public sealed record MeasurableEdgeDto(
    string EdgeKey,
    string SourceArtifactKind,
    Guid SourceArtifactId,
    Guid GeometryPathId,
    decimal LengthSourceUnits,
    decimal LengthMillimeters,
    decimal OrientationDegrees,
    decimal StartAnchorX,
    decimal StartAnchorY,
    decimal EndAnchorX,
    decimal EndAnchorY);
