namespace FloorplanFit.Domain.FloorPlans;

public sealed class CuratedSpace
{
    public CuratedSpace(
        Guid id,
        Guid floorPlanCurationId,
        string stableSpaceId,
        string name,
        SpaceType spaceType,
        Guid? geometryPathId,
        decimal areaSquareMeters,
        decimal? minWidthMm,
        decimal? minDepthMm,
        bool isProtected,
        string? functionalTagsJson,
        string? notes,
        int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(stableSpaceId))
        {
            throw new ArgumentException("Stable space id is required.", nameof(stableSpaceId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Space name is required.", nameof(name));
        }

        Id = id;
        FloorPlanCurationId = floorPlanCurationId;
        StableSpaceId = stableSpaceId;
        Name = name;
        SpaceType = spaceType;
        GeometryPathId = geometryPathId;
        AreaSquareMeters = areaSquareMeters;
        MinWidthMm = minWidthMm;
        MinDepthMm = minDepthMm;
        IsProtected = isProtected;
        FunctionalTagsJson = functionalTagsJson;
        Notes = notes;
        SortOrder = sortOrder;
    }

    public Guid Id { get; }

    public Guid FloorPlanCurationId { get; }

    public string StableSpaceId { get; }

    public string Name { get; }

    public SpaceType SpaceType { get; }

    public Guid? GeometryPathId { get; }

    public decimal AreaSquareMeters { get; }

    public decimal? MinWidthMm { get; }

    public decimal? MinDepthMm { get; }

    public bool IsProtected { get; }

    public string? FunctionalTagsJson { get; }

    public string? Notes { get; }

    public int SortOrder { get; }
}
