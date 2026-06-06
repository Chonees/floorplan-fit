using System.Globalization;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Review;

public static class DimensionDisplayTextFormatter
{
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
        var rounded = decimal.Round(totalInches, 0, MidpointRounding.AwayFromZero);
        var feet = decimal.ToInt32(decimal.Truncate(rounded / 12m));
        var inches = decimal.ToInt32(rounded % 12m);
        return feet > 0
            ? $"{feet}'-{inches}\""
            : $"{inches}\"";
    }
}

public readonly record struct DimensionDisplayTextResult(
    string DisplayText,
    string DisplayTextSource);
