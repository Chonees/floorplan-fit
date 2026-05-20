namespace FloorplanFit.Application.Abstractions;

public sealed record DetectedDimensionLineSegment(
    decimal StartX,
    decimal StartY,
    decimal EndX,
    decimal EndY);
