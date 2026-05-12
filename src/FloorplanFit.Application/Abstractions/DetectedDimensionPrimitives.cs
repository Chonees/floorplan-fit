namespace FloorplanFit.Application.Abstractions;

public sealed record DetectedDimensionLinePrimitive(
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

public sealed record DetectedDimensionTextPrimitive(
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

public sealed record DetectedDimensionInsertPrimitive(
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

public sealed record DetectedDimensionCirclePrimitive(
    string PrimitiveKey,
    int SortOrder,
    decimal CenterX,
    decimal CenterY,
    decimal Radius)
{
    public string? SourceHandle { get; init; }

    public string? SourceLayer { get; init; }
}

public sealed record DetectedDimensionArcPrimitive(
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

public sealed record DetectedDimensionSolidPrimitive(
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
