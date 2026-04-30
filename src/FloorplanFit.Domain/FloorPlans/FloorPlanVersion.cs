namespace FloorplanFit.Domain.FloorPlans;

public sealed class FloorPlanVersion
{
    public FloorPlanVersion(
        Guid id,
        Guid floorPlanTemplateId,
        Guid importedDocumentId,
        string geometryFingerprint,
        int versionNumber,
        DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(geometryFingerprint))
        {
            throw new ArgumentException("Geometry fingerprint is required.", nameof(geometryFingerprint));
        }

        if (versionNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(versionNumber), "Version number must be positive.");
        }

        Id = id;
        FloorPlanTemplateId = floorPlanTemplateId;
        ImportedDocumentId = importedDocumentId;
        GeometryFingerprint = geometryFingerprint;
        VersionNumber = versionNumber;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public Guid FloorPlanTemplateId { get; }

    public Guid ImportedDocumentId { get; }

    public string GeometryFingerprint { get; }

    public int VersionNumber { get; }

    public DateTime CreatedAtUtc { get; }
}
