using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Review;

public sealed class ReactiveDimensionProjectorTests
{
    [Fact]
    public void Project_updates_associated_dimension_measurement_text_and_primitives_from_live_edges()
    {
        var dimension = CreateDimension();
        IReadOnlyList<DimensionAssociationDto> associations =
        [
            new DimensionAssociationDto(
                dimension.DimensionId,
                "MeasuredEndpointAnchors",
                true,
                1m,
                "Resolved.")
            {
                StartAnchor = new DimensionAnchorReferenceDto("edge-a", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), "Start", 100m, 100m, 0m),
                EndAnchor = new DimensionAnchorReferenceDto("edge-b", "OpeningCandidate", Guid.NewGuid(), Guid.NewGuid(), "Start", 260m, 100m, 0m)
            }
        ];
        IReadOnlyList<MeasurableEdgeDto> liveEdges =
        [
            new MeasurableEdgeDto("edge-a", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), 40m, 1016m, 90m, 100m, 100m, 100m, 140m),
            new MeasurableEdgeDto("edge-b", "OpeningCandidate", Guid.NewGuid(), Guid.NewGuid(), 40m, 1016m, 90m, 260m, 100m, 260m, 140m)
        ];

        var projected = ReactiveDimensionProjector.Project([dimension], associations, liveEdges);

        var updated = Assert.Single(projected);
        Assert.Equal(100m, updated.DefPointX);
        Assert.Equal(260m, updated.DefPoint2X);
        Assert.Equal(160m, updated.MeasurementSourceUnits);
        Assert.Equal("13'-4\"", updated.DisplayText);
        Assert.Equal(260m, updated.LinePrimitives[1].StartX);
        Assert.Equal(260m, updated.InsertPrimitives[1].X);
    }

    [Fact]
    public void Project_preserves_custom_text_override_while_recomputing_geometry()
    {
        var dimension = CreateDimension() with
        {
            RawTextOverride = "VERIFY",
            DisplayText = "VERIFY",
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto("TEXT-1", 1, "VERIFY", 162m, 148m, 3.5m, 0m)
            ]
        };
        IReadOnlyList<DimensionAssociationDto> associations =
        [
            new DimensionAssociationDto(
                dimension.DimensionId,
                "MeasuredEndpointAnchors",
                true,
                1m,
                "Resolved.")
            {
                StartAnchor = new DimensionAnchorReferenceDto("edge-a", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), "Start", 100m, 100m, 0m),
                EndAnchor = new DimensionAnchorReferenceDto("edge-b", "OpeningCandidate", Guid.NewGuid(), Guid.NewGuid(), "Start", 260m, 100m, 0m)
            }
        ];
        IReadOnlyList<MeasurableEdgeDto> liveEdges =
        [
            new MeasurableEdgeDto("edge-a", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), 40m, 1016m, 90m, 100m, 100m, 100m, 140m),
            new MeasurableEdgeDto("edge-b", "OpeningCandidate", Guid.NewGuid(), Guid.NewGuid(), 40m, 1016m, 90m, 260m, 100m, 260m, 140m)
        ];

        var projected = ReactiveDimensionProjector.Project([dimension], associations, liveEdges);

        var updated = Assert.Single(projected);
        Assert.Equal("VERIFY", updated.DisplayText);
        Assert.Equal("VERIFY", updated.TextPrimitives[0].Text);
        Assert.Equal(160m, updated.MeasurementSourceUnits);
    }

    [Fact]
    public void Project_updates_associated_dimension_using_segment_projected_anchors()
    {
        var dimension = CreateDimension() with
        {
            DefPointX = 100m,
            DefPointY = 120m,
            DefPoint2X = 224m,
            DefPoint2Y = 120m,
            DefPoint3X = 100m,
            DefPoint3Y = 160m,
            RenderTextX = 162m,
            RenderTextY = 168m,
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto("LINE-1", 1, 100m, 160m, 100m, 120m),
                new DimensionLinePrimitiveDto("LINE-2", 2, 224m, 160m, 224m, 120m),
                new DimensionLinePrimitiveDto("LINE-3", 3, 100m, 160m, 224m, 160m)
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("INSERT-1", 1, "_Dot", 100m, 160m, 0m),
                new DimensionInsertPrimitiveDto("INSERT-2", 2, "_Dot", 224m, 160m, 0m)
            ],
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto("TEXT-1", 1, "10'-4\"", 162m, 168m, 3.5m, 0m)
            ]
        };
        var measurementContext = new MeasurementContextDto("Inch", 25.4m, 1m, 1m);
        IReadOnlyList<MeasurableEdgeDto> baseEdges =
        [
            new MeasurableEdgeDto("edge-a", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), 40m, 1016m, 90m, 100m, 100m, 100m, 140m),
            new MeasurableEdgeDto("edge-b", "OpeningCandidate", Guid.NewGuid(), Guid.NewGuid(), 40m, 1016m, 90m, 224m, 100m, 224m, 140m)
        ];
        var associations = DimensionAssociationProjector.Build([dimension], baseEdges, measurementContext);
        IReadOnlyList<MeasurableEdgeDto> liveEdges =
        [
            new MeasurableEdgeDto("edge-a", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), 40m, 1016m, 90m, 90m, 100m, 90m, 140m),
            new MeasurableEdgeDto("edge-b", "OpeningCandidate", Guid.NewGuid(), Guid.NewGuid(), 40m, 1016m, 90m, 250m, 100m, 250m, 140m)
        ];

        var projected = ReactiveDimensionProjector.Project([dimension], associations, liveEdges);

        var updated = Assert.Single(projected);
        Assert.Equal(90m, updated.DefPointX);
        Assert.Equal(120m, updated.DefPointY);
        Assert.Equal(250m, updated.DefPoint2X);
        Assert.Equal(120m, updated.DefPoint2Y);
        Assert.Equal(160m, updated.MeasurementSourceUnits);
        Assert.Equal("13'-4\"", updated.DisplayText);
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
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("INSERT-1", 1, "_Dot", 100m, 140m, 0m),
                new DimensionInsertPrimitiveDto("INSERT-2", 2, "_Dot", 224m, 140m, 0m)
            ]
        };
    }
}
