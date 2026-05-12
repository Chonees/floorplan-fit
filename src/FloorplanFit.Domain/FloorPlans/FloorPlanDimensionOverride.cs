namespace FloorplanFit.Domain.FloorPlans;

public sealed class FloorPlanDimensionOverride
{
    private FloorPlanDimensionOverride(
        Guid floorPlanCurationId,
        string sourceDimensionKey,
        string sourceEntityRef,
        string? sourceHandle,
        string displayText,
        decimal defPointX,
        decimal defPointY,
        decimal defPointZ,
        decimal defPoint2X,
        decimal defPoint2Y,
        decimal defPoint2Z,
        decimal defPoint3X,
        decimal defPoint3Y,
        decimal defPoint3Z,
        decimal? renderTextX,
        decimal? renderTextY,
        decimal? renderTextHeight,
        decimal? renderTextRotationDegrees,
        string? renderTextStyleName,
        string? renderTextHorizontalAlignment,
        string? renderTextVerticalAlignment,
        string? renderTextAttachmentPoint,
        IReadOnlyList<ExtractedDimensionLinePrimitive> linePrimitives,
        IReadOnlyList<ExtractedDimensionTextPrimitive> textPrimitives,
        IReadOnlyList<ExtractedDimensionInsertPrimitive> insertPrimitives,
        IReadOnlyList<ExtractedDimensionCirclePrimitive> circlePrimitives,
        IReadOnlyList<ExtractedDimensionArcPrimitive> arcPrimitives,
        IReadOnlyList<ExtractedDimensionSolidPrimitive> solidPrimitives,
        DateTime updatedAtUtc,
        DateTime? lastExportedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(sourceDimensionKey))
        {
            throw new ArgumentException("Source dimension key is required.", nameof(sourceDimensionKey));
        }

        if (string.IsNullOrWhiteSpace(sourceEntityRef))
        {
            throw new ArgumentException("Source entity reference is required.", nameof(sourceEntityRef));
        }

        FloorPlanCurationId = floorPlanCurationId;
        SourceDimensionKey = sourceDimensionKey;
        SourceEntityRef = sourceEntityRef;
        SourceHandle = sourceHandle;
        DisplayText = displayText ?? string.Empty;
        DefPointX = defPointX;
        DefPointY = defPointY;
        DefPointZ = defPointZ;
        DefPoint2X = defPoint2X;
        DefPoint2Y = defPoint2Y;
        DefPoint2Z = defPoint2Z;
        DefPoint3X = defPoint3X;
        DefPoint3Y = defPoint3Y;
        DefPoint3Z = defPoint3Z;
        RenderTextX = renderTextX;
        RenderTextY = renderTextY;
        RenderTextHeight = renderTextHeight;
        RenderTextRotationDegrees = renderTextRotationDegrees;
        RenderTextStyleName = renderTextStyleName;
        RenderTextHorizontalAlignment = renderTextHorizontalAlignment;
        RenderTextVerticalAlignment = renderTextVerticalAlignment;
        RenderTextAttachmentPoint = renderTextAttachmentPoint;
        LinePrimitives = linePrimitives;
        TextPrimitives = textPrimitives;
        InsertPrimitives = insertPrimitives;
        CirclePrimitives = circlePrimitives;
        ArcPrimitives = arcPrimitives;
        SolidPrimitives = solidPrimitives;
        UpdatedAtUtc = updatedAtUtc;
        LastExportedAtUtc = lastExportedAtUtc;
    }

    public Guid FloorPlanCurationId { get; }

    public string SourceDimensionKey { get; }

    public string SourceEntityRef { get; }

    public string? SourceHandle { get; }

    public string DisplayText { get; }

    public decimal DefPointX { get; }

    public decimal DefPointY { get; }

    public decimal DefPointZ { get; }

    public decimal DefPoint2X { get; }

    public decimal DefPoint2Y { get; }

    public decimal DefPoint2Z { get; }

    public decimal DefPoint3X { get; }

    public decimal DefPoint3Y { get; }

    public decimal DefPoint3Z { get; }

    public decimal? RenderTextX { get; }

    public decimal? RenderTextY { get; }

    public decimal? RenderTextHeight { get; }

    public decimal? RenderTextRotationDegrees { get; }

    public string? RenderTextStyleName { get; }

    public string? RenderTextHorizontalAlignment { get; }

    public string? RenderTextVerticalAlignment { get; }

    public string? RenderTextAttachmentPoint { get; }

    public IReadOnlyList<ExtractedDimensionLinePrimitive> LinePrimitives { get; }

    public IReadOnlyList<ExtractedDimensionTextPrimitive> TextPrimitives { get; }

    public IReadOnlyList<ExtractedDimensionInsertPrimitive> InsertPrimitives { get; }

    public IReadOnlyList<ExtractedDimensionCirclePrimitive> CirclePrimitives { get; }

    public IReadOnlyList<ExtractedDimensionArcPrimitive> ArcPrimitives { get; }

    public IReadOnlyList<ExtractedDimensionSolidPrimitive> SolidPrimitives { get; }

    public DateTime UpdatedAtUtc { get; }

    public DateTime? LastExportedAtUtc { get; }

    public bool IsDirty => LastExportedAtUtc is null || UpdatedAtUtc > LastExportedAtUtc.Value;

    public static FloorPlanDimensionOverride CreateManualSnapshot(
        Guid floorPlanCurationId,
        string sourceDimensionKey,
        string sourceEntityRef,
        string? sourceHandle,
        string displayText,
        decimal defPointX,
        decimal defPointY,
        decimal defPointZ,
        decimal defPoint2X,
        decimal defPoint2Y,
        decimal defPoint2Z,
        decimal defPoint3X,
        decimal defPoint3Y,
        decimal defPoint3Z,
        decimal? renderTextX,
        decimal? renderTextY,
        decimal? renderTextHeight,
        decimal? renderTextRotationDegrees,
        string? renderTextStyleName,
        string? renderTextHorizontalAlignment,
        string? renderTextVerticalAlignment,
        string? renderTextAttachmentPoint,
        IReadOnlyList<ExtractedDimensionLinePrimitive> linePrimitives,
        IReadOnlyList<ExtractedDimensionTextPrimitive> textPrimitives,
        IReadOnlyList<ExtractedDimensionInsertPrimitive> insertPrimitives,
        IReadOnlyList<ExtractedDimensionCirclePrimitive> circlePrimitives,
        IReadOnlyList<ExtractedDimensionArcPrimitive> arcPrimitives,
        IReadOnlyList<ExtractedDimensionSolidPrimitive> solidPrimitives,
        DateTime updatedAtUtc,
        DateTime? lastExportedAtUtc)
        => new(
            floorPlanCurationId,
            sourceDimensionKey,
            sourceEntityRef,
            sourceHandle,
            displayText,
            defPointX,
            defPointY,
            defPointZ,
            defPoint2X,
            defPoint2Y,
            defPoint2Z,
            defPoint3X,
            defPoint3Y,
            defPoint3Z,
            renderTextX,
            renderTextY,
            renderTextHeight,
            renderTextRotationDegrees,
            renderTextStyleName,
            renderTextHorizontalAlignment,
            renderTextVerticalAlignment,
            renderTextAttachmentPoint,
            linePrimitives,
            textPrimitives,
            insertPrimitives,
            circlePrimitives,
            arcPrimitives,
            solidPrimitives,
            updatedAtUtc,
            lastExportedAtUtc);

    public FloorPlanDimensionOverride MarkExported(DateTime exportedAtUtc)
        => new(
            FloorPlanCurationId,
            SourceDimensionKey,
            SourceEntityRef,
            SourceHandle,
            DisplayText,
            DefPointX,
            DefPointY,
            DefPointZ,
            DefPoint2X,
            DefPoint2Y,
            DefPoint2Z,
            DefPoint3X,
            DefPoint3Y,
            DefPoint3Z,
            RenderTextX,
            RenderTextY,
            RenderTextHeight,
            RenderTextRotationDegrees,
            RenderTextStyleName,
            RenderTextHorizontalAlignment,
            RenderTextVerticalAlignment,
            RenderTextAttachmentPoint,
            LinePrimitives,
            TextPrimitives,
            InsertPrimitives,
            CirclePrimitives,
            ArcPrimitives,
            SolidPrimitives,
            UpdatedAtUtc,
            exportedAtUtc);
}
