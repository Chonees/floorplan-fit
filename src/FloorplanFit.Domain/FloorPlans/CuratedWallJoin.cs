namespace FloorplanFit.Domain.FloorPlans;

public sealed class CuratedWallJoin
{
    public CuratedWallJoin(
        Guid id,
        Guid floorPlanCurationId,
        Guid wallAId,
        Guid wallBId,
        string junctionType,
        decimal junctionX,
        decimal junctionY)
    {
        if (string.IsNullOrWhiteSpace(junctionType))
        {
            throw new ArgumentException("Junction type is required.", nameof(junctionType));
        }

        Id = id;
        FloorPlanCurationId = floorPlanCurationId;
        WallAId = wallAId;
        WallBId = wallBId;
        JunctionType = junctionType;
        JunctionX = junctionX;
        JunctionY = junctionY;
    }

    public Guid Id { get; }

    public Guid FloorPlanCurationId { get; }

    public Guid WallAId { get; }

    public Guid WallBId { get; }

    public string JunctionType { get; }

    public decimal JunctionX { get; }

    public decimal JunctionY { get; }
}
