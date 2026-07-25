namespace FloorplanFit.Contracts.FloorPlans;

public sealed record FloorPlanLibraryVersionDto(
    Guid VersionId,
    int VersionNumber,
    string Status,
    DateTime ImportedAtUtc,
    string SourceUnit,
    bool IsCurrent,
    Guid? ActivePublishedCurationId = null,
    int? ActivePublishedCurationVersion = null,
    int PublishedCurationCount = 0,
    int? LatestPublishedCurationVersion = null,
    int? LatestDraftCurationVersion = null,
    bool IsAutoFitReady = false)
{
    public string DisplayName => $"v{VersionNumber}";

    public string CurrentLabel => IsCurrent ? "Current" : string.Empty;

    public bool CanAdjustToSitePlan =>
        IsAutoFitReady && ActivePublishedCurationId is { } curationId && curationId != Guid.Empty;

    public string AutoFitReadinessLabel => IsAutoFitReady ? "Auto-fit ready" : "Setup required";

    public bool HasCurationHistory =>
        ActivePublishedCurationId.HasValue ||
        PublishedCurationCount > 0 ||
        LatestPublishedCurationVersion is not null ||
        LatestDraftCurationVersion is not null;

    public bool CanExtract => !HasCurationHistory;

    public string CurationHistoryLabel
    {
        get
        {
            var parts = new List<string>();
            var publishedVersion = ActivePublishedCurationVersion ?? LatestPublishedCurationVersion;
            if (publishedVersion is not null)
            {
                parts.Add($"Published v{publishedVersion}");
            }
            else if (ActivePublishedCurationId.HasValue)
            {
                parts.Add("Published");
            }

            if (LatestDraftCurationVersion is not null)
            {
                parts.Add($"Draft v{LatestDraftCurationVersion}");
            }

            if (PublishedCurationCount > 1)
            {
                parts.Add($"{PublishedCurationCount} published total");
            }

            return string.Join(" · ", parts);
        }
    }
}
