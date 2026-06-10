using System.Globalization;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Review;

public static class DimensionDisplayTextFormatter
{
    private const int ArchitecturalInchFractionDenominator = 16;
    private const decimal ArchitecturalFractionToleranceInches = 0.001m;

    public static DimensionDisplayTextResult Resolve(
        DimensionDto dimension,
        decimal measurementSourceUnits,
        string generatedDisplayTextSource)
    {
        var measuredText = FormatMeasurementFallback(measurementSourceUnits, dimension.SourceUnit);
        var rawOverride = dimension.RawTextOverride?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(rawOverride) ||
            string.Equals(rawOverride, "<>", StringComparison.Ordinal))
        {
            return new DimensionDisplayTextResult(measuredText, generatedDisplayTextSource);
        }

        if (rawOverride.Contains("<>", StringComparison.Ordinal))
        {
            return new DimensionDisplayTextResult(
                rawOverride.Replace("<>", measuredText, StringComparison.Ordinal),
                generatedDisplayTextSource);
        }

        return new DimensionDisplayTextResult(dimension.DisplayText, dimension.DisplayTextSource);
    }

    private static string FormatMeasurementFallback(decimal measurement, string sourceUnit)
    {
        return string.Equals(sourceUnit, "Inch", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(sourceUnit, "Foot", StringComparison.OrdinalIgnoreCase)
            ? FormatArchitecturalInches(measurement)
            : measurement.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string FormatArchitecturalInches(decimal totalInches)
    {
        var sign = totalInches < 0m ? "-" : string.Empty;
        var absoluteInches = decimal.Abs(totalInches);
        var totalFractionUnits = decimal.ToInt32(decimal.Round(
            absoluteInches * ArchitecturalInchFractionDenominator,
            0,
            MidpointRounding.AwayFromZero));
        var snappedFractionalInches = totalFractionUnits / (decimal)ArchitecturalInchFractionDenominator;
        if (decimal.Abs(absoluteInches - snappedFractionalInches) > ArchitecturalFractionToleranceInches)
        {
            var rounded = decimal.Round(totalInches, 0, MidpointRounding.AwayFromZero);
            var roundedFeet = decimal.ToInt32(decimal.Truncate(rounded / 12m));
            var roundedInches = decimal.ToInt32(rounded % 12m);

            return roundedFeet > 0
                ? $"{roundedFeet}'-{roundedInches}\""
                : $"{roundedInches}\"";
        }

        var fractionUnitsPerFoot = 12 * ArchitecturalInchFractionDenominator;
        var feet = totalFractionUnits / fractionUnitsPerFoot;
        var remainingFractionUnits = totalFractionUnits % fractionUnitsPerFoot;
        var inches = remainingFractionUnits / ArchitecturalInchFractionDenominator;
        var fractionNumerator = remainingFractionUnits % ArchitecturalInchFractionDenominator;
        var inchText = FormatInchesWithFraction(inches, fractionNumerator);

        return feet > 0
            ? $"{sign}{feet}'-{inchText}"
            : $"{sign}{inchText}";
    }

    private static string FormatInchesWithFraction(int inches, int fractionNumerator)
    {
        if (fractionNumerator == 0)
        {
            return $"{inches}\"";
        }

        var divisor = GreatestCommonDivisor(fractionNumerator, ArchitecturalInchFractionDenominator);
        var simplifiedNumerator = fractionNumerator / divisor;
        var simplifiedDenominator = ArchitecturalInchFractionDenominator / divisor;
        var fractionText = $"{simplifiedNumerator}/{simplifiedDenominator}\"";

        return inches > 0
            ? $"{inches} {fractionText}"
            : fractionText;
    }

    private static int GreatestCommonDivisor(int left, int right)
    {
        while (right != 0)
        {
            var remainder = left % right;
            left = right;
            right = remainder;
        }

        return Math.Abs(left);
    }
}

public readonly record struct DimensionDisplayTextResult(
    string DisplayText,
    string DisplayTextSource);
