using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FloorplanFit.Desktop.Controls;
using FloorplanFit.Desktop.ViewModels;

namespace FloorplanFit.Desktop;

public partial class ReviewFloorPlanWindow : UserControl
{
    public ReviewFloorPlanWindow()
    {
        InitializeComponent();
    }

    private async void ReviewFloorPlanWindow_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete || e.Source is TextBox)
        {
            return;
        }

        if (DataContext is not FloorPlanReviewViewModel viewModel || !viewModel.CanDeleteSelectedItem)
        {
            return;
        }

        e.Handled = true;
        await viewModel.DeleteSelectedItemAsync(CancellationToken.None);
    }

    private async void DeleteSelectedItemButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        await viewModel.DeleteSelectedItemAsync(CancellationToken.None);
    }

    private async void ExcludeSelectedArtifactButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        await viewModel.ExcludeSelectedArtifactAsync(CancellationToken.None);
    }

    private void AddPinchButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        viewModel.TogglePinchPlacement();
    }

    private void AddManualWallLineButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        viewModel.ToggleManualWallLinePlacement();
    }

    private async void AddPinchGroupButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        var groupName = await PromptForPinchGroupNameAsync(
            "Crear grupo de pinches",
            "Nombrá la zona que se puede ajustar. Ej: Patio, Porche, Garage, Lateral.",
            viewModel.SuggestedPinchGroupName);
        if (groupName is null)
        {
            return;
        }

        await viewModel.AddPinchGroupAsync(groupName, CancellationToken.None);
    }

    private async void RenamePinchGroupButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel ||
            viewModel.SelectedPinchGroup is null)
        {
            return;
        }

        var groupName = await PromptForPinchGroupNameAsync(
            "Renombrar grupo de pinches",
            "Cambiá solo el nombre humano. Los pinches, eje y capacidad quedan intactos.",
            viewModel.SelectedPinchGroup.Name);
        if (groupName is null)
        {
            return;
        }

        await viewModel.RenameSelectedPinchGroupAsync(groupName, CancellationToken.None);
    }

    private async void RemovePinchGroupButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        await viewModel.RemoveSelectedPinchGroupAsync(CancellationToken.None);
    }

    private async Task<string?> PromptForPinchGroupNameAsync(string title, string description, string initialName)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
        {
            return null;
        }

        var dialog = new PinchGroupNameDialog(title, description, initialName);
        return await dialog.ShowDialog<string?>(owner);
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

    private async void PreviewControl_OnManualWallLinePointClicked(object? sender, FloorPlanPreviewControl.ManualWallLinePointClickedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        await viewModel.HandleManualWallLinePointAsync(e.X, e.Y, CancellationToken.None);
    }

    private void PreviewControl_OnManualWallLinePreviewPointChanged(object? sender, FloorPlanPreviewControl.ManualWallLinePreviewPointChangedEventArgs e)
    {
        if (DataContext is FloorPlanReviewViewModel viewModel)
        {
            viewModel.UpdateManualWallLinePreviewPoint(e.X, e.Y);
        }
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
