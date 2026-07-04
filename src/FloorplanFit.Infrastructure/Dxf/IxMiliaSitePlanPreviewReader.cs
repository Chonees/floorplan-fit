using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.Measurement;
using IxMilia.Dxf;
using IxMilia.Dxf.Blocks;
using IxMilia.Dxf.Entities;

namespace FloorplanFit.Infrastructure.Dxf;

public sealed class IxMiliaSitePlanPreviewReader : ISitePlanPreviewReader
{
    private const int CircleSegmentCount = 72;
    private const double ArcSegmentDegrees = 10d;
    private const string SetbackFallbackColorArgb = "#FFFFB000";

    public Task<SitePlanPreviewDto> ReadAsync(string filePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("A site plan DXF file path is required.", nameof(filePath));
        }

        var dxf = DxfFile.Load(filePath);
        var sourceUnit = DetectSourceUnit(dxf);
        var model = ExtractSitePlanModel(dxf, cancellationToken);
        var geometryPaths = model.RenderPaths
            .Select(path => new GeometryPathDto(path.Id, path.IsClosed, path.Segments))
            .ToArray();
        var buildableArea = ResolveBuildableArea(model.RenderPaths);
        var preview = new SitePlanPreviewDto(
            Path.GetFileName(filePath),
            sourceUnit.ToString().ToLowerInvariant(),
            ToMillimetersFactorOrIdentity(sourceUnit),
            geometryPaths,
            buildableArea,
            model.RenderPaths,
            model.Texts);

        return Task.FromResult(preview);
    }

    private static SitePlanModel ExtractSitePlanModel(DxfFile dxf, CancellationToken cancellationToken)
    {
        var layerColors = dxf.Layers.ToDictionary(
            layer => layer.Name,
            layer => ToColorArgb(layer.Color),
            StringComparer.OrdinalIgnoreCase);
        var blockLookup = dxf.Blocks.ToDictionary(block => block.Name, StringComparer.OrdinalIgnoreCase);
        var state = new ExtractionState(layerColors, blockLookup);

        foreach (var entity in dxf.Entities)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AddEntity(
                state,
                entity,
                ComponentTransform.Identity,
                fallbackLayer: entity.Layer,
                byBlockColorArgb: null,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }

        return new SitePlanModel(state.RenderPaths, state.Texts);
    }

    private static void AddEntity(
        ExtractionState state,
        DxfEntity entity,
        ComponentTransform transform,
        string? fallbackLayer,
        string? byBlockColorArgb,
        ISet<string> visitedBlockNames)
    {
        switch (entity)
        {
            case DxfText text:
                AddText(state, text, transform, fallbackLayer, byBlockColorArgb);
                return;
            case DxfMText text:
                AddMText(state, text, transform, fallbackLayer, byBlockColorArgb);
                return;
            case DxfInsert insert:
                AddInsert(state, insert, transform, fallbackLayer, byBlockColorArgb, visitedBlockNames);
                return;
        }

        foreach (var path in ExtractEntityPaths(entity, transform))
        {
            AddPath(state, entity, fallbackLayer, byBlockColorArgb, path);
        }
    }

    private static void AddInsert(
        ExtractionState state,
        DxfInsert insert,
        ComponentTransform parentTransform,
        string? fallbackLayer,
        string? byBlockColorArgb,
        ISet<string> visitedBlockNames)
    {
        if (!TryEnterBlock(insert.Name, visitedBlockNames, state.BlockLookup, out var block))
        {
            return;
        }

        try
        {
            var insertColor = ResolveColorArgb(insert.Color, insert.Layer, fallbackLayer, state.LayerColors, byBlockColorArgb);
            var transform = parentTransform.Append(ComponentTransform.FromInsert(insert, block?.BasePoint));
            var insertLayer = ResolveLayerName(insert.Layer, fallbackLayer);
            foreach (var entity in GetInsertEntities(insert, block))
            {
                AddEntity(state, entity, transform, insertLayer, insertColor, visitedBlockNames);
            }
        }
        finally
        {
            ExitBlock(block, visitedBlockNames);
        }
    }

    private static void AddPath(
        ExtractionState state,
        DxfEntity entity,
        string? fallbackLayer,
        string? byBlockColorArgb,
        SitePlanPathGeometry geometry)
    {
        if (geometry.Points.Count < 2 || !HasExtent(geometry.Points))
        {
            return;
        }

        var layer = ResolveLayerName(entity.Layer, fallbackLayer);
        var isSetback = IsSetback(layer, text: null);
        var colorArgb = ResolveColorArgb(entity.Color, entity.Layer, fallbackLayer, state.LayerColors, byBlockColorArgb);
        var pathId = Guid.NewGuid();
        var segments = new List<GeometrySegmentDto>();
        for (var index = 0; index < geometry.Points.Count - 1; index++)
        {
            segments.Add(new GeometrySegmentDto(
                pathId,
                state.NextSortOrder(),
                ToDecimal(geometry.Points[index].X),
                ToDecimal(geometry.Points[index].Y),
                ToDecimal(geometry.Points[index + 1].X),
                ToDecimal(geometry.Points[index + 1].Y)));
        }

        state.RenderPaths.Add(new SitePlanRenderPathDto(
            pathId,
            layer,
            geometry.SourceEntityKind,
            geometry.IsClosed,
            segments,
            ResolveRenderColor(colorArgb, isSetback),
            isSetback));
    }

    private static void AddText(
        ExtractionState state,
        DxfText text,
        ComponentTransform transform,
        string? fallbackLayer,
        string? byBlockColorArgb)
    {
        if (string.IsNullOrWhiteSpace(text.Value))
        {
            return;
        }

        var point = transform.Apply(text.Location.X, text.Location.Y);
        var layer = ResolveLayerName(text.Layer, fallbackLayer);
        var isSetback = IsSetback(layer, text.Value);
        var colorArgb = ResolveColorArgb(text.Color, text.Layer, fallbackLayer, state.LayerColors, byBlockColorArgb);
        state.Texts.Add(new SitePlanTextDto(
            Guid.NewGuid(),
            layer,
            "TEXT",
            text.Value,
            ToDecimal(point.X),
            ToDecimal(point.Y),
            ToDecimal(text.TextHeight * transform.ApproximateScale),
            ToDecimal(text.Rotation + transform.RotationDegrees),
            ResolveRenderColor(colorArgb, isSetback),
            isSetback));
    }

    private static void AddMText(
        ExtractionState state,
        DxfMText text,
        ComponentTransform transform,
        string? fallbackLayer,
        string? byBlockColorArgb)
    {
        if (string.IsNullOrWhiteSpace(text.Text))
        {
            return;
        }

        var point = transform.Apply(text.InsertionPoint.X, text.InsertionPoint.Y);
        var layer = ResolveLayerName(text.Layer, fallbackLayer);
        var isSetback = IsSetback(layer, text.Text);
        var colorArgb = ResolveColorArgb(text.Color, text.Layer, fallbackLayer, state.LayerColors, byBlockColorArgb);
        state.Texts.Add(new SitePlanTextDto(
            Guid.NewGuid(),
            layer,
            "MTEXT",
            NormalizeMText(text.Text),
            ToDecimal(point.X),
            ToDecimal(point.Y),
            ToDecimal(text.InitialTextHeight * transform.ApproximateScale),
            ToDecimal(text.RotationAngle + transform.RotationDegrees),
            ResolveRenderColor(colorArgb, isSetback),
            isSetback));
    }

    private static IReadOnlyList<SitePlanPathGeometry> ExtractEntityPaths(DxfEntity entity, ComponentTransform transform)
    {
        return entity switch
        {
            DxfLine line => [new SitePlanPathGeometry(
                "LINE",
                IsClosed: false,
                [transform.Apply(line.P1.X, line.P1.Y), transform.Apply(line.P2.X, line.P2.Y)])],
            DxfArc arc => [new SitePlanPathGeometry(
                "ARC",
                IsClosed: false,
                FlattenArc(arc).Select(point => transform.Apply(point.X, point.Y)).ToArray())],
            DxfCircle circle => [new SitePlanPathGeometry(
                "CIRCLE",
                IsClosed: true,
                FlattenCircle(circle).Select(point => transform.Apply(point.X, point.Y)).ToArray())],
            DxfEllipse ellipse => [new SitePlanPathGeometry(
                "ELLIPSE",
                IsClosed: true,
                FlattenEllipse(ellipse).Select(point => transform.Apply(point.X, point.Y)).ToArray())],
            DxfLwPolyline polyline => [new SitePlanPathGeometry(
                "LWPOLYLINE",
                polyline.IsClosed,
                ExtractPolylinePoints(polyline).Select(point => transform.Apply(point.X, point.Y)).ToArray())],
            Dxf3DFace face => [new SitePlanPathGeometry(
                "3DFACE",
                IsClosed: true,
                ExtractFacePoints(face).Select(point => transform.Apply(point.X, point.Y)).ToArray())],
            DxfSolid solid => [new SitePlanPathGeometry(
                "SOLID",
                IsClosed: true,
                ExtractSolidPoints(solid).Select(point => transform.Apply(point.X, point.Y)).ToArray())],
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

    private static IReadOnlyList<DxfEntity> GetInsertEntities(DxfInsert insert, DxfBlock? block)
    {
        var insertEntities = insert.Entities?.ToArray() ?? [];
        return insertEntities.Length > 0
            ? insertEntities
            : block?.Entities?.ToArray() ?? [];
    }

    private static bool TryEnterBlock(
        string? blockName,
        ISet<string> visitedBlockNames,
        IReadOnlyDictionary<string, DxfBlock> blockLookup,
        out DxfBlock? block)
    {
        if (string.IsNullOrWhiteSpace(blockName) || !blockLookup.TryGetValue(blockName, out block))
        {
            block = null;
            return true;
        }

        return visitedBlockNames.Add(block.Name);
    }

    private static void ExitBlock(DxfBlock? block, ISet<string> visitedBlockNames)
    {
        if (block is not null)
        {
            visitedBlockNames.Remove(block.Name);
        }
    }

    private static SitePlanBuildableAreaDto ResolveBuildableArea(IReadOnlyList<SitePlanRenderPathDto> renderPaths)
    {
        var setbackBounds = TryBoundsOfRenderPaths(renderPaths.Where(path => path.IsSetback));
        if (setbackBounds is { Area: > 0m } buildableBounds)
        {
            return ToBuildableArea(buildableBounds);
        }

        var closedBounds = renderPaths
            .Where(path => path.IsClosed)
            .Select(path => TryBoundsOfRenderPaths([path]))
            .Where(bounds => bounds.HasValue && bounds.Value.Area > 0m)
            .Select(bounds => bounds!.Value)
            .OrderByDescending(bounds => bounds.Area)
            .ToArray();

        var selected = closedBounds.Length switch
        {
            >= 2 => closedBounds[1],
            1 => closedBounds[0],
            _ => TryBoundsOfRenderPaths(renderPaths)
        };

        return selected is { } fallbackBounds
            ? ToBuildableArea(fallbackBounds)
            : new SitePlanBuildableAreaDto(0m, 0m, 0m, 0m);
    }

    private static SitePlanBuildableAreaDto ToBuildableArea(GeometryBounds bounds)
        => new(bounds.MinX, bounds.MinY, bounds.MaxX, bounds.MaxY);

    private static GeometryBounds? TryBoundsOfRenderPaths(IEnumerable<SitePlanRenderPathDto> paths)
    {
        var points = paths
            .SelectMany(path => path.Segments)
            .SelectMany(segment => new[]
            {
                (X: segment.StartX, Y: segment.StartY),
                (X: segment.EndX, Y: segment.EndY)
            })
            .ToArray();

        if (points.Length == 0)
        {
            return null;
        }

        return new GeometryBounds(
            points.Min(point => point.X),
            points.Min(point => point.Y),
            points.Max(point => point.X),
            points.Max(point => point.Y));
    }

    private static LengthUnit DetectSourceUnit(DxfFile dxfFile)
    {
        var insUnitsCode = TryReadHeaderInt(dxfFile.Header, "$INSUNITS");

        if (insUnitsCode is not null and not 0)
        {
            return MapLengthUnit(insUnitsCode.Value);
        }

        var measurementCode = TryReadHeaderInt(dxfFile.Header, "$MEASUREMENT");

        return measurementCode switch
        {
            1 => LengthUnit.Millimeter,
            0 => LengthUnit.Inch,
            _ => LengthUnit.Unknown
        };
    }

    private static int? TryReadHeaderInt(DxfHeader header, string variableName)
    {
        try
        {
            var value = header[variableName];

            if (value is null)
            {
                return null;
            }

            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }

    private static LengthUnit MapLengthUnit(int insUnitsCode)
    {
        return insUnitsCode switch
        {
            1 => LengthUnit.Inch,
            2 => LengthUnit.Foot,
            4 => LengthUnit.Millimeter,
            5 => LengthUnit.Centimeter,
            6 => LengthUnit.Meter,
            _ => LengthUnit.Unknown
        };
    }

    private static decimal ToMillimetersFactorOrIdentity(LengthUnit sourceUnit)
    {
        return sourceUnit switch
        {
            LengthUnit.Millimeter => 1m,
            LengthUnit.Centimeter => 10m,
            LengthUnit.Meter => 1000m,
            LengthUnit.Inch => 25.4m,
            LengthUnit.Foot => 304.8m,
            _ => 1m
        };
    }

    private static string ResolveLayerName(string? entityLayer, string? fallbackLayer)
    {
        if (string.IsNullOrWhiteSpace(entityLayer) ||
            string.Equals(entityLayer, "0", StringComparison.OrdinalIgnoreCase))
        {
            return fallbackLayer ?? string.Empty;
        }

        return entityLayer;
    }

    // Collapsed entities (all points coincident) carry no drawable geometry, and because
    // the buildable area is the bounding box of the setback paths, letting one through
    // would silently stretch the fit envelope.
    private const double ExtentTolerance = 0.000001d;

    private static bool HasExtent(IReadOnlyList<DxfPoint> points)
    {
        var origin = points[0];
        for (var index = 1; index < points.Count; index++)
        {
            if (Math.Abs(points[index].X - origin.X) > ExtentTolerance ||
                Math.Abs(points[index].Y - origin.Y) > ExtentTolerance)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSetback(string? layer, string? text)
        => ContainsSetback(layer) || ContainsSetback(text);

    private static bool ContainsSetback(string? value)
        => value?.Contains("SETBACK", StringComparison.OrdinalIgnoreCase) == true;

    private static string NormalizeMText(string value)
        => value
            .Replace("\\P", Environment.NewLine, StringComparison.Ordinal)
            .Replace("{", string.Empty, StringComparison.Ordinal)
            .Replace("}", string.Empty, StringComparison.Ordinal);

    private static string? ResolveRenderColor(string? colorArgb, bool isSetback)
        => colorArgb ?? (isSetback ? SetbackFallbackColorArgb : null);

    private static string? ResolveColorArgb(
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
            var layer = ResolveLayerName(entityLayer, fallbackLayer);
            return layerColors.TryGetValue(layer, out var layerColor)
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

        return FormattableString.Invariant($"#FF{red:X2}{green:X2}{blue:X2}");
    }

    private static decimal ToDecimal(double value)
        => (decimal)value;

    private sealed class ExtractionState(
        IReadOnlyDictionary<string, string?> layerColors,
        IReadOnlyDictionary<string, DxfBlock> blockLookup)
    {
        private int sortOrder;

        public IReadOnlyDictionary<string, string?> LayerColors { get; } = layerColors;

        public IReadOnlyDictionary<string, DxfBlock> BlockLookup { get; } = blockLookup;

        public List<SitePlanRenderPathDto> RenderPaths { get; } = [];

        public List<SitePlanTextDto> Texts { get; } = [];

        public int NextSortOrder() => ++sortOrder;
    }

    private sealed record SitePlanModel(
        IReadOnlyList<SitePlanRenderPathDto> RenderPaths,
        IReadOnlyList<SitePlanTextDto> Texts);

    private sealed record SitePlanPathGeometry(
        string SourceEntityKind,
        bool IsClosed,
        IReadOnlyList<DxfPoint> Points);

    private readonly record struct GeometryBounds(decimal MinX, decimal MinY, decimal MaxX, decimal MaxY)
    {
        public decimal Area => (MaxX - MinX) * (MaxY - MinY);
    }

    private readonly record struct ComponentTransform(
        double M11,
        double M12,
        double M21,
        double M22,
        double Tx,
        double Ty)
    {
        public static ComponentTransform Identity { get; } = new(1d, 0d, 0d, 1d, 0d, 0d);

        public double ApproximateScale => Math.Sqrt(Math.Abs((M11 * M22) - (M12 * M21)));

        public double RotationDegrees => Math.Atan2(M21, M11) * 180d / Math.PI;

        public static ComponentTransform FromInsert(DxfInsert insert, DxfPoint? blockBasePoint)
        {
            var scaleX = insert.XScaleFactor == 0d ? 1d : insert.XScaleFactor;
            var scaleY = insert.YScaleFactor == 0d ? 1d : insert.YScaleFactor;
            var rotationRadians = insert.Rotation * Math.PI / 180d;
            var cos = Math.Cos(rotationRadians);
            var sin = Math.Sin(rotationRadians);
            var baseX = blockBasePoint?.X ?? 0d;
            var baseY = blockBasePoint?.Y ?? 0d;
            var m11 = scaleX * cos;
            var m12 = -scaleY * sin;
            var m21 = scaleX * sin;
            var m22 = scaleY * cos;
            var tx = insert.Location.X - (m11 * baseX) - (m12 * baseY);
            var ty = insert.Location.Y - (m21 * baseX) - (m22 * baseY);
            return new ComponentTransform(m11, m12, m21, m22, tx, ty);
        }

        public ComponentTransform Append(ComponentTransform child)
        {
            return new ComponentTransform(
                (M11 * child.M11) + (M12 * child.M21),
                (M11 * child.M12) + (M12 * child.M22),
                (M21 * child.M11) + (M22 * child.M21),
                (M21 * child.M12) + (M22 * child.M22),
                (M11 * child.Tx) + (M12 * child.Ty) + Tx,
                (M21 * child.Tx) + (M22 * child.Ty) + Ty);
        }

        public DxfPoint Apply(double x, double y)
        {
            return new DxfPoint(
                (M11 * x) + (M12 * y) + Tx,
                (M21 * x) + (M22 * y) + Ty,
                0d);
        }
    }
}
