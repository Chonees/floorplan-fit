namespace FloorplanFit.Domain.Measurement;

public sealed class MeasurementContext
{
    public MeasurementContext(
        Guid id,
        LengthUnit sourceUnit,
        decimal toMillimetersFactor,
        decimal linearToleranceMm,
        decimal angularToleranceDeg,
        DateTime createdAtUtc)
    {
        if (sourceUnit == LengthUnit.Unknown)
        {
            throw new ArgumentException("Source unit must be known.", nameof(sourceUnit));
        }

        if (toMillimetersFactor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(toMillimetersFactor), "Conversion factor must be positive.");
        }

        if (linearToleranceMm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(linearToleranceMm), "Linear tolerance must be positive.");
        }

        if (angularToleranceDeg <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(angularToleranceDeg), "Angular tolerance must be positive.");
        }

        Id = id;
        SourceUnit = sourceUnit;
        ToMillimetersFactor = toMillimetersFactor;
        LinearToleranceMm = linearToleranceMm;
        AngularToleranceDeg = angularToleranceDeg;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public LengthUnit SourceUnit { get; }

    public decimal ToMillimetersFactor { get; }

    public decimal LinearToleranceMm { get; }

    public decimal AngularToleranceDeg { get; }

    public DateTime CreatedAtUtc { get; }
}
