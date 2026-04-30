using Avalonia.Controls;
using Avalonia.Platform.Storage;
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
}
