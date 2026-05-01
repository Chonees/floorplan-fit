using Avalonia.Controls;
using FloorplanFit.Desktop.ViewModels;

namespace FloorplanFit.Desktop;

public partial class ReviewFloorPlanWindow : Window
{
    public ReviewFloorPlanWindow()
    {
        InitializeComponent();
    }

    private async void AcceptButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        await viewModel.AcceptSelectedCandidateAsync(CancellationToken.None);
    }

    private async void RejectButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        await viewModel.RejectSelectedCandidateAsync(CancellationToken.None);
    }

    private async void SaveMetadataButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        await viewModel.SaveSelectedWallMetadataAsync(CancellationToken.None);
    }

    private async void PublishButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not FloorPlanReviewViewModel viewModel)
        {
            return;
        }

        await viewModel.PublishAsync(CancellationToken.None);
    }
}
