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
    private const decimal DirectComponentGroupingTolerance = 2.0m;
    private const decimal DirectComponentTextHintDistance = 36.0m;
    private const decimal DirectComponentTextHintSeedCenterDistance = 28.0m;
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
        var directComponentSeeds = new List<DirectComponentSeed>();
        var directComponentTextHints = ExtractDirectComponentTextHints(dxf);
        var sourceIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var entity in dxf.Entities)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entity is DxfInsert insert)
            {
                AddInsertComponent(components, sourceIndexes, insert, blockLookup, layerColors);
                continue;
            }

            CollectDirectEntityComponentSeed(directComponentSeeds, entity, layerColors);
        }

        AddGroupedDirectEntityComponents(components, sourceIndexes, directComponentSeeds, directComponentTextHints);
        return Task.FromResult<IReadOnlyList<DetectedFixedPlanComponent>>(components);
    }

    private void AddInsertComponent(
        List<DetectedFixedPlanComponent> components,
        Dictionary<string, int> sourceIndexes,
        DxfInsert insert,
        IReadOnlyDictionary<string, DxfBlock> blockLookup,
        IReadOnlyDictionary<string, string?> layerColors)
    {
        var resolution = ResolveInsertComponentResolution(
            insert,
            blockLookup,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        if (resolution is null)
        {
            return;
        }

        var block = blockLookup.TryGetValue(insert.Name, out var value) ? value : null;
        var transform = ComponentTransform.FromInsert(insert, block?.BasePoint);
        var geometryPaths = GetInsertEntities(insert, block)
            .SelectMany(entity => ExtractEntityGeometryPaths(
                entity,
                transform,
                blockLookup,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)))
            .Where(path => path.Count >= 2)
            .ToArray();

        if (geometryPaths.Length == 0)
        {
            return;
        }

        components.Add(new DetectedFixedPlanComponent(
            BuildSourceEntityRef("INSERT", NextIndex(sourceIndexes, "INSERT")),
            resolution.Value.SourceLayer,
            resolution.Value.Kind,
            "INSERT",
            insert.Name,
            geometryPaths,
            0.95m,
            $"Detected from {insert.Name} block insert.",
            ResolveInsertColorArgb(
                insert,
                block,
                layerColors,
                blockLookup,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase))));
    }

    private void CollectDirectEntityComponentSeed(
        List<DirectComponentSeed> seeds,
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

        var geometryPaths = ExtractEntityGeometryPaths(
            entity,
            ComponentTransform.Identity,
            blockLookup: null,
            visitedBlockNames: null)
            .Where(path => path.Count >= 2)
            .ToArray();
        if (geometryPaths.Length == 0)
        {
            return;
        }

        seeds.Add(new DirectComponentSeed(
            seeds.Count,
            entity.Layer ?? string.Empty,
            kind,
            sourceEntityKind,
            geometryPaths,
            ComputeBoundingBox(geometryPaths),
            $"Detected from {entity.Layer} {sourceEntityKind} entity.",
            ResolveNestedEntityColorArgb(entity.Color, entity.Layer, entity.Layer, layerColors, byBlockColorArgb: null)));
    }

    private static void AddGroupedDirectEntityComponents(
        List<DetectedFixedPlanComponent> components,
        Dictionary<string, int> sourceIndexes,
        IReadOnlyList<DirectComponentSeed> seeds,
        IReadOnlyList<DirectComponentTextHint> textHints)
    {
        foreach (var cluster in ClusterDirectComponentSeeds(seeds)
                     .OrderBy(item => item.FirstOrder))
        {
            if (string.Equals(cluster.Kind, "Cabinet", StringComparison.OrdinalIgnoreCase))
            {
                AddPartitionedCabinetClusterComponents(components, sourceIndexes, cluster, textHints);
                continue;
            }

            AddDirectComponentFromSeeds(
                components,
                sourceIndexes,
                cluster.SourceLayer,
                cluster.Kind,
                cluster.Seeds,
                cluster.ColorArgb,
                textHint: null);
        }
    }

    private static void AddPartitionedCabinetClusterComponents(
        List<DetectedFixedPlanComponent> components,
        Dictionary<string, int> sourceIndexes,
        DirectComponentCluster cluster,
        IReadOnlyList<DirectComponentTextHint> textHints)
    {
        var remainingSeeds = cluster.Seeds
            .OrderBy(seed => seed.Order)
            .ToList();
        var nearbyHints = textHints
            .Where(hint => ComputePointToBoundingBoxDistance(hint.X, hint.Y, cluster.BoundingBox) <= DirectComponentTextHintDistance)
            .OrderBy(hint => ComputePointToBoundingBoxDistance(hint.X, hint.Y, cluster.BoundingBox))
            .ToArray();

        foreach (var hint in nearbyHints)
        {
            var assignedSeeds = remainingSeeds
                .Where(seed => IsSeedNearTextHint(seed, hint))
                .OrderBy(seed => seed.Order)
                .ToArray();
            if (assignedSeeds.Length == 0)
            {
                continue;
            }

            AddDirectComponentFromSeeds(
                components,
                sourceIndexes,
                cluster.SourceLayer,
                hint.Kind,
                assignedSeeds,
                cluster.ColorArgb,
                hint);

            foreach (var assignedSeed in assignedSeeds)
            {
                remainingSeeds.Remove(assignedSeed);
            }
        }

        if (remainingSeeds.Count > 0)
        {
            AddDirectComponentFromSeeds(
                components,
                sourceIndexes,
                cluster.SourceLayer,
                cluster.Kind,
                remainingSeeds,
                cluster.ColorArgb,
                textHint: null);
        }
    }

    private static bool IsSeedNearTextHint(DirectComponentSeed seed, DirectComponentTextHint hint)
    {
        var centerDistance = ComputePointToPointDistance(seed.BoundingBox.CenterX, seed.BoundingBox.CenterY, hint.X, hint.Y);
        return centerDistance <= DirectComponentTextHintSeedCenterDistance;
    }

    private static void AddDirectComponentFromSeeds(
        List<DetectedFixedPlanComponent> components,
        Dictionary<string, int> sourceIndexes,
        string sourceLayer,
        string resolvedKind,
        IReadOnlyList<DirectComponentSeed> seeds,
        string? colorArgb,
        DirectComponentTextHint? textHint)
    {
        var sourceEntityKind = seeds.Count == 1
            ? seeds[0].SourceEntityKind
            : "COMPONENT-GROUP";
        var sourceKey = sourceEntityKind == "COMPONENT-GROUP"
            ? $"DIRECT:{sourceLayer}:{resolvedKind}"
            : sourceEntityKind;
        var sourceEntityRef = BuildSourceEntityRef(
            sourceEntityKind == "COMPONENT-GROUP" ? "DIRECT" : sourceEntityKind,
            NextIndex(sourceIndexes, sourceKey));
        var geometryPaths = seeds
            .SelectMany(seed => seed.GeometryPaths)
            .Where(path => path.Count >= 2)
            .ToArray();
        if (geometryPaths.Length == 0)
        {
            return;
        }

        var detectionNotes = seeds.Count == 1
            ? seeds[0].DetectionNotes
            : $"Detected grouped {resolvedKind} geometry from {sourceLayer} CAD entities.";

        if (textHint is not null)
        {
            var hintText = textHint.Value.Text;
            detectionNotes = string.IsNullOrWhiteSpace(detectionNotes)
                ? $"Nearby text hint: {hintText}."
                : $"{detectionNotes} Nearby text hint: {hintText}.";
        }

        components.Add(new DetectedFixedPlanComponent(
            sourceEntityRef,
            sourceLayer,
            resolvedKind,
            sourceEntityKind,
            SourceBlockName: null,
            geometryPaths,
            0.90m,
            detectionNotes,
            colorArgb));
    }

    private static IReadOnlyList<DirectComponentTextHint> ExtractDirectComponentTextHints(DxfFile dxf)
    {
        var hints = new List<DirectComponentTextHint>();

        foreach (var text in dxf.Entities.OfType<DxfText>())
        {
            AddDirectComponentTextHint(hints, text.Value, text.Location.X, text.Location.Y);
        }

        foreach (var text in dxf.Entities.OfType<DxfMText>())
        {
            AddDirectComponentTextHint(hints, text.Text, text.InsertionPoint.X, text.InsertionPoint.Y);
        }

        return hints;
    }

    private static void AddDirectComponentTextHint(
        List<DirectComponentTextHint> hints,
        string? rawText,
        double x,
        double y)
    {
        var normalizedText = NormalizeCadText(rawText);
        var kind = ResolveDirectComponentTextHintKind(normalizedText);
        if (kind is null)
        {
            return;
        }

        hints.Add(new DirectComponentTextHint(
            kind,
            normalizedText,
            (decimal)x,
            (decimal)y));
    }

    private static string? ResolveDirectComponentTextHintKind(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (ContainsAnyToken(text, "COOKTOP", "OVEN", "RANGE", "STOVE", "DISHWASHER", "WASH", "DRY", "REF", "REFRIG"))
        {
            return "Appliance";
        }

        if (ContainsAnyToken(text, "SINK", "DISP", "TUB", "SHWR", "SHOWER", "LAV", "LAVA"))
        {
            return "Fixture";
        }

        return null;
    }

    private static bool ContainsAnyToken(string value, params string[] tokens)
    {
        return tokens.Any(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeCadText(string? value)
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

    private static DirectComponentTextHint? FindNearestTextHint(
        DirectComponentCluster cluster,
        IReadOnlyList<DirectComponentTextHint> textHints)
    {
        DirectComponentTextHint? nearest = null;
        decimal nearestDistance = decimal.MaxValue;
        foreach (var hint in textHints)
        {
            var distance = ComputePointToBoundingBoxDistance(hint.X, hint.Y, cluster.BoundingBox);
            if (distance > DirectComponentTextHintDistance || distance >= nearestDistance)
            {
                continue;
            }

            nearest = hint;
            nearestDistance = distance;
        }

        return nearest;
    }

    private static decimal ComputePointToBoundingBoxDistance(decimal x, decimal y, BoundingBox boundingBox)
    {
        var horizontalGap = Math.Max(0m, Math.Max(boundingBox.MinX - x, x - boundingBox.MaxX));
        var verticalGap = Math.Max(0m, Math.Max(boundingBox.MinY - y, y - boundingBox.MaxY));
        if (horizontalGap == 0m && verticalGap == 0m)
        {
            return 0m;
        }

        return (decimal)Math.Sqrt((double)((horizontalGap * horizontalGap) + (verticalGap * verticalGap)));
    }

    private static decimal ComputePointToPointDistance(decimal x1, decimal y1, decimal x2, decimal y2)
    {
        return (decimal)Math.Sqrt((double)(((x2 - x1) * (x2 - x1)) + ((y2 - y1) * (y2 - y1))));
    }

    private static IReadOnlyList<DirectComponentCluster> ClusterDirectComponentSeeds(IReadOnlyList<DirectComponentSeed> seeds)
    {
        var clusters = new List<DirectComponentCluster>();
        foreach (var layerGroup in seeds.GroupBy(seed => new DirectComponentGroupKey(seed.SourceLayer, seed.Kind)))
        {
            foreach (var seed in layerGroup.OrderBy(item => item.Order))
            {
                var matchingClusters = clusters
                    .Where(cluster =>
                        cluster.SourceLayer == seed.SourceLayer &&
                        cluster.Kind == seed.Kind &&
                        cluster.Seeds.Any(existing => BoundingBoxesTouchOrNear(existing.BoundingBox, seed.BoundingBox, DirectComponentGroupingTolerance)))
                    .ToArray();

                if (matchingClusters.Length == 0)
                {
                    clusters.Add(new DirectComponentCluster(seed));
                    continue;
                }

                var primary = matchingClusters[0];
                primary.Add(seed);
                foreach (var duplicate in matchingClusters.Skip(1))
                {
                    primary.AddRange(duplicate.Seeds);
                    clusters.Remove(duplicate);
                }
            }
        }

        return clusters;
    }

    private static BoundingBox ComputeBoundingBox(IReadOnlyList<IReadOnlyList<GeometryPoint>> geometryPaths)
    {
        var points = geometryPaths
            .SelectMany(path => path)
            .ToArray();
        return new BoundingBox(
            points.Min(point => point.X),
            points.Min(point => point.Y),
            points.Max(point => point.X),
            points.Max(point => point.Y));
    }

    private static bool BoundingBoxesTouchOrNear(BoundingBox left, BoundingBox right, decimal tolerance)
    {
        var horizontalGap = Math.Max(0m, Math.Max(right.MinX - left.MaxX, left.MinX - right.MaxX));
        var verticalGap = Math.Max(0m, Math.Max(right.MinY - left.MaxY, left.MinY - right.MaxY));
        return horizontalGap <= tolerance && verticalGap <= tolerance;
    }

    private InsertComponentResolution? ResolveInsertComponentResolution(
        DxfInsert insert,
        IReadOnlyDictionary<string, DxfBlock> blockLookup,
        ISet<string> visitedBlockNames)
    {
        var kindFromBlockName = profile.ResolveFixedComponentBlockKind(insert.Name);
        if (kindFromBlockName is not null)
        {
            return new InsertComponentResolution(kindFromBlockName, insert.Layer ?? string.Empty);
        }

        var kindFromInsertLayer = profile.ResolveFixedComponentLayerKind(insert.Layer);
        if (kindFromInsertLayer is not null)
        {
            return new InsertComponentResolution(kindFromInsertLayer, insert.Layer ?? string.Empty);
        }

        if (!TryEnterBlock(insert.Name, visitedBlockNames, blockLookup, out var block))
        {
            return null;
        }

        try
        {
            foreach (var entity in GetInsertEntities(insert, block))
            {
                var nestedResolution = ResolveEntityComponentResolution(entity, blockLookup, visitedBlockNames);
                if (nestedResolution is not null)
                {
                    return nestedResolution;
                }
            }

            return null;
        }
        finally
        {
            ExitBlock(block, visitedBlockNames);
        }
    }

    private InsertComponentResolution? ResolveEntityComponentResolution(
        DxfEntity entity,
        IReadOnlyDictionary<string, DxfBlock> blockLookup,
        ISet<string> visitedBlockNames)
    {
        if (entity is DxfInsert nestedInsert)
        {
            return ResolveInsertComponentResolution(nestedInsert, blockLookup, visitedBlockNames);
        }

        var kind = profile.ResolveFixedComponentLayerKind(entity.Layer);
        return kind is null
            ? null
            : new InsertComponentResolution(kind, entity.Layer ?? string.Empty);
    }

    private static IReadOnlyList<IReadOnlyList<GeometryPoint>> ExtractEntityGeometryPaths(
        DxfEntity entity,
        ComponentTransform transform,
        IReadOnlyDictionary<string, DxfBlock>? blockLookup,
        ISet<string>? visitedBlockNames)
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
            DxfInsert insert when blockLookup is not null && visitedBlockNames is not null
                => ExtractInsertGeometryPaths(insert, transform, blockLookup, visitedBlockNames),
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

    private static IReadOnlyList<IReadOnlyList<GeometryPoint>> ExtractInsertGeometryPaths(
        DxfInsert insert,
        ComponentTransform parentTransform,
        IReadOnlyDictionary<string, DxfBlock> blockLookup,
        ISet<string> visitedBlockNames)
    {
        if (!TryEnterBlock(insert.Name, visitedBlockNames, blockLookup, out var block))
        {
            return [];
        }

        try
        {
            var transform = parentTransform.Append(ComponentTransform.FromInsert(insert, block?.BasePoint));
            return GetInsertEntities(insert, block)
                .SelectMany(entity => ExtractEntityGeometryPaths(entity, transform, blockLookup, visitedBlockNames))
                .Where(path => path.Count >= 2)
                .ToArray();
        }
        finally
        {
            ExitBlock(block, visitedBlockNames);
        }
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

    private static string? ResolveInsertColorArgb(
        DxfInsert insert,
        DxfBlock? block,
        IReadOnlyDictionary<string, string?> layerColors,
        IReadOnlyDictionary<string, DxfBlock> blockLookup,
        ISet<string> visitedBlockNames)
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

        if (!TryEnterBlock(insert.Name, visitedBlockNames, blockLookup, out var currentBlock))
        {
            return null;
        }

        try
        {
            return GetInsertEntities(insert, currentBlock)
                .Select(entity => ResolveEntityColorArgb(
                    entity,
                    insert.Layer,
                    layerColors,
                    insertColor,
                    blockLookup,
                    visitedBlockNames))
                .FirstOrDefault(color => color is not null);
        }
        finally
        {
            ExitBlock(currentBlock, visitedBlockNames);
        }
    }

    private static string? ResolveEntityColorArgb(
        DxfEntity entity,
        string? fallbackLayer,
        IReadOnlyDictionary<string, string?> layerColors,
        string? byBlockColorArgb,
        IReadOnlyDictionary<string, DxfBlock> blockLookup,
        ISet<string> visitedBlockNames)
    {
        if (entity is DxfInsert nestedInsert)
        {
            var nestedBlock = blockLookup.TryGetValue(nestedInsert.Name, out var block) ? block : null;
            var nestedInsertColor = ResolveNestedEntityColorArgb(
                nestedInsert.Color,
                nestedInsert.Layer,
                fallbackLayer,
                layerColors,
                byBlockColorArgb);
            if (nestedInsertColor is not null)
            {
                return nestedInsertColor;
            }

            return ResolveInsertColorArgb(
                nestedInsert,
                nestedBlock,
                layerColors,
                blockLookup,
                visitedBlockNames);
        }

        return ResolveNestedEntityColorArgb(
                entity.Color,
                entity.Layer,
                fallbackLayer,
                layerColors,
                byBlockColorArgb);
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

    private readonly record struct InsertComponentResolution(string Kind, string SourceLayer);

    private readonly record struct DirectComponentGroupKey(string SourceLayer, string Kind);

    private readonly record struct BoundingBox(decimal MinX, decimal MinY, decimal MaxX, decimal MaxY)
    {
        public decimal CenterX => (MinX + MaxX) / 2m;

        public decimal CenterY => (MinY + MaxY) / 2m;
    }

    private sealed class DirectComponentCluster
    {
        public DirectComponentCluster(DirectComponentSeed seed)
        {
            Seeds.Add(seed);
        }

        public List<DirectComponentSeed> Seeds { get; } = [];

        public string SourceLayer => Seeds[0].SourceLayer;

        public string Kind => Seeds[0].Kind;

        public string? ColorArgb => Seeds.Select(seed => seed.ColorArgb).FirstOrDefault(color => color is not null);

        public BoundingBox BoundingBox => new(
            Seeds.Min(seed => seed.BoundingBox.MinX),
            Seeds.Min(seed => seed.BoundingBox.MinY),
            Seeds.Max(seed => seed.BoundingBox.MaxX),
            Seeds.Max(seed => seed.BoundingBox.MaxY));

        public int FirstOrder => Seeds.Min(seed => seed.Order);

        public void Add(DirectComponentSeed seed)
        {
            Seeds.Add(seed);
        }

        public void AddRange(IEnumerable<DirectComponentSeed> seeds)
        {
            Seeds.AddRange(seeds);
        }
    }

    private readonly record struct DirectComponentSeed(
        int Order,
        string SourceLayer,
        string Kind,
        string SourceEntityKind,
        IReadOnlyList<IReadOnlyList<GeometryPoint>> GeometryPaths,
        BoundingBox BoundingBox,
        string? DetectionNotes,
        string? ColorArgb);

    private readonly record struct DirectComponentTextHint(
        string Kind,
        string Text,
        decimal X,
        decimal Y);

    private readonly record struct ComponentTransform(
        double M11,
        double M12,
        double M21,
        double M22,
        double Tx,
        double Ty)
    {
        public static ComponentTransform Identity { get; } = new(1d, 0d, 0d, 1d, 0d, 0d);

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

        public GeometryPoint Apply(double x, double y)
        {
            return new GeometryPoint(
                (decimal)((M11 * x) + (M12 * y) + Tx),
                (decimal)((M21 * x) + (M22 * y) + Ty));
        }
    }
}
