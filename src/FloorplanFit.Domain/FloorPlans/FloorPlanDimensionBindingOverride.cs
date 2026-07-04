namespace FloorplanFit.Domain.FloorPlans;

public sealed class FloorPlanDimensionBindingOverride
{
    private FloorPlanDimensionBindingOverride(
        Guid floorPlanCurationId,
        string sourceDimensionKey,
        string bindingKind,
        bool isResolved,
        decimal confidence,
        string notes,
        FloorPlanDimensionMeasuredSpanOverride? measuredSpan,
        IReadOnlyList<FloorPlanDimensionBindingAnchorOverride> anchors,
        DateTime updatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(sourceDimensionKey))
        {
            throw new ArgumentException("Source dimension key is required.", nameof(sourceDimensionKey));
        }

        if (string.IsNullOrWhiteSpace(bindingKind))
        {
            throw new ArgumentException("Binding kind is required.", nameof(bindingKind));
        }

        if (confidence < 0m || confidence > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be between 0 and 1.");
        }

        FloorPlanCurationId = floorPlanCurationId;
        SourceDimensionKey = sourceDimensionKey;
        BindingKind = bindingKind.Trim();
        IsResolved = isResolved;
        Confidence = confidence;
        Notes = notes ?? string.Empty;
        MeasuredSpan = measuredSpan;
        Anchors = anchors ?? [];
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid FloorPlanCurationId { get; }

    public string SourceDimensionKey { get; }

    public string BindingKind { get; }

    public bool IsResolved { get; }

    public decimal Confidence { get; }

    public string Notes { get; }

    public FloorPlanDimensionMeasuredSpanOverride? MeasuredSpan { get; }

    public IReadOnlyList<FloorPlanDimensionBindingAnchorOverride> Anchors { get; }

    public DateTime UpdatedAtUtc { get; }

    public static FloorPlanDimensionBindingOverride CreateManualOverride(
        Guid floorPlanCurationId,
        string sourceDimensionKey,
        string bindingKind,
        bool isResolved,
        decimal confidence,
        string notes,
        FloorPlanDimensionMeasuredSpanOverride? measuredSpan,
        IReadOnlyList<FloorPlanDimensionBindingAnchorOverride> anchors,
        DateTime updatedAtUtc)
        => new(
            floorPlanCurationId,
            sourceDimensionKey,
            bindingKind,
            isResolved,
            confidence,
            notes,
            measuredSpan,
            anchors,
            updatedAtUtc);
}

public sealed class FloorPlanDimensionMeasuredSpanOverride
{
    public FloorPlanDimensionMeasuredSpanOverride(
        string axisTag,
        decimal startCoordinate,
        decimal endCoordinate,
        decimal orientationDegrees)
    {
        if (string.IsNullOrWhiteSpace(axisTag))
        {
            throw new ArgumentException("Axis tag is required.", nameof(axisTag));
        }

        AxisTag = axisTag.Trim();
        StartCoordinate = startCoordinate;
        EndCoordinate = endCoordinate;
        OrientationDegrees = orientationDegrees;
    }

    public string AxisTag { get; }

    public decimal StartCoordinate { get; }

    public decimal EndCoordinate { get; }

    public decimal OrientationDegrees { get; }
}

public sealed class FloorPlanDimensionBindingAnchorOverride
{
    public FloorPlanDimensionBindingAnchorOverride(
        int sortOrder,
        string edgeKey,
        string sourceArtifactKind,
        Guid sourceArtifactId,
        Guid geometryPathId,
        string edgeAnchorKind,
        decimal anchorX,
        decimal anchorY,
        decimal distanceSourceUnits,
        decimal? segmentRatio)
    {
        if (sortOrder <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder), "Sort order must be positive.");
        }

        if (string.IsNullOrWhiteSpace(edgeKey))
        {
            throw new ArgumentException("Edge key is required.", nameof(edgeKey));
        }

        if (string.IsNullOrWhiteSpace(sourceArtifactKind))
        {
            throw new ArgumentException("Source artifact kind is required.", nameof(sourceArtifactKind));
        }

        if (string.IsNullOrWhiteSpace(edgeAnchorKind))
        {
            throw new ArgumentException("Edge anchor kind is required.", nameof(edgeAnchorKind));
        }

        SortOrder = sortOrder;
        EdgeKey = edgeKey.Trim();
        SourceArtifactKind = sourceArtifactKind.Trim();
        SourceArtifactId = sourceArtifactId;
        GeometryPathId = geometryPathId;
        EdgeAnchorKind = edgeAnchorKind.Trim();
        AnchorX = anchorX;
        AnchorY = anchorY;
        DistanceSourceUnits = distanceSourceUnits;
        SegmentRatio = segmentRatio;
    }

    public int SortOrder { get; }

    public string EdgeKey { get; }

    public string SourceArtifactKind { get; }

    public Guid SourceArtifactId { get; }

    public Guid GeometryPathId { get; }

    public string EdgeAnchorKind { get; }

    public decimal AnchorX { get; }

    public decimal AnchorY { get; }

    public decimal DistanceSourceUnits { get; }

    public decimal? SegmentRatio { get; }
}
