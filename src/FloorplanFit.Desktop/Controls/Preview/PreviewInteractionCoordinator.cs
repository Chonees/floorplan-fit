using Avalonia;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class PreviewInteractionCoordinator
{
    private const double UserZoomStep = 2d;
    private const double DimensionBodyDragThresholdPixels = 4d;

    internal readonly record struct MiddleButtonPanStartRequest(
        Point PointerPosition,
        FloorPlanPreviewControl.PreviewZoomState CurrentZoomState);

    internal readonly record struct MiddleButtonPanStartOutcome(
        bool Handled,
        bool CapturePointer,
        bool InvalidateVisual,
        bool IsPanningPreview,
        Point PanStartPoint,
        FloorPlanPreviewControl.PreviewZoomState PanStartZoomState);

    internal readonly record struct GeometryHit(Guid GeometryPathId, decimal PositionRatio);

    internal readonly record struct LeftButtonPressRequest(
        Point PointerPosition,
        PinchAxisTag? AxisTag,
        bool IsPinchPlacementArmed,
        Func<Point, PinchAxisTag, FloorPlanPreviewGeometry.PreviewCompressionEdge?> ResolveEdgeDrag,
        Func<Point, FloorPlanPreviewControl.DimensionHandleHit?> ResolveDimensionHandleHit,
        Func<Point, FloorPlanPreviewControl.DimensionHit?> ResolveDimensionHit,
        Func<Point, RoomLabelDto?> ResolveRoomLabelHit,
        Func<Point, OpeningLabelDto?> ResolveOpeningLabelHit,
        Func<Point, GeometryHit?> ResolveGeometryHit,
        Func<Guid, FloorPlanPreviewControl.PreviewMovableArtifactDescriptor?> ResolveMovableArtifact);

    internal readonly record struct LeftButtonPressOutcome(
        bool Handled,
        bool CapturePointer,
        bool InvalidateVisual,
        Guid? DimensionClickedId,
        Guid? RoomLabelClickedId,
        Guid? OpeningLabelClickedId,
        GeometryHit? GeometryClick,
        FloorPlanPreviewGeometry.PreviewCompressionEdge? StartedEdgeDrag,
        FloorPlanPreviewControl.PreviewArtifactMoveState? StartedArtifactMove,
        FloorPlanPreviewControl.PreviewPendingDimensionEditState? PendingDimensionEdit,
        FloorPlanPreviewControl.PreviewDimensionEditState? StartedDimensionEdit);

    internal readonly record struct InteractionState(
        FloorPlanPreviewControl.PreviewZoomState PreviewZoomState,
        bool IsPanningPreview,
        Point PanStartPoint,
        FloorPlanPreviewControl.PreviewZoomState PanStartZoomState,
        FloorPlanPreviewGeometry.PreviewCompressionEdge? ActiveDragEdge,
        Point DragStartPoint,
        decimal ActivePreviewTrimSourceUnits,
        FloorPlanPreviewControl.PreviewArtifactMoveState? ActiveArtifactMove,
        FloorPlanPreviewControl.PreviewPendingDimensionEditState? PendingDimensionEdit,
        FloorPlanPreviewControl.PreviewDimensionEditState? ActiveDimensionEdit)
    {
        public static InteractionState Default { get; } = new(
            FloorPlanPreviewControl.PreviewZoomState.Default,
            false,
            default,
            FloorPlanPreviewControl.PreviewZoomState.Default,
            null,
            default,
            0m,
            null,
            null,
            null);

        public static InteractionState ForArtifactMove(
            FloorPlanPreviewControl.PreviewZoomState previewZoomState,
            FloorPlanPreviewControl.PreviewArtifactMoveState move)
            => new(
                previewZoomState,
                false,
                default,
                FloorPlanPreviewControl.PreviewZoomState.Default,
                null,
                default,
                0m,
                move,
                null,
                null);
    }

    internal readonly record struct PointerMovedRequest(
        InteractionState CurrentState,
        Point PointerPosition,
        FloorPlanPreviewGeometry.PreviewViewport? Viewport,
        Func<FloorPlanPreviewControl.PreviewDimensionEditState, Point, Point>? ResolveSnappedDimensionWorldPoint);

    internal readonly record struct PointerMovedOutcome(
        bool Handled,
        bool InvalidateVisual,
        InteractionState NextState);

    internal readonly record struct PointerReleasedRequest(
        InteractionState CurrentState,
        Point PointerPosition,
        FloorPlanPreviewGeometry.PreviewViewport? Viewport,
        Func<FloorPlanPreviewControl.PreviewDimensionEditState, FloorPlanPreviewControl.DimensionEditedEventArgs?>? BuildDimensionEditedEventArgs);

    internal readonly record struct PointerReleasedOutcome(
        bool Handled,
        bool InvalidateVisual,
        bool ReleasePointerCapture,
        InteractionState NextState,
        FloorPlanPreviewControl.MovableArtifactMovedEventArgs? CommittedArtifactMove,
        FloorPlanPreviewControl.DimensionEditedEventArgs? CommittedDimensionEdit);

    public static double CalculateWheelZoomFactor(double currentZoomFactor, double wheelDeltaY)
    {
        var safeCurrentZoom = double.IsFinite(currentZoomFactor)
            ? currentZoomFactor
            : 1d;
        var zoomStep = UserZoomStep;
        var requestedZoom = safeCurrentZoom * Math.Pow(zoomStep, wheelDeltaY);
        var minimumZoom = FloorPlanPreviewControl.MinimumUserZoomFactor;
        var maximumZoom = FloorPlanPreviewControl.MaximumUserZoomFactor;
        return Math.Clamp(
            requestedZoom,
            minimumZoom,
            maximumZoom);
    }

    public static FloorPlanPreviewControl.PreviewZoomState ResolveZoomStateForWheel(
        FloorPlanPreviewGeometry.PreviewViewport baseViewport,
        FloorPlanPreviewControl.PreviewZoomState currentState,
        Point pointerPosition,
        double wheelDeltaY)
    {
        var currentZoom = Math.Clamp(
            double.IsFinite(currentState.ZoomFactor) ? currentState.ZoomFactor : 1d,
            FloorPlanPreviewControl.MinimumUserZoomFactor,
            FloorPlanPreviewControl.MaximumUserZoomFactor);
        var currentViewport = baseViewport.WithUserTransform(currentZoom, currentState.PanOffset);
        var worldPoint = currentViewport.Unproject(pointerPosition);
        var nextZoom = CalculateWheelZoomFactor(currentZoom, wheelDeltaY);
        var nextViewportWithoutPan = baseViewport.WithUserTransform(nextZoom, default);
        var projectedWithoutPan = nextViewportWithoutPan.Project(worldPoint.X, worldPoint.Y);
        var nextPanOffset = new Vector(
            pointerPosition.X - projectedWithoutPan.X,
            pointerPosition.Y - projectedWithoutPan.Y);

        return new FloorPlanPreviewControl.PreviewZoomState(nextZoom, nextPanOffset);
    }

    public static FloorPlanPreviewControl.PreviewZoomState ResolvePanStateForDrag(
        FloorPlanPreviewControl.PreviewZoomState startState,
        Point startPointerPosition,
        Point currentPointerPosition)
    {
        var pointerDelta = currentPointerPosition - startPointerPosition;
        return new FloorPlanPreviewControl.PreviewZoomState(
            startState.ZoomFactor,
            startState.PanOffset + pointerDelta);
    }

    public static MiddleButtonPanStartRequest CreatePointerPressedRequestForPan(
        Point pointerPosition,
        FloorPlanPreviewControl.PreviewZoomState currentZoomState)
        => new(pointerPosition, currentZoomState);

    public static MiddleButtonPanStartOutcome HandleMiddleButtonPressed(MiddleButtonPanStartRequest request)
        => new(
            Handled: true,
            CapturePointer: true,
            InvalidateVisual: true,
            IsPanningPreview: true,
            PanStartPoint: request.PointerPosition,
            PanStartZoomState: request.CurrentZoomState);

    public static LeftButtonPressOutcome HandleLeftButtonPressed(LeftButtonPressRequest request)
    {
        if (!request.IsPinchPlacementArmed && request.AxisTag is { } axisTag)
        {
            var edge = request.ResolveEdgeDrag(request.PointerPosition, axisTag);
            if (edge is not null)
            {
                return new LeftButtonPressOutcome(
                    Handled: true,
                    CapturePointer: true,
                    InvalidateVisual: false,
                    DimensionClickedId: null,
                    RoomLabelClickedId: null,
                    OpeningLabelClickedId: null,
                    GeometryClick: null,
                    StartedEdgeDrag: edge,
                    StartedArtifactMove: null,
                    PendingDimensionEdit: null,
                    StartedDimensionEdit: null);
            }
        }

        if (!request.IsPinchPlacementArmed &&
            request.ResolveDimensionHandleHit(request.PointerPosition) is { } dimensionHandleHit)
        {
            return new LeftButtonPressOutcome(
                Handled: true,
                CapturePointer: true,
                InvalidateVisual: true,
                DimensionClickedId: dimensionHandleHit.Dimension.DimensionId,
                RoomLabelClickedId: null,
                OpeningLabelClickedId: null,
                GeometryClick: null,
                StartedEdgeDrag: null,
                StartedArtifactMove: null,
                PendingDimensionEdit: null,
                StartedDimensionEdit: FloorPlanPreviewControl.PreviewDimensionEditState.Start(
                    dimensionHandleHit.Dimension,
                    dimensionHandleHit.HandleKind,
                    request.PointerPosition,
                    dimensionHandleHit.ReferenceWorldPoint));
        }

        if (!request.IsPinchPlacementArmed &&
            request.ResolveDimensionHit(request.PointerPosition) is { } dimensionHit)
        {
            return new LeftButtonPressOutcome(
                Handled: true,
                CapturePointer: true,
                InvalidateVisual: true,
                DimensionClickedId: dimensionHit.Dimension.DimensionId,
                RoomLabelClickedId: null,
                OpeningLabelClickedId: null,
                GeometryClick: null,
                StartedEdgeDrag: null,
                StartedArtifactMove: null,
                PendingDimensionEdit: FloorPlanPreviewControl.PreviewPendingDimensionEditState.Start(
                    dimensionHit.Dimension,
                    dimensionHit.SuggestedHandle,
                    request.PointerPosition,
                    dimensionHit.ReferenceWorldPoint),
                StartedDimensionEdit: null);
        }

        if (!request.IsPinchPlacementArmed &&
            request.ResolveRoomLabelHit(request.PointerPosition) is { } roomLabel)
        {
            return new LeftButtonPressOutcome(
                Handled: true,
                CapturePointer: true,
                InvalidateVisual: true,
                DimensionClickedId: null,
                RoomLabelClickedId: roomLabel.RoomLabelId,
                OpeningLabelClickedId: null,
                GeometryClick: null,
                StartedEdgeDrag: null,
                StartedArtifactMove: FloorPlanPreviewControl.PreviewArtifactMoveState.ForAbsolutePoint(
                    FloorPlanArtifactPositionSourceKinds.RoomLabel,
                    roomLabel.RoomLabelId,
                    request.PointerPosition,
                    roomLabel.X,
                    roomLabel.Y),
                PendingDimensionEdit: null,
                StartedDimensionEdit: null);
        }

        if (!request.IsPinchPlacementArmed &&
            request.ResolveOpeningLabelHit(request.PointerPosition) is { } openingLabel)
        {
            return new LeftButtonPressOutcome(
                Handled: true,
                CapturePointer: true,
                InvalidateVisual: true,
                DimensionClickedId: null,
                RoomLabelClickedId: null,
                OpeningLabelClickedId: openingLabel.OpeningLabelId,
                GeometryClick: null,
                StartedEdgeDrag: null,
                StartedArtifactMove: FloorPlanPreviewControl.PreviewArtifactMoveState.ForAbsolutePoint(
                    FloorPlanArtifactPositionSourceKinds.OpeningLabel,
                    openingLabel.OpeningLabelId,
                    request.PointerPosition,
                    openingLabel.X,
                    openingLabel.Y),
                PendingDimensionEdit: null,
                StartedDimensionEdit: null);
        }

        if (request.ResolveGeometryHit(request.PointerPosition) is not { } geometryHit)
        {
            return default;
        }

        var movableArtifact = !request.IsPinchPlacementArmed
            ? request.ResolveMovableArtifact(geometryHit.GeometryPathId)
            : null;
        FloorPlanPreviewControl.PreviewArtifactMoveState? startedArtifactMove = movableArtifact is { } descriptor
            ? descriptor.PositionMode == FloorPlanArtifactPositionMode.AbsolutePoint
                ? FloorPlanPreviewControl.PreviewArtifactMoveState.ForAbsolutePoint(
                    descriptor.SourceArtifactKind,
                    descriptor.SourceArtifactId,
                    request.PointerPosition,
                    descriptor.BaseX,
                    descriptor.BaseY)
                : FloorPlanPreviewControl.PreviewArtifactMoveState.ForTranslation(
                    descriptor.SourceArtifactKind,
                    descriptor.SourceArtifactId,
                    request.PointerPosition,
                    descriptor.BaseDx,
                    descriptor.BaseDy)
            : null;

        return new LeftButtonPressOutcome(
            Handled: true,
            CapturePointer: startedArtifactMove is not null,
            InvalidateVisual: startedArtifactMove is not null,
            DimensionClickedId: null,
            RoomLabelClickedId: null,
            OpeningLabelClickedId: null,
            GeometryClick: geometryHit,
            StartedEdgeDrag: null,
            StartedArtifactMove: startedArtifactMove,
            PendingDimensionEdit: null,
            StartedDimensionEdit: null);
    }

    public static PointerMovedOutcome HandlePointerMoved(PointerMovedRequest request)
    {
        var state = request.CurrentState;
        if (state.IsPanningPreview)
        {
            return new PointerMovedOutcome(
                Handled: true,
                InvalidateVisual: true,
                NextState: state with
                {
                    PreviewZoomState = ResolvePanStateForDrag(state.PanStartZoomState, state.PanStartPoint, request.PointerPosition)
                });
        }

        if (state.ActiveArtifactMove is { } activeArtifactMove)
        {
            if (request.Viewport is null || request.Viewport.Value.Scale <= double.Epsilon)
            {
                return new PointerMovedOutcome(false, false, state);
            }

            var delta = ResolveWorldDelta(request.Viewport.Value, activeArtifactMove.PointerStart, request.PointerPosition);
            return new PointerMovedOutcome(
                Handled: true,
                InvalidateVisual: true,
                NextState: state with
                {
                    ActiveArtifactMove = activeArtifactMove with
                    {
                        CurrentDeltaX = delta.DeltaX,
                        CurrentDeltaY = delta.DeltaY
                    }
                });
        }

        if (state.PendingDimensionEdit is { } pendingDimensionEdit)
        {
            if (request.Viewport is null ||
                request.Viewport.Value.Scale <= double.Epsilon ||
                request.ResolveSnappedDimensionWorldPoint is null)
            {
                return new PointerMovedOutcome(false, false, state);
            }

            var pointerDelta = request.PointerPosition - pendingDimensionEdit.PointerStart;
            var distanceSquared = (pointerDelta.X * pointerDelta.X) + (pointerDelta.Y * pointerDelta.Y);
            if (distanceSquared < DimensionBodyDragThresholdPixels * DimensionBodyDragThresholdPixels)
            {
                return new PointerMovedOutcome(false, false, state);
            }

            var activeEdit = FloorPlanPreviewControl.PreviewDimensionEditState.Start(
                pendingDimensionEdit.BaseDimension,
                pendingDimensionEdit.HandleKind,
                pendingDimensionEdit.PointerStart,
                pendingDimensionEdit.ReferenceWorldPoint);
            var snappedWorld = request.ResolveSnappedDimensionWorldPoint(activeEdit, request.PointerPosition);
            return new PointerMovedOutcome(
                Handled: true,
                InvalidateVisual: true,
                NextState: state with
                {
                    PendingDimensionEdit = null,
                    ActiveDimensionEdit = activeEdit with
                    {
                        CurrentWorldPoint = snappedWorld
                    }
                });
        }

        if (state.ActiveDimensionEdit is { } activeDimensionEdit)
        {
            if (request.Viewport is null ||
                request.Viewport.Value.Scale <= double.Epsilon ||
                request.ResolveSnappedDimensionWorldPoint is null)
            {
                return new PointerMovedOutcome(false, false, state);
            }

            var snappedWorld = request.ResolveSnappedDimensionWorldPoint(activeDimensionEdit, request.PointerPosition);
            return new PointerMovedOutcome(
                Handled: true,
                InvalidateVisual: true,
                NextState: state with
                {
                    ActiveDimensionEdit = activeDimensionEdit with
                    {
                        CurrentWorldPoint = snappedWorld
                    }
                });
        }

        if (state.ActiveDragEdge is not { } activeDragEdge)
        {
            return new PointerMovedOutcome(false, false, state);
        }

        if (request.Viewport is null || request.Viewport.Value.Scale <= double.Epsilon)
        {
            return new PointerMovedOutcome(false, false, state);
        }

        var pixelDelta = activeDragEdge switch
        {
            FloorPlanPreviewGeometry.PreviewCompressionEdge.Right => Math.Max(0d, state.DragStartPoint.X - request.PointerPosition.X),
            FloorPlanPreviewGeometry.PreviewCompressionEdge.Left => Math.Max(0d, request.PointerPosition.X - state.DragStartPoint.X),
            FloorPlanPreviewGeometry.PreviewCompressionEdge.Top => Math.Max(0d, request.PointerPosition.Y - state.DragStartPoint.Y),
            FloorPlanPreviewGeometry.PreviewCompressionEdge.Bottom => Math.Max(0d, state.DragStartPoint.Y - request.PointerPosition.Y),
            _ => 0d
        };

        return new PointerMovedOutcome(
            Handled: false,
            InvalidateVisual: true,
            NextState: state with
            {
                ActivePreviewTrimSourceUnits = (decimal)(pixelDelta / request.Viewport.Value.Scale)
            });
    }

    public static PointerReleasedOutcome HandlePointerReleased(PointerReleasedRequest request)
    {
        var state = request.CurrentState;
        if (state.IsPanningPreview)
        {
            return new PointerReleasedOutcome(
                Handled: true,
                InvalidateVisual: true,
                ReleasePointerCapture: true,
                NextState: state with { IsPanningPreview = false },
                CommittedArtifactMove: null,
                CommittedDimensionEdit: null);
        }

        if (state.ActiveArtifactMove is { } activeArtifactMove)
        {
            var delta = request.Viewport is null || request.Viewport.Value.Scale <= double.Epsilon
                ? (activeArtifactMove.CurrentDeltaX, activeArtifactMove.CurrentDeltaY)
                : ResolveWorldDelta(request.Viewport.Value, activeArtifactMove.PointerStart, request.PointerPosition);
            var movement = ResolveMovement(activeArtifactMove, delta.Item1, delta.Item2);

            return new PointerReleasedOutcome(
                Handled: true,
                InvalidateVisual: true,
                ReleasePointerCapture: true,
                NextState: state with { ActiveArtifactMove = null },
                CommittedArtifactMove: movement,
                CommittedDimensionEdit: null);
        }

        if (state.PendingDimensionEdit is not null)
        {
            return new PointerReleasedOutcome(
                Handled: true,
                InvalidateVisual: false,
                ReleasePointerCapture: true,
                NextState: state with { PendingDimensionEdit = null },
                CommittedArtifactMove: null,
                CommittedDimensionEdit: null);
        }

        if (state.ActiveDimensionEdit is { } activeDimensionEdit)
        {
            return new PointerReleasedOutcome(
                Handled: true,
                InvalidateVisual: true,
                ReleasePointerCapture: true,
                NextState: state with { ActiveDimensionEdit = null },
                CommittedArtifactMove: null,
                CommittedDimensionEdit: request.BuildDimensionEditedEventArgs?.Invoke(activeDimensionEdit));
        }

        if (state.ActiveDragEdge is not null)
        {
            return new PointerReleasedOutcome(
                Handled: false,
                InvalidateVisual: true,
                ReleasePointerCapture: true,
                NextState: state with
                {
                    ActiveDragEdge = null,
                    ActivePreviewTrimSourceUnits = 0m
                },
                CommittedArtifactMove: null,
                CommittedDimensionEdit: null);
        }

        return new PointerReleasedOutcome(false, false, false, state, null, null);
    }

    private static (decimal DeltaX, decimal DeltaY) ResolveWorldDelta(
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        Point startPointer,
        Point currentPointer)
    {
        var startWorld = viewport.Unproject(startPointer);
        var currentWorld = viewport.Unproject(currentPointer);
        return (
            RoundModelValue((decimal)(currentWorld.X - startWorld.X)),
            RoundModelValue((decimal)(currentWorld.Y - startWorld.Y)));
    }

    private static FloorPlanPreviewControl.MovableArtifactMovedEventArgs? ResolveMovement(
        FloorPlanPreviewControl.PreviewArtifactMoveState move,
        decimal deltaX,
        decimal deltaY)
    {
        if (move.PositionMode == FloorPlanArtifactPositionMode.AbsolutePoint)
        {
            var moved = FloorPlanPreviewControl.ApplyAbsolutePointDelta(move.BaseX, move.BaseY, deltaX, deltaY);
            if (!HasMeaningfulDifference(move.BaseX, moved.X) && !HasMeaningfulDifference(move.BaseY, moved.Y))
            {
                return null;
            }

            return new FloorPlanPreviewControl.MovableArtifactMovedEventArgs(
                move.SourceArtifactKind,
                move.SourceArtifactId,
                move.PositionMode,
                moved.X,
                moved.Y,
                null,
                null);
        }

        var translated = FloorPlanPreviewControl.ApplyTranslationDelta(move.BaseDx, move.BaseDy, deltaX, deltaY);
        if (!HasMeaningfulDifference(move.BaseDx, translated.Dx) && !HasMeaningfulDifference(move.BaseDy, translated.Dy))
        {
            return null;
        }

        return new FloorPlanPreviewControl.MovableArtifactMovedEventArgs(
            move.SourceArtifactKind,
            move.SourceArtifactId,
            move.PositionMode,
            null,
            null,
            translated.Dx,
            translated.Dy);
    }

    private static bool HasMeaningfulDifference(decimal original, decimal updated)
    {
        return decimal.Abs(updated - original) >= FloorPlanPreviewControl.MovementPersistenceEpsilon;
    }

    private static decimal RoundModelValue(decimal value)
    {
        return decimal.Round(value, 3, MidpointRounding.AwayFromZero);
    }
}
