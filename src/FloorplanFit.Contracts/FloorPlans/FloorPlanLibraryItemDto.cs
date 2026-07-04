namespace FloorplanFit.Contracts.FloorPlans;

public sealed record FloorPlanLibraryItemDto(
    Guid TemplateId,
    string Code,
    string Name,
    int VersionCount,
    Guid? CurrentVersionId,
    int? CurrentVersionNumber,
    IReadOnlyList<FloorPlanLibraryVersionDto> Versions)
{
    public FloorPlanLibraryItemDto(
        Guid templateId,
        string code,
        string name,
        string status,
        int activeVersionNumber,
        DateTime importedAtUtc,
        string sourceUnit,
        Guid? activePublishedCurationId = null)
        : this(
            templateId,
            code,
            name,
            1,
            null,
            activeVersionNumber,
            [
                new FloorPlanLibraryVersionDto(
                    Guid.Empty,
                    activeVersionNumber,
                    status,
                    importedAtUtc,
                    sourceUnit,
                    IsCurrent: true,
                    activePublishedCurationId)
            ])
    {
    }

    public FloorPlanLibraryVersionDto? CurrentVersion =>
        Versions.FirstOrDefault(item => item.IsCurrent) ?? Versions.FirstOrDefault();

    public string Status => CurrentVersion?.Status ?? "No Versions";

    public int ActiveVersionNumber => CurrentVersionNumber ?? CurrentVersion?.VersionNumber ?? 0;

    public DateTime ImportedAtUtc => CurrentVersion?.ImportedAtUtc ?? DateTime.MinValue;

    public string SourceUnit => CurrentVersion?.SourceUnit ?? string.Empty;

    public Guid? ActivePublishedCurationId => CurrentVersion?.ActivePublishedCurationId;
}
