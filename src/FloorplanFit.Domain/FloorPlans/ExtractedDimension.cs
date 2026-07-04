namespace FloorplanFit.Domain.FloorPlans;

public sealed class ExtractedDimension
{
    public ExtractedDimension(
        Guid id,
        Guid wallExtractionRunId,
        string sourceEntityRef,
        string? sourceLayer,
        string sourceEntityKind,
        string? geometryBlockName,
        string displayText,
        string displayTextSource,
        string rawTextOverride,
        decimal measurementSourceUnits,
        decimal measurementMillimeters,
        string sourceUnit,
        int dimType,
        decimal angle,
        decimal obliqueAngle,
        decimal defPointX,
        decimal defPointY,
        decimal defPointZ,
        decimal defPoint2X,
        decimal defPoint2Y,
        decimal defPoint2Z,
        decimal defPoint3X,
        decimal defPoint3Y,
        decimal defPoint3Z,
        decimal confidence,
        string? detectionNotes,
        int sortOrder,
        decimal? renderTextX = null,
        decimal? renderTextY = null,
        decimal? renderTextHeight = null,
        decimal? renderTextRotationDegrees = null,
        string? renderTextStyleName = null,
        string? renderTextHorizontalAlignment = null,
        string? renderTextVerticalAlignment = null,
        string? renderTextAttachmentPoint = null,
        IReadOnlyList<ExtractedDimensionLineSegment>? lineSegments = null,
        string? sourceHandle = null,
        IReadOnlyList<ExtractedDimensionLinePrimitive>? linePrimitives = null,
        IReadOnlyList<ExtractedDimensionTextPrimitive>? textPrimitives = null,
        IReadOnlyList<ExtractedDimensionInsertPrimitive>? insertPrimitives = null,
        IReadOnlyList<ExtractedDimensionCirclePrimitive>? circlePrimitives = null,
        IReadOnlyList<ExtractedDimensionArcPrimitive>? arcPrimitives = null,
        IReadOnlyList<ExtractedDimensionSolidPrimitive>? solidPrimitives = null)
    {
        if (string.IsNullOrWhiteSpace(sourceEntityRef))
        {
            throw new ArgumentException("Source entity reference is required.", nameof(sourceEntityRef));
        }

        if (string.IsNullOrWhiteSpace(sourceEntityKind))
        {
            throw new ArgumentException("Source entity kind is required.", nameof(sourceEntityKind));
        }

        if (string.IsNullOrWhiteSpace(displayText))
        {
            throw new ArgumentException("Display text is required.", nameof(displayText));
        }

        if (string.IsNullOrWhiteSpace(displayTextSource))
        {
            throw new ArgumentException("Display text source is required.", nameof(displayTextSource));
        }

        if (string.IsNullOrWhiteSpace(sourceUnit))
        {
            throw new ArgumentException("Source unit is required.", nameof(sourceUnit));
        }

        if (confidence < 0m || confidence > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be between 0 and 1.");
        }

        if (sortOrder <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder), "Sort order must be positive.");
        }

        Id = id;
        WallExtractionRunId = wallExtractionRunId;
        SourceEntityRef = sourceEntityRef;
        SourceLayer = sourceLayer;
        SourceEntityKind = sourceEntityKind;
        GeometryBlockName = geometryBlockName;
        DisplayText = displayText.Trim();
        DisplayTextSource = displayTextSource.Trim();
        RawTextOverride = rawTextOverride.Trim();
        MeasurementSourceUnits = measurementSourceUnits;
        MeasurementMillimeters = measurementMillimeters;
        SourceUnit = sourceUnit.Trim();
        DimType = dimType;
        Angle = angle;
        ObliqueAngle = obliqueAngle;
        DefPointX = defPointX;
        DefPointY = defPointY;
        DefPointZ = defPointZ;
        DefPoint2X = defPoint2X;
        DefPoint2Y = defPoint2Y;
        DefPoint2Z = defPoint2Z;
        DefPoint3X = defPoint3X;
        DefPoint3Y = defPoint3Y;
        DefPoint3Z = defPoint3Z;
        Confidence = confidence;
        DetectionNotes = detectionNotes;
        SortOrder = sortOrder;
        RenderTextX = renderTextX;
        RenderTextY = renderTextY;
        RenderTextHeight = renderTextHeight;
        RenderTextRotationDegrees = renderTextRotationDegrees;
        RenderTextStyleName = renderTextStyleName;
        RenderTextHorizontalAlignment = renderTextHorizontalAlignment;
        RenderTextVerticalAlignment = renderTextVerticalAlignment;
        RenderTextAttachmentPoint = renderTextAttachmentPoint;
        LineSegments = lineSegments ?? [];
        SourceHandle = sourceHandle;
        LinePrimitives = linePrimitives ?? [];
        TextPrimitives = textPrimitives ?? [];
        InsertPrimitives = insertPrimitives ?? [];
        CirclePrimitives = circlePrimitives ?? [];
        ArcPrimitives = arcPrimitives ?? [];
        SolidPrimitives = solidPrimitives ?? [];
    }

    public Guid Id { get; }

    public Guid WallExtractionRunId { get; }

    public string SourceEntityRef { get; }

    public string? SourceLayer { get; }

    public string SourceEntityKind { get; }

    public string? GeometryBlockName { get; }

    public string DisplayText { get; }

    public string DisplayTextSource { get; }

    public string RawTextOverride { get; }

    public decimal MeasurementSourceUnits { get; }

    public decimal MeasurementMillimeters { get; }

    public string SourceUnit { get; }

    public int DimType { get; }

    public decimal Angle { get; }

    public decimal ObliqueAngle { get; }

    public decimal DefPointX { get; }

    public decimal DefPointY { get; }

    public decimal DefPointZ { get; }

    public decimal DefPoint2X { get; }

    public decimal DefPoint2Y { get; }

    public decimal DefPoint2Z { get; }

    public decimal DefPoint3X { get; }

    public decimal DefPoint3Y { get; }

    public decimal DefPoint3Z { get; }

    public decimal Confidence { get; }

    public string? DetectionNotes { get; }

    public int SortOrder { get; }

    public decimal? RenderTextX { get; }

    public decimal? RenderTextY { get; }

    public decimal? RenderTextHeight { get; }

    public decimal? RenderTextRotationDegrees { get; }

    public string? RenderTextStyleName { get; }

    public string? RenderTextHorizontalAlignment { get; }

    public string? RenderTextVerticalAlignment { get; }

    public string? RenderTextAttachmentPoint { get; }

    public IReadOnlyList<ExtractedDimensionLineSegment> LineSegments { get; }

    public string? SourceHandle { get; }

    public IReadOnlyList<ExtractedDimensionLinePrimitive> LinePrimitives { get; }

    public IReadOnlyList<ExtractedDimensionTextPrimitive> TextPrimitives { get; }

    public IReadOnlyList<ExtractedDimensionInsertPrimitive> InsertPrimitives { get; }

    public IReadOnlyList<ExtractedDimensionCirclePrimitive> CirclePrimitives { get; }

    public IReadOnlyList<ExtractedDimensionArcPrimitive> ArcPrimitives { get; }

    public IReadOnlyList<ExtractedDimensionSolidPrimitive> SolidPrimitives { get; }
}
