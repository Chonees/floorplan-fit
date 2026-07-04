using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.Tests.PlanSets.Projection;

public sealed class AdjustmentRecipeSummaryDtoTests
{
    [Fact]
    public void FromPlacement_maps_compression_steps_to_named_recipe_operations()
    {
        var placement = new AdjustedSitePlanPlacementDto(
            FloorToSiteScale: 1.2m,
            SiteOffsetX: 10m,
            SiteOffsetY: 20m,
            CompressionSteps:
            [
                new AdjustedCompressionStepDto(
                    "Width",
                    "Right",
                    [new AdjustedCompressionMarkerDto(50m, 2m)]),
                new AdjustedCompressionStepDto(
                    "Height",
                    "Top",
                    [new AdjustedCompressionMarkerDto(80m, 1.5m)])
            ]);

        var recipe = AdjustmentRecipeSummaryDto.FromPlacement(placement);

        Assert.Equal("v1", recipe.Version);
        Assert.Equal(1.2m, recipe.FloorToSiteScale);
        Assert.Equal(10m, recipe.SiteOffsetX);
        Assert.Equal(20m, recipe.SiteOffsetY);
        Assert.Collection(
            recipe.Operations,
            operation =>
            {
                Assert.Equal("HorizontalCompression", operation.Kind);
                Assert.Equal("Width", operation.AxisTag);
                Assert.Equal("Right", operation.Edge);
                Assert.Equal(50m, operation.Coordinate);
                Assert.Equal(2m, operation.DeltaSourceUnits);
            },
            operation =>
            {
                Assert.Equal("VerticalCompression", operation.Kind);
                Assert.Equal("Height", operation.AxisTag);
                Assert.Equal("Top", operation.Edge);
                Assert.Equal(80m, operation.Coordinate);
                Assert.Equal(1.5m, operation.DeltaSourceUnits);
            });
    }
}
