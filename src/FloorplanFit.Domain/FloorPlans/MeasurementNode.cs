namespace FloorplanFit.Domain.FloorPlans;

public sealed class MeasurementNode
{
    public MeasurementNode(
        Guid id,
        Guid floorPlanCurationId,
        Guid corridorId,
        int sortOrder,
        string referenceKind,
        string sourceArtifactKind,
        Guid sourceArtifactId,
        Guid geometryPathId,
        string snapKind,
        decimal anchorX,
        decimal anchorY,
        decimal axisCoordinate,
        decimal offsetAlongAxis,
        decimal offsetNormal,
        decimal positionRatio)
    {
        if (corridorId == Guid.Empty)
        {
            throw new ArgumentException("Corridor id is required.", nameof(corridorId));
        }

        if (sortOrder <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder), "Sort order must be positive.");
        }

        if (string.IsNullOrWhiteSpace(referenceKind))
        {
            throw new ArgumentException("Reference kind is required.", nameof(referenceKind));
        }

        if (string.IsNullOrWhiteSpace(sourceArtifactKind))
        {
            throw new ArgumentException("Source artifact kind is required.", nameof(sourceArtifactKind));
        }

        if (sourceArtifactId == Guid.Empty)
        {
            throw new ArgumentException("Source artifact id is required.", nameof(sourceArtifactId));
        }

        if (geometryPathId == Guid.Empty)
        {
            throw new ArgumentException("Geometry path id is required.", nameof(geometryPathId));
        }

        if (string.IsNullOrWhiteSpace(snapKind))
        {
            throw new ArgumentException("Snap kind is required.", nameof(snapKind));
        }

        if (positionRatio < 0m || positionRatio > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(positionRatio), "Position ratio must be between 0 and 1.");
        }

        Id = id;
        FloorPlanCurationId = floorPlanCurationId;
        CorridorId = corridorId;
        SortOrder = sortOrder;
        ReferenceKind = referenceKind.Trim();
        SourceArtifactKind = sourceArtifactKind.Trim();
        SourceArtifactId = sourceArtifactId;
        GeometryPathId = geometryPathId;
        SnapKind = snapKind.Trim();
        AnchorX = anchorX;
        AnchorY = anchorY;
        AxisCoordinate = axisCoordinate;
        OffsetAlongAxis = offsetAlongAxis;
        OffsetNormal = offsetNormal;
        PositionRatio = positionRatio;
    }

    public Guid Id { get; }

    public Guid FloorPlanCurationId { get; }

    public Guid CorridorId { get; }

    public int SortOrder { get; }

    public string ReferenceKind { get; }

    public string SourceArtifactKind { get; }

    public Guid SourceArtifactId { get; }

    public Guid GeometryPathId { get; }

    public string SnapKind { get; }

    public decimal AnchorX { get; }

    public decimal AnchorY { get; }

    public decimal AxisCoordinate { get; }

    public decimal OffsetAlongAxis { get; }

    public decimal OffsetNormal { get; }

    public decimal PositionRatio { get; }
}
