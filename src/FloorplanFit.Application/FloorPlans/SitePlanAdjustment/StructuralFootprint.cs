using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.SitePlanAdjustment;

/// <summary>
/// The structural footprint of a floor plan: the extent of its DOMINANT wall mass
/// on each axis, not the raw bounding box.
/// <para>
/// A raw min/max bounding box is hostage to a single stray segment — a wall-layer
/// line with no thickness, an extraction artifact, a thin projection — that juts past
/// the body of the plan. That one line can define the "width" while carrying a
/// negligible fraction of the actual wall length, which silently de-centers the plan
/// against a site-plan setback and corrupts the fit deficit. (The height of a clean
/// plan is unaffected because its top/bottom extremes are already backed by real wall
/// mass; only the axis with the stray appendage drifts.)
/// </para>
/// <para>
/// This computes, per axis, the span that contains all but a negligible tail of wall
/// length — "wall to wall where the mass is" — and snaps that span back to real wall
/// coordinates so the result is exact, never a bin boundary.
/// </para>
/// <para>
/// Trade-off, stated plainly because this is CAD: a genuine but tiny structural jog
/// whose wall length is below <see cref="DefaultMassTailFraction"/> of the total will
/// be treated as a non-structural appendage. The default (0.5%) sits well below any
/// real wing or bump (those carry several percent of the plan's wall length) yet well
/// above stray single lines, so it separates structure from artifacts with margin.
/// </para>
/// </summary>
public static class StructuralFootprint
{
    /// <summary>
    /// Fraction of total wall length, per side per axis, treated as a negligible tail
    /// that does not define the structural extent.
    /// </summary>
    public const decimal DefaultMassTailFraction = 0.005m;

    private const int ResolutionBins = 2048;
    private const decimal CoordinateEpsilon = 0.000001m;

    public static StructuralFootprintBounds? Resolve(
        IReadOnlyList<GeometryPathDto> paths,
        decimal massTailFraction = DefaultMassTailFraction)
    {
        ArgumentNullException.ThrowIfNull(paths);

        var segments = paths.SelectMany(path => path.Segments).ToArray();
        if (segments.Length == 0)
        {
            return null;
        }

        var fraction = decimal.Clamp(massTailFraction, 0m, 0.49m);
        var (minX, maxX) = RobustExtent(segments, horizontal: true, fraction);
        var (minY, maxY) = RobustExtent(segments, horizontal: false, fraction);
        return new StructuralFootprintBounds(minX, minY, maxX, maxY);
    }

    private static (decimal Min, decimal Max) RobustExtent(
        IReadOnlyList<GeometrySegmentDto> segments,
        bool horizontal,
        decimal massTailFraction)
    {
        decimal absoluteMin = decimal.MaxValue;
        decimal absoluteMax = decimal.MinValue;
        foreach (var segment in segments)
        {
            var (a, b) = ProjectedRange(segment, horizontal);
            absoluteMin = Math.Min(absoluteMin, a);
            absoluteMax = Math.Max(absoluteMax, b);
        }

        var extent = absoluteMax - absoluteMin;
        if (extent <= CoordinateEpsilon || massTailFraction <= 0m)
        {
            return (absoluteMin, absoluteMax);
        }

        var step = (double)extent / ResolutionBins;
        var origin = (double)absoluteMin;
        var binMass = new double[ResolutionBins + 1];
        var totalMass = 0d;

        foreach (var segment in segments)
        {
            var length = SegmentLength(segment);
            if (length <= 0d)
            {
                continue;
            }

            totalMass += length;
            var (aDec, bDec) = ProjectedRange(segment, horizontal);
            var a = (double)aDec;
            var b = (double)bDec;

            if (b - a <= step)
            {
                // Projects onto (effectively) a single column — e.g. a wall
                // perpendicular to this axis. Deposit its whole length there.
                binMass[BinIndex(a, origin, step)] += length;
                continue;
            }

            // Spread length uniformly across the columns it spans.
            var density = length / (b - a);
            var first = BinIndex(a, origin, step);
            var last = BinIndex(b, origin, step);
            for (var bin = first; bin <= last; bin++)
            {
                var binStart = origin + (bin * step);
                var binEnd = binStart + step;
                var overlap = Math.Min(b, binEnd) - Math.Max(a, binStart);
                if (overlap > 0d)
                {
                    binMass[bin] += density * overlap;
                }
            }
        }

        if (totalMass <= 0d)
        {
            return (absoluteMin, absoluteMax);
        }

        var tail = (double)massTailFraction * totalMass;

        var lowerBin = 0;
        var accumulated = 0d;
        for (var bin = 0; bin <= ResolutionBins; bin++)
        {
            accumulated += binMass[bin];
            if (accumulated > tail)
            {
                lowerBin = bin;
                break;
            }
        }

        var upperBin = ResolutionBins;
        accumulated = 0d;
        for (var bin = ResolutionBins; bin >= 0; bin--)
        {
            accumulated += binMass[bin];
            if (accumulated > tail)
            {
                upperBin = bin;
                break;
            }
        }

        var lowerThreshold = absoluteMin + ((decimal)(lowerBin * step));
        var upperThreshold = absoluteMin + ((decimal)((upperBin + 1) * step));

        // Snap back to real wall coordinates inside the retained span so the result is
        // an actual wall position, not an approximate bin edge.
        return SnapToWallCoordinates(segments, horizontal, lowerThreshold, upperThreshold, absoluteMin, absoluteMax);
    }

    private static (decimal Min, decimal Max) SnapToWallCoordinates(
        IReadOnlyList<GeometrySegmentDto> segments,
        bool horizontal,
        decimal lowerThreshold,
        decimal upperThreshold,
        decimal absoluteMin,
        decimal absoluteMax)
    {
        decimal? min = null;
        decimal? max = null;
        foreach (var segment in segments)
        {
            var (a, b) = ProjectedRange(segment, horizontal);
            ConsiderCoordinate(a);
            ConsiderCoordinate(b);
        }

        return (min ?? absoluteMin, max ?? absoluteMax);

        void ConsiderCoordinate(decimal coordinate)
        {
            if (coordinate < lowerThreshold - CoordinateEpsilon ||
                coordinate > upperThreshold + CoordinateEpsilon)
            {
                return;
            }

            min = min is null ? coordinate : Math.Min(min.Value, coordinate);
            max = max is null ? coordinate : Math.Max(max.Value, coordinate);
        }
    }

    private static int BinIndex(double coordinate, double origin, double step)
    {
        var index = (int)((coordinate - origin) / step);
        return Math.Clamp(index, 0, ResolutionBins);
    }

    private static (decimal Min, decimal Max) ProjectedRange(GeometrySegmentDto segment, bool horizontal)
    {
        var start = horizontal ? segment.StartX : segment.StartY;
        var end = horizontal ? segment.EndX : segment.EndY;
        return (Math.Min(start, end), Math.Max(start, end));
    }

    private static double SegmentLength(GeometrySegmentDto segment)
    {
        var deltaX = (double)(segment.EndX - segment.StartX);
        var deltaY = (double)(segment.EndY - segment.StartY);
        return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
    }
}

public readonly record struct StructuralFootprintBounds(decimal MinX, decimal MinY, decimal MaxX, decimal MaxY)
{
    public decimal Width => Math.Max(0m, MaxX - MinX);

    public decimal Height => Math.Max(0m, MaxY - MinY);
}
