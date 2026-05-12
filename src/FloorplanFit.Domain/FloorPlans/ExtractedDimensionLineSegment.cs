namespace FloorplanFit.Domain.FloorPlans;

public sealed record ExtractedDimensionLineSegment(
    decimal StartX,
    decimal StartY,
    decimal EndX,
    decimal EndY);
