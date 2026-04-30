namespace FloorplanFit.Contracts.FloorPlans;

public sealed record FloorPlanLibraryItemDto(
    Guid TemplateId,
    string Code,
    string Name,
    string Status,
    int ActiveVersionNumber,
    DateTime ImportedAtUtc,
    string SourceUnit,
    Guid? ActivePublishedCurationId = null);
