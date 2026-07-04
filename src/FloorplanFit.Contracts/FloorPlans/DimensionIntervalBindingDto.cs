namespace FloorplanFit.Contracts.FloorPlans;

public sealed record DimensionIntervalBindingDto(
    Guid DimensionId,
    Guid CorridorId,
    Guid StartNodeId,
    Guid EndNodeId,
    string BindingStatus,
    decimal IntervalStartCoordinate,
    decimal IntervalEndCoordinate)
{
    public string BindingKind { get; init; } = "LinearInterval";
}
