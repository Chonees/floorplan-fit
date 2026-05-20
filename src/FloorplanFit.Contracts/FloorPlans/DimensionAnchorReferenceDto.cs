namespace FloorplanFit.Contracts.FloorPlans;

public sealed record DimensionAnchorReferenceDto(
    string EdgeKey,
    string SourceArtifactKind,
    Guid SourceArtifactId,
    Guid GeometryPathId,
    string EdgeAnchorKind,
    decimal AnchorX,
    decimal AnchorY,
    decimal DistanceSourceUnits)
{
    public decimal? SegmentRatio { get; init; }
}
