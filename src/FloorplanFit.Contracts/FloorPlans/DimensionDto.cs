namespace FloorplanFit.Contracts.FloorPlans;

public sealed record DimensionDto(
    Guid DimensionId,
    string SourceEntityRef,
    string SourceLayer,
    string SourceEntityKind,
    string? GeometryBlockName,
    string DisplayText,
    string DisplayTextSource,
    string RawTextOverride,
    decimal MeasurementSourceUnits,
    decimal MeasurementMillimeters,
    string SourceUnit,
    int DimType,
    decimal Angle,
    decimal ObliqueAngle,
    decimal DefPointX,
    decimal DefPointY,
    decimal DefPointZ,
    decimal DefPoint2X,
    decimal DefPoint2Y,
    decimal DefPoint2Z,
    decimal DefPoint3X,
    decimal DefPoint3Y,
    decimal DefPoint3Z,
    decimal Confidence,
    string? DetectionNotes,
    int SortOrder)
{
    public string? SourceHandle { get; init; }

    public decimal? RenderTextX { get; init; }

    public decimal? RenderTextY { get; init; }

    public decimal? RenderTextHeight { get; init; }

    public decimal? RenderTextRotationDegrees { get; init; }

    public string? RenderTextStyleName { get; init; }

    public string? RenderTextHorizontalAlignment { get; init; }

    public string? RenderTextVerticalAlignment { get; init; }

    public string? RenderTextAttachmentPoint { get; init; }

    public IReadOnlyList<DimensionLineSegmentDto> LineSegments { get; init; } = [];

    public IReadOnlyList<DimensionLinePrimitiveDto> LinePrimitives { get; init; } = [];

    public IReadOnlyList<DimensionTextPrimitiveDto> TextPrimitives { get; init; } = [];

    public IReadOnlyList<DimensionInsertPrimitiveDto> InsertPrimitives { get; init; } = [];

    public IReadOnlyList<DimensionCirclePrimitiveDto> CirclePrimitives { get; init; } = [];

    public IReadOnlyList<DimensionArcPrimitiveDto> ArcPrimitives { get; init; } = [];

    public IReadOnlyList<DimensionSolidPrimitiveDto> SolidPrimitives { get; init; } = [];

    public bool IsEdited { get; init; }

    public bool IsDirty { get; init; }

    public DateTime? LastExportedAtUtc { get; init; }
}
