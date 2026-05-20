namespace FloorplanFit.Domain.FloorPlans;

public sealed class FloorPlanArtifactClassification
{
    public FloorPlanArtifactClassification(
        Guid floorPlanCurationId,
        string sourceArtifactKind,
        Guid sourceArtifactId,
        string resolvedFamily,
        string resolvedCategory,
        string resolvedType,
        FloorPlanArtifactDecisionState decisionState,
        DateTime updatedAtUtc)
    {
        if (!FloorPlanArtifactSourceKinds.IsSupported(sourceArtifactKind))
        {
            throw new ArgumentException("Unsupported source artifact kind.", nameof(sourceArtifactKind));
        }

        if (string.IsNullOrWhiteSpace(resolvedFamily))
        {
            throw new ArgumentException("Resolved family is required.", nameof(resolvedFamily));
        }

        if (string.IsNullOrWhiteSpace(resolvedCategory))
        {
            throw new ArgumentException("Resolved category is required.", nameof(resolvedCategory));
        }

        if (string.IsNullOrWhiteSpace(resolvedType))
        {
            throw new ArgumentException("Resolved type is required.", nameof(resolvedType));
        }

        if (!FloorPlanArtifactTaxonomy.IsValidClassification(resolvedFamily, resolvedCategory, resolvedType))
        {
            throw new ArgumentException("Resolved classification is not part of the supported taxonomy.");
        }

        FloorPlanCurationId = floorPlanCurationId;
        SourceArtifactKind = sourceArtifactKind;
        SourceArtifactId = sourceArtifactId;
        ResolvedFamily = resolvedFamily;
        ResolvedCategory = resolvedCategory;
        ResolvedType = resolvedType;
        DecisionState = decisionState;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid FloorPlanCurationId { get; }

    public string SourceArtifactKind { get; }

    public Guid SourceArtifactId { get; }

    public string ResolvedFamily { get; }

    public string ResolvedCategory { get; }

    public string ResolvedType { get; }

    public FloorPlanArtifactDecisionState DecisionState { get; }

    public DateTime UpdatedAtUtc { get; }
}
