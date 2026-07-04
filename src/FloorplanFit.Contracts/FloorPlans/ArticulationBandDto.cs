namespace FloorplanFit.Contracts.FloorPlans;

public sealed record ArticulationBandDto(
    Guid PinchGroupId,
    string PinchGroupName,
    string AxisTag,
    decimal BandStartCoordinate,
    decimal BandEndCoordinate,
    decimal MaxTrimMm,
    string Status);
