using System.Globalization;
using System.Text.RegularExpressions;
using FloorplanFit.Application.Abstractions;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;

namespace FloorplanFit.Infrastructure.Dxf;

public sealed partial class IxMiliaOpeningExtractor : IOpeningExtractor
{
    private const double ArcSegmentDegrees = 10d;
    private readonly DxfExtractionProfile profile;

    public IxMiliaOpeningExtractor()
        : this(DxfExtractionProfile.PointeHomes)
    {
    }

    public IxMiliaOpeningExtractor(DxfExtractionProfile profile)
    {
        this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    public Task<DetectedOpeningExtraction> ExtractAsync(string managedFilePath, CancellationToken cancellationToken)
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

        return Task.FromResult(new DetectedOpeningExtraction(
            ExtractCandidates(dxf, cancellationToken),
            ExtractLabels(dxf, layerColors, cancellationToken)));
    }

    private IReadOnlyList<DetectedOpeningCandidate> ExtractCandidates(DxfFile dxf, CancellationToken cancellationToken)
    {
        var candidates = new List<DetectedOpeningCandidate>();
        var lineIndex = 0;
        var arcIndex = 0;
        var polylineIndex = 0;

        foreach (var line in dxf.Entities.OfType<DxfLine>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            lineIndex++;

            var kind = profile.ResolveOpeningGeometryKind(line.Layer);
            if (kind is null)
            {
                continue;
            }

            candidates.Add(new DetectedOpeningCandidate(
                BuildSourceEntityRef("LINE", lineIndex),
                line.Layer ?? string.Empty,
                kind,
                "LINE",
                [ToPoint(line.P1.X, line.P1.Y), ToPoint(line.P2.X, line.P2.Y)],
                0.95m,
                $"Detected from {line.Layer} line entity."));
        }

        foreach (var arc in dxf.Entities.OfType<DxfArc>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            arcIndex++;

            var kind = profile.ResolveOpeningGeometryKind(arc.Layer);
            if (kind is null)
            {
                continue;
            }

            var points = FlattenArc(arc);
            if (points.Count < 2)
            {
                continue;
            }

            candidates.Add(new DetectedOpeningCandidate(
                BuildSourceEntityRef("ARC", arcIndex),
                arc.Layer ?? string.Empty,
                kind,
                "ARC",
                points,
                0.95m,
                $"Detected from {arc.Layer} arc entity."));
        }

        foreach (var polyline in dxf.Entities.OfType<DxfLwPolyline>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            polylineIndex++;

            var kind = profile.ResolveOpeningGeometryKind(polyline.Layer);
            if (kind is null)
            {
                continue;
            }

            var points = polyline.Vertices
                .Select(vertex => ToPoint(vertex.X, vertex.Y))
                .ToList();
            if (polyline.IsClosed && points.Count > 0)
            {
                points.Add(points[0]);
            }

            if (points.Count < 2)
            {
                continue;
            }

            candidates.Add(new DetectedOpeningCandidate(
                BuildSourceEntityRef("LWPOLYLINE", polylineIndex),
                polyline.Layer ?? string.Empty,
                kind,
                "LWPOLYLINE",
                points,
                0.95m,
                $"Detected from {polyline.Layer} lightweight polyline entity."));
        }

        return candidates;
    }

    private IReadOnlyList<DetectedOpeningLabel> ExtractLabels(
        DxfFile dxf,
        IReadOnlyDictionary<string, string?> layerColors,
        CancellationToken cancellationToken)
    {
        var labels = new List<DetectedOpeningLabel>();
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

        return labels;
    }

    private void AddLabel(
        List<DetectedOpeningLabel> labels,
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
        var kind = profile.ResolveOpeningLabelKind(sourceLayer);
        if (kind is null)
        {
            return;
        }

        var normalizedText = NormalizeLabelText(rawText);
        if (string.IsNullOrWhiteSpace(normalizedText))
        {
            return;
        }

        if (!profile.LooksLikeOpeningModelOrSizeLabel(normalizedText))
        {
            return;
        }

        var normalizedX = Math.Round(x, 1, MidpointRounding.AwayFromZero);
        var normalizedY = Math.Round(y, 1, MidpointRounding.AwayFromZero);
        var dedupeKey = string.Create(
            CultureInfo.InvariantCulture,
            $"{kind}|{normalizedText}|{normalizedX:0.0}|{normalizedY:0.0}");

        if (!seen.Add(dedupeKey))
        {
            return;
        }

        labels.Add(new DetectedOpeningLabel(
            sourceEntityRef,
            sourceLayer,
            kind,
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

    private static IReadOnlyList<GeometryPoint> FlattenArc(DxfArc arc)
    {
        var startAngle = arc.StartAngle;
        var endAngle = arc.EndAngle;
        if (endAngle < startAngle)
        {
            endAngle += 360d;
        }

        var sweep = Math.Max(endAngle - startAngle, 0d);
        var segmentCount = Math.Max(2, (int)Math.Ceiling(sweep / ArcSegmentDegrees));
        var points = new List<GeometryPoint>(segmentCount + 1);

        for (var index = 0; index <= segmentCount; index++)
        {
            var angle = startAngle + (sweep * index / segmentCount);
            var radians = angle * Math.PI / 180d;
            var x = arc.Center.X + (Math.Cos(radians) * arc.Radius);
            var y = arc.Center.Y + (Math.Sin(radians) * arc.Radius);
            points.Add(ToPoint(x, y));
        }

        return points;
    }

    private static GeometryPoint ToPoint(double x, double y)
    {
        return new GeometryPoint((decimal)x, (decimal)y);
    }

    private static string NormalizeLabelText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var text = value
            .Replace("\\P", " ", StringComparison.OrdinalIgnoreCase)
            .Replace("{", " ", StringComparison.Ordinal)
            .Replace("}", " ", StringComparison.Ordinal);

        text = DxfFormattingCommandRegex().Replace(text, string.Empty);

        return string.Join(
                " ",
                text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
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

    [GeneratedRegex(@"\\p[a-z0-9.,;+-]*;", RegexOptions.IgnoreCase)]
    private static partial Regex DxfFormattingCommandRegex();

}
