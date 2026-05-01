namespace FloorplanFit.Contracts.FloorPlans;

public sealed record GeometrySegmentDto(
    Guid GeometryPathId,
    int SortOrder,
    decimal StartX,
    decimal StartY,
    decimal EndX,
    decimal EndY);
