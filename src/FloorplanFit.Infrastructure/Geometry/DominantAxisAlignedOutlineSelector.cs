namespace FloorplanFit.Infrastructure.Geometry;

internal readonly record struct AxisAlignedStructuralSegment(
    decimal X1,
    decimal Y1,
    decimal X2,
    decimal Y2);

internal enum DominantOutlineSelectionStatus
{
    Selected,
    InsufficientEvidence,
    ExpectedDimensionMismatch,
    Ambiguous
}

internal sealed record DominantAxisAlignedOutline(
    decimal MinX,
    decimal MinY,
    decimal MaxX,
    decimal MaxY)
{
    public decimal Width => MaxX - MinX;

    public decimal Height => MaxY - MinY;
}

internal sealed record DominantAxisAlignedOutlineSelection(
    DominantOutlineSelectionStatus Status,
    DominantAxisAlignedOutline? Outline,
    string Reason)
{
    public bool IsSelected => Status == DominantOutlineSelectionStatus.Selected && Outline is not null;
}

internal sealed record DominantAxisAlignedOutlineCandidate(
    DominantAxisAlignedOutline Outline,
    decimal BalancedSupport,
    decimal SupportBalance,
    decimal SharedCoverage,
    decimal MinimumContinuity)
{
    public decimal Width => Outline.Width;

    public decimal Height => Outline.Height;
}

internal sealed record DominantAxisAlignedOutlineCandidateSet(
    DominantOutlineSelectionStatus Status,
    IReadOnlyList<DominantAxisAlignedOutlineCandidate> Candidates,
    string Reason)
{
    public bool HasCandidates => Candidates.Count > 0;
}

internal static class DominantAxisAlignedOutlineSelector
{
    internal const decimal CoordinateTolerance = 0.05m;

    private const decimal RatioTolerance = 0.0001m;

    public static DominantAxisAlignedOutlineSelection Select(
        IEnumerable<AxisAlignedStructuralSegment> segments,
        decimal? expectedWidth = null,
        decimal? expectedHeight = null)
    {
        var candidateSet = SelectConnectedFrames(segments, expectedWidth, expectedHeight);
        if (!candidateSet.HasCandidates)
        {
            return new DominantAxisAlignedOutlineSelection(
                candidateSet.Status,
                null,
                candidateSet.Reason);
        }

        return SelectConnectedFrame(candidateSet.Candidates, expectedWidth, expectedHeight);
    }

    internal static DominantAxisAlignedOutlineCandidateSet SelectConnectedFrames(
        IEnumerable<AxisAlignedStructuralSegment> segments,
        decimal? expectedWidth = null,
        decimal? expectedHeight = null)
    {
        ArgumentNullException.ThrowIfNull(segments);

        var vertical = new List<AxisInterval>();
        var horizontal = new List<AxisInterval>();
        foreach (var segment in segments)
        {
            var width = Math.Abs(segment.X2 - segment.X1);
            var height = Math.Abs(segment.Y2 - segment.Y1);
            if (width <= CoordinateTolerance && height > CoordinateTolerance)
            {
                vertical.Add(new AxisInterval(
                    (segment.X1 + segment.X2) / 2m,
                    Math.Min(segment.Y1, segment.Y2),
                    Math.Max(segment.Y1, segment.Y2)));
            }
            else if (height <= CoordinateTolerance && width > CoordinateTolerance)
            {
                horizontal.Add(new AxisInterval(
                    (segment.Y1 + segment.Y2) / 2m,
                    Math.Min(segment.X1, segment.X2),
                    Math.Max(segment.X1, segment.X2)));
            }
        }

        var x = BuildAxisCandidates("X/width", vertical, expectedWidth);
        var y = BuildAxisCandidates("Y/height", horizontal, expectedHeight);
        if (x.Candidates.Count == 0 || y.Candidates.Count == 0)
        {
            var failures = new[] { x, y }.Where(result => result.Candidates.Count == 0).ToArray();
            var status = failures.Any(result => result.Status == DominantOutlineSelectionStatus.ExpectedDimensionMismatch)
                ? DominantOutlineSelectionStatus.ExpectedDimensionMismatch
                : DominantOutlineSelectionStatus.InsufficientEvidence;
            return new DominantAxisAlignedOutlineCandidateSet(
                status,
                [],
                string.Join(" ", failures.Select(result => result.Reason)));
        }

        var frames = BuildConnectedFrames(x.Candidates, y.Candidates);
        if (frames.Count == 0)
        {
            return new DominantAxisAlignedOutlineCandidateSet(
                DominantOutlineSelectionStatus.InsufficientEvidence,
                [],
                $"Insufficient cross-axis corner connectivity: X/width and Y/height parallel-edge pairs formed no connected four-corner frame within coordinate tolerance {CoordinateTolerance}. All four selected corners require incident support from both parallel edge pairs.");
        }

        return new DominantAxisAlignedOutlineCandidateSet(
            DominantOutlineSelectionStatus.Selected,
            frames,
            $"Found {frames.Count} connected four-corner structural frame candidates.");
    }

    private static AxisCandidateSet BuildAxisCandidates(
        string axis,
        IReadOnlyList<AxisInterval> intervals,
        decimal? expectedDimension)
    {
        if (expectedDimension.HasValue && expectedDimension.Value <= 0m)
        {
            return new AxisCandidateSet(
                DominantOutlineSelectionStatus.ExpectedDimensionMismatch,
                [],
                $"{axis} expected dimension mismatch: expected dimension must be positive, but was {expectedDimension}.");
        }

        var runs = BuildRuns(intervals);
        var candidates = BuildCandidates(runs);
        if (candidates.Count > 0)
        {
            return new AxisCandidateSet(
                DominantOutlineSelectionStatus.Selected,
                candidates,
                string.Empty);
        }

        var expectation = expectedDimension.HasValue
            ? $" Expected {axis} dimension {expectedDimension.Value} could not be validated."
            : string.Empty;
        return new AxisCandidateSet(
            expectedDimension.HasValue
                ? DominantOutlineSelectionStatus.ExpectedDimensionMismatch
                : DominantOutlineSelectionStatus.InsufficientEvidence,
            [],
            $"Insufficient {axis} structural evidence: {intervals.Count} axis-aligned segments formed {runs.Count} clustered edge runs and no overlapping parallel-edge pair.{expectation}");
    }

    private static IReadOnlyList<DominantAxisAlignedOutlineCandidate> BuildConnectedFrames(
        IReadOnlyList<AxisPairCandidate> xCandidates,
        IReadOnlyList<AxisPairCandidate> yCandidates)
    {
        var frames = new List<DominantAxisAlignedOutlineCandidate>();
        foreach (var x in xCandidates)
        {
            foreach (var y in yCandidates)
            {
                if (!SupportsCoordinate(x.SharedIntervals, y.Minimum) ||
                    !SupportsCoordinate(x.SharedIntervals, y.Maximum) ||
                    !SupportsCoordinate(y.SharedIntervals, x.Minimum) ||
                    !SupportsCoordinate(y.SharedIntervals, x.Maximum))
                {
                    continue;
                }

                frames.Add(new DominantAxisAlignedOutlineCandidate(
                    new DominantAxisAlignedOutline(
                        x.Minimum,
                        y.Minimum,
                        x.Maximum,
                        y.Maximum),
                    Math.Min(x.BalancedSupport, y.BalancedSupport),
                    Math.Min(x.SupportBalance, y.SupportBalance),
                    Math.Min(x.SharedCoverage, y.SharedCoverage),
                    Math.Min(x.MinimumContinuity, y.MinimumContinuity)));
            }
        }

        return frames;
    }

    private static DominantAxisAlignedOutlineSelection SelectConnectedFrame(
        IReadOnlyList<DominantAxisAlignedOutlineCandidate> frames,
        decimal? expectedWidth,
        decimal? expectedHeight)
    {
        IReadOnlyList<DominantAxisAlignedOutlineCandidate> top = frames;
        top = KeepNearActualMaximum(top, candidate => candidate.BalancedSupport, CoordinateTolerance);
        top = KeepNearActualMaximum(top, candidate => candidate.SupportBalance, RatioTolerance);
        top = KeepNearActualMaximum(top, candidate => candidate.SharedCoverage, CoordinateTolerance);
        top = KeepNearActualMaximum(top, candidate => candidate.MinimumContinuity, RatioTolerance);

        if (expectedWidth.HasValue || expectedHeight.HasValue)
        {
            var eligible = top
                .Where(candidate =>
                    (!expectedWidth.HasValue || Math.Abs(candidate.Width - expectedWidth.Value) <= CoordinateTolerance) &&
                    (!expectedHeight.HasValue || Math.Abs(candidate.Height - expectedHeight.Value) <= CoordinateTolerance))
                .ToArray();
            if (eligible.Length == 0)
            {
                var closest = top
                    .OrderBy(candidate => ExpectedDimensionResidual(candidate, expectedWidth, expectedHeight))
                    .First();
                return new DominantAxisAlignedOutlineSelection(
                    DominantOutlineSelectionStatus.ExpectedDimensionMismatch,
                    null,
                    BuildExpectedDimensionMismatchReason(closest, expectedWidth, expectedHeight));
            }

            var bestWidthResidual = expectedWidth.HasValue
                ? eligible.Min(candidate => Math.Abs(candidate.Width - expectedWidth.Value))
                : 0m;
            var bestHeightResidual = expectedHeight.HasValue
                ? eligible.Min(candidate => Math.Abs(candidate.Height - expectedHeight.Value))
                : 0m;
            top = eligible
                .Where(candidate =>
                    (!expectedWidth.HasValue ||
                     Math.Abs(Math.Abs(candidate.Width - expectedWidth.Value) - bestWidthResidual) <= RatioTolerance) &&
                    (!expectedHeight.HasValue ||
                     Math.Abs(Math.Abs(candidate.Height - expectedHeight.Value) - bestHeightResidual) <= RatioTolerance))
                .ToArray();
        }
        else if (top.Count > 1)
        {
            var widest = top.Max(candidate => candidate.Width);
            var tallest = top.Max(candidate => candidate.Height);
            top = top
                .Where(candidate =>
                    widest - candidate.Width <= CoordinateTolerance &&
                    tallest - candidate.Height <= CoordinateTolerance)
                .ToArray();
        }

        if (top.Count != 1)
        {
            var outlines = string.Join(
                ", ",
                top.Select(candidate =>
                    $"[{candidate.Outline.MinX}, {candidate.Outline.MinY}] - [{candidate.Outline.MaxX}, {candidate.Outline.MaxY}]"));
            return new DominantAxisAlignedOutlineSelection(
                DominantOutlineSelectionStatus.Ambiguous,
                null,
                $"Dominant connected outline is ambiguous: {top.Count} four-corner frames survived hierarchical balanced-support, support-balance, shared-coverage, and minimum-continuity ranking ({outlines}).");
        }

        var selected = top[0];
        return new DominantAxisAlignedOutlineSelection(
            DominantOutlineSelectionStatus.Selected,
            selected.Outline,
            $"Selected connected dominant axis-aligned outline. X/width edges [{selected.Outline.MinX}, {selected.Outline.MaxX}] and Y/height edges [{selected.Outline.MinY}, {selected.Outline.MaxY}] have balanced support {selected.BalancedSupport}, support balance {selected.SupportBalance}, shared coverage {selected.SharedCoverage}, and minimum continuity {selected.MinimumContinuity}.");
    }

    private static decimal ExpectedDimensionResidual(
        DominantAxisAlignedOutlineCandidate candidate,
        decimal? expectedWidth,
        decimal? expectedHeight)
        => (expectedWidth.HasValue ? Math.Abs(candidate.Width - expectedWidth.Value) : 0m) +
           (expectedHeight.HasValue ? Math.Abs(candidate.Height - expectedHeight.Value) : 0m);

    private static string BuildExpectedDimensionMismatchReason(
        DominantAxisAlignedOutlineCandidate candidate,
        decimal? expectedWidth,
        decimal? expectedHeight)
    {
        var mismatches = new List<string>();
        if (expectedWidth.HasValue)
        {
            mismatches.Add(
                $"expected width {expectedWidth.Value}, but strongest connected four-corner evidence spans {candidate.Width} (residual {candidate.Width - expectedWidth.Value}, tolerance {CoordinateTolerance})");
        }

        if (expectedHeight.HasValue)
        {
            mismatches.Add(
                $"expected height {expectedHeight.Value}, but strongest connected four-corner evidence spans {candidate.Height} (residual {candidate.Height - expectedHeight.Value}, tolerance {CoordinateTolerance})");
        }

        return $"Connected dominant outline expected dimension mismatch: {string.Join("; ", mismatches)}; canonical dimensions cannot supply a hidden registration scale.";
    }

    private static IReadOnlyList<AxisRun> BuildRuns(IReadOnlyList<AxisInterval> intervals)
    {
        var clusters = new List<List<AxisInterval>>();
        foreach (var interval in intervals.OrderBy(item => item.Axis))
        {
            if (clusters.Count == 0 || interval.Axis - clusters[^1][0].Axis > CoordinateTolerance)
            {
                clusters.Add([interval]);
            }
            else
            {
                clusters[^1].Add(interval);
            }
        }

        return clusters.Select(cluster =>
        {
            var merged = MergeIntervals(cluster);
            var coordinate = (cluster.Min(item => item.Axis) + cluster.Max(item => item.Axis)) / 2m;
            var support = merged.Sum(item => item.End - item.Start);
            var span = merged.Count == 0 ? 0m : merged[^1].End - merged[0].Start;
            return new AxisRun(
                coordinate,
                merged,
                support,
                span <= 0m ? 0m : support / span);
        }).ToArray();
    }

    private static IReadOnlyList<Interval> MergeIntervals(IEnumerable<AxisInterval> intervals)
    {
        var merged = new List<Interval>();
        foreach (var interval in intervals.OrderBy(item => item.Start).ThenBy(item => item.End))
        {
            if (interval.End - interval.Start <= CoordinateTolerance)
            {
                continue;
            }

            if (merged.Count == 0 || interval.Start > merged[^1].End + CoordinateTolerance)
            {
                merged.Add(new Interval(interval.Start, interval.End));
                continue;
            }

            merged[^1] = merged[^1] with { End = Math.Max(merged[^1].End, interval.End) };
        }

        return merged;
    }

    private static IReadOnlyList<AxisPairCandidate> BuildCandidates(IReadOnlyList<AxisRun> runs)
    {
        var candidates = new List<AxisPairCandidate>();
        for (var leftIndex = 0; leftIndex < runs.Count; leftIndex++)
        {
            for (var rightIndex = leftIndex + 1; rightIndex < runs.Count; rightIndex++)
            {
                var left = runs[leftIndex];
                var right = runs[rightIndex];
                var sharedIntervals = IntersectIntervals(left.Intervals, right.Intervals);
                var sharedCoverage = sharedIntervals.Sum(interval => interval.End - interval.Start);
                if (sharedCoverage <= CoordinateTolerance)
                {
                    continue;
                }

                candidates.Add(new AxisPairCandidate(
                    left.Coordinate,
                    right.Coordinate,
                    Math.Min(left.Support, right.Support),
                    Math.Min(left.Support, right.Support) / Math.Max(left.Support, right.Support),
                    sharedCoverage,
                    Math.Min(left.Continuity, right.Continuity),
                    sharedIntervals));
            }
        }

        return candidates;
    }

    private static IReadOnlyList<Interval> IntersectIntervals(
        IReadOnlyList<Interval> left,
        IReadOnlyList<Interval> right)
    {
        var intersections = new List<Interval>();
        var leftIndex = 0;
        var rightIndex = 0;
        while (leftIndex < left.Count && rightIndex < right.Count)
        {
            var start = Math.Max(left[leftIndex].Start, right[rightIndex].Start);
            var end = Math.Min(left[leftIndex].End, right[rightIndex].End);
            if (end > start)
            {
                intersections.Add(new Interval(start, end));
            }

            if (left[leftIndex].End <= right[rightIndex].End)
            {
                leftIndex++;
            }
            else
            {
                rightIndex++;
            }
        }

        return intersections;
    }

    private static bool SupportsCoordinate(
        IReadOnlyList<Interval> intervals,
        decimal coordinate)
        => intervals.Any(interval =>
            coordinate >= interval.Start - CoordinateTolerance &&
            coordinate <= interval.End + CoordinateTolerance);

    private static IReadOnlyList<TCandidate> KeepNearActualMaximum<TCandidate>(
        IReadOnlyList<TCandidate> candidates,
        Func<TCandidate, decimal> criterion,
        decimal tolerance)
    {
        var maximum = candidates.Max(criterion);
        return candidates
            .Where(candidate => maximum - criterion(candidate) <= tolerance)
            .ToArray();
    }

    private sealed record AxisCandidateSet(
        DominantOutlineSelectionStatus Status,
        IReadOnlyList<AxisPairCandidate> Candidates,
        string Reason);

    private sealed record AxisInterval(decimal Axis, decimal Start, decimal End);

    private sealed record Interval(decimal Start, decimal End);

    private sealed record AxisRun(
        decimal Coordinate,
        IReadOnlyList<Interval> Intervals,
        decimal Support,
        decimal Continuity);

    private sealed record AxisPairCandidate(
        decimal Minimum,
        decimal Maximum,
        decimal BalancedSupport,
        decimal SupportBalance,
        decimal SharedCoverage,
        decimal MinimumContinuity,
        IReadOnlyList<Interval> SharedIntervals)
    {
        public decimal Separation => Maximum - Minimum;
    }

}
