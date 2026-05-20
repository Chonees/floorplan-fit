namespace FloorplanFit.Domain.FloorPlans;

public sealed class FloorPlanCuration
{
    public FloorPlanCuration(
        Guid id,
        Guid floorPlanVersionId,
        int curationVersion,
        FloorPlanCurationStatus status,
        Guid? basedOnCurationId,
        string? notes,
        DateTime createdAtUtc,
        DateTime? publishedAtUtc)
    {
        if (curationVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(curationVersion), "Curation version must be positive.");
        }

        Id = id;
        FloorPlanVersionId = floorPlanVersionId;
        CurationVersion = curationVersion;
        Status = status;
        BasedOnCurationId = basedOnCurationId;
        Notes = notes;
        CreatedAtUtc = createdAtUtc;
        PublishedAtUtc = publishedAtUtc;
    }

    public Guid Id { get; }

    public Guid FloorPlanVersionId { get; }

    public int CurationVersion { get; }

    public FloorPlanCurationStatus Status { get; private set; }

    public Guid? BasedOnCurationId { get; }

    public string? Notes { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public DateTime? PublishedAtUtc { get; private set; }

    public void UpdateNotes(string? notes)
    {
        EnsureDraft();
        Notes = notes;
    }

    public void Publish(DateTime publishedAtUtc)
    {
        EnsureDraft();
        Status = FloorPlanCurationStatus.Published;
        PublishedAtUtc = publishedAtUtc;
    }

    public void MarkSuperseded()
    {
        if (Status != FloorPlanCurationStatus.Published)
        {
            throw new InvalidOperationException("Only published curations can become superseded.");
        }

        Status = FloorPlanCurationStatus.Superseded;
    }

    private void EnsureDraft()
    {
        if (Status != FloorPlanCurationStatus.Draft)
        {
            throw new InvalidOperationException("Only draft curations can be edited.");
        }
    }
}
