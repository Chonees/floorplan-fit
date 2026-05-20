namespace FloorplanFit.Contracts.FloorPlans;

public sealed record PinchGroupDto(
    Guid PinchGroupId,
    string Name,
    string AxisTag,
    int SortOrder);
