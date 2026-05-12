using Avalonia;
using FloorplanFit.Desktop.Controls;
using FloorplanFit.Desktop.Controls.Preview;
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
}
