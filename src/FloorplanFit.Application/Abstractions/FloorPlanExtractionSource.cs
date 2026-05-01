namespace FloorplanFit.Application.Abstractions;

public sealed record FloorPlanExtractionSource(
    Guid TemplateId,
    Guid FloorPlanVersionId,
    string ManagedFilePath);
