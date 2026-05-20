namespace FloorplanFit.Domain.FloorPlans;

public sealed class MeasurementCorridor
{
    public MeasurementCorridor(
        Guid id,
        Guid floorPlanCurationId,
        string name,
        PinchAxisTag axisTag,
        Guid guideGeometryPathId,
        decimal bandMinCoordinate,
        decimal bandMaxCoordinate,
        string status,
        int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Measurement corridor name is required.", nameof(name));
        }

        if (guideGeometryPathId == Guid.Empty)
        {
            throw new ArgumentException("Guide geometry path id is required.", nameof(guideGeometryPathId));
        }

        if (sortOrder <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder), "Sort order must be positive.");
        }

        Id = id;
        FloorPlanCurationId = floorPlanCurationId;
        Name = name.Trim();
        AxisTag = axisTag;
        GuideGeometryPathId = guideGeometryPathId;
        BandMinCoordinate = decimal.Min(bandMinCoordinate, bandMaxCoordinate);
        BandMaxCoordinate = decimal.Max(bandMinCoordinate, bandMaxCoordinate);
        Status = string.IsNullOrWhiteSpace(status) ? "Draft" : status.Trim();
        SortOrder = sortOrder;
    }

    public Guid Id { get; }

    public Guid FloorPlanCurationId { get; }

    public string Name { get; }

    public PinchAxisTag AxisTag { get; }

    public Guid GuideGeometryPathId { get; }

    public decimal BandMinCoordinate { get; }

    public decimal BandMaxCoordinate { get; }

    public string Status { get; }

    public int SortOrder { get; }
}
