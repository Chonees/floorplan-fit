namespace FloorplanFit.Desktop.Controls.Preview;

internal readonly record struct CadViewportContext(
    double WorldUnitsPerPixel,
    double MinorGridSpacingWorld,
    double MajorGridSpacingWorld,
    double SnappingToleranceWorld)
{
    private const double TargetMinorGridSpacingPixels = 32d;
    private const double SnappingTolerancePixels = 8d;

    public static CadViewportContext Create(FloorPlanPreviewGeometry.PreviewViewport viewport)
    {
        var worldUnitsPerPixel = viewport.Scale <= double.Epsilon
            ? 0d
            : 1d / viewport.Scale;
        var minorGridSpacingWorld = ChooseOneTwoFiveSpacing(worldUnitsPerPixel * TargetMinorGridSpacingPixels);
        var majorGridSpacingWorld = minorGridSpacingWorld * 5d;
        var snappingToleranceWorld = worldUnitsPerPixel * SnappingTolerancePixels;

        return new CadViewportContext(
            worldUnitsPerPixel,
            minorGridSpacingWorld,
            majorGridSpacingWorld,
            snappingToleranceWorld);
    }

    internal static double ChooseOneTwoFiveSpacing(double minimumSpacingWorld)
    {
        if (minimumSpacingWorld <= double.Epsilon)
        {
            return 1d;
        }

        var exponent = Math.Floor(Math.Log10(minimumSpacingWorld));
        var baseValue = Math.Pow(10d, exponent);
        foreach (var multiplier in new[] { 1d, 2d, 5d, 10d })
        {
            var candidate = baseValue * multiplier;
            if (candidate >= minimumSpacingWorld - 0.0000001d)
            {
                return candidate;
            }
        }

        return baseValue * 10d;
    }
}
