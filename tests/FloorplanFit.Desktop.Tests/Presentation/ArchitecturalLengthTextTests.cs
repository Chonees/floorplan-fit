using System.Globalization;
using FloorplanFit.Desktop.Presentation;

namespace FloorplanFit.Desktop.Tests.Presentation;

public sealed class ArchitecturalLengthTextTests
{
    [Theory]
    [InlineData("5'", 60)]
    [InlineData("60\"", 60)]
    [InlineData("5'-9\"", 69)]
    [InlineData("5' 9\"", 69)]
    [InlineData("5'9\"", 69)]
    [InlineData("5'-1/2\"", 60.5)]
    [InlineData("5'-9-1/2\"", 69.5)]
    [InlineData("5'9-1/2", 69.5)]
    [InlineData("5'-9 1/2\"", 69.5)]
    [InlineData("5'-9 1/2", 69.5)]
    [InlineData("5'-9.5\"", 69.5)]
    [InlineData("5′-9.5″", 69.5)]
    public void TryParsePositiveInches_accepts_architectural_notation(string text, double expectedInches)
    {
        var parsed = ArchitecturalLengthText.TryParsePositiveInches(
            text,
            ArchitecturalLengthDefaultUnit.Inches,
            out var totalInches);

        Assert.True(parsed);
        Assert.Equal((decimal)expectedInches, totalInches);
    }

    [Fact]
    public void TryParsePositiveInches_preserves_exact_one_over_256_inch()
    {
        var parsed = ArchitecturalLengthText.TryParsePositiveInches(
            "1/256\"",
            ArchitecturalLengthDefaultUnit.Inches,
            out var totalInches);

        Assert.True(parsed);
        Assert.Equal(0.00390625m, totalInches);
    }

    [Fact]
    public void TryParsePositiveInches_and_TryAdjustInches_reject_a_mixed_number_that_overflows_decimal()
    {
        var text = $"{decimal.MaxValue.ToString(CultureInfo.InvariantCulture)} 1/2\"";

        Assert.False(ArchitecturalLengthText.TryParsePositiveInches(
            text,
            ArchitecturalLengthDefaultUnit.Inches,
            out _));
        Assert.False(ArchitecturalLengthText.TryAdjustInches(
            text,
            ArchitecturalLengthDefaultUnit.Inches,
            0.5m,
            out var formatted));
        Assert.Equal(string.Empty, formatted);
    }

    [Theory]
    [InlineData(ArchitecturalLengthDefaultUnit.Feet, "5.75", 69)]
    [InlineData(ArchitecturalLengthDefaultUnit.Inches, "5.75", 5.75)]
    [InlineData(ArchitecturalLengthDefaultUnit.Inches, "+1", 1)]
    [InlineData(ArchitecturalLengthDefaultUnit.Inches, "1e2", 100)]
    public void TryParsePositiveInches_applies_the_caller_default_unit_to_bare_decimals(
        ArchitecturalLengthDefaultUnit defaultUnit,
        string text,
        double expectedInches)
    {
        var parsed = ArchitecturalLengthText.TryParsePositiveInches(text, defaultUnit, out var totalInches);

        Assert.True(parsed);
        Assert.Equal((decimal)expectedInches, totalInches);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1,000.5")]
    [InlineData("5'-12\"")]
    [InlineData("5'-9-1/3\"")]
    [InlineData("5'--9\"")]
    [InlineData("5'--1/2\"")]
    [InlineData("5' -9\"")]
    [InlineData("5'- 9\"")]
    [InlineData("5' - 9\"")]
    public void TryParsePositiveInches_rejects_invalid_ui_lengths(string text)
    {
        var parsed = ArchitecturalLengthText.TryParsePositiveInches(
            text,
            ArchitecturalLengthDefaultUnit.Inches,
            out _);

        Assert.False(parsed);
    }

    [Fact]
    public void TryParsePositiveInches_accepts_current_culture_comma_decimal_without_thousands_grouping()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("es-AR");

            Assert.True(ArchitecturalLengthText.TryParsePositiveInches(
                "5,75",
                ArchitecturalLengthDefaultUnit.Inches,
                out var totalInches));
            Assert.Equal(5.75m, totalInches);
            Assert.False(ArchitecturalLengthText.TryParsePositiveInches(
                "1.000,5",
                ArchitecturalLengthDefaultUnit.Inches,
                out _));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void TryAdjustInches_counts_down_in_exact_half_inch_steps_across_a_foot_boundary()
    {
        var current = "39'-0\"";

        foreach (var expected in new[] { "38'-11 1/2\"", "38'-11\"", "38'-10 1/2\"" })
        {
            Assert.True(ArchitecturalLengthText.TryAdjustInches(
                current,
                ArchitecturalLengthDefaultUnit.Inches,
                -0.5m,
                out var adjusted));
            Assert.Equal(expected, adjusted);
            current = adjusted;
        }
    }

    [Fact]
    public void TryAdjustInches_increments_by_exactly_one_half_inch()
    {
        var adjusted = ArchitecturalLengthText.TryAdjustInches(
            "38'-11 1/2\"",
            ArchitecturalLengthDefaultUnit.Inches,
            0.5m,
            out var formatted);

        Assert.True(adjusted);
        Assert.Equal("39'-0\"", formatted);
    }

    [Theory]
    [InlineData(ArchitecturalLengthDefaultUnit.Feet, "5.75", "5'-9 1/2\"")]
    [InlineData(ArchitecturalLengthDefaultUnit.Inches, "5.75", "6 1/4\"")]
    public void TryAdjustInches_applies_the_caller_default_unit_to_bare_decimals(
        ArchitecturalLengthDefaultUnit defaultUnit,
        string text,
        string expected)
    {
        var adjusted = ArchitecturalLengthText.TryAdjustInches(text, defaultUnit, 0.5m, out var formatted);

        Assert.True(adjusted);
        Assert.Equal(expected, formatted);
    }

    [Fact]
    public void TryAdjustInches_adds_before_formatting_a_supported_arbitrary_fraction()
    {
        var adjusted = ArchitecturalLengthText.TryAdjustInches(
            "1/256\"",
            ArchitecturalLengthDefaultUnit.Inches,
            0.5m,
            out var formatted);

        Assert.True(adjusted);
        Assert.Equal("129/256\"", formatted);
    }

    [Theory]
    [InlineData("not a length", 0.5)]
    [InlineData("1/2\"", -0.5)]
    [InlineData("1/2\"", -1)]
    public void TryAdjustInches_rejects_invalid_input_and_non_positive_results(string text, double deltaInches)
    {
        var adjusted = ArchitecturalLengthText.TryAdjustInches(
            text,
            ArchitecturalLengthDefaultUnit.Inches,
            (decimal)deltaInches,
            out var formatted);

        Assert.False(adjusted);
        Assert.Equal(string.Empty, formatted);
    }

    [Theory]
    [InlineData(2, "2\"")]
    [InlineData(6.5, "6 1/2\"")]
    [InlineData(14, "1'-2\"")]
    [InlineData(14.00390625, "1'-2 1/256\"")]
    [InlineData(0.0078125, "1/128\"")]
    [InlineData(11.998046875, "1'-0\"")]
    public void FormatInches_emits_canonical_reduced_architectural_text(double inches, string expected)
    {
        Assert.Equal(expected, ArchitecturalLengthText.FormatInches((decimal)inches));
    }

    [Theory]
    [InlineData("5.75", "5'-9\"", 5.75)]
    [InlineData("10", "10'-0\"", 10)]
    public void Decimal_feet_and_architectural_text_produce_the_same_decimal_feet(
        string decimalFeetText,
        string architecturalText,
        double expectedFeet)
    {
        Assert.True(ArchitecturalLengthText.TryParsePositiveInches(
            decimalFeetText,
            ArchitecturalLengthDefaultUnit.Feet,
            out var decimalFeetInches));
        Assert.True(ArchitecturalLengthText.TryParsePositiveInches(
            architecturalText,
            ArchitecturalLengthDefaultUnit.Feet,
            out var architecturalInches));

        Assert.Equal(decimalFeetInches, architecturalInches);
        Assert.Equal((decimal)expectedFeet, architecturalInches / 12m);
    }
}
