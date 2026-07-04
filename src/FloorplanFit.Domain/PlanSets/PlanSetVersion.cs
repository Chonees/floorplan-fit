namespace FloorplanFit.Domain.PlanSets;

public sealed class PlanSetVersion
{
    public PlanSetVersion(
        Guid id,
        Guid housePlanSetId,
        Guid canonicalFloorPlanVersionId,
        int versionNumber,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Plan-set version id is required.", nameof(id));
        }

        if (housePlanSetId == Guid.Empty)
        {
            throw new ArgumentException("House plan set id is required.", nameof(housePlanSetId));
        }

        if (canonicalFloorPlanVersionId == Guid.Empty)
        {
            throw new ArgumentException("Canonical floor-plan version id is required.", nameof(canonicalFloorPlanVersionId));
        }

        if (versionNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(versionNumber), "Version number must be positive.");
        }

        Id = id;
        HousePlanSetId = housePlanSetId;
        CanonicalFloorPlanVersionId = canonicalFloorPlanVersionId;
        VersionNumber = versionNumber;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public Guid HousePlanSetId { get; }

    public Guid CanonicalFloorPlanVersionId { get; }

    public int VersionNumber { get; }

    public DateTime CreatedAtUtc { get; }
}
