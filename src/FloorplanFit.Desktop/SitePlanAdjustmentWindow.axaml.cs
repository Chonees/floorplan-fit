using Avalonia.Controls;
using Avalonia.Interactivity;
using FloorplanFit.Desktop.Controls;
using FloorplanFit.Desktop.ViewModels;

namespace FloorplanFit.Desktop;

public partial class SitePlanAdjustmentWindow : Window
{
    public SitePlanAdjustmentWindow()
    {
        InitializeComponent();
        PreviewControl.FloorPlanMoveDeltaRequested += PreviewControl_OnFloorPlanMoveDeltaRequested;
    }

    private void PreviewControl_OnFloorPlanMoveDeltaRequested(
        object? sender,
        FloorPlanPreviewControl.FloorPlanMoveDeltaEventArgs e)
    {
        if (DataContext is SitePlanAdjustmentViewModel viewModel)
        {
            viewModel.MoveFloorPlanBy(e.DeltaX, e.DeltaY);
        }
    }

    private void ApplyAutoFitOptionButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SitePlanAdjustmentViewModel viewModel ||
            sender is not Button { DataContext: AutoFitSuggestionOptionViewModel option })
        {
            return;
        }

        viewModel.ApplyAutoFitPlan(option);
    }
}
