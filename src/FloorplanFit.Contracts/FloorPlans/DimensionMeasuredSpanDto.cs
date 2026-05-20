namespace FloorplanFit.Contracts.FloorPlans;

public sealed record DimensionMeasuredSpanDto(
    string AxisTag,
    decimal StartCoordinate,
    decimal EndCoordinate,
    decimal OrientationDegrees);
