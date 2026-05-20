namespace FloorplanFit.Contracts.FloorPlans;

public sealed record GeometryPathDto(
    Guid Id,
    bool IsClosed,
    IReadOnlyList<GeometrySegmentDto> Segments);
