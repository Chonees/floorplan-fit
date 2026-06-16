namespace FloorplanFit.Contracts.FloorPlans;

/// <summary>
/// Classifies geometry by spatial extent. Collapsed CAD entities (e.g. a 3DFACE whose
/// corners coincide) survive DXF extraction as zero-length segments; any consumer that
/// derives bounds, centers, or fit envelopes from raw segments must ignore them or the
/// degenerate point silently skews the result.
/// </summary>
public static class GeometryExtents
{
    /// <summary>
    /// Largest coordinate delta (in source units) still considered zero. CAD source units
    /// are millimeters or larger, so one millionth of a unit is far below any drawable feature.
    /// </summary>
    public const decimal CoordinateTolerance = 0.000001m;

    public static bool HasExtent(this GeometrySegmentDto segment) =>
        Math.Abs(segment.EndX - segment.StartX) > CoordinateTolerance ||
        Math.Abs(segment.EndY - segment.StartY) > CoordinateTolerance;
}
