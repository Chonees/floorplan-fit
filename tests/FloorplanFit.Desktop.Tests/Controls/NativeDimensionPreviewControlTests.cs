using Avalonia;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls;
using FloorplanFit.Desktop.Controls.Preview;
using Xunit;

namespace FloorplanFit.Desktop.Tests.Controls;

public sealed class NativeDimensionPreviewControlTests
{
    [Fact]
    public void TryResolveDimensionHit_prefers_dimension_text_and_lines_before_generic_geometry()
    {
        var pathId = Guid.NewGuid();
        var viewport = FloorPlanPreviewGeometry.CalculateViewport(
            [new GeometryPathDto(pathId, false, [new GeometrySegmentDto(pathId, 1, 0m, 0m, 300m, 0m)])],
            new Rect(0, 0, 800, 600),
            48d)!.Value;
        var dimension = CreateDimension();
        var pointer = viewport.Project(162m, 148m);

        var hit = FloorPlanPreviewControl.TryResolveDimensionHit(
            [dimension],
            viewport,
            pointer,
            hitTolerancePixels: 8d);

        Assert.NotNull(hit);
        Assert.Equal(dimension.DimensionId, hit.Value.Dimension.DimensionId);
        Assert.Equal(FloorPlanPreviewControl.DimensionHandleKind.TextAnchor, hit.Value.SuggestedHandle);
    }

    [Fact]
    public void ApplyDimensionHandleDelta_moves_native_dimension_in_world_units_and_updates_primitives()
    {
        var dimension = CreateDimension();

        var edited = FloorPlanPreviewControl.ApplyDimensionHandleDelta(
            dimension,
            FloorPlanPreviewControl.DimensionHandleKind.SecondDefinitionPoint,
            24m,
            0m);

        Assert.Equal(248m, edited.DefPoint2X);
        Assert.Equal(148m, edited.MeasurementSourceUnits);
        Assert.Equal(3759.2m, edited.MeasurementMillimeters);
        Assert.Equal("12'-4\"", edited.DisplayText);
        Assert.Equal(248m, edited.LinePrimitives[1].StartX);
        Assert.Equal(248m, edited.LinePrimitives[1].EndX);
        Assert.Equal(248m, edited.LinePrimitives[2].EndX);
        Assert.Equal("12'-4\"", edited.TextPrimitives[0].Text);
        Assert.Equal(248m, edited.InsertPrimitives[1].X);
    }

    [Fact]
    public void BuildRenderedDimensions_preserves_authored_dimension_geometry_when_there_is_no_manual_edit()
    {
        var dimension = CreateDimension();

        var rendered = DimensionPreviewProjector.BuildRenderedDimensions([dimension], activeEdit: null);

        var preserved = Assert.Single(rendered);
        Assert.Same(dimension, preserved);
        Assert.Equal(100m, preserved.LinePrimitives[0].StartX);
        Assert.Equal(224m, preserved.LinePrimitives[2].EndX);
        Assert.Equal(162m, preserved.TextPrimitives[0].X);
        Assert.Equal(100m, preserved.InsertPrimitives[0].X);
    }

    [Fact]
    public void ResolveSnappedWorldPoint_prefers_major_grid_when_within_snapping_tolerance()
    {
        var cadViewport = new CadViewportContext(
            WorldUnitsPerPixel: 1d,
            MinorGridSpacingWorld: 20d,
            MajorGridSpacingWorld: 100d,
            SnappingToleranceWorld: 1d);

        var snapped = FloorPlanPreviewControl.ResolveSnappedWorldPoint(
            new Point(99.4, 200.4),
            cadViewport,
            [new Point(99.7, 200.1)],
            enableGridSnap: true);

        Assert.Equal(100d, snapped.X);
        Assert.Equal(200d, snapped.Y);
    }

    private static DimensionDto CreateDimension()
    {
        return new DimensionDto(
            Guid.NewGuid(),
            "DIMENSION:AB12",
            "DIMS",
            "DIMENSION",
            "*D169",
            "10'-4\"",
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
            SourceHandle = "AB12",
            RenderTextX = 162m,
            RenderTextY = 148m,
            RenderTextHeight = 3.5m,
            RenderTextStyleName = "ARCH",
            RenderTextAttachmentPoint = "MiddleCenter",
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto("LINE-1", 1, 100m, 140m, 100m, 100m),
                new DimensionLinePrimitiveDto("LINE-2", 2, 224m, 140m, 224m, 100m),
                new DimensionLinePrimitiveDto("LINE-3", 3, 100m, 140m, 224m, 140m)
            ],
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto("TEXT-1", 1, "10'-4\"", 162m, 148m, 3.5m, 0m)
                {
                    StyleName = "ARCH",
                    AttachmentPoint = "MiddleCenter"
                }
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("INSERT-1", 1, "_Dot", 100m, 140m, 0m),
                new DimensionInsertPrimitiveDto("INSERT-2", 2, "_Dot", 224m, 140m, 0m)
            ]
        };
    }
}
