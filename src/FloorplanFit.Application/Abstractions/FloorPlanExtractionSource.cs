namespace FloorplanFit.Application.Abstractions;

public sealed record FloorPlanExtractionSource(
    Guid TemplateId,
    Guid FloorPlanVersionId,
    string ManagedFilePath)
{
    public Guid? ImportedDocumentId { get; init; }

    public Guid? MeasurementContextId { get; init; }

    public string? OriginalFileName { get; init; }

    public string? DxfVersion { get; init; }
}
