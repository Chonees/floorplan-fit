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

    private async void ExtractButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not LibraryViewModel viewModel)
        {
            return;
        }

        await viewModel.ExtractSelectedAsync(CancellationToken.None);
    }

    private async void ReviewButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not LibraryViewModel viewModel)
        {
            return;
        }

        var reviewViewModel = await viewModel.OpenSelectedReviewAsync(CancellationToken.None);
        if (reviewViewModel is null)
        {
            return;
        }

        var reviewWindow = new ReviewFloorPlanWindow
        {
            DataContext = reviewViewModel
        };

        await reviewWindow.ShowDialog(this);
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

        var reviewViewModel = await viewModel.OpenVersionReviewAsync(item, version, CancellationToken.None);
        if (reviewViewModel is null)
        {
            return;
        }

        var reviewWindow = new ReviewFloorPlanWindow
        {
            DataContext = reviewViewModel
        };

        await reviewWindow.ShowDialog(this);
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

    private async void DeleteSelectedVersionButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not LibraryViewModel viewModel)
        {
            return;
        }

        await viewModel.DeleteSelectedVersionAsync(CancellationToken.None);
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
