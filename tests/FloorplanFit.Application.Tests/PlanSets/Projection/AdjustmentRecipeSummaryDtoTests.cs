using FloorplanFit.Contracts.FloorPlans;
using System.Text.Json;

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

    [Fact]
    public void V1_json_without_stretch_actions_remains_readable()
    {
        const string json = """
            {
              "Version": "v1",
              "FloorToSiteScale": 1,
              "SiteOffsetX": 0,
              "SiteOffsetY": 0,
              "Operations": []
            }
            """;

        var recipe = JsonSerializer.Deserialize<AdjustmentRecipeSummaryDto>(json);

        Assert.NotNull(recipe);
        Assert.Equal("v1", recipe.Version);
        Assert.Empty(recipe.StretchActions);
    }

    [Fact]
    public void FromPlacement_persists_one_v2_action_with_two_target_spans_and_one_delta()
    {
        var firstPathId = Guid.NewGuid();
        var secondPathId = Guid.NewGuid();
        var action = new AdjustmentRecipeStretchActionDto(
            ActionId: "paired-wall",
            AxisTag: "Width",
            Edge: "Right",
            CutCoordinate: 5m,
            DeltaSourceUnits: 2m,
            MaxDeltaSourceUnits: 4m,
            CoordinateTolerance: 0.001m,
            CanonicalSourceBounds: new AdjustmentRecipeBoundsDto(0m, 0m, 20m, 10m),
            TargetSpans:
            [
                new AdjustmentRecipeTargetSpanDto("LINE:1", firstPathId, 0, 0m, 0m, 10m, 0m, 1),
                new AdjustmentRecipeTargetSpanDto("LINE:2", secondPathId, 0, 0m, 4m, 10m, 4m, 1)
            ],
            CanonicalEntityRoles:
            [
                new AdjustmentRecipeEntityRoleDto("LINE:1", firstPathId, 0, "Stretch", [1]),
                new AdjustmentRecipeEntityRoleDto("LINE:2", secondPathId, 0, "Stretch", [1])
            ]);
        var placement = new AdjustedSitePlanPlacementDto(
            FloorToSiteScale: 1m,
            SiteOffsetX: 0m,
            SiteOffsetY: 0m,
            CompressionSteps: [])
        {
            StretchActions = [action]
        };

        var recipe = AdjustmentRecipeSummaryDto.FromPlacement(placement);

        Assert.Equal("v2", recipe.Version);
        Assert.Empty(recipe.Operations);
        var persisted = Assert.Single(recipe.StretchActions);
        Assert.Equal(2m, persisted.DeltaSourceUnits);
        Assert.Equal(2, persisted.TargetSpans.Count);
        Assert.Equal(2, persisted.CanonicalEntityRoles.Count);
    }
}
