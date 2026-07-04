namespace FloorplanFit.Contracts.FloorPlans;

public sealed record DimensionLineSegmentDto(
    decimal StartX,
    decimal StartY,
    decimal EndX,
    decimal EndY);
