namespace FloorplanFit.Domain.FloorPlans;

public sealed class DimensionIntervalBinding
{
    public DimensionIntervalBinding(
        Guid floorPlanCurationId,
        Guid dimensionId,
        Guid corridorId,
        Guid startNodeId,
        Guid endNodeId,
        string bindingStatus,
        decimal intervalStartCoordinate,
        decimal intervalEndCoordinate,
        DateTime updatedAtUtc)
    {
        if (dimensionId == Guid.Empty)
        {
            throw new ArgumentException("Dimension id is required.", nameof(dimensionId));
        }

        if (corridorId == Guid.Empty)
        {
            throw new ArgumentException("Corridor id is required.", nameof(corridorId));
        }

        if (startNodeId == Guid.Empty || endNodeId == Guid.Empty)
        {
            throw new ArgumentException("Start and end nodes are required.");
        }

        if (string.IsNullOrWhiteSpace(bindingStatus))
        {
            throw new ArgumentException("Binding status is required.", nameof(bindingStatus));
        }

        FloorPlanCurationId = floorPlanCurationId;
        DimensionId = dimensionId;
        CorridorId = corridorId;
        StartNodeId = startNodeId;
        EndNodeId = endNodeId;
        BindingStatus = bindingStatus.Trim();
        IntervalStartCoordinate = intervalStartCoordinate;
        IntervalEndCoordinate = intervalEndCoordinate;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid FloorPlanCurationId { get; }

    public Guid DimensionId { get; }

    public Guid CorridorId { get; }

    public Guid StartNodeId { get; }

    public Guid EndNodeId { get; }

    public string BindingStatus { get; }

    public decimal IntervalStartCoordinate { get; }

    public decimal IntervalEndCoordinate { get; }

    public DateTime UpdatedAtUtc { get; }
}
