namespace FloorplanFit.Domain.FloorPlans;

public sealed class PinchMarker
{
    public PinchMarker(
        Guid id,
        Guid floorPlanCurationId,
        Guid pinchGroupId,
        Guid sourceCandidateId,
        Guid geometryPathId,
        decimal positionRatio,
        decimal maxTrimMm,
        int sortOrder)
    {
        if (positionRatio < 0m || positionRatio > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(positionRatio), "Position ratio must be between 0 and 1.");
        }

        if (maxTrimMm <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(maxTrimMm), "Max trim must be positive.");
        }

        if (sortOrder <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder), "Sort order must be positive.");
        }

        Id = id;
        FloorPlanCurationId = floorPlanCurationId;
        PinchGroupId = pinchGroupId;
        SourceCandidateId = sourceCandidateId;
        GeometryPathId = geometryPathId;
        PositionRatio = positionRatio;
        MaxTrimMm = maxTrimMm;
        SortOrder = sortOrder;
    }

    public Guid Id { get; }

    public Guid FloorPlanCurationId { get; }

    public Guid PinchGroupId { get; }

    public Guid SourceCandidateId { get; }

    public Guid GeometryPathId { get; }

    public decimal PositionRatio { get; }

    public decimal MaxTrimMm { get; private set; }

    public int SortOrder { get; }

    public void UpdateMaxTrim(decimal maxTrimMm)
    {
        if (maxTrimMm <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(maxTrimMm), "Max trim must be positive.");
        }

        MaxTrimMm = maxTrimMm;
    }
}
