using Avalonia.Media;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class PreviewSemanticPalette
{
    public const string TransparentArgb = "#00000000";
    public static readonly Color SelectionHighlight = Colors.SeaGreen;
    public const string SelectionHighlightArgb = "#FF2E8B57";
    public const string ReadablePreviewLabelColorArgb = "#FF000000";
    public static readonly Color WorkspaceBackground = Color.Parse("#FF0F172A");
    public static readonly Color WorkspaceDot = Color.FromArgb(92, 190, 190, 190);
    public static readonly Color MinorGrid = Color.Parse("#FF1E293B");
    public static readonly Color MajorGrid = Color.Parse("#FF334155");
    public static readonly Color HandleFill = Color.FromArgb(32, 0, 0, 0);
    public static readonly Color HandleStroke = Color.FromArgb(220, 0, 0, 0);

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
