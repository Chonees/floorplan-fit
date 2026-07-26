using FloorplanFit.Application.PlanSets.Projection;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets.Projection;

public sealed class ElectricalRecipeProjectionTests
{
    [Fact]
    public void ProjectPoint_maps_electrical_to_floor_applies_pinches_then_maps_to_output()
    {
        var registration = new SheetRegistrationTransform(
            scale: 2m,
            rotationDegrees: 0m,
            translateX: 10m,
            translateY: 20m);
        var recipe = new AdjustmentRecipeSummaryDto(
            "v1",
            FloorToSiteScale: 3m,
            SiteOffsetX: 100m,
            SiteOffsetY: 200m,
            Operations:
            [
                new AdjustmentRecipeOperationDto("HorizontalCompression", "Width", "Right", 50m, 2m),
                new AdjustmentRecipeOperationDto("VerticalCompression", "Height", "Top", 80m, 5m)
            ]);

        var projected = ElectricalRecipeProjection.ProjectPoint(30m, 40m, registration, recipe);

        Assert.Equal(304m, projected.X);
        Assert.Equal(485m, projected.Y);
    }

    [Fact]
    public void ProjectPoint_accumulates_multiple_markers_and_keeps_points_before_pinch_stationary()
    {
        var registration = new SheetRegistrationTransform(
            scale: 1m,
            rotationDegrees: 0m,
            translateX: 0m,
            translateY: 0m);
        var recipe = new AdjustmentRecipeSummaryDto(
            "v1",
            FloorToSiteScale: 1m,
            SiteOffsetX: 0m,
            SiteOffsetY: 0m,
            Operations:
            [
                new AdjustmentRecipeOperationDto("HorizontalCompression", "Width", "Right", 50m, 2m),
                new AdjustmentRecipeOperationDto("HorizontalCompression", "Width", "Right", 70m, 3m),
                new AdjustmentRecipeOperationDto("VerticalCompression", "Height", "Bottom", 20m, 4m)
            ]);

        var beforeRightPinch = ElectricalRecipeProjection.ProjectPoint(40m, 30m, registration, recipe);
        var afterBothRightPinches = ElectricalRecipeProjection.ProjectPoint(80m, 10m, registration, recipe);

        Assert.Equal((40m, 30m), beforeRightPinch);
        Assert.Equal((75m, 14m), afterBothRightPinches);
    }

    [Fact]
    public void ProjectPoint_treats_tiny_coordinate_jitter_as_same_pinch_edge()
    {
        var registration = new SheetRegistrationTransform(
            scale: 1m,
            rotationDegrees: 0m,
            translateX: 0m,
            translateY: 0m);
        var recipe = new AdjustmentRecipeSummaryDto(
            "v1",
            FloorToSiteScale: 1m,
            SiteOffsetX: 0m,
            SiteOffsetY: 0m,
            Operations:
            [
                new AdjustmentRecipeOperationDto("VerticalCompression", "Height", "Top", 90m, 5m),
                new AdjustmentRecipeOperationDto("VerticalCompression", "Height", "Top", 85m, 5m)
            ]);

        var projected = ElectricalRecipeProjection.ProjectPoint(10m, 89.995m, registration, recipe);

        Assert.Equal((10m, 79.995m), projected);
    }

    [Fact]
    public void ProjectPoint_normalizes_outline_before_applying_recipe()
    {
        var registration = new SheetRegistrationTransform(
            scale: 1m,
            rotationDegrees: 0m,
            translateX: 0m,
            translateY: 0m);
        var normalization = new ProjectedPlanSheetOutlineNormalization(
            SourceMinX: 0m,
            SourceMinY: 0m,
            SourceMaxX: 470m,
            SourceMaxY: 930m,
            ScaleX: 468m / 470m,
            ScaleY: 1m,
            AnchorX: "Max",
            AnchorY: "Max",
            Status: "MismatchRequiresNormalization",
            Reason: "test");
        var recipe = new AdjustmentRecipeSummaryDto(
            "v1",
            FloorToSiteScale: 1m,
            SiteOffsetX: 0m,
            SiteOffsetY: 0m,
            Operations:
            [
                new AdjustmentRecipeOperationDto("HorizontalCompression", "Width", "Right", 469m, 3.6m)
            ]);

        var projectedMin = ElectricalRecipeProjection.ProjectPoint(0m, 0m, registration, recipe, normalization);
        var projectedMax = ElectricalRecipeProjection.ProjectPoint(470m, 0m, registration, recipe, normalization);

        // Compare to ten decimal places. The projection divides and multiplies decimals, so
        // the exact result carries representation noise around the twenty-sixth decimal;
        // ten places is still sixteen orders of magnitude tighter than any geometric error
        // this contract exists to catch.
        Assert.Equal(464.4m, projectedMax.X - projectedMin.X, 10);
    }

    [Fact]
    public void ProjectPoint_applies_registration_rotation_before_floor_recipe()
    {
        var registration = new SheetRegistrationTransform(
            scale: 1m,
            rotationDegrees: 90m,
            translateX: 0m,
            translateY: 0m);
        var recipe = new AdjustmentRecipeSummaryDto(
            "v1",
            FloorToSiteScale: 1m,
            SiteOffsetX: 0m,
            SiteOffsetY: 0m,
            Operations:
            [
                new AdjustmentRecipeOperationDto("VerticalCompression", "Height", "Top", 5m, 2m)
            ]);

        var projected = ElectricalRecipeProjection.ProjectPoint(10m, 0m, registration, recipe);

        Assert.Equal((0m, 8m), projected);
    }
}
