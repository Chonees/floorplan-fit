using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Review;

public static class DimensionAxisClassifier
{
    public const decimal CoordinateEpsilon = 0.001m;
    public const string WidthAxisTag = "Width";
    public const string HeightAxisTag = "Height";
    public const string FreeAngleAxisTag = "FreeAngle";

    public static string ResolveAxisTag(DimensionDto dimension)
        => ResolveAxisTag(dimension.DefPointX, dimension.DefPointY, dimension.DefPoint2X, dimension.DefPoint2Y);

    public static string ResolveAxisTag(decimal startX, decimal startY, decimal endX, decimal endY)
        => ResolveAxisTagFromDelta(endX - startX, endY - startY);

    public static string ResolveAxisTagFromDelta(decimal dx, decimal dy)
    {
        if (Math.Abs((double)dy) <= (double)CoordinateEpsilon)
        {
            return WidthAxisTag;
        }

        if (Math.Abs((double)dx) <= (double)CoordinateEpsilon)
        {
            return HeightAxisTag;
        }

        return FreeAngleAxisTag;
    }

    public static bool IsFreeAngle(DimensionDto dimension)
        => string.Equals(ResolveAxisTag(dimension), FreeAngleAxisTag, StringComparison.OrdinalIgnoreCase);

    public static bool MatchesAxis(DimensionDto dimension, string axisTag)
        => string.Equals(ResolveAxisTag(dimension), axisTag, StringComparison.OrdinalIgnoreCase);
}
