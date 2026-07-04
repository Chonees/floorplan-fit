namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed record ExportAdjustedDxfResponse(
    Guid ImportedDocumentId,
    string ManagedFilePath,
    int ExportedDimensionCount);
