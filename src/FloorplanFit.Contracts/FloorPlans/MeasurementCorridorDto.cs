namespace FloorplanFit.Contracts.FloorPlans;

public sealed record MeasurementCorridorDto(
    Guid CorridorId,
    string Name,
    string AxisTag,
    Guid GuideGeometryPathId,
    decimal BandMinCoordinate,
    decimal BandMaxCoordinate,
    string Status,
    int SortOrder);
