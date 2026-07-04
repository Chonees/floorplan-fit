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

    [Fact]
    public void Project_remaps_projected_anchor_to_the_best_live_segment_after_topology_split()
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
        var wallCandidateId = Guid.NewGuid();
        var openingCandidateId = Guid.NewGuid();
        var wallPathId = Guid.NewGuid();
        var openingPathId = Guid.NewGuid();
        var measurementContext = new MeasurementContextDto("Inch", 25.4m, 1m, 1m);
        IReadOnlyList<MeasurableEdgeDto> baseEdges =
        [
            new MeasurableEdgeDto($"WallCandidate:{wallCandidateId:N}:{wallPathId:N}:1", "WallCandidate", wallCandidateId, wallPathId, 40m, 1016m, 90m, 100m, 100m, 100m, 140m),
            new MeasurableEdgeDto($"OpeningCandidate:{openingCandidateId:N}:{openingPathId:N}:1", "OpeningCandidate", openingCandidateId, openingPathId, 40m, 1016m, 90m, 224m, 100m, 224m, 140m)
        ];
        var associations = DimensionAssociationProjector.Build([dimension], baseEdges, measurementContext);
        var startAnchor = associations[0].StartAnchor!;
        var endAnchor = associations[0].EndAnchor!;
        var liveEdges =
            new[]
            {
                new MeasurableEdgeDto(startAnchor.EdgeKey, startAnchor.SourceArtifactKind, startAnchor.SourceArtifactId, startAnchor.GeometryPathId, 40m, 1016m, 90m, 90m, 100m, 90m, 140m),
                new MeasurableEdgeDto(endAnchor.EdgeKey, endAnchor.SourceArtifactKind, endAnchor.SourceArtifactId, endAnchor.GeometryPathId, 20m, 508m, 90m, 240m, 100m, 240m, 120m),
                new MeasurableEdgeDto($"OpeningCandidate:{endAnchor.SourceArtifactId:N}:{endAnchor.GeometryPathId:N}:2", endAnchor.SourceArtifactKind, endAnchor.SourceArtifactId, endAnchor.GeometryPathId, 40m, 1016m, 90m, 240m, 120m, 240m, 160m)
            };

        var projected = ReactiveDimensionProjector.Project([dimension], associations, liveEdges);

        var updated = Assert.Single(projected);
        Assert.Equal(90m, updated.DefPointX);
        Assert.Equal(120m, updated.DefPointY);
        Assert.Equal(240m, updated.DefPoint2X);
        Assert.Equal(120m, updated.DefPoint2Y);
        Assert.Equal(150m, updated.MeasurementSourceUnits);
        Assert.Equal("12'-6\"", updated.DisplayText);
        Assert.Equal(240m, updated.LinePrimitives[1].StartX);
        Assert.Equal(240m, updated.InsertPrimitives[1].X);
    }

    [Fact]
    public void Project_updates_edited_dimension_when_a_resolved_association_is_available()
    {
        var dimension = CreateDimension() with
        {
            IsEdited = true,
            IsDirty = true,
            RenderTextX = 170m,
            RenderTextY = 152m
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
        Assert.Equal(260m, updated.DefPoint2X);
        Assert.Equal(160m, updated.MeasurementSourceUnits);
        Assert.Equal("13'-4\"", updated.DisplayText);
        Assert.True(updated.IsEdited);
        Assert.True(updated.IsDirty);
    }

    [Fact]
    public void Project_rebuilds_resolved_ordinate_x_dimensions_from_datum_and_feature_anchors()
    {
        var dimension = CreateOrdinateXDimension();
        IReadOnlyList<DimensionAssociationDto> associations =
        [
            new DimensionAssociationDto(
                dimension.DimensionId,
                "MeasuredEndpointAnchors",
                true,
                1m,
                "Resolved ordinate anchors.")
            {
                StartAnchor = new DimensionAnchorReferenceDto("edge-a", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), "Start", 100m, 100m, 0m),
                EndAnchor = new DimensionAnchorReferenceDto("edge-b", "OpeningCandidate", Guid.NewGuid(), Guid.NewGuid(), "Start", 320m, 140m, 0m)
            }
        ];
        IReadOnlyList<DimensionBindingDto> bindings =
        [
            new DimensionBindingDto(
                dimension.DimensionId,
                "OrdinateX",
                true,
                0.95m,
                "Resolved ordinate X binding.",
                false)
            {
                Anchors =
                [
                    associations[0].StartAnchor!,
                    associations[0].EndAnchor!
                ],
                MeasuredSpan = new DimensionMeasuredSpanDto("OrdinateX", 100m, 320m, 0m)
            }
        ];
        IReadOnlyList<MeasurableEdgeDto> liveEdges =
        [
            new MeasurableEdgeDto("edge-a", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), 40m, 1016m, 90m, 100m, 100m, 100m, 140m),
            new MeasurableEdgeDto("edge-b", "OpeningCandidate", Guid.NewGuid(), Guid.NewGuid(), 40m, 1016m, 90m, 320m, 140m, 320m, 180m)
        ];

        var projected = ReactiveDimensionProjector.Project([dimension], associations, liveEdges, bindings);

        var updated = Assert.Single(projected);
        Assert.NotSame(dimension, updated);
        Assert.Equal(100m, updated.DefPointX);
        Assert.Equal(100m, updated.DefPointY);
        Assert.Equal(320m, updated.DefPoint2X);
        Assert.Equal(140m, updated.DefPoint2Y);
        Assert.Equal(350m, updated.DefPoint3X);
        Assert.Equal(170m, updated.DefPoint3Y);
        Assert.Equal(220m, updated.MeasurementSourceUnits);
        Assert.Equal("18'-4\"", updated.DisplayText);
        Assert.Equal(320m, updated.LinePrimitives[0].StartX);
        Assert.Equal(350m, updated.LinePrimitives[0].EndX);
        Assert.Equal(350m, updated.LinePrimitives[1].StartX);
        Assert.Equal(380m, updated.LinePrimitives[1].EndX);
        Assert.Equal(364m, updated.TextPrimitives[0].X);
        Assert.Equal(320m, updated.InsertPrimitives[0].X);
    }

    [Fact]
    public void Project_rebuilds_resolved_ordinate_y_dimensions_from_datum_and_feature_anchors()
    {
        var dimension = CreateOrdinateYDimension();
        IReadOnlyList<DimensionAssociationDto> associations =
        [
            new DimensionAssociationDto(
                dimension.DimensionId,
                "MeasuredEndpointAnchors",
                true,
                1m,
                "Resolved ordinate anchors.")
            {
                StartAnchor = new DimensionAnchorReferenceDto("edge-a", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), "Start", 100m, 100m, 0m),
                EndAnchor = new DimensionAnchorReferenceDto("edge-b", "OpeningCandidate", Guid.NewGuid(), Guid.NewGuid(), "Start", 140m, 320m, 0m)
            }
        ];
        IReadOnlyList<DimensionBindingDto> bindings =
        [
            new DimensionBindingDto(
                dimension.DimensionId,
                "OrdinateY",
                true,
                0.95m,
                "Resolved ordinate Y binding.",
                false)
            {
                Anchors =
                [
                    associations[0].StartAnchor!,
                    associations[0].EndAnchor!
                ],
                MeasuredSpan = new DimensionMeasuredSpanDto("OrdinateY", 100m, 320m, 90m)
            }
        ];
        IReadOnlyList<MeasurableEdgeDto> liveEdges =
        [
            new MeasurableEdgeDto("edge-a", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), 40m, 1016m, 0m, 100m, 100m, 140m, 100m),
            new MeasurableEdgeDto("edge-b", "OpeningCandidate", Guid.NewGuid(), Guid.NewGuid(), 40m, 1016m, 0m, 140m, 320m, 180m, 320m)
        ];

        var projected = ReactiveDimensionProjector.Project([dimension], associations, liveEdges, bindings);

        var updated = Assert.Single(projected);
        Assert.Equal(100m, updated.DefPointX);
        Assert.Equal(100m, updated.DefPointY);
        Assert.Equal(140m, updated.DefPoint2X);
        Assert.Equal(320m, updated.DefPoint2Y);
        Assert.Equal(170m, updated.DefPoint3X);
        Assert.Equal(350m, updated.DefPoint3Y);
        Assert.Equal(220m, updated.MeasurementSourceUnits);
        Assert.Equal("18'-4\"", updated.DisplayText);
        Assert.Equal(320m, updated.LinePrimitives[0].StartY);
        Assert.Equal(350m, updated.LinePrimitives[0].EndY);
        Assert.Equal(364m, updated.TextPrimitives[0].Y);
    }

    [Fact]
    public void Project_rebuilds_resolved_radius_dimensions_from_center_and_feature_anchors()
    {
        var dimension = CreateRadiusDimension();
        IReadOnlyList<DimensionAssociationDto> associations =
        [
            new DimensionAssociationDto(
                dimension.DimensionId,
                "MeasuredEndpointAnchors",
                true,
                1m,
                "Resolved radius anchors.")
            {
                StartAnchor = new DimensionAnchorReferenceDto("edge-center", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), "Start", 100m, 100m, 0m),
                EndAnchor = new DimensionAnchorReferenceDto("edge-feature", "OpeningCandidate", Guid.NewGuid(), Guid.NewGuid(), "Start", 100m, 200m, 0m)
            }
        ];
        IReadOnlyList<DimensionBindingDto> bindings =
        [
            new DimensionBindingDto(
                dimension.DimensionId,
                "Radius",
                true,
                0.95m,
                "Resolved radius binding.",
                false)
            {
                Anchors = [associations[0].StartAnchor!, associations[0].EndAnchor!],
                MeasuredSpan = new DimensionMeasuredSpanDto("Radial", 0m, 100m, 90m)
            }
        ];
        IReadOnlyList<MeasurableEdgeDto> liveEdges =
        [
            new MeasurableEdgeDto("edge-center", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), 40m, 1016m, 0m, 100m, 100m, 140m, 100m),
            new MeasurableEdgeDto("edge-feature", "OpeningCandidate", Guid.NewGuid(), Guid.NewGuid(), 0m, 0m, 0m, 100m, 200m, 100m, 200m)
        ];

        var projected = ReactiveDimensionProjector.Project([dimension], associations, liveEdges, bindings);

        var updated = Assert.Single(projected);
        Assert.Equal(100m, updated.DefPointX);
        Assert.Equal(100m, updated.DefPointY);
        Assert.Equal(100m, updated.DefPoint2X);
        Assert.Equal(200m, updated.DefPoint2Y);
        Assert.Equal(60m, updated.DefPoint3X);
        Assert.Equal(240m, updated.DefPoint3Y);
        Assert.Equal(100m, updated.MeasurementSourceUnits);
        Assert.Equal("8'-4\"", updated.DisplayText);
        Assert.Equal(100m, updated.LinePrimitives[0].StartX);
        Assert.Equal(200m, updated.LinePrimitives[0].StartY);
        Assert.Equal(60m, updated.LinePrimitives[0].EndX);
        Assert.Equal(240m, updated.LinePrimitives[0].EndY);
        Assert.Equal(50m, updated.TextPrimitives[0].X);
        Assert.Equal(260m, updated.TextPrimitives[0].Y);
    }

    [Fact]
    public void Project_rebuilds_resolved_diameter_dimensions_from_opposite_feature_anchors()
    {
        var dimension = CreateDiameterDimension();
        IReadOnlyList<DimensionAssociationDto> associations =
        [
            new DimensionAssociationDto(
                dimension.DimensionId,
                "MeasuredEndpointAnchors",
                true,
                1m,
                "Resolved diameter anchors.")
            {
                StartAnchor = new DimensionAnchorReferenceDto("edge-a", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), "Start", 100m, 40m, 0m),
                EndAnchor = new DimensionAnchorReferenceDto("edge-b", "OpeningCandidate", Guid.NewGuid(), Guid.NewGuid(), "Start", 100m, 160m, 0m)
            }
        ];
        IReadOnlyList<DimensionBindingDto> bindings =
        [
            new DimensionBindingDto(
                dimension.DimensionId,
                "Diameter",
                true,
                0.95m,
                "Resolved diameter binding.",
                false)
            {
                Anchors = [associations[0].StartAnchor!, associations[0].EndAnchor!],
                MeasuredSpan = new DimensionMeasuredSpanDto("Radial", 0m, 120m, 90m)
            }
        ];
        IReadOnlyList<MeasurableEdgeDto> liveEdges =
        [
            new MeasurableEdgeDto("edge-a", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), 0m, 0m, 0m, 100m, 40m, 100m, 40m),
            new MeasurableEdgeDto("edge-b", "OpeningCandidate", Guid.NewGuid(), Guid.NewGuid(), 0m, 0m, 0m, 100m, 160m, 100m, 160m)
        ];

        var projected = ReactiveDimensionProjector.Project([dimension], associations, liveEdges, bindings);

        var updated = Assert.Single(projected);
        Assert.Equal(100m, updated.DefPointX);
        Assert.Equal(40m, updated.DefPointY);
        Assert.Equal(100m, updated.DefPoint2X);
        Assert.Equal(160m, updated.DefPoint2Y);
        Assert.Equal(40m, updated.DefPoint3X);
        Assert.Equal(130m, updated.DefPoint3Y);
        Assert.Equal(120m, updated.MeasurementSourceUnits);
        Assert.Equal("10'-0\"", updated.DisplayText);
        Assert.Equal(100m, updated.LinePrimitives[0].StartX);
        Assert.Equal(40m, updated.LinePrimitives[0].StartY);
        Assert.Equal(100m, updated.LinePrimitives[0].EndX);
        Assert.Equal(160m, updated.LinePrimitives[0].EndY);
        Assert.Equal(25m, updated.TextPrimitives[0].X);
        Assert.Equal(160m, updated.TextPrimitives[0].Y);
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

    private static DimensionDto CreateOrdinateXDimension()
    {
        return new DimensionDto(
            Guid.NewGuid(),
            "DIMENSION:ORDX",
            "DIMS",
            "DIMENSION",
            "*D250",
            "13'-4\"",
            "GeometryBlock",
            string.Empty,
            160m,
            4064m,
            "Inch",
            6,
            0m,
            0m,
            100m,
            100m,
            0m,
            260m,
            140m,
            0m,
            290m,
            170m,
            0m,
            0.99m,
            null,
            1)
        {
            SourceHandle = "ORDX",
            RenderTextX = 304m,
            RenderTextY = 174m,
            RenderTextHeight = 3.5m,
            RenderTextStyleName = "ARCH",
            RenderTextAttachmentPoint = "MiddleLeft",
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto("LINE-1", 1, 260m, 140m, 290m, 170m),
                new DimensionLinePrimitiveDto("LINE-2", 2, 290m, 170m, 320m, 170m)
            ],
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto("TEXT-1", 1, "13'-4\"", 304m, 174m, 3.5m, 0m)
                {
                    StyleName = "ARCH",
                    AttachmentPoint = "MiddleLeft"
                }
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("INSERT-1", 1, "_Dot", 260m, 140m, 0m),
                new DimensionInsertPrimitiveDto("INSERT-2", 2, "_Dot", 290m, 170m, 0m)
            ]
        };
    }

    private static DimensionDto CreateRadiusDimension()
    {
        return new DimensionDto(
            Guid.NewGuid(),
            "DIMENSION:RADX",
            "DIMS",
            "DIMENSION",
            "*D260",
            "4'-2\"",
            "GeometryBlock",
            string.Empty,
            50m,
            1270m,
            "Inch",
            4,
            0m,
            0m,
            100m,
            100m,
            0m,
            150m,
            100m,
            0m,
            170m,
            120m,
            0m,
            0.99m,
            null,
            1)
        {
            SourceHandle = "RADX",
            RenderTextX = 180m,
            RenderTextY = 125m,
            RenderTextHeight = 3.5m,
            RenderTextStyleName = "ARCH",
            RenderTextAttachmentPoint = "MiddleLeft",
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto("RADX-LINE-1", 1, 150m, 100m, 170m, 120m),
                new DimensionLinePrimitiveDto("RADX-LINE-2", 2, 170m, 120m, 200m, 120m)
            ],
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto("RADX-TEXT-1", 1, "4'-2\"", 180m, 125m, 3.5m, 0m)
                {
                    StyleName = "ARCH",
                    AttachmentPoint = "MiddleLeft"
                }
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("RADX-INSERT-1", 1, "_Dot", 150m, 100m, 0m),
                new DimensionInsertPrimitiveDto("RADX-INSERT-2", 2, "_Dot", 170m, 120m, 0m)
            ]
        };
    }

    private static DimensionDto CreateDiameterDimension()
    {
        return new DimensionDto(
            Guid.NewGuid(),
            "DIMENSION:DIAMX",
            "DIMS",
            "DIMENSION",
            "*D261",
            "3'-4\"",
            "GeometryBlock",
            string.Empty,
            40m,
            1016m,
            "Inch",
            3,
            0m,
            0m,
            80m,
            100m,
            0m,
            120m,
            100m,
            0m,
            110m,
            120m,
            0m,
            0.99m,
            null,
            1)
        {
            SourceHandle = "DIAMX",
            RenderTextX = 120m,
            RenderTextY = 125m,
            RenderTextHeight = 3.5m,
            RenderTextStyleName = "ARCH",
            RenderTextAttachmentPoint = "MiddleLeft",
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto("DIAMX-LINE-1", 1, 80m, 100m, 120m, 100m),
                new DimensionLinePrimitiveDto("DIAMX-LINE-2", 2, 120m, 100m, 110m, 120m)
            ],
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto("DIAMX-TEXT-1", 1, "3'-4\"", 120m, 125m, 3.5m, 0m)
                {
                    StyleName = "ARCH",
                    AttachmentPoint = "MiddleLeft"
                }
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("DIAMX-INSERT-1", 1, "_Dot", 80m, 100m, 0m),
                new DimensionInsertPrimitiveDto("DIAMX-INSERT-2", 2, "_Dot", 120m, 100m, 0m)
            ]
        };
    }

    private static DimensionDto CreateOrdinateYDimension()
    {
        return new DimensionDto(
            Guid.NewGuid(),
            "DIMENSION:ORDY",
            "DIMS",
            "DIMENSION",
            "*D251",
            "13'-4\"",
            "GeometryBlock",
            string.Empty,
            160m,
            4064m,
            "Inch",
            6,
            0m,
            0m,
            100m,
            100m,
            0m,
            140m,
            260m,
            0m,
            170m,
            290m,
            0m,
            0.99m,
            null,
            1)
        {
            SourceHandle = "ORDY",
            RenderTextX = 174m,
            RenderTextY = 304m,
            RenderTextHeight = 3.5m,
            RenderTextStyleName = "ARCH",
            RenderTextAttachmentPoint = "BottomCenter",
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto("LINE-1", 1, 140m, 260m, 170m, 290m),
                new DimensionLinePrimitiveDto("LINE-2", 2, 170m, 290m, 170m, 320m)
            ],
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto("TEXT-1", 1, "13'-4\"", 174m, 304m, 3.5m, 90m)
                {
                    StyleName = "ARCH",
                    AttachmentPoint = "BottomCenter"
                }
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("INSERT-1", 1, "_Dot", 140m, 260m, 0m),
                new DimensionInsertPrimitiveDto("INSERT-2", 2, "_Dot", 170m, 290m, 0m)
            ]
        };
    }
}
