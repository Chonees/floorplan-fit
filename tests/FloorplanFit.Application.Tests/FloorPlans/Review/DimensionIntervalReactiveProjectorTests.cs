using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Review;

public sealed class DimensionIntervalReactiveProjectorTests
{
    [Fact]
    public void Project_rebuilds_manual_verified_linear_dimensions_for_the_active_axis_from_live_nodes()
    {
        var wallPathId = Guid.NewGuid();
        var openingPathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var corridorId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var dimension = CreateLinearDimension();
        IReadOnlyList<GeometryPathDto> previewGeometry =
        [
            new(wallPathId, false, [new GeometrySegmentDto(wallPathId, 1, 100m, 100m, 100m, 140m)]),
            new(openingPathId, false, [new GeometrySegmentDto(openingPathId, 1, 260m, 100m, 260m, 140m)])
        ];
        IReadOnlyList<MeasurementCorridorDto> corridors =
        [
            new(corridorId, "Patio-Width", "Width", wallPathId, 95m, 145m, "Verified", 1)
        ];
        IReadOnlyList<MeasurementNodeDto> nodes =
        [
            new(startNodeId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), wallPathId, "Projected", 100m, 120m, 100m, 0m, 0m, 0.5m),
            new(endNodeId, corridorId, 2, "ProjectedGeometry", FloorPlanArtifactSourceKinds.OpeningCandidate, Guid.NewGuid(), openingPathId, "Projected", 224m, 120m, 224m, 0m, 0m, 0.5m)
        ];
        IReadOnlyList<DimensionIntervalBindingDto> bindings =
        [
            new(dimension.DimensionId, corridorId, startNodeId, endNodeId, "ManualVerified", 100m, 224m)
        ];
        IReadOnlyList<ArticulationBandDto> articulationBands =
        [
            new(pinchGroupId, "Patio", "Width", 150m, 240m, 120m, "Verified")
        ];

        var rendered = DimensionIntervalReactiveProjector.Project(
            [dimension],
            previewGeometry,
            corridors,
            nodes,
            bindings,
            articulationBands,
            pinchGroupId);

        var updated = Assert.Single(rendered);
        Assert.Equal(100m, updated.DefPointX);
        Assert.Equal(260m, updated.DefPoint2X);
        Assert.Equal(160m, updated.MeasurementSourceUnits);
        Assert.Equal("13'-4\"", updated.DisplayText);
        Assert.Equal(260m, updated.LinePrimitives[1].StartX);
        Assert.Equal(260m, updated.InsertPrimitives[1].X);
    }

    [Fact]
    public void Project_replaces_dimension_placeholder_override_with_live_measurement_text_and_preserves_suffix()
    {
        var wallPathId = Guid.NewGuid();
        var openingPathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var corridorId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var dimension = CreateLinearDimension() with
        {
            RawTextOverride = "<> TO CL. OF EXH. VENT",
            DisplayText = "10'-4\" TO CL. OF EXH. VENT",
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto("TEXT-1", 1, "10'-4\" TO CL. OF EXH. VENT", 162m, 148m, 3.5m, 0m)
            ]
        };
        IReadOnlyList<GeometryPathDto> previewGeometry =
        [
            new(wallPathId, false, [new GeometrySegmentDto(wallPathId, 1, 100m, 100m, 100m, 140m)]),
            new(openingPathId, false, [new GeometrySegmentDto(openingPathId, 1, 260m, 100m, 260m, 140m)])
        ];
        IReadOnlyList<MeasurementCorridorDto> corridors =
        [
            new(corridorId, "Patio-Width", "Width", wallPathId, 95m, 145m, "Verified", 1)
        ];
        IReadOnlyList<MeasurementNodeDto> nodes =
        [
            new(startNodeId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), wallPathId, "Projected", 100m, 120m, 100m, 0m, 0m, 0.5m),
            new(endNodeId, corridorId, 2, "ProjectedGeometry", FloorPlanArtifactSourceKinds.OpeningCandidate, Guid.NewGuid(), openingPathId, "Projected", 224m, 120m, 224m, 0m, 0m, 0.5m)
        ];
        IReadOnlyList<DimensionIntervalBindingDto> bindings =
        [
            new(dimension.DimensionId, corridorId, startNodeId, endNodeId, "ManualVerified", 100m, 224m)
        ];
        IReadOnlyList<ArticulationBandDto> articulationBands =
        [
            new(pinchGroupId, "Patio", "Width", 150m, 240m, 120m, "Verified")
        ];

        var rendered = DimensionIntervalReactiveProjector.Project(
            [dimension],
            previewGeometry,
            corridors,
            nodes,
            bindings,
            articulationBands,
            pinchGroupId);

        var updated = Assert.Single(rendered);
        Assert.Equal(160m, updated.MeasurementSourceUnits);
        Assert.Equal("13'-4\" TO CL. OF EXH. VENT", updated.DisplayText);
        Assert.Equal("13'-4\" TO CL. OF EXH. VENT", updated.TextPrimitives[0].Text);
    }

    [Fact]
    public void Project_moves_bound_dimensions_by_node_deltas_even_when_interval_does_not_overlap_selected_band()
    {
        var leftWallPathId = Guid.NewGuid();
        var rightWallPathId = Guid.NewGuid();
        var guidePathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var corridorId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var dimension = CreateLinearDimension();
        IReadOnlyList<GeometryPathDto> previewGeometry =
        [
            new(leftWallPathId, false, [new GeometrySegmentDto(leftWallPathId, 1, 80m, 100m, 80m, 140m)]),
            new(rightWallPathId, false, [new GeometrySegmentDto(rightWallPathId, 1, 204m, 100m, 204m, 140m)]),
            new(guidePathId, false, [new GeometrySegmentDto(guidePathId, 1, 70m, 120m, 214m, 120m)])
        ];
        IReadOnlyList<MeasurementCorridorDto> corridors =
        [
            new(corridorId, "Facade-TotalWidth", "Width", guidePathId, 95m, 145m, "Verified", 1)
        ];
        IReadOnlyList<MeasurementNodeDto> nodes =
        [
            new(startNodeId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), leftWallPathId, "Projected", 100m, 100m, 100m, 0m, 0m, 0m),
            new(endNodeId, corridorId, 2, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), rightWallPathId, "Projected", 224m, 100m, 224m, 0m, 0m, 0m)
        ];
        IReadOnlyList<DimensionIntervalBindingDto> bindings =
        [
            new(dimension.DimensionId, corridorId, startNodeId, endNodeId, "ManualVerified", 100m, 224m)
        ];
        IReadOnlyList<ArticulationBandDto> articulationBands =
        [
            new(pinchGroupId, "Far Facade", "Width", 260m, 320m, 120m, "Verified")
        ];

        var rendered = DimensionIntervalReactiveProjector.Project(
            [dimension],
            previewGeometry,
            corridors,
            nodes,
            bindings,
            articulationBands,
            pinchGroupId);

        var updated = Assert.Single(rendered);
        Assert.Equal(80m, updated.DefPointX);
        Assert.Equal(100m, updated.DefPointY);
        Assert.Equal(204m, updated.DefPoint2X);
        Assert.Equal(100m, updated.DefPoint2Y);
        Assert.Equal(124m, updated.MeasurementSourceUnits);
        Assert.Equal("10'-4\"", updated.DisplayText);
        Assert.Equal(80m, updated.LinePrimitives[0].StartX);
        Assert.Equal(140m, updated.LinePrimitives[0].StartY);
        Assert.Equal(204m, updated.LinePrimitives[1].StartX);
        Assert.Equal(140m, updated.LinePrimitives[1].StartY);
        Assert.Equal(142m, updated.TextPrimitives[0].X);
        Assert.Equal(148m, updated.TextPrimitives[0].Y);
    }

    [Fact]
    public void Project_does_not_recalculate_same_axis_dimension_when_interval_does_not_overlap_selected_band()
    {
        var verticalWallPathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var corridorId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var dimension = CreateVerticalDimension(100m, 208m);
        IReadOnlyList<GeometryPathDto> previewGeometry =
        [
            new(verticalWallPathId, false, [new GeometrySegmentDto(verticalWallPathId, 1, 100m, 100m, 100m, 208.5m)])
        ];
        IReadOnlyList<MeasurementCorridorDto> corridors =
        [
            new(corridorId, "Sink-Height", "Height", verticalWallPathId, 95m, 215m, "Verified", 1)
        ];
        IReadOnlyList<MeasurementNodeDto> nodes =
        [
            new(startNodeId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), verticalWallPathId, "Projected", 100m, 100m, 100m, 0m, 0m, 0m),
            new(endNodeId, corridorId, 2, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), verticalWallPathId, "Projected", 100m, 208m, 100m, 0m, 0m, 1m)
        ];
        IReadOnlyList<DimensionIntervalBindingDto> bindings =
        [
            new(dimension.DimensionId, corridorId, startNodeId, endNodeId, "ManualVerified", 100m, 208m)
        ];
        IReadOnlyList<ArticulationBandDto> articulationBands =
        [
            new(pinchGroupId, "Patio", "Height", 300m, 340m, 120m, "Verified")
        ];

        var rendered = DimensionIntervalReactiveProjector.Project(
            [dimension],
            previewGeometry,
            corridors,
            nodes,
            bindings,
            articulationBands,
            pinchGroupId);

        var updated = Assert.Single(rendered);
        Assert.Equal(108m, updated.MeasurementSourceUnits);
        Assert.Equal("9'-0\"", updated.DisplayText);
        Assert.Equal("9'-0\"", updated.TextPrimitives[0].Text);
    }

    [Fact]
    public void Project_preserves_horizontal_dimension_appearance_for_width_bindings_when_nodes_are_vertically_misaligned_without_axis_delta()
    {
        var leftWallPathId = Guid.NewGuid();
        var rightWallPathId = Guid.NewGuid();
        var guidePathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var corridorId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var dimension = CreateLinearDimension();
        IReadOnlyList<GeometryPathDto> previewGeometry =
        [
            new(leftWallPathId, false, [new GeometrySegmentDto(leftWallPathId, 1, 100m, 100m, 100m, 140m)]),
            new(rightWallPathId, false, [new GeometrySegmentDto(rightWallPathId, 1, 260m, 180m, 260m, 220m)]),
            new(guidePathId, false, [new GeometrySegmentDto(guidePathId, 1, 90m, 120m, 270m, 120m)])
        ];
        IReadOnlyList<MeasurementCorridorDto> corridors =
        [
            new(corridorId, "Facade-TotalWidth", "Width", guidePathId, 95m, 145m, "Verified", 1)
        ];
        IReadOnlyList<MeasurementNodeDto> nodes =
        [
            new(startNodeId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), leftWallPathId, "Projected", 100m, 100m, 100m, 0m, 0m, 0m),
            new(endNodeId, corridorId, 2, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), rightWallPathId, "Projected", 260m, 220m, 260m, 0m, 0m, 1m)
        ];
        IReadOnlyList<DimensionIntervalBindingDto> bindings =
        [
            new(dimension.DimensionId, corridorId, startNodeId, endNodeId, "ManualVerified", 100m, 260m)
        ];
        IReadOnlyList<ArticulationBandDto> articulationBands =
        [
            new(pinchGroupId, "Facade", "Width", 150m, 240m, 120m, "Verified")
        ];

        var rendered = DimensionIntervalReactiveProjector.Project(
            [dimension],
            previewGeometry,
            corridors,
            nodes,
            bindings,
            articulationBands,
            pinchGroupId);

        var updated = Assert.Single(rendered);
        Assert.Equal(100m, updated.DefPointX);
        Assert.Equal(100m, updated.DefPointY);
        Assert.Equal(224m, updated.DefPoint2X);
        Assert.Equal(100m, updated.DefPoint2Y);
        Assert.Equal(124m, updated.MeasurementSourceUnits);
        Assert.Equal("10'-4\"", updated.DisplayText);
        Assert.Equal(224m, updated.InsertPrimitives[1].X);
        Assert.Equal(140m, updated.InsertPrimitives[1].Y);
    }

    [Fact]
    public void Project_uses_live_node_axis_delta_without_pulling_the_dimension_to_the_node_normal_position()
    {
        var leftWallPathId = Guid.NewGuid();
        var rightWallPathId = Guid.NewGuid();
        var guidePathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var corridorId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var dimension = CreateLinearDimension();
        IReadOnlyList<GeometryPathDto> previewGeometry =
        [
            new(leftWallPathId, false, [new GeometrySegmentDto(leftWallPathId, 1, 100m, 200m, 100m, 240m)]),
            new(rightWallPathId, false, [new GeometrySegmentDto(rightWallPathId, 1, 204m, 180m, 204m, 220m)]),
            new(guidePathId, false, [new GeometrySegmentDto(guidePathId, 1, 90m, 120m, 270m, 120m)])
        ];
        IReadOnlyList<MeasurementCorridorDto> corridors =
        [
            new(corridorId, "Facade-TotalWidth", "Width", guidePathId, 95m, 145m, "Verified", 1)
        ];
        IReadOnlyList<MeasurementNodeDto> nodes =
        [
            new(startNodeId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), leftWallPathId, "Projected", 100m, 220m, 100m, 0m, 0m, 0.5m),
            new(endNodeId, corridorId, 2, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), rightWallPathId, "Projected", 224m, 200m, 224m, 0m, 0m, 0.5m)
        ];
        IReadOnlyList<DimensionIntervalBindingDto> bindings =
        [
            new(dimension.DimensionId, corridorId, startNodeId, endNodeId, "ManualVerified", 100m, 224m)
        ];
        IReadOnlyList<ArticulationBandDto> articulationBands =
        [
            new(pinchGroupId, "Facade", "Width", 150m, 240m, 120m, "Verified")
        ];

        var rendered = DimensionIntervalReactiveProjector.Project(
            [dimension],
            previewGeometry,
            corridors,
            nodes,
            bindings,
            articulationBands,
            pinchGroupId);

        var updated = Assert.Single(rendered);
        Assert.Equal(100m, updated.DefPointX);
        Assert.Equal(100m, updated.DefPointY);
        Assert.Equal(204m, updated.DefPoint2X);
        Assert.Equal(100m, updated.DefPoint2Y);
        Assert.Equal(104m, updated.MeasurementSourceUnits);
        Assert.Equal("8'-8\"", updated.DisplayText);
        Assert.Equal(100m, updated.LinePrimitives[0].StartX);
        Assert.Equal(140m, updated.LinePrimitives[0].StartY);
        Assert.Equal(204m, updated.LinePrimitives[1].StartX);
        Assert.Equal(140m, updated.LinePrimitives[1].StartY);
        Assert.Equal(152m, updated.TextPrimitives[0].X);
        Assert.Equal(148m, updated.TextPrimitives[0].Y);
    }

    [Fact]
    public void Project_applies_live_node_delta_instead_of_anchoring_dimension_to_absolute_node_coordinates()
    {
        var leftWallPathId = Guid.NewGuid();
        var rightWallPathId = Guid.NewGuid();
        var guidePathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var corridorId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var dimension = CreateLinearDimension();
        IReadOnlyList<GeometryPathDto> previewGeometry =
        [
            new(leftWallPathId, false, [new GeometrySegmentDto(leftWallPathId, 1, 300m, 200m, 300m, 240m)]),
            new(rightWallPathId, false, [new GeometrySegmentDto(rightWallPathId, 1, 404m, 200m, 404m, 240m)]),
            new(guidePathId, false, [new GeometrySegmentDto(guidePathId, 1, 290m, 220m, 430m, 220m)])
        ];
        IReadOnlyList<MeasurementCorridorDto> corridors =
        [
            new(corridorId, "Remote-Width", "Width", guidePathId, 195m, 245m, "Verified", 1)
        ];
        IReadOnlyList<MeasurementNodeDto> nodes =
        [
            new(startNodeId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), leftWallPathId, "Projected", 300m, 220m, 300m, 0m, 0m, 0.5m),
            new(endNodeId, corridorId, 2, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), rightWallPathId, "Projected", 424m, 220m, 424m, 0m, 0m, 0.5m)
        ];
        IReadOnlyList<DimensionIntervalBindingDto> bindings =
        [
            new(dimension.DimensionId, corridorId, startNodeId, endNodeId, "ManualVerified", 300m, 424m)
        ];
        IReadOnlyList<ArticulationBandDto> articulationBands =
        [
            new(pinchGroupId, "Remote", "Width", 350m, 430m, 120m, "Verified")
        ];

        var rendered = DimensionIntervalReactiveProjector.Project(
            [dimension],
            previewGeometry,
            corridors,
            nodes,
            bindings,
            articulationBands,
            pinchGroupId);

        var updated = Assert.Single(rendered);
        Assert.Equal(100m, updated.DefPointX);
        Assert.Equal(100m, updated.DefPointY);
        Assert.Equal(204m, updated.DefPoint2X);
        Assert.Equal(100m, updated.DefPoint2Y);
        Assert.Equal(104m, updated.MeasurementSourceUnits);
        Assert.Equal("8'-8\"", updated.DisplayText);
        Assert.Equal(100m, updated.LinePrimitives[0].StartX);
        Assert.Equal(140m, updated.LinePrimitives[0].StartY);
        Assert.Equal(204m, updated.LinePrimitives[1].StartX);
        Assert.Equal(140m, updated.LinePrimitives[1].StartY);
        Assert.Equal(152m, updated.TextPrimitives[0].X);
        Assert.Equal(148m, updated.TextPrimitives[0].Y);
    }

    [Fact]
    public void Project_preserves_visual_dimension_axis_when_definition_points_are_not_parallel_to_the_cota_line()
    {
        var leftWallPathId = Guid.NewGuid();
        var rightWallPathId = Guid.NewGuid();
        var guidePathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var corridorId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var dimension = CreateLinearDimension() with
        {
            DefPointX = 100m,
            DefPointY = 100m,
            DefPoint2X = 224m,
            DefPoint2Y = 120m
        };
        IReadOnlyList<GeometryPathDto> previewGeometry =
        [
            new(leftWallPathId, false, [new GeometrySegmentDto(leftWallPathId, 1, 100m, 100m, 100m, 140m)]),
            new(rightWallPathId, false, [new GeometrySegmentDto(rightWallPathId, 1, 204m, 100m, 204m, 140m)]),
            new(guidePathId, false, [new GeometrySegmentDto(guidePathId, 1, 90m, 120m, 230m, 120m)])
        ];
        IReadOnlyList<MeasurementCorridorDto> corridors =
        [
            new(corridorId, "Facade-Width", "Width", guidePathId, 95m, 145m, "Verified", 1)
        ];
        IReadOnlyList<MeasurementNodeDto> nodes =
        [
            new(startNodeId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), leftWallPathId, "Projected", 100m, 120m, 100m, 0m, 0m, 0.5m),
            new(endNodeId, corridorId, 2, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), rightWallPathId, "Projected", 224m, 120m, 224m, 0m, 0m, 0.5m)
        ];
        IReadOnlyList<DimensionIntervalBindingDto> bindings =
        [
            new(dimension.DimensionId, corridorId, startNodeId, endNodeId, "ManualVerified", 100m, 224m)
        ];
        IReadOnlyList<ArticulationBandDto> articulationBands =
        [
            new(pinchGroupId, "Facade", "Width", 150m, 240m, 120m, "Verified")
        ];

        var rendered = DimensionIntervalReactiveProjector.Project(
            [dimension],
            previewGeometry,
            corridors,
            nodes,
            bindings,
            articulationBands,
            pinchGroupId);

        var updated = Assert.Single(rendered);
        Assert.Equal(100m, updated.LinePrimitives[2].StartX);
        Assert.Equal(140m, updated.LinePrimitives[2].StartY);
        Assert.Equal(204m, updated.LinePrimitives[2].EndX);
        Assert.Equal(140m, updated.LinePrimitives[2].EndY);
        Assert.Equal(100m, updated.InsertPrimitives[0].X);
        Assert.Equal(140m, updated.InsertPrimitives[0].Y);
        Assert.Equal(204m, updated.InsertPrimitives[1].X);
        Assert.Equal(140m, updated.InsertPrimitives[1].Y);
    }

    [Fact]
    public void Project_does_not_snap_to_same_wall_vertical_nodes_when_there_is_no_live_axis_delta()
    {
        var verticalWallPathId = Guid.NewGuid();
        var guidePathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var corridorId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var dimension = CreateVerticalLinearDimension();
        IReadOnlyList<GeometryPathDto> previewGeometry =
        [
            new(verticalWallPathId, false, [new GeometrySegmentDto(verticalWallPathId, 1, 100m, 100m, 100m, 220m)]),
            new(guidePathId, false, [new GeometrySegmentDto(guidePathId, 1, 120m, 90m, 120m, 230m)])
        ];
        IReadOnlyList<MeasurementCorridorDto> corridors =
        [
            new(corridorId, "Any-A-B", "Height", guidePathId, 90m, 230m, "Verified", 1)
        ];
        IReadOnlyList<MeasurementNodeDto> nodes =
        [
            new(startNodeId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), verticalWallPathId, "Projected", 100m, 100m, 100m, 0m, 0m, 0m),
            new(endNodeId, corridorId, 2, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), verticalWallPathId, "Projected", 100m, 220m, 220m, 0m, 0m, 1m)
        ];
        IReadOnlyList<DimensionIntervalBindingDto> bindings =
        [
            new(dimension.DimensionId, corridorId, startNodeId, endNodeId, "ManualVerified", 100m, 220m)
        ];
        IReadOnlyList<ArticulationBandDto> articulationBands =
        [
            new(pinchGroupId, "Any", "Height", 95m, 105m, 120m, "Verified")
        ];

        var rendered = DimensionIntervalReactiveProjector.Project(
            [dimension],
            previewGeometry,
            corridors,
            nodes,
            bindings,
            articulationBands,
            pinchGroupId);

        var updated = Assert.Single(rendered);
        Assert.Equal(100m, updated.DefPointX);
        Assert.Equal(100m, updated.DefPointY);
        Assert.Equal(100m, updated.DefPoint2X);
        Assert.Equal(224m, updated.DefPoint2Y);
        Assert.Equal(124m, updated.MeasurementSourceUnits);
        Assert.Equal("10'-4\"", updated.DisplayText);
    }

    [Fact]
    public void Project_preserves_vertical_dimension_appearance_for_height_bindings_when_nodes_are_horizontally_misaligned_without_axis_delta()
    {
        var topWallPathId = Guid.NewGuid();
        var bottomWallPathId = Guid.NewGuid();
        var guidePathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var corridorId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var dimension = CreateVerticalLinearDimension();
        IReadOnlyList<GeometryPathDto> previewGeometry =
        [
            new(topWallPathId, false, [new GeometrySegmentDto(topWallPathId, 1, 100m, 100m, 140m, 100m)]),
            new(bottomWallPathId, false, [new GeometrySegmentDto(bottomWallPathId, 1, 210m, 260m, 250m, 260m)]),
            new(guidePathId, false, [new GeometrySegmentDto(guidePathId, 1, 120m, 90m, 120m, 270m)])
        ];
        IReadOnlyList<MeasurementCorridorDto> corridors =
        [
            new(corridorId, "Facade-TotalHeight", "Height", guidePathId, 95m, 145m, "Verified", 1)
        ];
        IReadOnlyList<MeasurementNodeDto> nodes =
        [
            new(startNodeId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), topWallPathId, "Projected", 100m, 100m, 100m, 0m, 0m, 0m),
            new(endNodeId, corridorId, 2, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), bottomWallPathId, "Projected", 250m, 260m, 260m, 0m, 0m, 1m)
        ];
        IReadOnlyList<DimensionIntervalBindingDto> bindings =
        [
            new(dimension.DimensionId, corridorId, startNodeId, endNodeId, "ManualVerified", 100m, 260m)
        ];
        IReadOnlyList<ArticulationBandDto> articulationBands =
        [
            new(pinchGroupId, "Facade", "Height", 150m, 240m, 120m, "Verified")
        ];

        var rendered = DimensionIntervalReactiveProjector.Project(
            [dimension],
            previewGeometry,
            corridors,
            nodes,
            bindings,
            articulationBands,
            pinchGroupId);

        var updated = Assert.Single(rendered);
        Assert.Equal(100m, updated.DefPointX);
        Assert.Equal(100m, updated.DefPointY);
        Assert.Equal(100m, updated.DefPoint2X);
        Assert.Equal(224m, updated.DefPoint2Y);
        Assert.Equal(124m, updated.MeasurementSourceUnits);
        Assert.Equal("10'-4\"", updated.DisplayText);
        Assert.Equal(140m, updated.InsertPrimitives[1].X);
        Assert.Equal(224m, updated.InsertPrimitives[1].Y);
    }

    [Fact]
    public void Project_preserves_free_angle_dimension_appearance_when_live_nodes_have_no_authored_axis_delta()
    {
        var leftWallPathId = Guid.NewGuid();
        var rightWallPathId = Guid.NewGuid();
        var guidePathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var corridorId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var dimension = CreateFreeAngleDimension();
        IReadOnlyList<GeometryPathDto> previewGeometry =
        [
            new(leftWallPathId, false, [new GeometrySegmentDto(leftWallPathId, 1, 100m, 100m, 100m, 140m)]),
            new(rightWallPathId, false, [new GeometrySegmentDto(rightWallPathId, 1, 250m, 80m, 250m, 120m)]),
            new(guidePathId, false, [new GeometrySegmentDto(guidePathId, 1, 90m, 120m, 270m, 120m)])
        ];
        IReadOnlyList<MeasurementCorridorDto> corridors =
        [
            new(corridorId, "Diagonal", "Width", guidePathId, 95m, 145m, "Verified", 1)
        ];
        IReadOnlyList<MeasurementNodeDto> nodes =
        [
            new(startNodeId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), leftWallPathId, "Projected", 100m, 100m, 100m, 0m, 0m, 0m),
            new(endNodeId, corridorId, 2, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), rightWallPathId, "Projected", 250m, 120m, 250m, 0m, 0m, 1m)
        ];
        IReadOnlyList<DimensionIntervalBindingDto> bindings =
        [
            new(dimension.DimensionId, corridorId, startNodeId, endNodeId, "ManualVerified", 100m, 260m)
        ];
        IReadOnlyList<ArticulationBandDto> articulationBands =
        [
            new(pinchGroupId, "Diagonal", "Width", 150m, 240m, 120m, "Verified")
        ];

        var rendered = DimensionIntervalReactiveProjector.Project(
            [dimension],
            previewGeometry,
            corridors,
            nodes,
            bindings,
            articulationBands,
            pinchGroupId);

        var updated = Assert.Single(rendered);
        Assert.NotSame(dimension, updated);
        Assert.Equal(100m, updated.DefPointX);
        Assert.Equal(100m, updated.DefPointY);
        Assert.Equal(200m, updated.DefPoint2X);
        Assert.Equal(200m, updated.DefPoint2Y);
        Assert.Equal(141.421m, updated.MeasurementSourceUnits);
        Assert.Equal("11'-9\"", updated.DisplayText);
    }

    [Fact]
    public void Project_preserves_reversed_split_dimension_shape_when_dimension_line_is_below_the_measure()
    {
        var leftWallPathId = Guid.NewGuid();
        var rightWallPathId = Guid.NewGuid();
        var guidePathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var corridorId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var dimension = CreateReversedSplitLinearDimensionBelow();
        IReadOnlyList<GeometryPathDto> previewGeometry =
        [
            new(leftWallPathId, false, [new GeometrySegmentDto(leftWallPathId, 1, 80m, 100m, 80m, 140m)]),
            new(rightWallPathId, false, [new GeometrySegmentDto(rightWallPathId, 1, 204m, 100m, 204m, 140m)]),
            new(guidePathId, false, [new GeometrySegmentDto(guidePathId, 1, 70m, 120m, 214m, 120m)])
        ];
        IReadOnlyList<MeasurementCorridorDto> corridors =
        [
            new(corridorId, "Reversed-Below", "Width", guidePathId, 95m, 145m, "Verified", 1)
        ];
        IReadOnlyList<MeasurementNodeDto> nodes =
        [
            new(startNodeId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), leftWallPathId, "Projected", 100m, 100m, 100m, 0m, 0m, 0m),
            new(endNodeId, corridorId, 2, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), rightWallPathId, "Projected", 224m, 100m, 224m, 0m, 0m, 0m)
        ];
        IReadOnlyList<DimensionIntervalBindingDto> bindings =
        [
            new(dimension.DimensionId, corridorId, startNodeId, endNodeId, "ManualVerified", 100m, 224m)
        ];
        IReadOnlyList<ArticulationBandDto> articulationBands =
        [
            new(pinchGroupId, "Reversed", "Width", 260m, 320m, 120m, "Verified")
        ];

        var rendered = DimensionIntervalReactiveProjector.Project(
            [dimension],
            previewGeometry,
            corridors,
            nodes,
            bindings,
            articulationBands,
            pinchGroupId);

        var updated = Assert.Single(rendered);
        Assert.Equal(204m, updated.DefPointX);
        Assert.Equal(100m, updated.DefPointY);
        Assert.Equal(80m, updated.DefPoint2X);
        Assert.Equal(100m, updated.DefPoint2Y);
        Assert.Equal(124m, updated.MeasurementSourceUnits);
        Assert.Equal("10'-4\"", updated.DisplayText);
        Assert.Equal(204m, updated.LinePrimitives[0].StartX);
        Assert.Equal(60m, updated.LinePrimitives[0].StartY);
        Assert.Equal(154m, updated.LinePrimitives[0].EndX);
        Assert.Equal(60m, updated.LinePrimitives[0].EndY);
        Assert.Equal(130m, updated.LinePrimitives[1].StartX);
        Assert.Equal(60m, updated.LinePrimitives[1].StartY);
        Assert.Equal(80m, updated.LinePrimitives[1].EndX);
        Assert.Equal(60m, updated.LinePrimitives[1].EndY);
        Assert.Equal(204m, updated.LinePrimitives[2].StartX);
        Assert.Equal(60m, updated.LinePrimitives[2].StartY);
        Assert.Equal(204m, updated.LinePrimitives[2].EndX);
        Assert.Equal(100m, updated.LinePrimitives[2].EndY);
        Assert.Equal(80m, updated.LinePrimitives[3].StartX);
        Assert.Equal(60m, updated.LinePrimitives[3].StartY);
        Assert.Equal(80m, updated.LinePrimitives[3].EndX);
        Assert.Equal(100m, updated.LinePrimitives[3].EndY);
    }

    [Fact]
    public void Project_supports_manual_verified_nodes_on_non_wall_architectural_geometry()
    {
        var fixturePathId = Guid.NewGuid();
        var wallPathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var corridorId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var dimension = CreateLinearDimension() with
        {
            DefPointX = 140m,
            DefPoint2X = 224m,
            MeasurementSourceUnits = 84m,
            MeasurementMillimeters = 2133.6m,
            DisplayText = "7'-0\"",
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto("LINE-1", 1, 140m, 140m, 140m, 100m),
                new DimensionLinePrimitiveDto("LINE-2", 2, 224m, 140m, 224m, 100m),
                new DimensionLinePrimitiveDto("LINE-3", 3, 140m, 140m, 224m, 140m)
            ],
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto("TEXT-1", 1, "7'-0\"", 182m, 148m, 3.5m, 0m)
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("INSERT-1", 1, "_Dot", 140m, 140m, 0m),
                new DimensionInsertPrimitiveDto("INSERT-2", 2, "_Dot", 224m, 140m, 0m)
            ]
        };
        IReadOnlyList<GeometryPathDto> previewGeometry =
        [
            new(fixturePathId, false, [new GeometrySegmentDto(fixturePathId, 1, 160m, 100m, 160m, 140m)]),
            new(wallPathId, false, [new GeometrySegmentDto(wallPathId, 1, 224m, 100m, 224m, 140m)])
        ];
        IReadOnlyList<MeasurementCorridorDto> corridors =
        [
            new(corridorId, "Bathroom-Clear", "Width", fixturePathId, 95m, 145m, "Verified", 1)
        ];
        IReadOnlyList<MeasurementNodeDto> nodes =
        [
            new(startNodeId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.FixedPlanComponent, Guid.NewGuid(), fixturePathId, "Projected", 140m, 120m, 140m, 0m, 0m, 0.5m),
            new(endNodeId, corridorId, 2, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), wallPathId, "Projected", 224m, 120m, 224m, 0m, 0m, 0.5m)
        ];
        IReadOnlyList<DimensionIntervalBindingDto> bindings =
        [
            new(dimension.DimensionId, corridorId, startNodeId, endNodeId, "ManualVerified", 140m, 224m)
        ];
        IReadOnlyList<ArticulationBandDto> articulationBands =
        [
            new(pinchGroupId, "Bathroom", "Width", 150m, 240m, 120m, "Verified")
        ];

        var rendered = DimensionIntervalReactiveProjector.Project(
            [dimension],
            previewGeometry,
            corridors,
            nodes,
            bindings,
            articulationBands,
            pinchGroupId);

        var updated = Assert.Single(rendered);
        Assert.Equal(160m, updated.DefPointX);
        Assert.Equal(224m, updated.DefPoint2X);
        Assert.Equal(64m, updated.MeasurementSourceUnits);
        Assert.Equal("5'-4\"", updated.DisplayText);
    }

    [Fact]
    public void Project_resolves_nodes_by_original_segment_location_when_preview_compression_changes_path_lengths()
    {
        var wallPathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var corridorId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var dimension = CreateHorizontalDimension(250m, 300m);
        IReadOnlyList<GeometryPathDto> sourceGeometry =
        [
            new(wallPathId, false,
            [
                new GeometrySegmentDto(wallPathId, 1, 100m, 100m, 200m, 100m),
                new GeometrySegmentDto(wallPathId, 2, 200m, 100m, 300m, 100m)
            ])
        ];
        IReadOnlyList<GeometryPathDto> previewGeometry =
        [
            new(wallPathId, false,
            [
                new GeometrySegmentDto(wallPathId, 1, 180m, 100m, 200m, 100m),
                new GeometrySegmentDto(wallPathId, 2, 200m, 100m, 300m, 100m)
            ])
        ];
        IReadOnlyList<MeasurementCorridorDto> corridors =
        [
            new(corridorId, "Right room width", "Width", wallPathId, 95m, 145m, "Verified", 1)
        ];
        IReadOnlyList<MeasurementNodeDto> nodes =
        [
            new(startNodeId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), wallPathId, "Projected", 250m, 100m, 250m, 0m, 0m, 0.75m),
            new(endNodeId, corridorId, 2, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), wallPathId, "Projected", 300m, 100m, 300m, 0m, 0m, 1m)
        ];
        IReadOnlyList<DimensionIntervalBindingDto> bindings =
        [
            new(dimension.DimensionId, corridorId, startNodeId, endNodeId, "ManualVerified", 250m, 300m)
        ];
        IReadOnlyList<ArticulationBandDto> articulationBands =
        [
            new(pinchGroupId, "Left compression", "Width", 100m, 200m, 80m, "Verified")
        ];

        var rendered = DimensionIntervalReactiveProjector.Project(
            dimensions: [dimension],
            sourceGeometry: sourceGeometry,
            previewGeometry: previewGeometry,
            measurementCorridors: corridors,
            measurementNodes: nodes,
            dimensionIntervalBindings: bindings,
            articulationBands: articulationBands,
            previewPinchGroupId: pinchGroupId);

        var updated = Assert.Single(rendered);
        Assert.Equal(250m, updated.DefPointX);
        Assert.Equal(300m, updated.DefPoint2X);
        Assert.Equal(50m, updated.MeasurementSourceUnits);
        Assert.Equal("4'-2\"", updated.DisplayText);
        Assert.Equal(250m, updated.InsertPrimitives[0].X);
        Assert.Equal(300m, updated.InsertPrimitives[1].X);
    }

    [Fact]
    public void Project_uses_source_geometry_as_authored_anchor_space_when_measurement_nodes_are_unprojected()
    {
        var leftWallPathId = Guid.NewGuid();
        var rightWallPathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var corridorId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var dimension = CreateHorizontalDimension(1000m, 1124m);
        IReadOnlyList<GeometryPathDto> sourceGeometry =
        [
            new(leftWallPathId, false, [new GeometrySegmentDto(leftWallPathId, 1, 1000m, 100m, 1000m, 140m)]),
            new(rightWallPathId, false, [new GeometrySegmentDto(rightWallPathId, 1, 1124m, 100m, 1124m, 140m)])
        ];
        IReadOnlyList<GeometryPathDto> previewGeometry =
        [
            new(leftWallPathId, false, [new GeometrySegmentDto(leftWallPathId, 1, 1000m, 100m, 1000m, 140m)]),
            new(rightWallPathId, false, [new GeometrySegmentDto(rightWallPathId, 1, 1122m, 100m, 1122m, 140m)])
        ];
        IReadOnlyList<MeasurementCorridorDto> corridors =
        [
            new(corridorId, "Projected facade width", "Width", leftWallPathId, 95m, 145m, "Verified", 1)
        ];
        IReadOnlyList<MeasurementNodeDto> nodes =
        [
            new(startNodeId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), leftWallPathId, "Projected", 100m, 120m, 100m, 0m, 0m, 0.5m),
            new(endNodeId, corridorId, 2, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), rightWallPathId, "Projected", 224m, 120m, 224m, 0m, 0m, 0.5m)
        ];
        IReadOnlyList<DimensionIntervalBindingDto> bindings =
        [
            new(dimension.DimensionId, corridorId, startNodeId, endNodeId, "ManualVerified", 100m, 224m)
        ];
        IReadOnlyList<ArticulationBandDto> articulationBands =
        [
            new(pinchGroupId, "Projected facade", "Width", 1050m, 1140m, 120m, "Verified")
        ];

        var rendered = DimensionIntervalReactiveProjector.Project(
            dimensions: [dimension],
            sourceGeometry: sourceGeometry,
            previewGeometry: previewGeometry,
            measurementCorridors: corridors,
            measurementNodes: nodes,
            dimensionIntervalBindings: bindings,
            articulationBands: articulationBands,
            previewPinchGroupId: pinchGroupId);

        var updated = Assert.Single(rendered);
        Assert.Equal(1000m, updated.DefPointX);
        Assert.Equal(1122m, updated.DefPoint2X);
        Assert.Equal(122m, updated.MeasurementSourceUnits);
        Assert.Equal("10'-2\"", updated.DisplayText);
        Assert.Equal(1000m, updated.InsertPrimitives[0].X);
        Assert.Equal(1122m, updated.InsertPrimitives[1].X);
        Assert.Equal(1000m, updated.LinePrimitives[0].StartX);
        Assert.Equal(1122m, updated.LinePrimitives[1].StartX);
    }

    [Fact]
    public void Project_translates_height_dimensions_with_width_preview_delta_without_recalculating_the_height()
    {
        var verticalWallPathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var corridorId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var dimension = CreateVerticalLinearDimension();
        IReadOnlyList<GeometryPathDto> previewGeometry =
        [
            new(verticalWallPathId, false, [new GeometrySegmentDto(verticalWallPathId, 1, 80m, 100m, 80m, 224m)])
        ];
        IReadOnlyList<MeasurementCorridorDto> corridors =
        [
            new(corridorId, "Bedroom-Height", "Height", verticalWallPathId, 95m, 145m, "Verified", 1)
        ];
        IReadOnlyList<MeasurementNodeDto> nodes =
        [
            new(startNodeId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), verticalWallPathId, "Projected", 100m, 100m, 100m, 0m, 0m, 0m),
            new(endNodeId, corridorId, 2, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), verticalWallPathId, "Projected", 100m, 224m, 224m, 0m, 0m, 1m)
        ];
        IReadOnlyList<DimensionIntervalBindingDto> bindings =
        [
            new(dimension.DimensionId, corridorId, startNodeId, endNodeId, "ManualVerified", 100m, 224m)
        ];
        IReadOnlyList<ArticulationBandDto> articulationBands =
        [
            new(pinchGroupId, "Width compression", "Width", 150m, 240m, 120m, "Verified")
        ];

        var rendered = DimensionIntervalReactiveProjector.Project(
            [dimension],
            previewGeometry,
            corridors,
            nodes,
            bindings,
            articulationBands,
            pinchGroupId);

        var updated = Assert.Single(rendered);
        Assert.Equal(80m, updated.DefPointX);
        Assert.Equal(100m, updated.DefPointY);
        Assert.Equal(80m, updated.DefPoint2X);
        Assert.Equal(224m, updated.DefPoint2Y);
        Assert.Equal(120m, updated.DefPoint3X);
        Assert.Equal(100m, updated.DefPoint3Y);
        Assert.Equal(124m, updated.MeasurementSourceUnits);
        Assert.Equal("10'-4\"", updated.DisplayText);
        Assert.Equal(120m, updated.LinePrimitives[0].StartX);
        Assert.Equal(100m, updated.LinePrimitives[0].StartY);
        Assert.Equal(80m, updated.LinePrimitives[0].EndX);
        Assert.Equal(100m, updated.LinePrimitives[0].EndY);
        Assert.Equal(128m, updated.TextPrimitives[0].X);
        Assert.Equal(162m, updated.TextPrimitives[0].Y);
        Assert.Equal(120m, updated.InsertPrimitives[0].X);
        Assert.Equal(100m, updated.InsertPrimitives[0].Y);
    }

    private static DimensionDto CreateLinearDimension()
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
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("INSERT-1", 1, "_Dot", 100m, 140m, 0m),
                new DimensionInsertPrimitiveDto("INSERT-2", 2, "_Dot", 224m, 140m, 0m)
            ]
        };
    }

    private static DimensionDto CreateHorizontalDimension(decimal startX, decimal endX)
    {
        var span = decimal.Abs(endX - startX);
        var midX = startX + ((endX - startX) / 2m);

        return CreateLinearDimension() with
        {
            DefPointX = startX,
            DefPointY = 100m,
            DefPoint2X = endX,
            DefPoint2Y = 100m,
            DefPoint3X = startX,
            DefPoint3Y = 140m,
            MeasurementSourceUnits = span,
            MeasurementMillimeters = span * 25.4m,
            DisplayText = "4'-2\"",
            RenderTextX = midX,
            RenderTextY = 148m,
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto("LINE-1", 1, startX, 140m, startX, 100m),
                new DimensionLinePrimitiveDto("LINE-2", 2, endX, 140m, endX, 100m),
                new DimensionLinePrimitiveDto("LINE-3", 3, startX, 140m, endX, 140m)
            ],
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto("TEXT-1", 1, "4'-2\"", midX, 148m, 3.5m, 0m)
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("INSERT-1", 1, "_Dot", startX, 140m, 0m),
                new DimensionInsertPrimitiveDto("INSERT-2", 2, "_Dot", endX, 140m, 0m)
            ]
        };
    }

    private static DimensionDto CreateVerticalLinearDimension()
    {
        return new DimensionDto(
            Guid.NewGuid(),
            "DIMENSION:VERT",
            "DIMS",
            "DIMENSION",
            "*D170",
            "10'-4\"",
            "GeometryBlock",
            string.Empty,
            124m,
            3149.6m,
            "Inch",
            90,
            0m,
            0m,
            100m,
            100m,
            0m,
            100m,
            224m,
            0m,
            140m,
            100m,
            0m,
            0.99m,
            null,
            1)
        {
            SourceHandle = "VERT",
            RenderTextX = 148m,
            RenderTextY = 162m,
            RenderTextHeight = 3.5m,
            RenderTextStyleName = "ARCH",
            RenderTextAttachmentPoint = "MiddleCenter",
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto("LINE-1", 1, 140m, 100m, 100m, 100m),
                new DimensionLinePrimitiveDto("LINE-2", 2, 140m, 224m, 100m, 224m),
                new DimensionLinePrimitiveDto("LINE-3", 3, 140m, 100m, 140m, 224m)
            ],
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto("TEXT-1", 1, "10'-4\"", 148m, 162m, 3.5m, 90m)
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("INSERT-1", 1, "_Dot", 140m, 100m, 0m),
                new DimensionInsertPrimitiveDto("INSERT-2", 2, "_Dot", 140m, 224m, 0m)
            ]
        };
    }

    private static DimensionDto CreateVerticalDimension(decimal startY, decimal endY)
    {
        var span = decimal.Abs(endY - startY);
        var midY = startY + ((endY - startY) / 2m);
        var displayText = DimensionDisplayTextFormatter.Resolve(
            CreateVerticalLinearDimension(),
            span,
            "TestMeasurement").DisplayText;

        return CreateVerticalLinearDimension() with
        {
            DefPointY = startY,
            DefPoint2Y = endY,
            DefPoint3Y = startY,
            MeasurementSourceUnits = span,
            MeasurementMillimeters = span * 25.4m,
            DisplayText = displayText,
            RenderTextY = midY,
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto("LINE-1", 1, 140m, startY, 100m, startY),
                new DimensionLinePrimitiveDto("LINE-2", 2, 140m, endY, 100m, endY),
                new DimensionLinePrimitiveDto("LINE-3", 3, 140m, startY, 140m, endY)
            ],
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto("TEXT-1", 1, displayText, 148m, midY, 3.5m, 90m)
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("INSERT-1", 1, "_Dot", 140m, startY, 0m),
                new DimensionInsertPrimitiveDto("INSERT-2", 2, "_Dot", 140m, endY, 0m)
            ]
        };
    }

    private static DimensionDto CreateFreeAngleDimension()
    {
        return CreateLinearDimension() with
        {
            Angle = 45m,
            DefPoint2X = 200m,
            DefPoint2Y = 200m,
            DefPoint3X = 90m,
            DefPoint3Y = 110m,
            MeasurementSourceUnits = 141.421m,
            MeasurementMillimeters = 3592.093m,
            DisplayText = "11'-9\"",
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto("LINE-1", 1, 90m, 110m, 100m, 100m),
                new DimensionLinePrimitiveDto("LINE-2", 2, 190m, 210m, 200m, 200m),
                new DimensionLinePrimitiveDto("LINE-3", 3, 90m, 110m, 190m, 210m)
            ],
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto("TEXT-1", 1, "11'-9\"", 140m, 160m, 3.5m, 45m)
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("INSERT-1", 1, "_Dot", 90m, 110m, 0m),
                new DimensionInsertPrimitiveDto("INSERT-2", 2, "_Dot", 190m, 210m, 0m)
            ]
        };
    }

    private static DimensionDto CreateReversedSplitLinearDimensionBelow()
    {
        return CreateLinearDimension() with
        {
            DefPointX = 224m,
            DefPointY = 100m,
            DefPoint2X = 100m,
            DefPoint2Y = 100m,
            DefPoint3X = 224m,
            DefPoint3Y = 60m,
            RenderTextX = 162m,
            RenderTextY = 52m,
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto("ROOF-RIGHT", 1, 224m, 60m, 174m, 60m),
                new DimensionLinePrimitiveDto("ROOF-LEFT", 2, 150m, 60m, 100m, 60m),
                new DimensionLinePrimitiveDto("LEG-RIGHT", 3, 224m, 60m, 224m, 100m),
                new DimensionLinePrimitiveDto("LEG-LEFT", 4, 100m, 60m, 100m, 100m)
            ],
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto("TEXT-1", 1, "10'-4\"", 162m, 52m, 3.5m, 0m)
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("INSERT-RIGHT", 1, "_Dot", 224m, 60m, 0m),
                new DimensionInsertPrimitiveDto("INSERT-LEFT", 2, "_Dot", 100m, 60m, 0m)
            ]
        };
    }
}
