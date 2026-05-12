namespace FloorplanFit.Domain.FloorPlans;

public sealed class FloorPlanArtifactPosition
{
    private FloorPlanArtifactPosition(
        Guid floorPlanCurationId,
        string sourceArtifactKind,
        Guid sourceArtifactId,
        FloorPlanArtifactPositionMode positionMode,
        decimal? resolvedX,
        decimal? resolvedY,
        decimal? translationDx,
        decimal? translationDy,
        DateTime updatedAtUtc)
    {
        if (!FloorPlanArtifactPositionSourceKinds.IsSupported(sourceArtifactKind))
        {
            throw new ArgumentException("Unsupported source artifact kind.", nameof(sourceArtifactKind));
        }

        if (positionMode == FloorPlanArtifactPositionMode.AbsolutePoint &&
            !FloorPlanArtifactPositionSourceKinds.SupportsAbsolutePoint(sourceArtifactKind))
        {
            throw new ArgumentException("AbsolutePoint mode is only valid for label artifacts.", nameof(sourceArtifactKind));
        }

        if (positionMode == FloorPlanArtifactPositionMode.Translation &&
            !FloorPlanArtifactPositionSourceKinds.SupportsTranslation(sourceArtifactKind))
        {
            throw new ArgumentException("Translation mode is only valid for movable geometry artifacts.", nameof(sourceArtifactKind));
        }

        if (positionMode == FloorPlanArtifactPositionMode.AbsolutePoint &&
            (!resolvedX.HasValue || !resolvedY.HasValue))
        {
            throw new ArgumentException("AbsolutePoint mode requires resolved X/Y coordinates.");
        }

        if (positionMode == FloorPlanArtifactPositionMode.Translation &&
            (!translationDx.HasValue || !translationDy.HasValue))
        {
            throw new ArgumentException("Translation mode requires translation dx/dy.");
        }

        FloorPlanCurationId = floorPlanCurationId;
        SourceArtifactKind = sourceArtifactKind;
        SourceArtifactId = sourceArtifactId;
        PositionMode = positionMode;
        ResolvedX = resolvedX;
        ResolvedY = resolvedY;
        TranslationDx = translationDx;
        TranslationDy = translationDy;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid FloorPlanCurationId { get; }

    public string SourceArtifactKind { get; }

    public Guid SourceArtifactId { get; }

    public FloorPlanArtifactPositionMode PositionMode { get; }

    public decimal? ResolvedX { get; }

    public decimal? ResolvedY { get; }

    public decimal? TranslationDx { get; }

    public decimal? TranslationDy { get; }

    public DateTime UpdatedAtUtc { get; }

    public static FloorPlanArtifactPosition CreateAbsolutePoint(
        Guid floorPlanCurationId,
        string sourceArtifactKind,
        Guid sourceArtifactId,
        decimal resolvedX,
        decimal resolvedY,
        DateTime updatedAtUtc)
    {
        return new FloorPlanArtifactPosition(
            floorPlanCurationId,
            sourceArtifactKind,
            sourceArtifactId,
            FloorPlanArtifactPositionMode.AbsolutePoint,
            resolvedX,
            resolvedY,
            null,
            null,
            updatedAtUtc);
    }

    public static FloorPlanArtifactPosition CreateTranslation(
        Guid floorPlanCurationId,
        string sourceArtifactKind,
        Guid sourceArtifactId,
        decimal translationDx,
        decimal translationDy,
        DateTime updatedAtUtc)
    {
        return new FloorPlanArtifactPosition(
            floorPlanCurationId,
            sourceArtifactKind,
            sourceArtifactId,
            FloorPlanArtifactPositionMode.Translation,
            null,
            null,
            translationDx,
            translationDy,
            updatedAtUtc);
    }
}
