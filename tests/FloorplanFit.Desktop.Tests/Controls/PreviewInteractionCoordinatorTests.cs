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
    public void CalculateWheelZoomFactor_clamps_to_practically_unbounded_maximum()
    {
        var clampedMaximum = PreviewInteractionCoordinator.CalculateWheelZoomFactor(1_000_000d, wheelDeltaY: 10d);

        Assert.Equal(1_000_000d, clampedMaximum);
    }

    [Fact]
    public void CalculateWheelZoomFactor_zooms_in_and_out_with_bounds()
    {
        var zoomedIn = PreviewInteractionCoordinator.CalculateWheelZoomFactor(1d, wheelDeltaY: 1d);
        var zoomedOut = PreviewInteractionCoordinator.CalculateWheelZoomFactor(1d, wheelDeltaY: -1d);
        var clampedMinimum = PreviewInteractionCoordinator.CalculateWheelZoomFactor(0.2d, wheelDeltaY: -10d);
        var clampedMaximum = PreviewInteractionCoordinator.CalculateWheelZoomFactor(100d, wheelDeltaY: 20d);

        Assert.Equal(2d, zoomedIn);
        Assert.Equal(0.5d, zoomedOut, precision: 12);
        Assert.Equal(FloorPlanPreviewControl.MinimumUserZoomFactor, clampedMinimum);
        Assert.Equal(FloorPlanPreviewControl.MaximumUserZoomFactor, clampedMaximum);
    }

    [Fact]
    public void CalculateWheelZoomFactor_reaches_wall_inspection_zoom_with_few_wheel_ticks()
    {
        var zoomAfterTenTicks = PreviewInteractionCoordinator.CalculateWheelZoomFactor(1d, wheelDeltaY: 10d);

        Assert.True(zoomAfterTenTicks > 1000d);
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
                    FloorPlanPreviewControl.DimensionHandleKind.FirstDefinitionPoint,
                    new Point(100d, 140d)),
                ResolveDimensionHit: _ => new FloorPlanPreviewControl.DimensionHit(
                    bodyDimension,
                    FloorPlanPreviewControl.DimensionHandleKind.DimensionLinePoint,
                    FloorPlanPreviewControl.DimensionHitArea.BodyLine,
                    new Point(160d, 140d)),
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
    public void HandleLeftButtonPressed_selects_dimension_body_without_starting_edit_until_the_drag_threshold_is_crossed()
    {
        var bodyDimension = CreateDimension("DIM-BODY");
        var outcome = PreviewInteractionCoordinator.HandleLeftButtonPressed(
            new PreviewInteractionCoordinator.LeftButtonPressRequest(
                PointerPosition: new Point(320d, 160d),
                AxisTag: PinchAxisTag.Width,
                IsPinchPlacementArmed: false,
                ResolveEdgeDrag: (_, _) => null,
                ResolveDimensionHandleHit: _ => null,
                ResolveDimensionHit: _ => new FloorPlanPreviewControl.DimensionHit(
                    bodyDimension,
                    FloorPlanPreviewControl.DimensionHandleKind.DimensionLinePoint,
                    FloorPlanPreviewControl.DimensionHitArea.BodyLine,
                    new Point(160d, 140d)),
                ResolveRoomLabelHit: _ => null,
                ResolveOpeningLabelHit: _ => null,
                ResolveGeometryHit: _ => null,
                ResolveMovableArtifact: _ => null));

        Assert.True(outcome.Handled);
        Assert.True(outcome.CapturePointer);
        Assert.Equal(bodyDimension.DimensionId, outcome.DimensionClickedId);
        Assert.Null(outcome.StartedDimensionEdit);
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

    [Fact]
    public void HandlePointerMoved_updates_preview_trim_while_edge_drag_is_active()
    {
        var viewport = new FloorPlanPreviewGeometry.PreviewViewport(
            new Rect(0, 0, 800, 600),
            MinX: 0d,
            MinY: 0d,
            Scale: 2d,
            OffsetX: 100d,
            OffsetY: 80d);

        var state = new PreviewInteractionCoordinator.InteractionState(
            PreviewZoomState: FloorPlanPreviewControl.PreviewZoomState.Default,
            IsPanningPreview: false,
            PanStartPoint: default,
            PanStartZoomState: FloorPlanPreviewControl.PreviewZoomState.Default,
            ActiveDragEdge: FloorPlanPreviewGeometry.PreviewCompressionEdge.Right,
            DragStartPoint: new Point(420d, 200d),
            ActivePreviewTrimSourceUnits: 0m,
            ActiveArtifactMove: null,
            PendingDimensionEdit: null,
            ActiveDimensionEdit: null);

        var outcome = PreviewInteractionCoordinator.HandlePointerMoved(
            new PreviewInteractionCoordinator.PointerMovedRequest(
                CurrentState: state,
                PointerPosition: new Point(390d, 200d),
                Viewport: viewport,
                ResolveSnappedDimensionWorldPoint: null));

        Assert.False(outcome.Handled);
        Assert.True(outcome.InvalidateVisual);
        Assert.Equal(15m, outcome.NextState.ActivePreviewTrimSourceUnits);
    }

    [Fact]
    public void HandlePointerReleased_clears_edge_drag_without_marking_release_as_handled()
    {
        var state = new PreviewInteractionCoordinator.InteractionState(
            PreviewZoomState: FloorPlanPreviewControl.PreviewZoomState.Default,
            IsPanningPreview: false,
            PanStartPoint: default,
            PanStartZoomState: FloorPlanPreviewControl.PreviewZoomState.Default,
            ActiveDragEdge: FloorPlanPreviewGeometry.PreviewCompressionEdge.Left,
            DragStartPoint: new Point(200d, 120d),
            ActivePreviewTrimSourceUnits: 18m,
            ActiveArtifactMove: null,
            PendingDimensionEdit: null,
            ActiveDimensionEdit: null);

        var outcome = PreviewInteractionCoordinator.HandlePointerReleased(
            new PreviewInteractionCoordinator.PointerReleasedRequest(
                CurrentState: state,
                PointerPosition: new Point(215d, 120d),
                Viewport: null,
                BuildDimensionEditedEventArgs: null));

        Assert.False(outcome.Handled);
        Assert.True(outcome.ReleasePointerCapture);
        Assert.True(outcome.InvalidateVisual);
        Assert.Null(outcome.NextState.ActiveDragEdge);
        Assert.Equal(0m, outcome.NextState.ActivePreviewTrimSourceUnits);
    }

    [Fact]
    public void HandlePointerReleased_commits_translation_only_when_delta_is_meaningful()
    {
        var move = FloorPlanPreviewControl.PreviewArtifactMoveState.ForTranslation(
            FloorPlanArtifactSourceKinds.OpeningCandidate,
            Guid.NewGuid(),
            new Point(100d, 100d),
            baseDx: 0m,
            baseDy: 0m);
        var state = PreviewInteractionCoordinator.InteractionState.ForArtifactMove(
            FloorPlanPreviewControl.PreviewZoomState.Default,
            move with { CurrentDeltaX = 12m, CurrentDeltaY = -4m });

        var outcome = PreviewInteractionCoordinator.HandlePointerReleased(
            new PreviewInteractionCoordinator.PointerReleasedRequest(
                CurrentState: state,
                PointerPosition: new Point(124d, 92d),
                Viewport: null,
                BuildDimensionEditedEventArgs: null));

        Assert.True(outcome.Handled);
        Assert.True(outcome.ReleasePointerCapture);
        Assert.NotNull(outcome.CommittedArtifactMove);
        Assert.Equal(12m, outcome.CommittedArtifactMove!.TranslationDx);
        Assert.Equal(-4m, outcome.CommittedArtifactMove.TranslationDy);
    }

    [Fact]
    public void HandlePointerReleased_commits_high_zoom_translation_precision()
    {
        var viewport = new FloorPlanPreviewGeometry.PreviewViewport(
            new Rect(0d, 0d, 200d, 200d),
            MinX: 0d,
            MinY: 0d,
            Scale: 10_000_000d,
            OffsetX: 0d,
            OffsetY: 0d);
        var move = FloorPlanPreviewControl.PreviewArtifactMoveState.ForTranslation(
            FloorPlanArtifactSourceKinds.OpeningCandidate,
            Guid.NewGuid(),
            new Point(20d, 120d),
            baseDx: 0m,
            baseDy: 0m);
        var state = PreviewInteractionCoordinator.InteractionState.ForArtifactMove(
            FloorPlanPreviewControl.PreviewZoomState.Default,
            move);

        var outcome = PreviewInteractionCoordinator.HandlePointerReleased(
            new PreviewInteractionCoordinator.PointerReleasedRequest(
                CurrentState: state,
                PointerPosition: new Point(30d, 110d),
                Viewport: viewport,
                BuildDimensionEditedEventArgs: null));

        Assert.True(outcome.Handled);
        Assert.NotNull(outcome.CommittedArtifactMove);
        Assert.Equal(0.000001m, outcome.CommittedArtifactMove!.TranslationDx);
        Assert.Equal(0.000001m, outcome.CommittedArtifactMove.TranslationDy);
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
