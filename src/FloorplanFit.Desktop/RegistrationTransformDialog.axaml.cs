using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FloorplanFit.Contracts.PlanSets;

namespace FloorplanFit.Desktop;

public partial class RegistrationTransformDialog : Window
{
    public RegistrationTransformDialog()
    {
        InitializeComponent();
    }

    public RegistrationTransformDialog(PlanSetSheetDto sheet)
        : this()
    {
        Title = $"Register {sheet.SheetType}";
        TitleText.Text = $"Register {sheet.SheetType}";
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        ScaleTextBox.Focus();
        ScaleTextBox.SelectAll();
    }

    private void RegisterButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!TryParseDecimal(ScaleTextBox.Text, out var scale) ||
            !TryParseDecimal(RotationDegreesTextBox.Text, out var rotationDegrees) ||
            !TryParseDecimal(TranslateXTextBox.Text, out var translateX) ||
            !TryParseDecimal(TranslateYTextBox.Text, out var translateY) ||
            !TryParseDecimal(ConfidenceTextBox.Text, out var confidence) ||
            !TryParseDecimal(OverhangInchesTextBox.Text, out var overhangInches) ||
            scale <= 0m ||
            confidence < 0m ||
            confidence > 1m ||
            overhangInches < 0m)
        {
            ValidationText.IsVisible = true;
            return;
        }

        var horizontalReferenceName = HorizontalReferenceTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(horizontalReferenceName))
        {
            horizontalReferenceName = null;
        }

        Close(new RegistrationTransformDialogResult(
            new SheetRegistrationTransformDto(scale, rotationDegrees, translateX, translateY),
            confidence,
            overhangInches,
            horizontalReferenceName));
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private static bool TryParseDecimal(string? text, out decimal value)
    {
        var trimmed = text?.Trim();
        return decimal.TryParse(trimmed, NumberStyles.Float, CultureInfo.CurrentCulture, out value) ||
               decimal.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}

public sealed record RegistrationTransformDialogResult(
    SheetRegistrationTransformDto Transform,
    decimal Confidence,
    decimal OverhangInches,
    string? HorizontalReferenceName);
