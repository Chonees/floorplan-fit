namespace FloorplanFit.Application.Abstractions;

public sealed record DetectedDimension(
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
    string? DetectionNotes)
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

    public IReadOnlyList<DetectedDimensionLineSegment> LineSegments { get; init; } = [];

    public IReadOnlyList<DetectedDimensionLinePrimitive> LinePrimitives { get; init; } = [];

    public IReadOnlyList<DetectedDimensionTextPrimitive> TextPrimitives { get; init; } = [];

    public IReadOnlyList<DetectedDimensionInsertPrimitive> InsertPrimitives { get; init; } = [];

    public IReadOnlyList<DetectedDimensionCirclePrimitive> CirclePrimitives { get; init; } = [];

    public IReadOnlyList<DetectedDimensionArcPrimitive> ArcPrimitives { get; init; } = [];

    public IReadOnlyList<DetectedDimensionSolidPrimitive> SolidPrimitives { get; init; } = [];
}
