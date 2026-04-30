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

    public WallRole WallRole { get; private set; }

    public WallMobilityLevel MobilityLevel { get; private set; }

    public WallProtectionLevel ProtectionLevel { get; private set; }

    public decimal? ThicknessMm { get; private set; }

    public string? AssemblyCode { get; private set; }

    public decimal? HeightMm { get; private set; }

    public bool IsExterior { get; private set; }

    public bool IsStructuralHint { get; private set; }

    public Guid? WallGroupId { get; }

    public int SortOrder { get; }

    public string? Notes { get; private set; }

    public void UpdateMetadata(
        WallRole wallRole,
        WallMobilityLevel mobilityLevel,
        WallProtectionLevel protectionLevel,
        decimal thicknessMm,
        string assemblyCode,
        decimal? heightMm,
        bool isExterior,
        bool isStructuralHint,
        string? notes)
    {
        if (thicknessMm <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(thicknessMm), "Wall thickness must be positive.");
        }

        if (string.IsNullOrWhiteSpace(assemblyCode))
        {
            throw new ArgumentException("Assembly code is required.", nameof(assemblyCode));
        }

        WallRole = wallRole;
        MobilityLevel = mobilityLevel;
        ProtectionLevel = protectionLevel;
        ThicknessMm = thicknessMm;
        AssemblyCode = assemblyCode;
        HeightMm = heightMm;
        IsExterior = isExterior;
        IsStructuralHint = isStructuralHint;
        Notes = notes;
    }
}
