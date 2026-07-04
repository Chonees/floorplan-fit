namespace FloorplanFit.Application.Abstractions;

/// <summary>
/// Spatial-extent checks for extracted point paths. Collapsed CAD entities (e.g. a 3DFACE
/// whose corners coincide) produce paths whose points all share one location; persisting
/// them creates phantom geometry that skews preview bounds and fit math downstream.
/// </summary>
public static class GeometryPointPaths
{
    /// <summary>
    /// Largest coordinate delta (in source units) still considered zero. CAD source units
    /// are millimeters or larger, so one millionth of a unit is far below any drawable feature.
    /// </summary>
    public const decimal CoordinateTolerance = 0.000001m;

    public static bool HasExtent(IReadOnlyList<GeometryPoint> path)
    {
        if (path.Count < 2)
        {
            return false;
        }

        var origin = path[0];
        for (var index = 1; index < path.Count; index++)
        {
            if (Math.Abs(path[index].X - origin.X) > CoordinateTolerance ||
                Math.Abs(path[index].Y - origin.Y) > CoordinateTolerance)
            {
                return true;
            }
        }

        return false;
    }
}
