using Avalonia;

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
}
