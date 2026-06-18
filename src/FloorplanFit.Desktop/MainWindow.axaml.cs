using Avalonia.Controls;
using Avalonia.Platform.Storage;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.ViewModels;

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
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select floor plan DXF",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("DXF files")
                {
                    Patterns = ["*.dxf"]
                }
            ]
        });

        var file = files.FirstOrDefault();

        if (file is null)
        {
            return;
        }

        if (DataContext is not LibraryViewModel viewModel)
        {
            return;
        }

        await viewModel.ImportAsync(file.Path.LocalPath, CancellationToken.None);
    }

    private async void MainWindow_OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is not LibraryViewModel viewModel)
        {
            return;
        }

        await viewModel.LoadAsync(CancellationToken.None);
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

        var file = files.FirstOrDefault();
        if (file is null)
        {
            return;
        }

        await viewModel.ShowVersionSitePlanAdjustmentAsync(
            item,
            version,
            file.Path.LocalPath,
            CancellationToken.None);
    }

    private async void BackToLibraryButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not LibraryViewModel viewModel)
        {
            return;
        }

        await viewModel.ShowLibraryAsync(CancellationToken.None);
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
