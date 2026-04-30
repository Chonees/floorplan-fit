namespace FloorplanFit.Domain.FloorPlans;

public sealed class ExtractedWallCandidate
{
    public ExtractedWallCandidate(
        Guid id,
        Guid floorPlanVersionId,
        string sourceEntityRef,
        string? sourceLayer,
        Guid? geometryPathId,
        decimal? thicknessMm,
        decimal confidence,
        string? detectionNotes,
        ExtractedWallCandidateStatus status)
    {
        if (string.IsNullOrWhiteSpace(sourceEntityRef))
        {
            throw new ArgumentException("Source entity reference is required.", nameof(sourceEntityRef));
        }

        if (confidence < 0m || confidence > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be between 0 and 1.");
        }

        Id = id;
        FloorPlanVersionId = floorPlanVersionId;
        SourceEntityRef = sourceEntityRef;
        SourceLayer = sourceLayer;
        GeometryPathId = geometryPathId;
        ThicknessMm = thicknessMm;
        Confidence = confidence;
        DetectionNotes = detectionNotes;
        Status = status;
    }

    public Guid Id { get; }

    public Guid FloorPlanVersionId { get; }

    public string SourceEntityRef { get; }

    public string? SourceLayer { get; }

    public Guid? GeometryPathId { get; }

    public decimal? ThicknessMm { get; }

    public decimal Confidence { get; }

    public string? DetectionNotes { get; }

    public ExtractedWallCandidateStatus Status { get; }
}
