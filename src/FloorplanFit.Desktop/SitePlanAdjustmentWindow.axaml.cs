using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FloorplanFit.Desktop.Controls;
using FloorplanFit.Desktop.ViewModels;

namespace FloorplanFit.Desktop;

public partial class SitePlanAdjustmentWindow : UserControl
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

    private async void ExportAdjustedSitePlanButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SitePlanAdjustmentViewModel viewModel)
        {
            return;
        }

        var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storageProvider is null)
        {
            return;
        }

        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Exportar DXF",
            SuggestedFileName = "plano-ajustado-al-sitio.dxf",
            DefaultExtension = "dxf",
            FileTypeChoices =
            [
                new FilePickerFileType("DXF files")
                {
                    Patterns = ["*.dxf"]
                }
            ]
        });

        if (file is null)
        {
            return;
        }

        await viewModel.ExportAdjustedSitePlanAsync(file.Path.LocalPath, CancellationToken.None);
    }
}
