namespace FloorplanFit.Domain.FloorPlans;

public sealed class PinchGroup
{
    public PinchGroup(
        Guid id,
        Guid floorPlanCurationId,
        string name,
        PinchAxisTag axisTag,
        int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Pinch group name is required.", nameof(name));
        }

        if (sortOrder <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder), "Sort order must be positive.");
        }

        Id = id;
        FloorPlanCurationId = floorPlanCurationId;
        Name = name.Trim();
        AxisTag = axisTag;
        SortOrder = sortOrder;
    }

    public Guid Id { get; }

    public Guid FloorPlanCurationId { get; }

    public string Name { get; }

    public PinchAxisTag AxisTag { get; }

    public int SortOrder { get; }
}
