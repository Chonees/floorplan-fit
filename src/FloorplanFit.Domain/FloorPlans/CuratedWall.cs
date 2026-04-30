namespace FloorplanFit.Domain.FloorPlans;

public sealed class CuratedWall
{
    public CuratedWall(
        Guid id,
        Guid floorPlanCurationId,
        string stableWallId,
        Guid? sourceCandidateId,
        string? sourceEntityRef,
        Guid? geometryPathId,
        WallRole wallRole,
        WallMobilityLevel mobilityLevel,
        WallProtectionLevel protectionLevel,
        decimal? thicknessMm,
        string? assemblyCode,
        decimal? heightMm,
        bool isExterior,
        bool isStructuralHint,
        Guid? wallGroupId,
        int sortOrder,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(stableWallId))
        {
            throw new ArgumentException("Stable wall id is required.", nameof(stableWallId));
        }

        Id = id;
        FloorPlanCurationId = floorPlanCurationId;
        StableWallId = stableWallId;
        SourceCandidateId = sourceCandidateId;
        SourceEntityRef = sourceEntityRef;
        GeometryPathId = geometryPathId;
        WallRole = wallRole;
        MobilityLevel = mobilityLevel;
        ProtectionLevel = protectionLevel;
        ThicknessMm = thicknessMm;
        AssemblyCode = assemblyCode;
        HeightMm = heightMm;
        IsExterior = isExterior;
        IsStructuralHint = isStructuralHint;
        WallGroupId = wallGroupId;
        SortOrder = sortOrder;
        Notes = notes;
    }

    public Guid Id { get; }

    public Guid FloorPlanCurationId { get; }

    public string StableWallId { get; }

    public Guid? SourceCandidateId { get; }

    public string? SourceEntityRef { get; }

    public Guid? GeometryPathId { get; }

    public WallRole WallRole { get; }

    public WallMobilityLevel MobilityLevel { get; }

    public WallProtectionLevel ProtectionLevel { get; }

    public decimal? ThicknessMm { get; }

    public string? AssemblyCode { get; }

    public decimal? HeightMm { get; }

    public bool IsExterior { get; }

    public bool IsStructuralHint { get; }

    public Guid? WallGroupId { get; }

    public int SortOrder { get; }

    public string? Notes { get; }
}
