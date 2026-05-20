namespace FloorplanFit.Contracts.FloorPlans;

public sealed record FloorPlanLibraryVersionDto(
    Guid VersionId,
    int VersionNumber,
    string Status,
    DateTime ImportedAtUtc,
    string SourceUnit,
    bool IsCurrent,
    Guid? ActivePublishedCurationId = null)
{
    public string DisplayName => $"v{VersionNumber}";

    public string CurrentLabel => IsCurrent ? "Current" : string.Empty;
}
