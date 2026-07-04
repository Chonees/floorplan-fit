namespace FloorplanFit.Domain.PlanSets;

public sealed class PlanSetExport
{
    public PlanSetExport(
        Guid id,
        Guid planSetVersionId,
        Guid canonicalAdjustmentId,
        PlanSetExportStatus status,
        string confidenceSummaryJson,
        string? packageManifestPath,
        DateTime createdAtUtc,
        IReadOnlyList<PlanSetExportedSheet> sheets)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Plan-set export id is required.", nameof(id));
        }

        if (planSetVersionId == Guid.Empty)
        {
            throw new ArgumentException("Plan-set version is required.", nameof(planSetVersionId));
        }

        if (canonicalAdjustmentId == Guid.Empty)
        {
            throw new ArgumentException("Canonical adjustment is required.", nameof(canonicalAdjustmentId));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentException("Export status is required.", nameof(status));
        }

        if (string.IsNullOrWhiteSpace(confidenceSummaryJson))
        {
            throw new ArgumentException("Confidence summary is required.", nameof(confidenceSummaryJson));
        }

        ArgumentNullException.ThrowIfNull(sheets);

        if (sheets.Count == 0)
        {
            throw new ArgumentException("At least one exported sheet is required.", nameof(sheets));
        }

        Id = id;
        PlanSetVersionId = planSetVersionId;
        CanonicalAdjustmentId = canonicalAdjustmentId;
        Status = status;
        ConfidenceSummaryJson = confidenceSummaryJson;
        PackageManifestPath = string.IsNullOrWhiteSpace(packageManifestPath) ? null : packageManifestPath.Trim();
        CreatedAtUtc = createdAtUtc;
        Sheets = sheets.ToArray();
    }

    public Guid Id { get; }

    public Guid PlanSetVersionId { get; }

    public Guid CanonicalAdjustmentId { get; }

    public PlanSetExportStatus Status { get; }

    public string ConfidenceSummaryJson { get; }

    public string? PackageManifestPath { get; }

    public DateTime CreatedAtUtc { get; }

    public IReadOnlyList<PlanSetExportedSheet> Sheets { get; }
}
