namespace FloorplanFit.Domain.FloorPlans;

public sealed class ExtractedWallCandidate
{
    public ExtractedWallCandidate(
        Guid id,
        Guid wallExtractionRunId,
        string sourceEntityRef,
        string? sourceLayer,
        Guid? geometryPathId,
        decimal? thicknessMm,
        decimal confidence,
        string? detectionNotes,
        ExtractedWallCandidateStatus status,
        int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(sourceEntityRef))
        {
            throw new ArgumentException("Source entity reference is required.", nameof(sourceEntityRef));
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
        GeometryPathId = geometryPathId;
        ThicknessMm = thicknessMm;
        Confidence = confidence;
        DetectionNotes = detectionNotes;
        Status = status;
        SortOrder = sortOrder;
    }

    public Guid Id { get; }

    public Guid WallExtractionRunId { get; }

    public string SourceEntityRef { get; }

    public string? SourceLayer { get; }

    public Guid? GeometryPathId { get; }

    public decimal? ThicknessMm { get; }

    public decimal Confidence { get; }

    public string? DetectionNotes { get; }

    public ExtractedWallCandidateStatus Status { get; private set; }

    public int SortOrder { get; }

    public void Accept()
    {
        if (Status != ExtractedWallCandidateStatus.Pending)
        {
            throw new InvalidOperationException("Only pending wall candidates can be accepted.");
        }

        Status = ExtractedWallCandidateStatus.Accepted;
    }

    public void Reject()
    {
        if (Status != ExtractedWallCandidateStatus.Pending)
        {
            throw new InvalidOperationException("Only pending wall candidates can be rejected.");
        }

        Status = ExtractedWallCandidateStatus.Rejected;
    }
}
