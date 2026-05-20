using System.Globalization;
using FloorplanFit.Application.Abstractions;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;

namespace FloorplanFit.Infrastructure.Dxf;

public sealed class IxMiliaProtectedDetailAssemblyExtractor : IProtectedDetailAssemblyExtractor
{
    private const double ArcSegmentDegrees = 10d;
    private const int CircleSegmentCount = 36;
    private readonly DxfExtractionProfile profile;

    public IxMiliaProtectedDetailAssemblyExtractor()
        : this(DxfExtractionProfile.PointeHomes)
    {
    }

    public IxMiliaProtectedDetailAssemblyExtractor(DxfExtractionProfile profile)
    {
        this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    public Task<IReadOnlyList<DetectedProtectedDetailAssembly>> ExtractAsync(string managedFilePath, CancellationToken cancellationToken)
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
        var groups = new Dictionary<AssemblyGroupKey, AssemblyAccumulator>();

        foreach (var entity in dxf.Entities)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var kind = profile.ResolveProtectedDetailLayerKind(entity.Layer);
            if (kind is null)
            {
                continue;
            }

            var sourceEntityKind = ResolveEntityKind(entity);
            if (sourceEntityKind is null)
            {
                continue;
            }

            var geometryPaths = ExtractEntityGeometryPaths(entity);
            if (geometryPaths.Count == 0)
            {
                continue;
            }

            var sourceLayer = entity.Layer ?? string.Empty;
            var key = new AssemblyGroupKey(sourceLayer, kind);
            if (!groups.TryGetValue(key, out var accumulator))
            {
                accumulator = new AssemblyAccumulator(sourceLayer, kind);
                groups[key] = accumulator;
            }

            accumulator.AddRange(
                geometryPaths,
                ResolveColorArgb(entity.Color, entity.Layer, layerColors),
                sourceEntityKind);
        }

        var layerIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        return Task.FromResult<IReadOnlyList<DetectedProtectedDetailAssembly>>(groups
            .Values
            .Where(item => item.GeometryPaths.Count > 0)
            .OrderBy(item => item.SourceLayer, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Kind, StringComparer.OrdinalIgnoreCase)
            .Select(item =>
            {
                var layerIndex = NextIndex(layerIndexes, item.SourceLayer);
                return new DetectedProtectedDetailAssembly(
                    BuildSourceEntityRef(item.SourceLayer, layerIndex),
                    item.SourceLayer,
                    item.Kind,
                    "DETAIL-GROUP",
                    item.GeometryPaths,
                    0.90m,
                    $"Detected protected detail assembly from {item.SourceLayer} CAD detail geometry.",
                    item.ColorArgb);
            })
            .ToArray());
    }

    private static IReadOnlyList<IReadOnlyList<GeometryPoint>> ExtractEntityGeometryPaths(DxfEntity entity)
    {
        return entity switch
        {
            DxfLine line => [[ToPoint(line.P1.X, line.P1.Y), ToPoint(line.P2.X, line.P2.Y)]],
            DxfArc arc => [FlattenArc(arc)],
            DxfCircle circle => [FlattenCircle(circle)],
            DxfEllipse ellipse => [FlattenEllipse(ellipse)],
            DxfLwPolyline polyline => [ExtractPolylinePoints(polyline)],
            Dxf3DFace face => [ExtractFacePoints(face)],
            DxfSolid solid => [ExtractSolidPoints(solid)],
            _ => []
        };
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
            points.Add(ToPoint(
                arc.Center.X + (Math.Cos(radians) * arc.Radius),
                arc.Center.Y + (Math.Sin(radians) * arc.Radius)));
        }

        return points;
    }

    private static IReadOnlyList<GeometryPoint> FlattenCircle(DxfCircle circle)
    {
        var points = new List<GeometryPoint>(CircleSegmentCount + 1);
        for (var index = 0; index <= CircleSegmentCount; index++)
        {
            var radians = 2d * Math.PI * index / CircleSegmentCount;
            points.Add(ToPoint(
                circle.Center.X + (Math.Cos(radians) * circle.Radius),
                circle.Center.Y + (Math.Sin(radians) * circle.Radius)));
        }

        return points;
    }

    private static IReadOnlyList<GeometryPoint> FlattenEllipse(DxfEllipse ellipse)
    {
        var start = ellipse.StartParameter;
        var end = ellipse.EndParameter;
        if (end <= start)
        {
            end += 2d * Math.PI;
        }

        var sweep = end - start;
        var segmentCount = Math.Max(8, (int)Math.Ceiling(sweep / (Math.PI / 18d)));
        var majorX = ellipse.MajorAxis.X;
        var majorY = ellipse.MajorAxis.Y;
        var minorX = -majorY * ellipse.MinorAxisRatio;
        var minorY = majorX * ellipse.MinorAxisRatio;
        var points = new List<GeometryPoint>(segmentCount + 1);

        for (var index = 0; index <= segmentCount; index++)
        {
            var parameter = start + (sweep * index / segmentCount);
            points.Add(ToPoint(
                ellipse.Center.X + (majorX * Math.Cos(parameter)) + (minorX * Math.Sin(parameter)),
                ellipse.Center.Y + (majorY * Math.Cos(parameter)) + (minorY * Math.Sin(parameter))));
        }

        return points;
    }

    private static IReadOnlyList<GeometryPoint> ExtractPolylinePoints(DxfLwPolyline polyline)
    {
        var points = polyline.Vertices
            .Select(vertex => ToPoint(vertex.X, vertex.Y))
            .ToList();
        if (polyline.IsClosed && points.Count > 0)
        {
            points.Add(points[0]);
        }

        return points;
    }

    private static IReadOnlyList<GeometryPoint> ExtractFacePoints(Dxf3DFace face)
    {
        return [ToPoint(face.FirstCorner.X, face.FirstCorner.Y), ToPoint(face.SecondCorner.X, face.SecondCorner.Y), ToPoint(face.ThirdCorner.X, face.ThirdCorner.Y), ToPoint(face.FourthCorner.X, face.FourthCorner.Y), ToPoint(face.FirstCorner.X, face.FirstCorner.Y)];
    }

    private static IReadOnlyList<GeometryPoint> ExtractSolidPoints(DxfSolid solid)
    {
        return [ToPoint(solid.FirstCorner.X, solid.FirstCorner.Y), ToPoint(solid.SecondCorner.X, solid.SecondCorner.Y), ToPoint(solid.ThirdCorner.X, solid.ThirdCorner.Y), ToPoint(solid.FourthCorner.X, solid.FourthCorner.Y), ToPoint(solid.FirstCorner.X, solid.FirstCorner.Y)];
    }

    private static string? ResolveEntityKind(DxfEntity entity)
    {
        return entity switch
        {
            DxfLine => "LINE",
            DxfArc => "ARC",
            DxfCircle => "CIRCLE",
            DxfEllipse => "ELLIPSE",
            DxfLwPolyline => "LWPOLYLINE",
            Dxf3DFace => "3DFACE",
            DxfSolid => "SOLID",
            _ => null
        };
    }

    private static GeometryPoint ToPoint(double x, double y)
    {
        return new GeometryPoint((decimal)x, (decimal)y);
    }

    private static string BuildSourceEntityRef(string layerName, int layerIndex)
    {
        var safeLayer = string.IsNullOrWhiteSpace(layerName)
            ? "UNKNOWN"
            : layerName.Replace(' ', '-');
        return $"DETAIL:{safeLayer}:{layerIndex.ToString(CultureInfo.InvariantCulture)}";
    }

    private static int NextIndex(IDictionary<string, int> indexes, string sourceLayer)
    {
        indexes.TryGetValue(sourceLayer, out var current);
        var next = current + 1;
        indexes[sourceLayer] = next;
        return next;
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

    private readonly record struct AssemblyGroupKey(string SourceLayer, string Kind);

    private sealed class AssemblyAccumulator
    {
        private readonly HashSet<string> sourceEntityKinds = new(StringComparer.OrdinalIgnoreCase);

        public AssemblyAccumulator(string sourceLayer, string kind)
        {
            SourceLayer = sourceLayer;
            Kind = kind;
        }

        public string SourceLayer { get; }

        public string Kind { get; }

        public List<IReadOnlyList<GeometryPoint>> GeometryPaths { get; } = [];

        public string? ColorArgb { get; private set; }

        public void AddRange(IReadOnlyList<IReadOnlyList<GeometryPoint>> geometryPaths, string? colorArgb, string sourceEntityKind)
        {
            foreach (var geometryPath in geometryPaths.Where(path => path.Count >= 2))
            {
                GeometryPaths.Add(geometryPath);
            }

            if (ColorArgb is null)
            {
                ColorArgb = colorArgb;
            }

            sourceEntityKinds.Add(sourceEntityKind);
        }
    }
}
