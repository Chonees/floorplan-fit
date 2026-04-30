namespace FloorplanFit.Domain.FloorPlans;

public sealed class WallExtractionRun
{
    public WallExtractionRun(
        Guid id,
        Guid floorPlanVersionId,
        string status,
        DateTime startedAtUtc,
        DateTime? finishedAtUtc,
        string extractorVersion,
        string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Status is required.", nameof(status));
        }

        if (string.IsNullOrWhiteSpace(extractorVersion))
        {
            throw new ArgumentException("Extractor version is required.", nameof(extractorVersion));
        }

        Id = id;
        FloorPlanVersionId = floorPlanVersionId;
        Status = status;
        StartedAtUtc = startedAtUtc;
        FinishedAtUtc = finishedAtUtc;
        ExtractorVersion = extractorVersion;
        ErrorMessage = errorMessage;
    }

    public Guid Id { get; }

    public Guid FloorPlanVersionId { get; }

    public string Status { get; }

    public DateTime StartedAtUtc { get; }

    public DateTime? FinishedAtUtc { get; }

    public string ExtractorVersion { get; }

    public string? ErrorMessage { get; }
}
