using Avalonia.Controls;
using Avalonia.Interactivity;
using FloorplanFit.Desktop.Controls;
using FloorplanFit.Desktop.ViewModels;

namespace FloorplanFit.Desktop;

public partial class ReviewFloorPlanWindow : Window
{
    public ReviewFloorPlanWindow()
    {
        InitializeComponent();
    }

    private async void ExcludeSelectedArtifactButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        await viewModel.ExcludeSelectedArtifactAsync(CancellationToken.None);
    }

    private async void PublishButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        await viewModel.PublishAsync(CancellationToken.None);
    }

    private async void EditPublishedButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        await viewModel.StartEditingPublishedCurationAsync(CancellationToken.None);
    }

    private void AddPinchButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        viewModel.TogglePinchPlacement();
    }

    private async void AddPinchGroupButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        await viewModel.AddPinchGroupAsync(CancellationToken.None);
    }

    private async void RemovePinchGroupButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        await viewModel.RemoveSelectedPinchGroupAsync(CancellationToken.None);
    }

    private async void AddMeasurementCorridorButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        await viewModel.AddMeasurementCorridorAsync(CancellationToken.None);
    }

    private async void RemoveMeasurementCorridorButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        await viewModel.RemoveSelectedMeasurementCorridorAsync(CancellationToken.None);
    }

    private async void RemoveMeasurementNodeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        await viewModel.RemoveSelectedMeasurementNodeAsync(CancellationToken.None);
    }

    private async void ChangeMeasurementCorridorAxisButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        await viewModel.ChangeSelectedMeasurementCorridorAxisAsync(CancellationToken.None);
    }

    private void AddMeasurementNodeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        viewModel.ToggleMeasurementNodePlacement();
    }

    private async void SaveDimensionIntervalBindingButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        await viewModel.SaveSelectedDimensionIntervalBindingAsync(CancellationToken.None);
    }

    private async void RestoreDimensionIntervalBindingButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        await viewModel.RestoreSelectedDimensionIntervalBindingAsync(CancellationToken.None);
    }

    private async void SaveClassificationButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        await viewModel.SaveSelectedCuratedArtifactClassificationAsync(CancellationToken.None);
    }

    private async void RestoreClassificationButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        await viewModel.RestoreSelectedCuratedArtifactClassificationAsync(CancellationToken.None);
    }

    private async void RemovePinchButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        await viewModel.RemoveSelectedPinchAsync(CancellationToken.None);
    }

    private async void PreviewControl_OnGeometryPathClicked(object? sender, FloorPlanPreviewControl.GeometryPathClickedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        await viewModel.HandlePreviewInteractionAsync(e.GeometryPathId, e.PositionRatio, CancellationToken.None);
    }

    private void PreviewControl_OnRoomLabelClicked(object? sender, FloorPlanPreviewControl.RoomLabelClickedEventArgs e)
    {
        if (DataContext is FloorPlanReviewViewModel viewModel)
        {
            viewModel.SelectRoomLabel(e.RoomLabelId);
        }
    }

    private void PreviewControl_OnOpeningLabelClicked(object? sender, FloorPlanPreviewControl.OpeningLabelClickedEventArgs e)
    {
        if (DataContext is FloorPlanReviewViewModel viewModel)
        {
            viewModel.SelectOpeningLabel(e.OpeningLabelId);
        }
    }

    private async void PreviewControl_OnMovableArtifactMoved(object? sender, FloorPlanPreviewControl.MovableArtifactMovedEventArgs e)
    {
        if (DataContext is FloorPlanReviewViewModel viewModel)
        {
            await viewModel.SaveMovedArtifactPositionAsync(e, CancellationToken.None);
        }
    }

    private void PreviewControl_OnDimensionClicked(object? sender, FloorPlanPreviewControl.DimensionClickedEventArgs e)
    {
        if (DataContext is FloorPlanReviewViewModel viewModel)
        {
            viewModel.SelectDimension(e.DimensionId);
        }
    }

    private async void PreviewControl_OnDimensionEdited(object? sender, FloorPlanPreviewControl.DimensionEditedEventArgs e)
    {
        if (DataContext is FloorPlanReviewViewModel viewModel)
        {
            await viewModel.SaveEditedDimensionAsync(e, CancellationToken.None);
        }
    }

    private async void RestorePositionButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is FloorPlanReviewViewModel viewModel)
        {
            await viewModel.RestoreSelectedArtifactPositionAsync(CancellationToken.None);
        }
    }

    private async void SaveLabelSizeButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is FloorPlanReviewViewModel viewModel)
        {
            await viewModel.SaveSelectedLabelTextHeightAsync(CancellationToken.None);
        }
    }

    private async void RestoreLabelSizeButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is FloorPlanReviewViewModel viewModel)
        {
            await viewModel.RestoreSelectedLabelTextHeightAsync(CancellationToken.None);
        }
    }

    private async void ExportAdjustedDxfButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is FloorPlanReviewViewModel viewModel)
        {
            await viewModel.ExportAdjustedDxfAsync(CancellationToken.None);
        }
    }

    private void InspectorToolButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel ||
            sender is not Button button ||
            button.Tag is not string tool)
        {
            return;
        }

        viewModel.SelectInspectorTool(tool);
    }
}
