using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Platform.Storage;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Desktop.ViewModels;
using FloorplanFit.Infrastructure.Dxf;

namespace FloorplanFit.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += MainWindow_OnOpened;
    }

    private async void ImportButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var filePath = await OpenDxfPickerAsync("Select floor plan DXF");

        if (filePath is null)
        {
            return;
        }

        if (DataContext is not LibraryViewModel viewModel)
        {
            return;
        }

        await viewModel.ImportAsync(filePath, CancellationToken.None);
    }

    private async void ImportElectricalSheetButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ImportDependentSheetAsync("Select electrical plan DXF", "ElectricalPlan");
    }

    private async void ImportRoofSheetButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ImportDependentSheetAsync("Select roof plan DXF", "RoofPlan");
    }

    private async void ImportFacadeSheetButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ImportDependentSheetAsync("Select facade/elevation plan DXF", "FacadeElevation");
    }

    private async Task ImportDependentSheetAsync(string pickerTitle, string sheetType)
    {
        var filePath = await OpenDxfPickerAsync(pickerTitle);

        if (filePath is null)
        {
            return;
        }

        if (DataContext is not LibraryViewModel viewModel)
        {
            return;
        }

        try
        {
            await viewModel.ImportDependentSheetAsync(
                filePath,
                sheetType,
                name: null,
                cancellationToken: CancellationToken.None);
        }
        catch (ArgumentException exception)
        {
            viewModel.StatusMessage = $"Dependent sheet import needs review: {exception.Message}";
        }
    }

    private async Task<string?> OpenDxfPickerAsync(string title)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("DXF files")
                {
                    Patterns = ["*.dxf"]
                }
            ]
        });

        return files.FirstOrDefault()?.Path.LocalPath;
    }

    private async void MainWindow_OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is not LibraryViewModel viewModel)
        {
            return;
        }

        await viewModel.LoadAsync(CancellationToken.None);
    }

    private void SelectVersionButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: FloorPlanLibraryVersionDto version })
        {
            return;
        }

        if (DataContext is not LibraryViewModel viewModel)
        {
            return;
        }

        var item = viewModel.Items.FirstOrDefault(libraryItem =>
            libraryItem.Versions.Any(candidate => candidate.VersionId == version.VersionId));
        if (item is null)
        {
            return;
        }

        viewModel.SelectVersion(item, version);
    }

    private async void UnlinkDependentSheetButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PlanSetSheetDto sheet })
        {
            return;
        }

        if (DataContext is not LibraryViewModel viewModel)
        {
            return;
        }

        await viewModel.UnlinkDependentSheetAsync(sheet, CancellationToken.None);
    }

    private async void RegisterDependentSheetButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PlanSetSheetDto sheet })
        {
            return;
        }

        if (DataContext is not LibraryViewModel viewModel)
        {
            return;
        }

        if (sheet.SheetType == "ElectricalPlan")
        {
            // ponytail: These placeholders never cross the identifier-only Electrical request boundary.
            await viewModel.RegisterDependentSheetAsync(
                sheet,
                transform: new SheetRegistrationTransformDto(1m, 0m, 0m, 0m),
                confidence: 0m,
                confirmRegistration: false,
                overhangInches: 0m,
                horizontalReferenceName: null,
                cancellationToken: CancellationToken.None);
            return;
        }

        var dialog = new RegistrationTransformDialog(sheet);
        var registrationResult = await dialog.ShowDialog<RegistrationTransformDialogResult?>(this);
        if (registrationResult is null)
        {
            return;
        }

        await viewModel.RegisterDependentSheetAsync(
            sheet,
            transform: registrationResult.Transform,
            confidence: registrationResult.Confidence,
            confirmRegistration: false,
            overhangInches: registrationResult.OverhangInches,
            horizontalReferenceName: registrationResult.HorizontalReferenceName,
            cancellationToken: CancellationToken.None);
    }

    private async void ReviewSheetRegistrationButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PlanSetSheetDto sheet })
        {
            return;
        }

        if (sheet.SheetType != "ElectricalPlan")
        {
            return;
        }

        if (DataContext is not LibraryViewModel viewModel)
        {
            return;
        }

        var review = await viewModel.LoadDependentSheetRegistrationReviewAsync(
            sheet,
            CancellationToken.None);
        var confirmed = await new RegistrationReviewDialog(review).ShowDialog<bool>(this);
        if (!confirmed)
        {
            return;
        }

        if (!review.CanConfirm)
        {
            viewModel.StatusMessage = review.ErrorMessage ?? "Registration review could not be confirmed.";
            return;
        }

        try
        {
            await viewModel.ConfirmDependentSheetRegistrationAsync(sheet, CancellationToken.None);
        }
        catch (Exception exception)
        {
            viewModel.StatusMessage = $"Could not confirm {sheet.SheetType} registration: {exception.Message}";
        }
    }

    private async void ConfirmSheetRegistrationButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PlanSetSheetDto sheet } ||
            sheet.SheetType == "ElectricalPlan")
        {
            return;
        }

        if (DataContext is not LibraryViewModel viewModel)
        {
            return;
        }

        try
        {
            await viewModel.ConfirmDependentSheetRegistrationAsync(sheet, CancellationToken.None);
        }
        catch (Exception exception)
        {
            viewModel.StatusMessage = $"Could not confirm {sheet.SheetType} registration: {exception.Message}";
        }
    }

    private async void RejectSheetRegistrationButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PlanSetSheetDto sheet })
        {
            return;
        }

        if (DataContext is not LibraryViewModel viewModel)
        {
            return;
        }

        await viewModel.RejectDependentSheetRegistrationAsync(sheet, CancellationToken.None);
    }

    private async void ConfirmSheetProjectionButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PlanSetSheetDto sheet })
        {
            return;
        }

        if (DataContext is not LibraryViewModel viewModel)
        {
            return;
        }

        await viewModel.ConfirmDependentSheetProjectionAsync(sheet, CancellationToken.None);
    }

    private async void CorrectSheetToElectricalButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => await CorrectSheetTypeAsync(sender, "ElectricalPlan");

    private async void CorrectSheetToRoofButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => await CorrectSheetTypeAsync(sender, "RoofPlan");

    private async void CorrectSheetToFacadeButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => await CorrectSheetTypeAsync(sender, "FacadeElevation");

    private async Task CorrectSheetTypeAsync(object? sender, string sheetType)
    {
        if (sender is not Button { DataContext: PlanSetSheetDto sheet })
        {
            return;
        }

        if (DataContext is not LibraryViewModel viewModel)
        {
            return;
        }

        await viewModel.CorrectDependentSheetTypeAsync(sheet, sheetType, CancellationToken.None);
    }

    private async void OpenVersionButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: FloorPlanLibraryVersionDto version })
        {
            return;
        }

        if (DataContext is not LibraryViewModel viewModel)
        {
            return;
        }

        var item = viewModel.Items.FirstOrDefault(libraryItem =>
            libraryItem.Versions.Any(candidate => candidate.VersionId == version.VersionId));
        if (item is null)
        {
            return;
        }

        await viewModel.ShowVersionReviewAsync(item, version, CancellationToken.None);
    }

    private async void AdjustVersionToSitePlanButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: FloorPlanLibraryVersionDto version })
        {
            return;
        }

        if (DataContext is not LibraryViewModel viewModel)
        {
            return;
        }

        var item = viewModel.Items.FirstOrDefault(libraryItem =>
            libraryItem.Versions.Any(candidate => candidate.VersionId == version.VersionId));
        if (item is null)
        {
            return;
        }

        var setup = await new AdjustSitePlanSetupDialog().ShowDialog<AdjustSitePlanSetupResult?>(this);
        if (setup is null)
        {
            return;
        }

        var sitePlanFilePath = setup.Mode == AdjustSitePlanSetupMode.Import
            ? await OpenSitePlanPickerAsync()
            : SyntheticSitePlanDxfWriter.WriteToTempFile(setup.BuildableWidthFeet, setup.BuildableHeightFeet);
        if (sitePlanFilePath is null)
        {
            return;
        }

        await viewModel.ShowVersionSitePlanAdjustmentAsync(
            item,
            version,
            sitePlanFilePath,
            CancellationToken.None);
    }

    private async Task<string?> OpenSitePlanPickerAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select site plan DXF",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("DXF files")
                {
                    Patterns = ["*.dxf"]
                }
            ]
        });

        return files.FirstOrDefault()?.Path.LocalPath;
    }

    private async void BackToLibraryButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not LibraryViewModel viewModel)
        {
            return;
        }

        await viewModel.ShowLibraryAsync(CancellationToken.None);
    }

    private async void EditPublishedButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not LibraryViewModel { ActiveReviewViewModel: { } reviewViewModel })
        {
            return;
        }

        await reviewViewModel.StartEditingPublishedCurationAsync(CancellationToken.None);
    }

    private async void PublishButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not LibraryViewModel { ActiveReviewViewModel: { } reviewViewModel })
        {
            return;
        }

        await reviewViewModel.PublishAsync(CancellationToken.None);
    }

    private async void DeleteVersionButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: FloorPlanLibraryVersionDto version })
        {
            return;
        }

        if (DataContext is not LibraryViewModel viewModel)
        {
            return;
        }

        await viewModel.DeleteVersionAsync(version, CancellationToken.None);
    }
}

public sealed class PendingRegistrationActionVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not PlanSetSheetDto { CanConfirmRegistration: true } sheet)
        {
            return false;
        }

        return (parameter as string) switch
        {
            "Electrical" => sheet.SheetType == "ElectricalPlan",
            "NonElectrical" => sheet.SheetType != "ElectricalPlan",
            _ => false
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
