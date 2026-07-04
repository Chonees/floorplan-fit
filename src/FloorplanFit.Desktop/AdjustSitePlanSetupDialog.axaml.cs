using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;

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
        if (!TryParsePositiveFeet(BuildableWidthFeetTextBox.Text, out var widthFeet) ||
            !TryParsePositiveFeet(BuildableHeightFeetTextBox.Text, out var heightFeet))
        {
            ValidationText.IsVisible = true;
            return;
        }

        Close(AdjustSitePlanSetupResult.Simulate(widthFeet, heightFeet));
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private static bool TryParsePositiveFeet(string? text, out decimal feet)
    {
        var value = text?.Trim();
        var parsed =
            decimal.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out feet) ||
            decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out feet);

        return parsed && feet > 0m;
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
}
