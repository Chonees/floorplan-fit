using System.Globalization;
using FloorplanFit.Application.Abstractions;
using IxMilia.Dxf;
using IxMilia.Dxf.Blocks;
using IxMilia.Dxf.Entities;

namespace FloorplanFit.Infrastructure.Dxf;

public sealed class IxMiliaFixedPlanComponentExtractor : IFixedPlanComponentExtractor
{
    private const double ArcSegmentDegrees = 10d;
    private const int CircleSegmentCount = 36;
    private readonly DxfExtractionProfile profile;

    public IxMiliaFixedPlanComponentExtractor()
        : this(DxfExtractionProfile.PointeHomes)
    {
    }

    public IxMiliaFixedPlanComponentExtractor(DxfExtractionProfile profile)
    {
        this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    public Task<IReadOnlyList<DetectedFixedPlanComponent>> ExtractAsync(string managedFilePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(managedFilePath))
        {
            throw new ArgumentException("A DXF file path is required.", nameof(managedFilePath));
        }

        var dxf = DxfFile.Load(managedFilePath);
        var blockLookup = dxf.Blocks.ToDictionary(item => item.Name, StringComparer.OrdinalIgnoreCase);
        var layerColors = dxf.Layers.ToDictionary(
            layer => layer.Name,
            layer => ToColorArgb(layer.Color),
            StringComparer.OrdinalIgnoreCase);
        var components = new List<DetectedFixedPlanComponent>();
        var sourceIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var entity in dxf.Entities)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entity is DxfInsert insert)
            {
                AddInsertComponent(components, sourceIndexes, insert, blockLookup, layerColors);
                continue;
            }

            AddDirectEntityComponent(components, sourceIndexes, entity, layerColors);
        }

        return Task.FromResult<IReadOnlyList<DetectedFixedPlanComponent>>(components);
    }

    private void AddInsertComponent(
        List<DetectedFixedPlanComponent> components,
        Dictionary<string, int> sourceIndexes,
        DxfInsert insert,
        IReadOnlyDictionary<string, DxfBlock> blockLookup,
        IReadOnlyDictionary<string, string?> layerColors)
    {
        var kind = profile.ResolveFixedComponentBlockKind(insert.Name) ?? profile.ResolveFixedComponentLayerKind(insert.Layer);
        if (kind is null)
        {
            return;
        }

        var block = blockLookup.TryGetValue(insert.Name, out var value) ? value : null;
        var transform = CreateTransform(insert, block?.BasePoint);
        var geometryPaths = insert.Entities
            .SelectMany(entity => ExtractEntityGeometryPaths(entity, transform))
            .Where(path => path.Count >= 2)
            .ToArray();

        if (geometryPaths.Length == 0)
        {
            return;
        }

        components.Add(new DetectedFixedPlanComponent(
            BuildSourceEntityRef("INSERT", NextIndex(sourceIndexes, "INSERT")),
            insert.Layer ?? string.Empty,
            kind,
            "INSERT",
            insert.Name,
            geometryPaths,
            0.95m,
            $"Detected from {insert.Name} block insert.",
            ResolveInsertColorArgb(insert, block, layerColors)));
    }

    private void AddDirectEntityComponent(
        List<DetectedFixedPlanComponent> components,
        Dictionary<string, int> sourceIndexes,
        DxfEntity entity,
        IReadOnlyDictionary<string, string?> layerColors)
    {
        var kind = profile.ResolveFixedComponentLayerKind(entity.Layer);
        if (kind is null)
        {
            return;
        }

        var sourceEntityKind = ResolveEntityKind(entity);
        if (sourceEntityKind is null)
        {
            return;
        }

        var geometryPaths = ExtractEntityGeometryPaths(entity, ComponentTransform.Identity)
            .Where(path => path.Count >= 2)
            .ToArray();
        if (geometryPaths.Length == 0)
        {
            return;
        }

        components.Add(new DetectedFixedPlanComponent(
            BuildSourceEntityRef(sourceEntityKind, NextIndex(sourceIndexes, sourceEntityKind)),
            entity.Layer ?? string.Empty,
            kind,
            sourceEntityKind,
            SourceBlockName: null,
            geometryPaths,
            0.90m,
            $"Detected from {entity.Layer} {sourceEntityKind} entity.",
            ResolveNestedEntityColorArgb(entity.Color, entity.Layer, entity.Layer, layerColors, byBlockColorArgb: null)));
    }

    private static IReadOnlyList<IReadOnlyList<GeometryPoint>> ExtractEntityGeometryPaths(DxfEntity entity, ComponentTransform transform)
    {
        return entity switch
        {
            DxfLine line => [[transform.Apply(line.P1.X, line.P1.Y), transform.Apply(line.P2.X, line.P2.Y)]],
            DxfArc arc => [FlattenArc(arc).Select(point => transform.Apply(point.X, point.Y)).ToArray()],
            DxfCircle circle => [FlattenCircle(circle).Select(point => transform.Apply(point.X, point.Y)).ToArray()],
            DxfEllipse ellipse => [FlattenEllipse(ellipse).Select(point => transform.Apply(point.X, point.Y)).ToArray()],
            DxfLwPolyline polyline => [ExtractPolylinePoints(polyline).Select(point => transform.Apply(point.X, point.Y)).ToArray()],
            Dxf3DFace face => [ExtractFacePoints(face).Select(point => transform.Apply(point.X, point.Y)).ToArray()],
            DxfSolid solid => [ExtractSolidPoints(solid).Select(point => transform.Apply(point.X, point.Y)).ToArray()],
            _ => []
        };
    }

    private static IReadOnlyList<DxfPoint> FlattenArc(DxfArc arc)
    {
        var startAngle = arc.StartAngle;
        var endAngle = arc.EndAngle;
        if (endAngle < startAngle)
        {
            endAngle += 360d;
        }

        var sweep = Math.Max(endAngle - startAngle, 0d);
        var segmentCount = Math.Max(2, (int)Math.Ceiling(sweep / ArcSegmentDegrees));
        var points = new List<DxfPoint>(segmentCount + 1);

        for (var index = 0; index <= segmentCount; index++)
        {
            var angle = startAngle + (sweep * index / segmentCount);
            var radians = angle * Math.PI / 180d;
            points.Add(new DxfPoint(
                arc.Center.X + (Math.Cos(radians) * arc.Radius),
                arc.Center.Y + (Math.Sin(radians) * arc.Radius),
                0d));
        }

        return points;
    }

    private static IReadOnlyList<DxfPoint> FlattenCircle(DxfCircle circle)
    {
        var points = new List<DxfPoint>(CircleSegmentCount + 1);
        for (var index = 0; index <= CircleSegmentCount; index++)
        {
            var radians = 2d * Math.PI * index / CircleSegmentCount;
            points.Add(new DxfPoint(
                circle.Center.X + (Math.Cos(radians) * circle.Radius),
                circle.Center.Y + (Math.Sin(radians) * circle.Radius),
                0d));
        }

        return points;
    }

    private static IReadOnlyList<DxfPoint> FlattenEllipse(DxfEllipse ellipse)
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
        var points = new List<DxfPoint>(segmentCount + 1);

        for (var index = 0; index <= segmentCount; index++)
        {
            var parameter = start + (sweep * index / segmentCount);
            points.Add(new DxfPoint(
                ellipse.Center.X + (majorX * Math.Cos(parameter)) + (minorX * Math.Sin(parameter)),
                ellipse.Center.Y + (majorY * Math.Cos(parameter)) + (minorY * Math.Sin(parameter)),
                0d));
        }

        return points;
    }

    private static IReadOnlyList<DxfPoint> ExtractPolylinePoints(DxfLwPolyline polyline)
    {
        var points = polyline.Vertices
            .Select(vertex => new DxfPoint(vertex.X, vertex.Y, 0d))
            .ToList();
        if (polyline.IsClosed && points.Count > 0)
        {
            points.Add(points[0]);
        }

        return points;
    }

    private static IReadOnlyList<DxfPoint> ExtractFacePoints(Dxf3DFace face)
    {
        return [face.FirstCorner, face.SecondCorner, face.ThirdCorner, face.FourthCorner, face.FirstCorner];
    }

    private static IReadOnlyList<DxfPoint> ExtractSolidPoints(DxfSolid solid)
    {
        return [solid.FirstCorner, solid.SecondCorner, solid.ThirdCorner, solid.FourthCorner, solid.FirstCorner];
    }

    private static ComponentTransform CreateTransform(DxfInsert insert, DxfPoint? blockBasePoint)
    {
        return new ComponentTransform(
            insert.Location.X,
            insert.Location.Y,
            insert.XScaleFactor,
            insert.YScaleFactor,
            insert.Rotation * Math.PI / 180d,
            blockBasePoint?.X ?? 0d,
            blockBasePoint?.Y ?? 0d);
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

    private static string? ResolveInsertColorArgb(
        DxfInsert insert,
        DxfBlock? block,
        IReadOnlyDictionary<string, string?> layerColors)
    {
        var insertColor = ResolveNestedEntityColorArgb(
            insert.Color,
            insert.Layer,
            insert.Layer,
            layerColors,
            byBlockColorArgb: null);
        if (insertColor is not null)
        {
            return insertColor;
        }

        var blockEntities = block?.Entities ?? Enumerable.Empty<DxfEntity>();
        return insert.Entities
            .Concat(blockEntities)
            .Select(entity => ResolveNestedEntityColorArgb(
                entity.Color,
                entity.Layer,
                insert.Layer,
                layerColors,
                insertColor))
            .FirstOrDefault(color => color is not null);
    }

    private static string? ResolveNestedEntityColorArgb(
        DxfColor entityColor,
        string? entityLayer,
        string? fallbackLayer,
        IReadOnlyDictionary<string, string?> layerColors,
        string? byBlockColorArgb)
    {
        if (entityColor.IsByBlock)
        {
            return byBlockColorArgb;
        }

        if (entityColor.IsByLayer)
        {
            var effectiveLayer = string.Equals(entityLayer, "0", StringComparison.OrdinalIgnoreCase)
                ? fallbackLayer
                : entityLayer;

            return effectiveLayer is not null && layerColors.TryGetValue(effectiveLayer, out var layerColor)
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

    private static int NextIndex(IDictionary<string, int> indexes, string sourceEntityKind)
    {
        indexes.TryGetValue(sourceEntityKind, out var current);
        var next = current + 1;
        indexes[sourceEntityKind] = next;
        return next;
    }

    private static string BuildSourceEntityRef(string entityType, int entityIndex)
    {
        return $"{entityType}:{entityIndex.ToString(CultureInfo.InvariantCulture)}";
    }

    private readonly record struct ComponentTransform(
        double LocationX,
        double LocationY,
        double ScaleX,
        double ScaleY,
        double RotationRadians,
        double BaseX,
        double BaseY)
    {
        public static ComponentTransform Identity { get; } = new(0d, 0d, 1d, 1d, 0d, 0d, 0d);

        public GeometryPoint Apply(double x, double y)
        {
            var localX = (x - BaseX) * ScaleX;
            var localY = (y - BaseY) * ScaleY;
            var cos = Math.Cos(RotationRadians);
            var sin = Math.Sin(RotationRadians);

            return new GeometryPoint(
                (decimal)(LocationX + (localX * cos) - (localY * sin)),
                (decimal)(LocationY + (localX * sin) + (localY * cos)));
        }
    }
}
