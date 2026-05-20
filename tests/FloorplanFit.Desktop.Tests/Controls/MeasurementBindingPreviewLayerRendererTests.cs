using Avalonia;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls;
using FloorplanFit.Desktop.Controls.Preview;
using FloorplanFit.Domain.FloorPlans;
using Xunit;

namespace FloorplanFit.Desktop.Tests.Controls;

public sealed class MeasurementBindingPreviewLayerRendererTests
{
    [Fact]
    public void ResolveOverlay_returns_selected_dimension_interval_and_nodes_for_the_active_corridor()
    {
        var corridorId = Guid.NewGuid();
        var pathId = Guid.NewGuid();
        var dimensionId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var scene = CreateScene(
            geometry:
            [
                new GeometryPathDto(pathId, false, [new GeometrySegmentDto(pathId, 1, 100m, 120m, 260m, 120m)])
            ],
            measurementCorridors:
            [
                new MeasurementCorridorDto(corridorId, "Patio-Width", nameof(PinchAxisTag.Width), pathId, 95m, 145m, "Verified", 1)
            ],
            measurementNodes:
            [
                new MeasurementNodeDto(startNodeId, corridorId, 1, "ProjectedGeometry", "WallCandidate", Guid.NewGuid(), pathId, "Projected", 100m, 120m, 100m, 0m, 0m, 0m),
                new MeasurementNodeDto(endNodeId, corridorId, 2, "ProjectedGeometry", "OpeningCandidate", Guid.NewGuid(), pathId, "Projected", 260m, 120m, 260m, 0m, 0m, 1m)
            ],
            intervalBindings:
            [
                new DimensionIntervalBindingDto(dimensionId, corridorId, startNodeId, endNodeId, "ManualVerified", 100m, 260m)
            ],
            highlightDimensionId: dimensionId,
            selectedMeasurementCorridorId: corridorId);

        var overlay = MeasurementBindingPreviewLayerRenderer.ResolveOverlay(scene);

        Assert.NotNull(overlay.ActiveCorridor);
        Assert.Equal(corridorId, overlay.ActiveCorridor!.CorridorId);
        Assert.Equal(2, overlay.CorridorNodes.Count);
        Assert.NotNull(overlay.ActiveInterval);
        Assert.Equal(startNodeId, overlay.ActiveInterval!.StartNodeId);
        Assert.Equal(endNodeId, overlay.ActiveInterval.EndNodeId);
        Assert.Equal(new Point(100d, 120d), overlay.ActiveInterval.StartPoint);
        Assert.Equal(new Point(260d, 120d), overlay.ActiveInterval.EndPoint);
    }

    [Fact]
    public void ResolveOverlay_uses_the_raw_two_node_line_when_dimension_is_highlighted()
    {
        var corridorId = Guid.NewGuid();
        var leftWallPathId = Guid.NewGuid();
        var rightWallPathId = Guid.NewGuid();
        var guidePathId = Guid.NewGuid();
        var dimensionId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var dimension = CreateHorizontalDimension(dimensionId);
        var scene = CreateScene(
            geometry:
            [
                new GeometryPathDto(leftWallPathId, false, [new GeometrySegmentDto(leftWallPathId, 1, 100m, 120m, 100m, 160m)]),
                new GeometryPathDto(rightWallPathId, false, [new GeometrySegmentDto(rightWallPathId, 1, 260m, 180m, 260m, 220m)]),
                new GeometryPathDto(guidePathId, false, [new GeometrySegmentDto(guidePathId, 1, 90m, 300m, 270m, 300m)])
            ],
            dimensions:
            [
                dimension
            ],
            measurementCorridors:
            [
                new MeasurementCorridorDto(corridorId, "Facade-TotalWidth", nameof(PinchAxisTag.Width), guidePathId, 95m, 145m, "Verified", 1)
            ],
            measurementNodes:
            [
                new MeasurementNodeDto(startNodeId, corridorId, 1, "ProjectedGeometry", "WallCandidate", Guid.NewGuid(), leftWallPathId, "Projected", 100m, 120m, 100m, 0m, 0m, 0m),
                new MeasurementNodeDto(endNodeId, corridorId, 2, "ProjectedGeometry", "WallCandidate", Guid.NewGuid(), rightWallPathId, "Projected", 260m, 220m, 260m, 0m, 0m, 1m)
            ],
            intervalBindings:
            [
                new DimensionIntervalBindingDto(dimensionId, corridorId, startNodeId, endNodeId, "ManualVerified", 100m, 260m)
            ],
            highlightDimensionId: dimensionId,
            selectedMeasurementCorridorId: corridorId);

        var overlay = MeasurementBindingPreviewLayerRenderer.ResolveOverlay(scene);

        Assert.NotNull(overlay.ActiveInterval);
        Assert.Equal(new Point(100d, 120d), overlay.ActiveInterval!.StartPoint);
        Assert.Equal(new Point(260d, 220d), overlay.ActiveInterval.EndPoint);
        Assert.Equal(new Point(100d, 120d), overlay.CorridorNodes[0].WorldPoint);
        Assert.Equal(new Point(260d, 220d), overlay.CorridorNodes[1].WorldPoint);
    }

    [Fact]
    public void ResolveOverlay_uses_the_raw_two_node_line_when_no_dimension_is_highlighted()
    {
        var corridorId = Guid.NewGuid();
        var leftWallPathId = Guid.NewGuid();
        var rightWallPathId = Guid.NewGuid();
        var guidePathId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var scene = CreateScene(
            geometry:
            [
                new GeometryPathDto(leftWallPathId, false, [new GeometrySegmentDto(leftWallPathId, 1, 100m, 120m, 100m, 160m)]),
                new GeometryPathDto(rightWallPathId, false, [new GeometrySegmentDto(rightWallPathId, 1, 260m, 180m, 260m, 220m)]),
                new GeometryPathDto(guidePathId, false, [new GeometrySegmentDto(guidePathId, 1, 90m, 300m, 270m, 300m)])
            ],
            measurementCorridors:
            [
                new MeasurementCorridorDto(corridorId, "Facade-TotalWidth", nameof(PinchAxisTag.Width), guidePathId, 95m, 145m, "Verified", 1)
            ],
            measurementNodes:
            [
                new MeasurementNodeDto(startNodeId, corridorId, 1, "ProjectedGeometry", "WallCandidate", Guid.NewGuid(), leftWallPathId, "Projected", 100m, 120m, 100m, 0m, 0m, 0m),
                new MeasurementNodeDto(endNodeId, corridorId, 2, "ProjectedGeometry", "WallCandidate", Guid.NewGuid(), rightWallPathId, "Projected", 260m, 220m, 260m, 0m, 0m, 1m)
            ],
            selectedMeasurementCorridorId: corridorId,
            selectedMeasurementStartNodeId: startNodeId,
            selectedMeasurementEndNodeId: endNodeId);

        var overlay = MeasurementBindingPreviewLayerRenderer.ResolveOverlay(scene);

        Assert.NotNull(overlay.ActiveInterval);
        Assert.Equal(new Point(100d, 120d), overlay.ActiveInterval!.StartPoint);
        Assert.Equal(new Point(260d, 220d), overlay.ActiveInterval.EndPoint);
    }

    [Fact]
    public void ResolveOverlay_returns_selected_articulation_band_for_the_preview_pinch_group()
    {
        var pinchGroupId = Guid.NewGuid();
        var scene = CreateScene(
            geometry:
            [
                new GeometryPathDto(Guid.NewGuid(), false, [new GeometrySegmentDto(Guid.NewGuid(), 1, 100m, 120m, 260m, 120m)])
            ],
            articulationBands:
            [
                new ArticulationBandDto(pinchGroupId, "Patio", nameof(PinchAxisTag.Width), 150m, 220m, 120m, "Verified")
            ],
            previewPinchGroupId: pinchGroupId,
            previewAxisTag: nameof(PinchAxisTag.Width));

        var overlay = MeasurementBindingPreviewLayerRenderer.ResolveOverlay(scene);

        Assert.NotNull(overlay.ActiveArticulationBand);
        Assert.Equal(150m, overlay.ActiveArticulationBand!.BandStartCoordinate);
        Assert.Equal(220m, overlay.ActiveArticulationBand.BandEndCoordinate);
    }

    private static PreviewRenderScene CreateScene(
        IReadOnlyList<GeometryPathDto>? geometry = null,
        IReadOnlyList<DimensionDto>? dimensions = null,
        IReadOnlyList<MeasurementCorridorDto>? measurementCorridors = null,
        IReadOnlyList<MeasurementNodeDto>? measurementNodes = null,
        IReadOnlyList<DimensionIntervalBindingDto>? intervalBindings = null,
        IReadOnlyList<ArticulationBandDto>? articulationBands = null,
        Guid? highlightDimensionId = null,
        Guid? selectedMeasurementCorridorId = null,
        Guid? selectedMeasurementStartNodeId = null,
        Guid? selectedMeasurementEndNodeId = null,
        Guid? previewPinchGroupId = null,
        string? previewAxisTag = null)
    {
        return new PreviewRenderScene(
            Bounds: new Rect(0, 0, 800, 600),
            AxisTag: previewAxisTag is null ? null : Enum.Parse<PinchAxisTag>(previewAxisTag),
            IsPinchPlacementArmed: false,
            Viewport: FloorPlanPreviewGeometry.CalculateViewport(geometry ?? [], new Rect(0, 0, 800, 600), 48d),
            PreviewGeometry: geometry ?? [],
            RoomLabels: [],
            OpeningLabels: [],
            Dimensions: dimensions ?? [],
            AreDimensionsVisible: true,
            ArtifactIndex: PreviewArtifactGeometryIndex.Create(openingCandidates: null, fixedPlanComponents: null),
            OpeningCandidates: [],
            FixedPlanComponents: [],
            ProtectedDetailAssemblies: [],
            CuratedPlanArtifacts: [],
            PinchMarkers: [],
            MeasurementCorridors: measurementCorridors ?? [],
            MeasurementNodes: measurementNodes ?? [],
            DimensionIntervalBindings: intervalBindings ?? [],
            ArticulationBands: articulationBands ?? [],
            SelectedMeasurementCorridorId: selectedMeasurementCorridorId,
            SelectedMeasurementNodeId: null,
            SelectedMeasurementStartNodeId: selectedMeasurementStartNodeId,
            SelectedMeasurementEndNodeId: selectedMeasurementEndNodeId,
            HighlightGeometryPathId: null,
            HighlightRoomLabelId: null,
            HighlightOpeningLabelId: null,
            HighlightDimensionId: highlightDimensionId,
            PreviewPinchGroupId: previewPinchGroupId,
            PreviewAxisTag: previewAxisTag,
            ActiveDimensionHandleKind: null);
    }

    private static DimensionDto CreateHorizontalDimension(Guid dimensionId)
    {
        return new DimensionDto(
            dimensionId,
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
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto("LINE-1", 1, 100m, 140m, 100m, 100m),
                new DimensionLinePrimitiveDto("LINE-2", 2, 224m, 140m, 224m, 100m),
                new DimensionLinePrimitiveDto("LINE-3", 3, 100m, 140m, 224m, 140m)
            ]
        };
    }
}
