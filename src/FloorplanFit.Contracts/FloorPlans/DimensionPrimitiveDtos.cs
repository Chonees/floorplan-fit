namespace FloorplanFit.Contracts.FloorPlans;

public sealed record DimensionLinePrimitiveDto(
    string PrimitiveKey,
    int SortOrder,
    decimal StartX,
    decimal StartY,
    decimal EndX,
    decimal EndY)
{
    public string? SourceHandle { get; init; }

    public string? SourceLayer { get; init; }
}

public sealed record DimensionTextPrimitiveDto(
    string PrimitiveKey,
    int SortOrder,
    string Text,
    decimal X,
    decimal Y,
    decimal Height,
    decimal RotationDegrees)
{
    public string? SourceHandle { get; init; }

    public string? SourceLayer { get; init; }

    public string? StyleName { get; init; }

    public string? HorizontalAlignment { get; init; }

    public string? VerticalAlignment { get; init; }

    public string? AttachmentPoint { get; init; }
}

public sealed record DimensionInsertPrimitiveDto(
    string PrimitiveKey,
    int SortOrder,
    string Name,
    decimal X,
    decimal Y,
    decimal Z)
{
    public string? SourceHandle { get; init; }

    public string? SourceLayer { get; init; }

    public decimal RotationDegrees { get; init; }

    public decimal ScaleX { get; init; } = 1m;

    public decimal ScaleY { get; init; } = 1m;

    public decimal ScaleZ { get; init; } = 1m;
}

public sealed record DimensionCirclePrimitiveDto(
    string PrimitiveKey,
    int SortOrder,
    decimal CenterX,
    decimal CenterY,
    decimal Radius)
{
    public string? SourceHandle { get; init; }

    public string? SourceLayer { get; init; }
}

public sealed record DimensionArcPrimitiveDto(
    string PrimitiveKey,
    int SortOrder,
    decimal CenterX,
    decimal CenterY,
    decimal Radius,
    decimal StartAngleDegrees,
    decimal EndAngleDegrees)
{
    public string? SourceHandle { get; init; }

    public string? SourceLayer { get; init; }
}

public sealed record DimensionSolidPrimitiveDto(
    string PrimitiveKey,
    int SortOrder,
    decimal Point1X,
    decimal Point1Y,
    decimal Point2X,
    decimal Point2Y,
    decimal Point3X,
    decimal Point3Y,
    decimal Point4X,
    decimal Point4Y)
{
    public string? SourceHandle { get; init; }

    public string? SourceLayer { get; init; }
}
