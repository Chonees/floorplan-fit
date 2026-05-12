using Avalonia;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class PreviewInteractionCoordinator
{
    private const double UserZoomStep = 1.12d;

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
        FloorPlanPreviewControl.PreviewDimensionEditState? StartedDimensionEdit);

    public static double CalculateWheelZoomFactor(double currentZoomFactor, double wheelDeltaY)
    {
        var safeCurrentZoom = double.IsFinite(currentZoomFactor)
            ? currentZoomFactor
            : 1d;
        var requestedZoom = safeCurrentZoom * Math.Pow(UserZoomStep, wheelDeltaY);
        return Math.Clamp(
            requestedZoom,
            FloorPlanPreviewControl.MinimumUserZoomFactor,
            FloorPlanPreviewControl.MaximumUserZoomFactor);
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
                StartedDimensionEdit: FloorPlanPreviewControl.PreviewDimensionEditState.Start(
                    dimensionHandleHit.Dimension,
                    dimensionHandleHit.HandleKind,
                    request.PointerPosition));
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
                StartedDimensionEdit: FloorPlanPreviewControl.PreviewDimensionEditState.Start(
                    dimensionHit.Dimension,
                    dimensionHit.SuggestedHandle,
                    request.PointerPosition));
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
            StartedDimensionEdit: null);
    }
}
