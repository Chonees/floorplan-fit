using Avalonia.Media;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class PreviewSemanticPalette
{
    public static readonly Color SelectionHighlight = Colors.SeaGreen;
    public const string SelectionHighlightArgb = "#FF2E8B57";
    public const string ReadablePreviewLabelColorArgb = "#FF000000";

    public static readonly Color Wall = Colors.SlateGray;
    public static readonly Color WallHighlight = SelectionHighlight;

    public static readonly Color Window = Color.FromRgb(0, 188, 212);
    public static readonly Color WindowHighlight = SelectionHighlight;

    public static readonly Color Door = Color.FromRgb(69, 86, 104);
    public static readonly Color DoorHighlight = SelectionHighlight;

    public static readonly Color FixedElement = Color.FromRgb(220, 38, 38);
    public static readonly Color FixedElementHighlight = SelectionHighlight;
    public static readonly Color Cabinet = Color.FromRgb(0, 188, 212);

    public static readonly Color ProtectedDetailHighlight = SelectionHighlight;

    public static readonly Color ActivePinchGroup = SelectionHighlight;
    public static readonly Color ActivePinchAxis = Colors.DodgerBlue;
    public static readonly Color InactivePinch = Colors.SlateGray;

    public static SolidColorBrush Brush(Color color) => new(color);
}
