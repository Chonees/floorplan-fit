using System.Globalization;
using System.Text.RegularExpressions;

namespace FloorplanFit.Desktop.Presentation;

public enum ArchitecturalLengthDefaultUnit
{
    Feet,
    Inches
}

public static class ArchitecturalLengthText
{
    private const int FractionDenominator = 256;
    private const int InchesPerFoot = 12;

    public static bool TryParsePositiveInches(
        string? text,
        ArchitecturalLengthDefaultUnit defaultUnit,
        out decimal totalInches)
    {
        totalInches = 0m;
        var normalized = NormalizeMarks(text).Trim();
        if (normalized.Length == 0)
        {
            return false;
        }

        if (!normalized.Contains('\'') && !normalized.Contains('"'))
        {
            if (!TryParseBareDecimal(normalized, out var bareValue) ||
                bareValue <= 0m)
            {
                return false;
            }

            if (defaultUnit == ArchitecturalLengthDefaultUnit.Feet &&
                bareValue > decimal.MaxValue / InchesPerFoot)
            {
                return false;
            }

            totalInches = defaultUnit == ArchitecturalLengthDefaultUnit.Feet
                ? bareValue * InchesPerFoot
                : bareValue;
            return true;
        }

        return TryParseArchitectural(normalized, out totalInches);
    }

    public static bool TryAdjustInches(
        string? currentText,
        ArchitecturalLengthDefaultUnit defaultUnit,
        decimal deltaInches,
        out string formatted)
    {
        formatted = string.Empty;
        if (!TryParsePositiveInches(currentText, defaultUnit, out var currentInches))
        {
            return false;
        }

        decimal adjustedInches;
        try
        {
            adjustedInches = currentInches + deltaInches;
        }
        catch (OverflowException)
        {
            return false;
        }

        if (adjustedInches <= 0m)
        {
            return false;
        }

        formatted = FormatInches(adjustedInches);
        return true;
    }

    public static string FormatInches(decimal totalInches)
    {
        if (totalInches <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(totalInches), "Length must be positive.");
        }

        var feet = decimal.Truncate(totalInches / InchesPerFoot);
        var remainingInches = totalInches - (feet * InchesPerFoot);
        var remainingFractionUnits = decimal.ToInt32(decimal.Round(
            remainingInches * FractionDenominator,
            0,
            MidpointRounding.AwayFromZero));
        var fractionUnitsPerFoot = InchesPerFoot * FractionDenominator;
        if (remainingFractionUnits == fractionUnitsPerFoot)
        {
            feet += 1m;
            remainingFractionUnits = 0;
        }

        var inches = remainingFractionUnits / FractionDenominator;
        var fractionNumerator = remainingFractionUnits % FractionDenominator;
        var inchText = FormatInches(inches, fractionNumerator);

        return feet > 0
            ? $"{feet.ToString("0", CultureInfo.InvariantCulture)}'-{inchText}"
            : inchText;
    }

    private static bool TryParseArchitectural(string text, out decimal totalInches)
    {
        totalInches = 0m;
        var primeIndex = text.IndexOf('\'');
        var hasFeet = primeIndex >= 0;
        decimal feet = 0m;
        string inchBody;

        if (hasFeet)
        {
            if (primeIndex != text.LastIndexOf('\'') ||
                !decimal.TryParse(text[..primeIndex].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out feet))
            {
                return false;
            }

            var rawRemainder = text[(primeIndex + 1)..];
            if (Regex.IsMatch(rawRemainder, @"^\s+-|^-\s+", RegexOptions.CultureInvariant))
            {
                return false;
            }

            var remainder = rawRemainder.Trim();
            if (remainder.Length == 0)
            {
                if (feet > decimal.MaxValue / InchesPerFoot)
                {
                    return false;
                }

                totalInches = feet * InchesPerFoot;
                return totalInches > 0m;
            }

            if (remainder.StartsWith("-", StringComparison.Ordinal))
            {
                remainder = remainder[1..].TrimStart();
            }

            if (remainder.Contains('"'))
            {
                if (!TryRemoveTerminalDoublePrime(remainder, out inchBody))
                {
                    return false;
                }
            }
            else
            {
                if (remainder.Contains('\''))
                {
                    return false;
                }

                inchBody = remainder;
            }
        }
        else if (!TryRemoveTerminalDoublePrime(text, out inchBody))
        {
            return false;
        }

        if (!TryParseInchBody(inchBody, out var inches) || hasFeet && inches >= InchesPerFoot)
        {
            return false;
        }

        if (feet > (decimal.MaxValue - inches) / InchesPerFoot)
        {
            return false;
        }

        totalInches = (feet * InchesPerFoot) + inches;
        return totalInches > 0m;
    }

    private static bool TryRemoveTerminalDoublePrime(string text, out string inchBody)
    {
        inchBody = string.Empty;
        if (!text.EndsWith('"') || text.IndexOf('"') != text.Length - 1 || text.Contains('\''))
        {
            return false;
        }

        inchBody = text[..^1].Trim();
        return inchBody.Length > 0;
    }

    private static bool TryParseInchBody(string text, out decimal inches)
    {
        inches = 0m;

        var mixedMatch = Regex.Match(
            text,
            @"^(?<whole>\d+)(?:\s+|-)(?<numerator>\d+)/(?<denominator>\d+)$",
            RegexOptions.CultureInvariant);
        if (mixedMatch.Success)
        {
            if (!decimal.TryParse(mixedMatch.Groups["whole"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var whole) ||
                !TryParseFraction(mixedMatch.Groups["numerator"].Value, mixedMatch.Groups["denominator"].Value, out var fraction))
            {
                return false;
            }

            if (whole > decimal.MaxValue - fraction)
            {
                return false;
            }

            inches = whole + fraction;
            return true;
        }

        var fractionMatch = Regex.Match(
            text,
            @"^(?<numerator>\d+)/(?<denominator>\d+)$",
            RegexOptions.CultureInvariant);
        if (fractionMatch.Success)
        {
            return TryParseFraction(
                fractionMatch.Groups["numerator"].Value,
                fractionMatch.Groups["denominator"].Value,
                out inches);
        }

        return Regex.IsMatch(text, @"^\d+(?:\.\d+)?$", RegexOptions.CultureInvariant) &&
               decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out inches);
    }

    private static bool TryParseFraction(string numeratorText, string denominatorText, out decimal fraction)
    {
        fraction = 0m;
        if (!int.TryParse(numeratorText, NumberStyles.None, CultureInfo.InvariantCulture, out var numerator) ||
            !int.TryParse(denominatorText, NumberStyles.None, CultureInfo.InvariantCulture, out var denominator) ||
            numerator <= 0 ||
            numerator > denominator ||
            !IsSupportedDenominator(denominator))
        {
            return false;
        }

        fraction = numerator / (decimal)denominator;
        return true;
    }

    private static bool TryParseBareDecimal(string text, out decimal value)
        => decimal.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) ||
           decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    private static bool IsSupportedDenominator(int denominator)
        => denominator is 1 or 2 or 4 or 8 or 16 or 32 or 64 or 128 or 256;

    private static string NormalizeMarks(string? text)
        => (text ?? string.Empty)
            .Replace('\u2018', '\'')
            .Replace('\u2019', '\'')
            .Replace('\u2032', '\'')
            .Replace('\uFF07', '\'')
            .Replace('\u201C', '"')
            .Replace('\u201D', '"')
            .Replace('\u2033', '"')
            .Replace('\uFF02', '"');

    private static string FormatInches(int inches, int fractionNumerator)
    {
        if (fractionNumerator == 0)
        {
            return $"{inches}\"";
        }

        var divisor = GreatestCommonDivisor(fractionNumerator, FractionDenominator);
        var reducedNumerator = fractionNumerator / divisor;
        var reducedDenominator = FractionDenominator / divisor;
        var fractionText = $"{reducedNumerator}/{reducedDenominator}\"";

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
