namespace FloorplanFit.Domain.FloorPlans;

public sealed class FloorPlanLabelOverride
{
    private FloorPlanLabelOverride(
        Guid floorPlanCurationId,
        string sourceArtifactKind,
        Guid sourceArtifactId,
        decimal? resolvedTextHeight,
        DateTime updatedAtUtc)
    {
        if (!FloorPlanLabelOverrideSourceKinds.IsSupported(sourceArtifactKind))
        {
            throw new ArgumentException("Unsupported label override source kind.", nameof(sourceArtifactKind));
        }

        if (resolvedTextHeight is <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(resolvedTextHeight), "Resolved text height must be positive when provided.");
        }

        FloorPlanCurationId = floorPlanCurationId;
        SourceArtifactKind = sourceArtifactKind;
        SourceArtifactId = sourceArtifactId;
        ResolvedTextHeight = resolvedTextHeight;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid FloorPlanCurationId { get; }

    public string SourceArtifactKind { get; }

    public Guid SourceArtifactId { get; }

    public decimal? ResolvedTextHeight { get; }

    public DateTime UpdatedAtUtc { get; }

    public static FloorPlanLabelOverride CreateResolvedTextHeight(
        Guid floorPlanCurationId,
        string sourceArtifactKind,
        Guid sourceArtifactId,
        decimal resolvedTextHeight,
        DateTime updatedAtUtc)
        => new(
            floorPlanCurationId,
            sourceArtifactKind,
            sourceArtifactId,
            resolvedTextHeight,
            updatedAtUtc);

    public static FloorPlanLabelOverride CreateDetectedDefault(
        Guid floorPlanCurationId,
        string sourceArtifactKind,
        Guid sourceArtifactId,
        DateTime updatedAtUtc)
        => new(
            floorPlanCurationId,
            sourceArtifactKind,
            sourceArtifactId,
            null,
            updatedAtUtc);
}
