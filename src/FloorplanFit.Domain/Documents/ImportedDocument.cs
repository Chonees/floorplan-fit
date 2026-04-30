namespace FloorplanFit.Domain.Documents;

public sealed class ImportedDocument
{
    public ImportedDocument(
        Guid id,
        ImportedDocumentType documentType,
        string originalFileName,
        string storagePath,
        string sha256,
        string? dxfVersion,
        Guid measurementContextId,
        DateTime importedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new ArgumentException("Original file name is required.", nameof(originalFileName));
        }

        if (string.IsNullOrWhiteSpace(storagePath))
        {
            throw new ArgumentException("Storage path is required.", nameof(storagePath));
        }

        if (string.IsNullOrWhiteSpace(sha256))
        {
            throw new ArgumentException("SHA256 hash is required.", nameof(sha256));
        }

        Id = id;
        DocumentType = documentType;
        OriginalFileName = originalFileName;
        StoragePath = storagePath;
        Sha256 = sha256;
        DxfVersion = dxfVersion;
        MeasurementContextId = measurementContextId;
        ImportedAtUtc = importedAtUtc;
    }

    public Guid Id { get; }

    public ImportedDocumentType DocumentType { get; }

    public string OriginalFileName { get; }

    public string StoragePath { get; }

    public string Sha256 { get; }

    public string? DxfVersion { get; }

    public Guid MeasurementContextId { get; }

    public DateTime ImportedAtUtc { get; }
}
