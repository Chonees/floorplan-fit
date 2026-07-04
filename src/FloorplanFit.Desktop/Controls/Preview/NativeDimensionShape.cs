using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class NativeDimensionShape
{
    private const decimal PointTolerance = 0.5m;

    internal enum LinearDimensionFamilyKind
    {
        Unknown = 0,
        StandardLinearC = 1,
        SplitLinear = 2,
        LeaderLinear = 3
    }

    internal readonly record struct Vec2(decimal X, decimal Y);

    internal readonly record struct ResolvedLayout(
        LinearDimensionFamilyKind FamilyKind,
        Vec2 StartExtent,
        Vec2 EndExtent,
        Vec2 Axis,
        Vec2 Normal,
        decimal SpanLength,
        Vec2? RemoteStart,
        Vec2? RemoteEnd,
        int? StartInsertIndex,
        int? EndInsertIndex);

    internal readonly record struct CanonicalCShape(
        Vec2 BaseLeft,
        Vec2 BaseRight,
        Vec2 EndpointLeft,
        Vec2 EndpointRight,
        int LeftLegIndex,
        int RightLegIndex,
        int RoofIndex);

    public static ResolvedLayout? TryResolve(DimensionDto dimension)
    {
        var canonical = TryResolveCanonicalCShape(dimension);
        var terminalPair = ResolveTerminalPair(dimension, canonical);
        if (terminalPair is null)
        {
            return null;
        }

        var (startExtent, endExtent, startInsertIndex, endInsertIndex) = terminalPair.Value;
        var axisVector = Subtract(endExtent, startExtent);
        var spanLength = Length(axisVector);
        if (spanLength <= PointTolerance)
        {
            return null;
        }

        var axis = Normalize(axisVector);
        var normal = new Vec2(-axis.Y, axis.X);
        var lineCount = dimension.LinePrimitives.Count > 0
            ? dimension.LinePrimitives.Count
            : dimension.LineSegments.Count;

        var familyKind = lineCount == 4
            ? LinearDimensionFamilyKind.SplitLinear
            : canonical is not null
                ? LinearDimensionFamilyKind.StandardLinearC
                : lineCount >= 5
                    ? LinearDimensionFamilyKind.LeaderLinear
                    : LinearDimensionFamilyKind.Unknown;

        var remoteStart = familyKind switch
        {
            LinearDimensionFamilyKind.StandardLinearC => canonical?.BaseLeft,
            LinearDimensionFamilyKind.SplitLinear => ResolveSplitRemotePoint(dimension, startExtent, axis, spanLength, atStartExtent: true),
            _ => null
        };

        var remoteEnd = familyKind switch
        {
            LinearDimensionFamilyKind.StandardLinearC => canonical?.BaseRight,
            LinearDimensionFamilyKind.SplitLinear => ResolveSplitRemotePoint(dimension, startExtent, axis, spanLength, atStartExtent: false),
            _ => null
        };

        return new ResolvedLayout(
            familyKind,
            startExtent,
            endExtent,
            axis,
            normal,
            spanLength,
            remoteStart,
            remoteEnd,
            startInsertIndex,
            endInsertIndex);
    }

    internal static CanonicalCShape? TryResolveCanonicalCShape(DimensionDto dimension)
    {
        var lines = dimension.LinePrimitives;
        if (lines.Count < 3)
        {
            return null;
        }

        var bestRoofIdx = -1;
        var bestLeg1Idx = -1;
        var bestLeg2Idx = -1;
        var bestScore = decimal.MinValue;

        for (var roofIndex = 0; roofIndex < lines.Count; roofIndex++)
        {
            var roof = lines[roofIndex];
            var roofLength = LineLength(roof);
            if (roofLength <= 0m)
            {
                continue;
            }

            var touchingLegIndices = new List<int>();
            for (var candidateIndex = 0; candidateIndex < lines.Count; candidateIndex++)
            {
                if (candidateIndex == roofIndex)
                {
                    continue;
                }

                if (TouchesAtExactlyOneEndpoint(lines[candidateIndex], roof))
                {
                    touchingLegIndices.Add(candidateIndex);
                }
            }

            if (touchingLegIndices.Count < 2)
            {
                continue;
            }

            for (var i = 0; i < touchingLegIndices.Count - 1; i++)
            {
                for (var j = i + 1; j < touchingLegIndices.Count; j++)
                {
                    var leftCandidate = lines[touchingLegIndices[i]];
                    var rightCandidate = lines[touchingLegIndices[j]];
                    if (SharesEndpoint(leftCandidate, rightCandidate))
                    {
                        continue;
                    }

                    var score = roofLength + LineLength(leftCandidate) + LineLength(rightCandidate);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestRoofIdx = roofIndex;
                        bestLeg1Idx = touchingLegIndices[i];
                        bestLeg2Idx = touchingLegIndices[j];
                    }
                }
            }
        }

        if (bestRoofIdx < 0)
        {
            return null;
        }

        return BuildCanonicalCShape(dimension, bestRoofIdx, bestLeg1Idx, bestLeg2Idx);
    }

    private static (Vec2 StartExtent, Vec2 EndExtent, int? StartInsertIndex, int? EndInsertIndex)? ResolveTerminalPair(
        DimensionDto dimension,
        CanonicalCShape? canonical)
    {
        if (dimension.InsertPrimitives.Count >= 2)
        {
            var unique = dimension.InsertPrimitives
                .Select((insert, index) => (Index: index, Point: new Vec2(insert.X, insert.Y)))
                .DistinctBy(item => (RoundKey(item.Point.X), RoundKey(item.Point.Y)))
                .ToArray();

            if (unique.Length >= 2)
            {
                var best = (
                    First: unique[0],
                    Second: unique[1],
                    DistanceSquared: DistanceSquared(unique[0].Point, unique[1].Point));

                for (var i = 0; i < unique.Length - 1; i++)
                {
                    for (var j = i + 1; j < unique.Length; j++)
                    {
                        var distanceSquared = DistanceSquared(unique[i].Point, unique[j].Point);
                        if (distanceSquared > best.DistanceSquared)
                        {
                            best = (unique[i], unique[j], distanceSquared);
                        }
                    }
                }

                return Compare(best.First.Point, best.Second.Point) <= 0
                    ? (best.First.Point, best.Second.Point, best.First.Index, best.Second.Index)
                    : (best.Second.Point, best.First.Point, best.Second.Index, best.First.Index);
            }
        }

        if (canonical is not null)
        {
            return (canonical.Value.EndpointLeft, canonical.Value.EndpointRight, null, null);
        }

        var lineSource = dimension.LinePrimitives.Count > 0
            ? dimension.LinePrimitives.SelectMany(line => new[]
            {
                new Vec2(line.StartX, line.StartY),
                new Vec2(line.EndX, line.EndY)
            })
            : dimension.LineSegments.SelectMany(line => new[]
            {
                new Vec2(line.StartX, line.StartY),
                new Vec2(line.EndX, line.EndY)
            });
        var endpoints = lineSource
            .DistinctBy(point => (RoundKey(point.X), RoundKey(point.Y)))
            .ToArray();
        if (endpoints.Length < 2)
        {
            return null;
        }

        var bestEndpoints = (First: endpoints[0], Second: endpoints[1], DistanceSquared: DistanceSquared(endpoints[0], endpoints[1]));
        for (var i = 0; i < endpoints.Length - 1; i++)
        {
            for (var j = i + 1; j < endpoints.Length; j++)
            {
                var distanceSquared = DistanceSquared(endpoints[i], endpoints[j]);
                if (distanceSquared > bestEndpoints.DistanceSquared)
                {
                    bestEndpoints = (endpoints[i], endpoints[j], distanceSquared);
                }
            }
        }

        return Compare(bestEndpoints.First, bestEndpoints.Second) <= 0
            ? (bestEndpoints.First, bestEndpoints.Second, null, null)
            : (bestEndpoints.Second, bestEndpoints.First, null, null);
    }

    private static CanonicalCShape BuildCanonicalCShape(
        DimensionDto dimension,
        int roofIndex,
        int leg1Index,
        int leg2Index)
    {
        var roof = dimension.LinePrimitives[roofIndex];
        var leg1 = dimension.LinePrimitives[leg1Index];
        var leg2 = dimension.LinePrimitives[leg2Index];

        ClassifyLeg(leg1, roof, out var leg1Extent, out var leg1Base);
        ClassifyLeg(leg2, roof, out var leg2Extent, out var leg2Base);

        var roofStart = new Vec2(roof.StartX, roof.StartY);
        var roofEnd = new Vec2(roof.EndX, roof.EndY);
        var roofAxis = Normalize(Subtract(roofEnd, roofStart));
        var leg1Projection = Dot(Subtract(leg1Extent, roofStart), roofAxis);
        var leg2Projection = Dot(Subtract(leg2Extent, roofStart), roofAxis);
        var leg1IsLeft = leg1Projection <= leg2Projection;

        return leg1IsLeft
            ? new CanonicalCShape(leg1Base, leg2Base, leg1Extent, leg2Extent, leg1Index, leg2Index, roofIndex)
            : new CanonicalCShape(leg2Base, leg1Base, leg2Extent, leg1Extent, leg2Index, leg1Index, roofIndex);
    }

    private static Vec2? ResolveSplitRemotePoint(
        DimensionDto dimension,
        Vec2 startExtent,
        Vec2 axis,
        decimal spanLength,
        bool atStartExtent)
    {
        var lineSource = dimension.LinePrimitives.Count > 0
            ? dimension.LinePrimitives.SelectMany(line => new[]
            {
                new Vec2(line.StartX, line.StartY),
                new Vec2(line.EndX, line.EndY)
            })
            : dimension.LineSegments.SelectMany(line => new[]
            {
                new Vec2(line.StartX, line.StartY),
                new Vec2(line.EndX, line.EndY)
            });

        var targetU = atStartExtent ? 0m : spanLength;
        var candidates = lineSource
            .DistinctBy(point => (RoundKey(point.X), RoundKey(point.Y)))
            .Select(point =>
            {
                var relative = Subtract(point, startExtent);
                var u = Dot(relative, axis);
                return new
                {
                    Point = point,
                    AxisDistance = decimal.Abs(u - targetU),
                    NormalDistance = decimal.Abs(Dot(relative, new Vec2(-axis.Y, axis.X)))
                };
            })
            .Where(item => item.AxisDistance <= 1m && item.NormalDistance > PointTolerance)
            .OrderByDescending(item => item.NormalDistance)
            .FirstOrDefault();

        return candidates?.Point;
    }

    private static void ClassifyLeg(
        DimensionLinePrimitiveDto leg,
        DimensionLinePrimitiveDto roof,
        out Vec2 extentPoint,
        out Vec2 basePoint)
    {
        var legStart = new Vec2(leg.StartX, leg.StartY);
        var legEnd = new Vec2(leg.EndX, leg.EndY);
        var roofStart = new Vec2(roof.StartX, roof.StartY);
        var roofEnd = new Vec2(roof.EndX, roof.EndY);

        var startTouchesRoof = ApproxEqual(legStart, roofStart) || ApproxEqual(legStart, roofEnd);
        if (startTouchesRoof)
        {
            extentPoint = legStart;
            basePoint = legEnd;
        }
        else
        {
            extentPoint = legEnd;
            basePoint = legStart;
        }
    }

    private static bool TouchesAtExactlyOneEndpoint(DimensionLinePrimitiveDto candidate, DimensionLinePrimitiveDto roof)
    {
        var candidateStart = new Vec2(candidate.StartX, candidate.StartY);
        var candidateEnd = new Vec2(candidate.EndX, candidate.EndY);
        var roofStart = new Vec2(roof.StartX, roof.StartY);
        var roofEnd = new Vec2(roof.EndX, roof.EndY);
        var startTouches = ApproxEqual(candidateStart, roofStart) || ApproxEqual(candidateStart, roofEnd);
        var endTouches = ApproxEqual(candidateEnd, roofStart) || ApproxEqual(candidateEnd, roofEnd);
        return startTouches ^ endTouches;
    }

    private static bool SharesEndpoint(DimensionLinePrimitiveDto left, DimensionLinePrimitiveDto right)
    {
        var leftStart = new Vec2(left.StartX, left.StartY);
        var leftEnd = new Vec2(left.EndX, left.EndY);
        var rightStart = new Vec2(right.StartX, right.StartY);
        var rightEnd = new Vec2(right.EndX, right.EndY);
        return ApproxEqual(leftStart, rightStart)
            || ApproxEqual(leftStart, rightEnd)
            || ApproxEqual(leftEnd, rightStart)
            || ApproxEqual(leftEnd, rightEnd);
    }

    internal static bool ApproxEqual(Vec2 left, Vec2 right)
    {
        return decimal.Abs(left.X - right.X) <= PointTolerance &&
               decimal.Abs(left.Y - right.Y) <= PointTolerance;
    }

    internal static Vec2 Add(Vec2 left, Vec2 right) => new(left.X + right.X, left.Y + right.Y);

    internal static Vec2 Subtract(Vec2 left, Vec2 right) => new(left.X - right.X, left.Y - right.Y);

    internal static Vec2 Scale(Vec2 vector, decimal scalar) => new(vector.X * scalar, vector.Y * scalar);

    internal static decimal Dot(Vec2 left, Vec2 right) => (left.X * right.X) + (left.Y * right.Y);

    internal static decimal Length(Vec2 vector)
    {
        return decimal.CreateChecked(Math.Sqrt((double)((vector.X * vector.X) + (vector.Y * vector.Y))));
    }

    internal static Vec2 Normalize(Vec2 vector)
    {
        var length = Length(vector);
        return length <= PointTolerance
            ? new Vec2(1m, 0m)
            : new Vec2(vector.X / length, vector.Y / length);
    }

    internal static decimal DistanceSquared(Vec2 left, Vec2 right)
    {
        var delta = Subtract(left, right);
        return (delta.X * delta.X) + (delta.Y * delta.Y);
    }

    private static decimal LineLength(DimensionLinePrimitiveDto line)
    {
        return Length(new Vec2(line.EndX - line.StartX, line.EndY - line.StartY));
    }

    private static int Compare(Vec2 left, Vec2 right)
    {
        var xCompare = left.X.CompareTo(right.X);
        if (xCompare != 0)
        {
            return xCompare;
        }

        return left.Y.CompareTo(right.Y);
    }

    private static decimal RoundKey(decimal value)
    {
        return decimal.Round(value, 3, MidpointRounding.AwayFromZero);
    }
}
