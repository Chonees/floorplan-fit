namespace FloorplanFit.Contracts.FloorPlans;

public sealed record MeasurementContextDto(
    string SourceUnit,
    decimal ToMillimetersFactor,
    decimal LinearToleranceMm,
    decimal AngularToleranceDeg);
