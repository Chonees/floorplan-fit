namespace FloorplanFit.Domain.FloorPlans;

public sealed class ExtractedOpeningCandidate
{
    public ExtractedOpeningCandidate(
        Guid id,
        Guid wallExtractionRunId,
        string sourceEntityRef,
        string? sourceLayer,
        string kind,
        string? sourceEntityKind,
        Guid? geometryPathId,
        decimal confidence,
        string? detectionNotes,
        int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(sourceEntityRef))
        {
            throw new ArgumentException("Source entity reference is required.", nameof(sourceEntityRef));
        }

        if (string.IsNullOrWhiteSpace(kind))
        {
            throw new ArgumentException("Opening kind is required.", nameof(kind));
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
        Kind = kind;
        SourceEntityKind = sourceEntityKind;
        GeometryPathId = geometryPathId;
        Confidence = confidence;
        DetectionNotes = detectionNotes;
        SortOrder = sortOrder;
    }

    public Guid Id { get; }

    public Guid WallExtractionRunId { get; }

    public string SourceEntityRef { get; }

    public string? SourceLayer { get; }

    public string Kind { get; }

    public string? SourceEntityKind { get; }

    public Guid? GeometryPathId { get; }

    public decimal Confidence { get; }

    public string? DetectionNotes { get; }

    public int SortOrder { get; }
}
