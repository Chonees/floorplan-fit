using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.SitePlanAdjustment;

public sealed class ChangedDimensionDetectorTests
{
    [Fact]
    public void ResolveAffectedDimensionIds_marks_dimension_when_visible_number_changes()
    {
        var dimension = CreateDimension(displayText: "10'-4\"");
        var adjusted = dimension with { DisplayText = "10'-3\"" };

        var affectedIds = ChangedDimensionDetector.ResolveAffectedDimensionIds([dimension], [adjusted]);

        Assert.Equal([dimension.DimensionId], affectedIds);
    }

    [Fact]
    public void ResolveAffectedDimensionIds_marks_dimension_when_geometry_changes_even_if_visible_number_does_not()
    {
        var dimension = CreateDimension(displayText: "10'-4\"");
        var adjusted = dimension with
        {
            DefPoint2X = dimension.DefPoint2X - 1m,
            LinePrimitives =
            [
                dimension.LinePrimitives[0],
                dimension.LinePrimitives[1] with { StartX = dimension.LinePrimitives[1].StartX - 1m, EndX = dimension.LinePrimitives[1].EndX - 1m },
                dimension.LinePrimitives[2] with { EndX = dimension.LinePrimitives[2].EndX - 1m }
            ]
        };

        var affectedIds = ChangedDimensionDetector.ResolveAffectedDimensionIds([dimension], [adjusted]);

        Assert.Equal([dimension.DimensionId], affectedIds);
    }

    [Fact]
    public void ResolveAffectedDimensionIds_ignores_unchanged_dimensions_even_when_instances_are_different()
    {
        var dimension = CreateDimension(displayText: "10'-4\"");
        var adjusted = dimension with
        {
            LinePrimitives = dimension.LinePrimitives.Select(item => item with { }).ToArray(),
            TextPrimitives = dimension.TextPrimitives.Select(item => item with { }).ToArray(),
            InsertPrimitives = dimension.InsertPrimitives.Select(item => item with { }).ToArray()
        };

        var affectedIds = ChangedDimensionDetector.ResolveAffectedDimensionIds([dimension], [adjusted]);

        Assert.Empty(affectedIds);
    }

    private static DimensionDto CreateDimension(string displayText)
    {
        var dimensionId = Guid.NewGuid();
        return new DimensionDto(
            dimensionId,
            "DIMENSION:AB12",
            "DIMS",
            "DIMENSION",
            "*D169",
            displayText,
            "GeometryBlock",
            string.Empty,
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
            1)
        {
            RenderTextX = 162m,
            RenderTextY = 148m,
            RenderTextHeight = 3.5m,
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto("LINE-1", 1, 100m, 140m, 100m, 100m),
                new DimensionLinePrimitiveDto("LINE-2", 2, 224m, 140m, 224m, 100m),
                new DimensionLinePrimitiveDto("LINE-3", 3, 100m, 140m, 224m, 140m)
            ],
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto("TEXT-1", 1, displayText, 162m, 148m, 3.5m, 0m)
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("INSERT-1", 1, "_Dot", 100m, 140m, 0m),
                new DimensionInsertPrimitiveDto("INSERT-2", 2, "_Dot", 224m, 140m, 0m)
            ]
        };
    }
}
