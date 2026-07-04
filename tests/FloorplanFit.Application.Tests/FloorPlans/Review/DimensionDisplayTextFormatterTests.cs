using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Contracts.FloorPlans;
using Xunit;

namespace FloorplanFit.Application.Tests.FloorPlans.Review;

public sealed class DimensionDisplayTextFormatterTests
{
    [Fact]
    public void Resolve_formats_half_inches_in_architectural_dimension_text()
    {
        var dimension = CreateDimension(rawTextOverride: string.Empty);

        var result = DimensionDisplayTextFormatter.Resolve(
            dimension,
            measurementSourceUnits: 123.5m,
            generatedDisplayTextSource: "IntervalBinding");

        Assert.Equal("10'-3 1/2\"", result.DisplayText);
        Assert.Equal("IntervalBinding", result.DisplayTextSource);
    }

    [Fact]
    public void Resolve_replaces_placeholder_with_fractional_architectural_measurement_and_suffix()
    {
        var dimension = CreateDimension(rawTextOverride: "<> TO FACE OF WALL");

        var result = DimensionDisplayTextFormatter.Resolve(
            dimension,
            measurementSourceUnits: 123.5m,
            generatedDisplayTextSource: "IntervalBinding");

        Assert.Equal("10'-3 1/2\" TO FACE OF WALL", result.DisplayText);
        Assert.Equal("IntervalBinding", result.DisplayTextSource);
    }

    private static DimensionDto CreateDimension(string rawTextOverride)
    {
        return new DimensionDto(
            Guid.NewGuid(),
            "DIMENSION:AB12",
            "DIMS",
            "DIMENSION",
            "*D169",
            "10'-4\"",
            "GeometryBlock",
            rawTextOverride,
            124m,
            3149.6m,
            "Inch",
            0,
            0m,
            0m,
            100m,
            100m,
            0m,
            224m,
            100m,
            0m,
            100m,
            140m,
            0m,
            0.99m,
            null,
            1);
    }
}
