namespace FloorplanFit.Domain.FloorPlans;

public sealed class ExtractedOpeningLabel
{
    public ExtractedOpeningLabel(
        Guid id,
        Guid wallExtractionRunId,
        string sourceEntityRef,
        string? sourceLayer,
        string kind,
        string text,
        decimal x,
        decimal y,
        decimal confidence,
        string? detectionNotes,
        int sortOrder,
        string? sourceEntityKind = null,
        decimal? textHeight = null,
        decimal rotationDegrees = 0m,
        string? textStyleName = null,
        string? horizontalAlignment = null,
        string? verticalAlignment = null,
        string? attachmentPoint = null,
        string? colorArgb = null)
    {
        if (string.IsNullOrWhiteSpace(sourceEntityRef))
        {
            throw new ArgumentException("Source entity reference is required.", nameof(sourceEntityRef));
        }

        if (string.IsNullOrWhiteSpace(kind))
        {
            throw new ArgumentException("Opening label kind is required.", nameof(kind));
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Opening label text is required.", nameof(text));
        }

        if (confidence < 0m || confidence > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be between 0 and 1.");
        }

        if (sortOrder <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder), "Sort order must be positive.");
        }

        if (textHeight is <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(textHeight), "Text height must be positive when provided.");
        }

        Id = id;
        WallExtractionRunId = wallExtractionRunId;
        SourceEntityRef = sourceEntityRef;
        SourceLayer = sourceLayer;
        Kind = kind;
        Text = text.Trim();
        X = x;
        Y = y;
        Confidence = confidence;
        DetectionNotes = detectionNotes;
        SortOrder = sortOrder;
        SourceEntityKind = sourceEntityKind;
        TextHeight = textHeight;
        RotationDegrees = rotationDegrees;
        TextStyleName = textStyleName;
        HorizontalAlignment = horizontalAlignment;
        VerticalAlignment = verticalAlignment;
        AttachmentPoint = attachmentPoint;
        ColorArgb = colorArgb;
    }

    public Guid Id { get; }

    public Guid WallExtractionRunId { get; }

    public string SourceEntityRef { get; }

    public string? SourceLayer { get; }

    public string Kind { get; }

    public string Text { get; }

    public decimal X { get; }

    public decimal Y { get; }

    public decimal Confidence { get; }

    public string? DetectionNotes { get; }

    public int SortOrder { get; }

    public string? SourceEntityKind { get; }

    public decimal? TextHeight { get; }

    public decimal RotationDegrees { get; }

    public string? TextStyleName { get; }

    public string? HorizontalAlignment { get; }

    public string? VerticalAlignment { get; }

    public string? AttachmentPoint { get; }

    public string? ColorArgb { get; }
}
