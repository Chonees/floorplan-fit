using Avalonia.Controls;
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
}
