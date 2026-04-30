namespace FloorplanFit.Domain.FloorPlans;

public sealed class CuratedWallGroup
{
    public CuratedWallGroup(
        Guid id,
        Guid floorPlanCurationId,
        string groupCode,
        string name,
        string groupType,
        int priority)
    {
        if (string.IsNullOrWhiteSpace(groupCode))
        {
            throw new ArgumentException("Group code is required.", nameof(groupCode));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Group name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(groupType))
        {
            throw new ArgumentException("Group type is required.", nameof(groupType));
        }

        Id = id;
        FloorPlanCurationId = floorPlanCurationId;
        GroupCode = groupCode;
        Name = name;
        GroupType = groupType;
        Priority = priority;
    }

    public Guid Id { get; }

    public Guid FloorPlanCurationId { get; }

    public string GroupCode { get; }

    public string Name { get; }

    public string GroupType { get; }

    public int Priority { get; }
}
