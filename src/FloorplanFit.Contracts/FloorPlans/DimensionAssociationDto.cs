namespace FloorplanFit.Contracts.FloorPlans;

public sealed record DimensionAssociationDto(
    Guid DimensionId,
    string AssociationKind,
    bool IsFullyResolved,
    decimal Confidence,
    string Notes)
{
    public DimensionAnchorReferenceDto? StartAnchor { get; init; }

    public DimensionAnchorReferenceDto? EndAnchor { get; init; }
}
