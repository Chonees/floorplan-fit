using System.Globalization;
using FloorplanFit.Application.Abstractions;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;

namespace FloorplanFit.Infrastructure.Dxf;

public sealed class IxMiliaRoomLabelExtractor : IRoomLabelExtractor
{
    private readonly DxfExtractionProfile profile;

    public IxMiliaRoomLabelExtractor()
        : this(DxfExtractionProfile.PointeHomes)
    {
    }

    public IxMiliaRoomLabelExtractor(DxfExtractionProfile profile)
    {
        this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    public Task<IReadOnlyList<DetectedRoomLabel>> ExtractAsync(string managedFilePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(managedFilePath))
        {
            throw new ArgumentException("A DXF file path is required.", nameof(managedFilePath));
        }

        var dxf = DxfFile.Load(managedFilePath);
        var layerColors = dxf.Layers.ToDictionary(
            layer => layer.Name,
            layer => ToColorArgb(layer.Color),
            StringComparer.OrdinalIgnoreCase);
        var labels = new List<DetectedRoomLabel>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var textIndex = 0;
        var mTextIndex = 0;

        foreach (var text in dxf.Entities.OfType<DxfText>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            textIndex++;

            AddLabel(
                labels,
                seen,
                BuildSourceEntityRef("TEXT", textIndex),
                text.Layer ?? string.Empty,
                text.Value,
                text.Location.X,
                text.Location.Y,
                "TEXT",
                text.TextHeight,
                text.Rotation,
                text.TextStyleName,
                text.HorizontalTextJustification.ToString(),
                text.VerticalTextJustification.ToString(),
                attachmentPoint: null,
                ResolveColorArgb(text.Color, text.Layer, layerColors));
        }

        foreach (var text in dxf.Entities.OfType<DxfMText>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            mTextIndex++;

            AddLabel(
                labels,
                seen,
                BuildSourceEntityRef("MTEXT", mTextIndex),
                text.Layer ?? string.Empty,
                text.Text,
                text.InsertionPoint.X,
                text.InsertionPoint.Y,
                "MTEXT",
                text.InitialTextHeight,
                text.RotationAngle,
                text.TextStyleName,
                horizontalAlignment: null,
                verticalAlignment: null,
                text.AttachmentPoint.ToString(),
                ResolveColorArgb(text.Color, text.Layer, layerColors));
        }

        return Task.FromResult<IReadOnlyList<DetectedRoomLabel>>(labels);
    }

    private void AddLabel(
        List<DetectedRoomLabel> labels,
        HashSet<string> seen,
        string sourceEntityRef,
        string sourceLayer,
        string? rawText,
        double x,
        double y,
        string sourceEntityKind,
        double textHeight,
        double rotationDegrees,
        string? textStyleName,
        string? horizontalAlignment,
        string? verticalAlignment,
        string? attachmentPoint,
        string? colorArgb)
    {
        if (!profile.IsRoomLabelLayer(sourceLayer))
        {
            return;
        }

        var normalizedText = NormalizeText(rawText);
        if (!profile.LooksLikeRoomName(normalizedText))
        {
            return;
        }

        var normalizedX = Math.Round(x, 1, MidpointRounding.AwayFromZero);
        var normalizedY = Math.Round(y, 1, MidpointRounding.AwayFromZero);
        var dedupeKey = string.Create(
            CultureInfo.InvariantCulture,
            $"{normalizedText}|{normalizedX:0.0}|{normalizedY:0.0}");

        if (!seen.Add(dedupeKey))
        {
            return;
        }

        labels.Add(new DetectedRoomLabel(
            sourceEntityRef,
            sourceLayer,
            normalizedText,
            (decimal)x,
            (decimal)y,
            0.95m,
            $"Detected from {sourceLayer} text entity.",
            sourceEntityKind,
            textHeight > 0d ? (decimal)textHeight : null,
            (decimal)rotationDegrees,
            textStyleName,
            horizontalAlignment,
            verticalAlignment,
            attachmentPoint,
            colorArgb));
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Join(
                " ",
                value
                    .Replace("\\P", " ", StringComparison.OrdinalIgnoreCase)
                    .Replace("{", " ", StringComparison.Ordinal)
                    .Replace("}", " ", StringComparison.Ordinal)
                    .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .Trim();
    }

    private static string BuildSourceEntityRef(string entityType, int entityIndex)
    {
        return $"{entityType}:{entityIndex.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string? ResolveColorArgb(DxfColor entityColor, string? layerName, IReadOnlyDictionary<string, string?> layerColors)
    {
        if (entityColor.IsByLayer)
        {
            return layerName is not null && layerColors.TryGetValue(layerName, out var layerColor)
                ? layerColor
                : null;
        }

        return ToColorArgb(entityColor);
    }

    private static string? ToColorArgb(DxfColor color)
    {
        if (color.IsByLayer || color.IsByBlock || color.IsTurnedOff)
        {
            return null;
        }

        var rgb = color.ToRGB();
        var red = (rgb >> 16) & 0xFF;
        var green = (rgb >> 8) & 0xFF;
        var blue = rgb & 0xFF;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"#FF{red:X2}{green:X2}{blue:X2}");
    }
}
