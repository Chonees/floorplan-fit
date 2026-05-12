using Avalonia;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls;
using FloorplanFit.Desktop.Controls.Preview;
using FloorplanFit.Domain.FloorPlans;
using Xunit;

namespace FloorplanFit.Desktop.Tests.Controls;

public sealed class PreviewInteractionCoordinatorTests
{
    [Fact]
    public void CalculateWheelZoomFactor_zooms_in_and_out_with_bounds()
    {
        var zoomedIn = PreviewInteractionCoordinator.CalculateWheelZoomFactor(1d, wheelDeltaY: 1d);
        var zoomedOut = PreviewInteractionCoordinator.CalculateWheelZoomFactor(1d, wheelDeltaY: -1d);
        var clampedMinimum = PreviewInteractionCoordinator.CalculateWheelZoomFactor(0.2d, wheelDeltaY: -10d);
        var clampedMaximum = PreviewInteractionCoordinator.CalculateWheelZoomFactor(20d, wheelDeltaY: 10d);

        Assert.True(zoomedIn > 1d);
        Assert.True(zoomedOut < 1d);
        Assert.Equal(FloorPlanPreviewControl.MinimumUserZoomFactor, clampedMinimum);
        Assert.Equal(FloorPlanPreviewControl.MaximumUserZoomFactor, clampedMaximum);
    }

    [Fact]
    public void ResolvePanStateForDrag_keeps_zoom_and_adds_pointer_delta_to_pan_offset()
    {
        var startState = new FloorPlanPreviewControl.PreviewZoomState(
            ZoomFactor: 2.5d,
            PanOffset: new Vector(30d, -12d));

        var nextState = PreviewInteractionCoordinator.ResolvePanStateForDrag(
            startState,
            new Point(100d, 120d),
            new Point(145d, 90d));

        Assert.Equal(startState.ZoomFactor, nextState.ZoomFactor);
        Assert.Equal(new Vector(75d, -42d), nextState.PanOffset);
    }

    [Fact]
    public void HandleMiddleButtonPressed_prefers_pan_before_any_left_button_hit_logic()
    {
        var request = PreviewInteractionCoordinator.CreatePointerPressedRequestForPan(
            pointerPosition: new Point(120d, 80d),
            currentZoomState: FloorPlanPreviewControl.PreviewZoomState.Default);

        var outcome = PreviewInteractionCoordinator.HandleMiddleButtonPressed(request);

        Assert.True(outcome.Handled);
        Assert.True(outcome.CapturePointer);
        Assert.True(outcome.InvalidateVisual);
        Assert.True(outcome.IsPanningPreview);
        Assert.Equal(new Point(120d, 80d), outcome.PanStartPoint);
        Assert.Equal(FloorPlanPreviewControl.PreviewZoomState.Default, outcome.PanStartZoomState);
    }

    [Fact]
    public void HandleLeftButtonPressed_prefers_dimension_handle_before_dimension_body()
    {
        var handleDimension = CreateDimension("DIM-HANDLE");
        var bodyDimension = CreateDimension("DIM-BODY");
        var outcome = PreviewInteractionCoordinator.HandleLeftButtonPressed(
            new PreviewInteractionCoordinator.LeftButtonPressRequest(
                PointerPosition: new Point(320d, 160d),
                AxisTag: PinchAxisTag.Width,
                IsPinchPlacementArmed: false,
                ResolveEdgeDrag: (_, _) => null,
                ResolveDimensionHandleHit: _ => new FloorPlanPreviewControl.DimensionHandleHit(
                    handleDimension,
                    FloorPlanPreviewControl.DimensionHandleKind.TextAnchor),
                ResolveDimensionHit: _ => new FloorPlanPreviewControl.DimensionHit(
                    bodyDimension,
                    FloorPlanPreviewControl.DimensionHandleKind.DimensionLinePoint),
                ResolveRoomLabelHit: _ => null,
                ResolveOpeningLabelHit: _ => null,
                ResolveGeometryHit: _ => null,
                ResolveMovableArtifact: _ => null));

        Assert.True(outcome.Handled);
        Assert.True(outcome.CapturePointer);
        Assert.Equal(handleDimension.DimensionId, outcome.DimensionClickedId);
        Assert.NotNull(outcome.StartedDimensionEdit);
        Assert.Equal("DIM-HANDLE", outcome.StartedDimensionEdit.Value.BaseDimension.SourceEntityRef);
        Assert.Null(outcome.StartedArtifactMove);
    }

    [Fact]
    public void HandleLeftButtonPressed_starts_room_label_absolute_move_before_generic_geometry_hit()
    {
        var roomLabel = new RoomLabelDto(Guid.NewGuid(), "TEXT:1", "ROOM LBLS", "KITCHEN", 40m, 75m, 0.95m, null, 1);
        var outcome = PreviewInteractionCoordinator.HandleLeftButtonPressed(
            new PreviewInteractionCoordinator.LeftButtonPressRequest(
                PointerPosition: new Point(100d, 140d),
                AxisTag: PinchAxisTag.Width,
                IsPinchPlacementArmed: false,
                ResolveEdgeDrag: (_, _) => null,
                ResolveDimensionHandleHit: _ => null,
                ResolveDimensionHit: _ => null,
                ResolveRoomLabelHit: _ => roomLabel,
                ResolveOpeningLabelHit: _ => null,
                ResolveGeometryHit: _ => new PreviewInteractionCoordinator.GeometryHit(Guid.NewGuid(), 0.4m),
                ResolveMovableArtifact: _ => null));

        Assert.True(outcome.Handled);
        Assert.NotNull(outcome.StartedArtifactMove);
        Assert.Equal(FloorPlanArtifactPositionMode.AbsolutePoint, outcome.StartedArtifactMove.Value.PositionMode);
        Assert.Equal(roomLabel.RoomLabelId, outcome.RoomLabelClickedId);
    }

    private static DimensionDto CreateDimension(string sourceKey)
        => new(
            Guid.NewGuid(),
            sourceKey,
            "DIMS",
            "DIMENSION",
            "*D1",
            "8'-0\"",
            "GeometryBlock",
            string.Empty,
            96m,
            2438.4m,
            "Inch",
            0,
            0m,
            0m,
            0m,
            0m,
            0m,
            100m,
            0m,
            0m,
            0m,
            20m,
            0m,
            0.99m,
            null,
            1);
}
