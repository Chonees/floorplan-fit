namespace FloorplanFit.Contracts.FloorPlans;

public sealed record GeometrySegmentDto(
    Guid GeometryPathId,
    int SortOrder,
    decimal StartX,
    decimal StartY,
    decimal EndX,
    decimal EndY)
{
    public string GeometrySegmentKey =>
        $"{GeometryPathId:N}:{SortOrder}:{StartX:0.###}:{StartY:0.###}:{EndX:0.###}:{EndY:0.###}";
}
