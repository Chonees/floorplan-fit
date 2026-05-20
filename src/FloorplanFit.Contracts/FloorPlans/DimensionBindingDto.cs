namespace FloorplanFit.Contracts.FloorPlans;

public sealed record DimensionBindingDto(
    Guid DimensionId,
    string BindingKind,
    bool IsResolved,
    decimal Confidence,
    string Notes,
    bool HasManualBindingOverride)
{
    public IReadOnlyList<DimensionAnchorReferenceDto> Anchors { get; init; } = [];

    public DimensionMeasuredSpanDto? MeasuredSpan { get; init; }
}
