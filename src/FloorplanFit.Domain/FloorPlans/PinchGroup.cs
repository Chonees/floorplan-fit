namespace FloorplanFit.Domain.FloorPlans;

public sealed class PinchGroup
{
    public PinchGroup(
        Guid id,
        Guid floorPlanCurationId,
        string name,
        PinchAxisTag axisTag,
        int sortOrder,
        string? closingEdge = null)
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
        ClosingEdge = NormalizeClosingEdge(axisTag, closingEdge);
    }

    public Guid Id { get; }

    public Guid FloorPlanCurationId { get; }

    public string Name { get; }

    public PinchAxisTag AxisTag { get; }

    public int SortOrder { get; }

    // Null until the operator commissions this group; the closing edge is chosen per axis, so a
    // width group closes Left or Right and a height group closes Top or Bottom.
    public string? ClosingEdge { get; }

    private static string? NormalizeClosingEdge(PinchAxisTag axisTag, string? closingEdge)
    {
        if (string.IsNullOrWhiteSpace(closingEdge))
        {
            return null;
        }

        var value = closingEdge.Trim();
        if (axisTag == PinchAxisTag.Width)
        {
            if (value.Equals("Left", StringComparison.OrdinalIgnoreCase))
            {
                return "Left";
            }

            if (value.Equals("Right", StringComparison.OrdinalIgnoreCase))
            {
                return "Right";
            }

            throw new ArgumentException("Width pinch group closing edge must be Left or Right.", nameof(closingEdge));
        }

        if (value.Equals("Top", StringComparison.OrdinalIgnoreCase))
        {
            return "Top";
        }

        if (value.Equals("Bottom", StringComparison.OrdinalIgnoreCase))
        {
            return "Bottom";
        }

        throw new ArgumentException("Height pinch group closing edge must be Top or Bottom.", nameof(closingEdge));
    }
}
