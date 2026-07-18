using Avalonia.Controls;
using Avalonia.Interactivity;
using FloorplanFit.Desktop.Presentation;

namespace FloorplanFit.Desktop;

public partial class AdjustSitePlanSetupDialog : Window
{
    public AdjustSitePlanSetupDialog()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        BuildableWidthFeetTextBox.Focus();
    }

    private void ImportButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(AdjustSitePlanSetupResult.Import());
    }

    private void SimulateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!AdjustSitePlanSetupResult.TryCreateSimulation(
                BuildableWidthFeetTextBox.Text,
                BuildableHeightFeetTextBox.Text,
                out var result))
        {
            ValidationText.IsVisible = true;
            return;
        }

        Close(result);
    }

    private void AdjustBuildableLength(TextBox textBox, decimal deltaInches)
    {
        if (!ArchitecturalLengthText.TryAdjustInches(
                textBox.Text,
                ArchitecturalLengthDefaultUnit.Feet,
                deltaInches,
                out var formatted))
        {
            ValidationText.IsVisible = true;
            return;
        }

        textBox.Text = formatted;
        ValidationText.IsVisible = false;
    }

    private void DecreaseBuildableWidthButton_OnClick(object? sender, RoutedEventArgs e)
        => AdjustBuildableLength(BuildableWidthFeetTextBox, -0.5m);

    private void IncreaseBuildableWidthButton_OnClick(object? sender, RoutedEventArgs e)
        => AdjustBuildableLength(BuildableWidthFeetTextBox, 0.5m);

    private void DecreaseBuildableHeightButton_OnClick(object? sender, RoutedEventArgs e)
        => AdjustBuildableLength(BuildableHeightFeetTextBox, -0.5m);

    private void IncreaseBuildableHeightButton_OnClick(object? sender, RoutedEventArgs e)
        => AdjustBuildableLength(BuildableHeightFeetTextBox, 0.5m);

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}

public enum AdjustSitePlanSetupMode
{
    Import,
    Simulate
}

public sealed record AdjustSitePlanSetupResult(
    AdjustSitePlanSetupMode Mode,
    decimal BuildableWidthFeet,
    decimal BuildableHeightFeet)
{
    public static AdjustSitePlanSetupResult Import()
        => new(AdjustSitePlanSetupMode.Import, 0m, 0m);

    public static AdjustSitePlanSetupResult Simulate(decimal buildableWidthFeet, decimal buildableHeightFeet)
        => new(AdjustSitePlanSetupMode.Simulate, buildableWidthFeet, buildableHeightFeet);

    public static bool TryCreateSimulation(string? widthText, string? heightText, out AdjustSitePlanSetupResult result)
    {
        result = Import();
        if (!ArchitecturalLengthText.TryParsePositiveInches(
                widthText,
                ArchitecturalLengthDefaultUnit.Feet,
                out var widthInches) ||
            !ArchitecturalLengthText.TryParsePositiveInches(
                heightText,
                ArchitecturalLengthDefaultUnit.Feet,
                out var heightInches))
        {
            return false;
        }

        result = Simulate(widthInches / 12m, heightInches / 12m);
        return true;
    }
}
