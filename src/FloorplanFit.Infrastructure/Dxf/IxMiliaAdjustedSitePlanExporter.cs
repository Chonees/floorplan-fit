using System.Globalization;
using System.Text;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.FloorPlans;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;

namespace FloorplanFit.Infrastructure.Dxf;

/// <summary>
/// Exports the adjusted floor plan combined with the site plan while preserving the original
/// AutoCAD-authored floor-plan DXF sections/objects. Real Pointe floor plans contain CAD
/// metadata (for example ACDSDATA/layout object graphs) that IxMilia can read for extraction
/// but must not reserialize wholesale for final AutoCAD-facing output.
/// </summary>
public sealed class IxMiliaAdjustedSitePlanExporter : IAdjustedSitePlanExporter
{
    public Task<AdjustedSitePlanExportResult> ExportAsync(
        string floorPlanSourcePath,
        string sitePlanSourcePath,
        string outputFilePath,
        AdjustedSitePlanPlacementDto placement,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(floorPlanSourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(sitePlanSourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFilePath);
        ArgumentNullException.ThrowIfNull(placement);

        if (placement.FloorToSiteScale <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(placement),
                placement.FloorToSiteScale,
                "Floor-to-site scale must be greater than zero.");
        }

        var warnings = new List<string>();
        var sourcePairs = ReadDxfPairs(floorPlanSourcePath);
        var compressedPairs = placement.CompressionSteps.Count == 0
            ? sourcePairs.ToList()
            : ApplyCompression(sourcePairs, placement.CompressionSteps, cancellationToken);
        var patchedPairs = placement.AdjustedDimensions.Count == 0
            ? compressedPairs
            : DxfDimensionBlockPatcher.PatchGeometryBlocks(compressedPairs, placement.AdjustedDimensions, cancellationToken);
        var handleGenerator = new DxfHandleGenerator(patchedPairs);
        var modelSpaceOwner = ResolveModelSpaceOwnerHandle(patchedPairs);
        var sitePlanInjection = BuildSitePlanInjection(
            sitePlanSourcePath,
            placement,
            modelSpaceOwner,
            handleGenerator,
            warnings,
            cancellationToken);

        var withLineTypes = InjectSymbolTableRecords(
            patchedPairs,
            "LTYPE",
            MissingSymbolNames(ReadSymbolTableNames(patchedPairs, "LTYPE"), sitePlanInjection.LineTypeRecords.Keys),
            sitePlanInjection.LineTypeRecords,
            ResolveTableHandle(patchedPairs, "LTYPE"),
            handleGenerator);
        var withTextStyles = InjectSymbolTableRecords(
            withLineTypes,
            "STYLE",
            MissingSymbolNames(ReadSymbolTableNames(withLineTypes, "STYLE"), sitePlanInjection.StyleRecords.Keys),
            sitePlanInjection.StyleRecords,
            ResolveTableHandle(withLineTypes, "STYLE"),
            handleGenerator);
        var existingLayerNames = ReadSymbolTableNames(withTextStyles, "LAYER");
        var missingLayerNames = sitePlanInjection.LayerNames
            .Where(layerName => !existingLayerNames.Contains(layerName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(layerName => layerName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var withLayers = InjectLayerRecords(
            withTextStyles,
            missingLayerNames,
            sitePlanInjection.LayerRecords,
            ResolveTableHandle(withTextStyles, "LAYER"),
            handleGenerator);
        var finalPairs = UpdateHandSeed(
            InjectEntityRecords(withLayers, sitePlanInjection.EntityRecords),
            handleGenerator.NextAvailableHandle);

        var outputDirectory = Path.GetDirectoryName(outputFilePath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        WriteDxfPairs(outputFilePath, finalPairs);
        return Task.FromResult(new AdjustedSitePlanExportResult(
            outputFilePath,
            sitePlanInjection.EntityRecords.Count,
            warnings));
    }

    // ----- floor plan compression --------------------------------------------------------

    private static List<DxfPair> ApplyCompression(
        IReadOnlyList<DxfPair> sourcePairs,
        IReadOnlyList<AdjustedCompressionStepDto> steps,
        CancellationToken cancellationToken)
    {
        var patchedPairs = new List<DxfPair>(sourcePairs.Count);
        string? currentSection = null;
        string? currentBlockName = null;

        for (var index = 0; index < sourcePairs.Count;)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pair = sourcePairs[index];
            if (IsEntityStart(pair, "SECTION"))
            {
                patchedPairs.Add(pair);
                index++;
                if (index < sourcePairs.Count)
                {
                    currentSection = string.Equals(sourcePairs[index].Code, "2", StringComparison.Ordinal)
                        ? sourcePairs[index].Value.Trim()
                        : null;
                    patchedPairs.Add(sourcePairs[index]);
                    index++;
                }

                continue;
            }

            if (IsEntityStart(pair, "ENDSEC"))
            {
                currentSection = null;
                currentBlockName = null;
                patchedPairs.Add(pair);
                index++;
                continue;
            }

            if (IsEntityStart(pair, "BLOCK"))
            {
                var record = ReadRecord(sourcePairs, ref index);
                currentBlockName = FirstGroupValue(record, "2");
                patchedPairs.AddRange(record);
                continue;
            }

            if (IsEntityStart(pair, "ENDBLK"))
            {
                currentBlockName = null;
                patchedPairs.Add(pair);
                index++;
                continue;
            }

            var shouldTransformEntity =
                string.Equals(currentSection, "ENTITIES", StringComparison.OrdinalIgnoreCase) ||
                (string.Equals(currentSection, "BLOCKS", StringComparison.OrdinalIgnoreCase) &&
                 currentBlockName?.StartsWith("*D", StringComparison.OrdinalIgnoreCase) == true);
            if (shouldTransformEntity && string.Equals(pair.Code, "0", StringComparison.Ordinal))
            {
                var record = ReadRecord(sourcePairs, ref index);
                patchedPairs.AddRange(PatchCompressedEntityRecord(record, steps));
                continue;
            }

            patchedPairs.Add(pair);
            index++;
        }

        return patchedPairs;
    }

    private static List<DxfPair> PatchCompressedEntityRecord(
        List<DxfPair> record,
        IReadOnlyList<AdjustedCompressionStepDto> steps)
    {
        var entityKind = record[0].Value.Trim().ToUpperInvariant();
        switch (entityKind)
        {
            case "LINE":
                ReplacePointIfPresent(record, "10", "20", steps);
                ReplacePointIfPresent(record, "11", "21", steps);
                break;
            case "ARC":
            case "CIRCLE":
            case "ELLIPSE":
            case "POINT":
            case "VERTEX":
                ReplacePointIfPresent(record, "10", "20", steps);
                break;
            case "TEXT":
                ReplacePointIfPresent(record, "10", "20", steps);
                ReplacePointIfPresent(record, "11", "21", steps);
                break;
            case "MTEXT":
                ReplacePointIfPresent(record, "10", "20", steps);
                break;
            case "LWPOLYLINE":
                ReplaceRepeatedPointPairs(record, "10", "20", steps);
                break;
            case "SOLID":
            case "3DFACE":
            case "TRACE":
                ReplacePointIfPresent(record, "10", "20", steps);
                ReplacePointIfPresent(record, "11", "21", steps);
                ReplacePointIfPresent(record, "12", "22", steps);
                ReplacePointIfPresent(record, "13", "23", steps);
                break;
        }

        return record;
    }

    private static (decimal X, decimal Y) ApplyCompressionPoint(
        decimal x,
        decimal y,
        IReadOnlyList<AdjustedCompressionStepDto> steps)
    {
        var deltaX = 0m;
        var deltaY = 0m;

        foreach (var step in steps)
        {
            var isWidth = !string.Equals(step.AxisTag, "Height", StringComparison.OrdinalIgnoreCase);
            foreach (var marker in step.Markers)
            {
                if (isWidth)
                {
                    if (string.Equals(step.Edge, "Right", StringComparison.OrdinalIgnoreCase) && x >= marker.Coordinate)
                    {
                        deltaX -= marker.TrimSourceUnits;
                    }
                    else if (string.Equals(step.Edge, "Left", StringComparison.OrdinalIgnoreCase) && x <= marker.Coordinate)
                    {
                        deltaX += marker.TrimSourceUnits;
                    }

                    continue;
                }

                if (string.Equals(step.Edge, "Top", StringComparison.OrdinalIgnoreCase) && y >= marker.Coordinate)
                {
                    deltaY -= marker.TrimSourceUnits;
                }
                else if (string.Equals(step.Edge, "Bottom", StringComparison.OrdinalIgnoreCase) && y <= marker.Coordinate)
                {
                    deltaY += marker.TrimSourceUnits;
                }
            }
        }

        return (x + deltaX, y + deltaY);
    }

    // ----- site plan injection -----------------------------------------------------------

    private static SitePlanInjection BuildSitePlanInjection(
        string sitePlanSourcePath,
        AdjustedSitePlanPlacementDto placement,
        string? modelSpaceOwner,
        DxfHandleGenerator handleGenerator,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var scale = (double)placement.FloorToSiteScale;
        var offsetX = (double)placement.SiteOffsetX;
        var offsetY = (double)placement.SiteOffsetY;

        DxfPoint Map(DxfPoint point) => new(
            (point.X - offsetX) / scale,
            (point.Y - offsetY) / scale,
            point.Z);
        var lengthScale = 1d / scale;
        var sitePairs = ReadDxfPairs(sitePlanSourcePath);
        var siteLayerRecords = ReadSymbolTableRecordMap(sitePairs, "LAYER");
        var siteLineTypeRecords = ReadSymbolTableRecordMap(sitePairs, "LTYPE");
        var siteStyleRecords = ReadSymbolTableRecordMap(sitePairs, "STYLE");

        var records = new List<IReadOnlyList<DxfPair>>();
        var layerNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skippedKinds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var sourceRecord in ReadEntityRecordsFromEntitiesSection(sitePairs))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var record = BuildInjectedEntityRecord(sourceRecord, Map, lengthScale, modelSpaceOwner, handleGenerator.Next());
            if (record is null)
            {
                var kind = sourceRecord[0].Value.Trim();
                skippedKinds[kind] = skippedKinds.TryGetValue(kind, out var count) ? count + 1 : 1;
                continue;
            }

            records.Add(record);
            layerNames.Add(ResolveLayerName(FirstGroupValue(sourceRecord, "8")));
        }

        foreach (var (kind, count) in skippedKinds.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
        {
            warnings.Add($"Site plan: {count} {kind} entity(ies) skipped; that kind references objects outside the entity and is not supported by the combined export yet.");
        }

        return new SitePlanInjection(
            records,
            layerNames,
            siteLayerRecords,
            siteLineTypeRecords,
            siteStyleRecords);
    }

    private static IReadOnlyList<DxfPair>? BuildInjectedEntityRecord(
        IReadOnlyList<DxfPair> sourceRecord,
        Func<DxfPoint, DxfPoint> map,
        double lengthScale,
        string? owner,
        string handle)
    {
        if (sourceRecord.Count == 0)
        {
            return null;
        }

        var entityKind = sourceRecord[0].Value.Trim().ToUpperInvariant();
        if (!IsSupportedSourcePreservedSitePlanEntity(entityKind))
        {
            return null;
        }

        var record = new List<DxfPair> { sourceRecord[0], new("5", handle) };
        if (!string.IsNullOrWhiteSpace(owner))
        {
            record.Add(new DxfPair("330", owner));
        }

        for (var index = 1; index < sourceRecord.Count; index++)
        {
            var pair = sourceRecord[index];
            if (IsSourceIdentityPair(pair))
            {
                continue;
            }

            if (TryTransformCoordinatePair(sourceRecord, index, map, out var transformedX, out var transformedY))
            {
                record.Add(transformedX);
                record.Add(transformedY);
                index++;
                continue;
            }

            if (ShouldScaleSitePlanLength(entityKind, pair.Code) &&
                double.TryParse(pair.Value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var length))
            {
                record.Add(pair with { Value = FormatDouble(length * lengthScale) });
                continue;
            }

            record.Add(pair);
        }

        return record;
    }

    private static bool IsSupportedSourcePreservedSitePlanEntity(string entityKind)
        => entityKind is
            "LINE" or
            "ARC" or
            "CIRCLE" or
            "TEXT" or
            "MTEXT" or
            "LWPOLYLINE" or
            "3DFACE" or
            "SOLID" or
            "POINT";

    private static bool IsSourceIdentityPair(DxfPair pair)
        => string.Equals(pair.Code, "5", StringComparison.Ordinal) ||
           string.Equals(pair.Code, "330", StringComparison.Ordinal);

    private static bool TryTransformCoordinatePair(
        IReadOnlyList<DxfPair> sourceRecord,
        int xIndex,
        Func<DxfPoint, DxfPoint> map,
        out DxfPair transformedX,
        out DxfPair transformedY)
    {
        transformedX = default!;
        transformedY = default!;
        var xCode = sourceRecord[xIndex].Code;
        var yCode = xCode switch
        {
            "10" => "20",
            "11" => "21",
            "12" => "22",
            "13" => "23",
            _ => null
        };

        if (yCode is null ||
            xIndex + 1 >= sourceRecord.Count ||
            !string.Equals(sourceRecord[xIndex + 1].Code, yCode, StringComparison.Ordinal) ||
            !double.TryParse(sourceRecord[xIndex].Value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
            !double.TryParse(sourceRecord[xIndex + 1].Value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
        {
            return false;
        }

        var mapped = map(new DxfPoint(x, y, 0d));
        transformedX = sourceRecord[xIndex] with { Value = FormatDouble(mapped.X) };
        transformedY = sourceRecord[xIndex + 1] with { Value = FormatDouble(mapped.Y) };
        return true;
    }

    private static bool ShouldScaleSitePlanLength(string entityKind, string code)
    {
        return entityKind switch
        {
            "ARC" or "CIRCLE" => code == "40",
            "TEXT" => code == "40",
            "MTEXT" => code is "40" or "41" or "42" or "43",
            "LWPOLYLINE" => code is "40" or "41" or "43",
            _ => false
        };
    }

    private static IReadOnlyList<DxfPair>? BuildInjectedEntityRecord(
        DxfEntity entity,
        Func<DxfPoint, DxfPoint> map,
        double lengthScale,
        string? modelSpaceOwner,
        DxfHandleGenerator handleGenerator)
    {
        return entity switch
        {
            DxfLine line => BuildLineRecord(line, map, modelSpaceOwner, handleGenerator.Next()),
            DxfArc arc => BuildArcRecord(arc, map, lengthScale, modelSpaceOwner, handleGenerator.Next()),
            DxfCircle circle => BuildCircleRecord(circle, map, lengthScale, modelSpaceOwner, handleGenerator.Next()),
            DxfText text => BuildTextRecord(text, map, lengthScale, modelSpaceOwner, handleGenerator.Next()),
            DxfMText text => BuildMTextRecord(text, map, lengthScale, modelSpaceOwner, handleGenerator.Next()),
            DxfLwPolyline polyline => BuildLwPolylineRecord(polyline, map, modelSpaceOwner, handleGenerator.Next()),
            Dxf3DFace face => Build3DFaceRecord(face, map, modelSpaceOwner, handleGenerator.Next()),
            DxfSolid solid => BuildSolidRecord(solid, map, modelSpaceOwner, handleGenerator.Next()),
            DxfModelPoint point => BuildPointRecord(point, map, modelSpaceOwner, handleGenerator.Next()),
            _ => null
        };
    }

    private static IReadOnlyList<DxfPair> BuildLineRecord(
        DxfLine line,
        Func<DxfPoint, DxfPoint> map,
        string? owner,
        string handle)
    {
        var p1 = map(line.P1);
        var p2 = map(line.P2);
        var record = StartEntityRecord("LINE", handle, owner, line.Layer, "AcDbLine");
        AddPoint(record, "10", "20", "30", p1);
        AddPoint(record, "11", "21", "31", p2);
        return record;
    }

    private static IReadOnlyList<DxfPair> BuildArcRecord(
        DxfArc arc,
        Func<DxfPoint, DxfPoint> map,
        double lengthScale,
        string? owner,
        string handle)
    {
        var center = map(arc.Center);
        var record = StartEntityRecord("ARC", handle, owner, arc.Layer, "AcDbCircle");
        AddPoint(record, "10", "20", "30", center);
        record.Add(new DxfPair("40", FormatDouble(arc.Radius * lengthScale)));
        record.Add(new DxfPair("100", "AcDbArc"));
        record.Add(new DxfPair("50", FormatDouble(arc.StartAngle)));
        record.Add(new DxfPair("51", FormatDouble(arc.EndAngle)));
        return record;
    }

    private static IReadOnlyList<DxfPair> BuildCircleRecord(
        DxfCircle circle,
        Func<DxfPoint, DxfPoint> map,
        double lengthScale,
        string? owner,
        string handle)
    {
        var center = map(circle.Center);
        var record = StartEntityRecord("CIRCLE", handle, owner, circle.Layer, "AcDbCircle");
        AddPoint(record, "10", "20", "30", center);
        record.Add(new DxfPair("40", FormatDouble(circle.Radius * lengthScale)));
        return record;
    }

    private static IReadOnlyList<DxfPair> BuildTextRecord(
        DxfText text,
        Func<DxfPoint, DxfPoint> map,
        double lengthScale,
        string? owner,
        string handle)
    {
        var location = map(text.Location);
        var record = StartEntityRecord("TEXT", handle, owner, text.Layer, "AcDbText");
        AddPoint(record, "10", "20", "30", location);
        record.Add(new DxfPair("40", FormatDouble(text.TextHeight * lengthScale)));
        record.Add(new DxfPair("1", text.Value ?? string.Empty));
        record.Add(new DxfPair("50", FormatDouble(text.Rotation)));
        record.Add(new DxfPair("100", "AcDbText"));
        return record;
    }

    private static IReadOnlyList<DxfPair> BuildMTextRecord(
        DxfMText text,
        Func<DxfPoint, DxfPoint> map,
        double lengthScale,
        string? owner,
        string handle)
    {
        var insertionPoint = map(text.InsertionPoint);
        var record = StartEntityRecord("MTEXT", handle, owner, text.Layer, "AcDbMText");
        AddPoint(record, "10", "20", "30", insertionPoint);
        record.Add(new DxfPair("40", FormatDouble(text.InitialTextHeight * lengthScale)));
        record.Add(new DxfPair("41", FormatDouble(text.ReferenceRectangleWidth * lengthScale)));
        record.Add(new DxfPair("1", text.Text ?? string.Empty));
        record.Add(new DxfPair("50", FormatDouble(text.RotationAngle)));
        return record;
    }

    private static IReadOnlyList<DxfPair> BuildLwPolylineRecord(
        DxfLwPolyline polyline,
        Func<DxfPoint, DxfPoint> map,
        string? owner,
        string handle)
    {
        var record = StartEntityRecord("LWPOLYLINE", handle, owner, polyline.Layer, "AcDbPolyline");
        record.Add(new DxfPair("90", polyline.Vertices.Count.ToString(CultureInfo.InvariantCulture)));
        record.Add(new DxfPair("70", polyline.IsClosed ? "1" : "0"));
        foreach (var vertex in polyline.Vertices)
        {
            var point = map(new DxfPoint(vertex.X, vertex.Y, 0d));
            record.Add(new DxfPair("10", FormatDouble(point.X)));
            record.Add(new DxfPair("20", FormatDouble(point.Y)));
        }

        return record;
    }

    private static IReadOnlyList<DxfPair> Build3DFaceRecord(
        Dxf3DFace face,
        Func<DxfPoint, DxfPoint> map,
        string? owner,
        string handle)
    {
        var record = StartEntityRecord("3DFACE", handle, owner, face.Layer, "AcDbFace");
        AddPoint(record, "10", "20", "30", map(face.FirstCorner));
        AddPoint(record, "11", "21", "31", map(face.SecondCorner));
        AddPoint(record, "12", "22", "32", map(face.ThirdCorner));
        AddPoint(record, "13", "23", "33", map(face.FourthCorner));
        return record;
    }

    private static IReadOnlyList<DxfPair> BuildSolidRecord(
        DxfSolid solid,
        Func<DxfPoint, DxfPoint> map,
        string? owner,
        string handle)
    {
        var record = StartEntityRecord("SOLID", handle, owner, solid.Layer, "AcDbTrace");
        AddPoint(record, "10", "20", "30", map(solid.FirstCorner));
        AddPoint(record, "11", "21", "31", map(solid.SecondCorner));
        AddPoint(record, "12", "22", "32", map(solid.ThirdCorner));
        AddPoint(record, "13", "23", "33", map(solid.FourthCorner));
        return record;
    }

    private static IReadOnlyList<DxfPair> BuildPointRecord(
        DxfModelPoint point,
        Func<DxfPoint, DxfPoint> map,
        string? owner,
        string handle)
    {
        var record = StartEntityRecord("POINT", handle, owner, point.Layer, "AcDbPoint");
        AddPoint(record, "10", "20", "30", map(point.Location));
        return record;
    }

    private static List<DxfPair> StartEntityRecord(
        string kind,
        string handle,
        string? owner,
        string? layer,
        string subclass)
    {
        var record = new List<DxfPair>
        {
            new("0", kind),
            new("5", handle)
        };
        if (!string.IsNullOrWhiteSpace(owner))
        {
            record.Add(new DxfPair("330", owner));
        }

        record.Add(new DxfPair("100", "AcDbEntity"));
        record.Add(new DxfPair("8", ResolveLayerName(layer)));
        record.Add(new DxfPair("100", subclass));
        return record;
    }

    private static List<DxfPair> InjectLayerRecords(
        IReadOnlyList<DxfPair> pairs,
        IReadOnlyList<string> layerNames,
        IReadOnlyDictionary<string, IReadOnlyList<DxfPair>> sourceLayerRecords,
        string? layerTableOwner,
        DxfHandleGenerator handleGenerator)
    {
        if (layerNames.Count == 0)
        {
            return pairs.ToList();
        }

        var patchedPairs = new List<DxfPair>(pairs.Count + (layerNames.Count * 14));
        var inLayerTable = false;
        var injected = false;

        for (var index = 0; index < pairs.Count;)
        {
            var pair = pairs[index];
            if (IsEntityStart(pair, "TABLE"))
            {
                var record = ReadRecord(pairs, ref index);
                if (string.Equals(FirstGroupValue(record, "2"), "LAYER", StringComparison.OrdinalIgnoreCase))
                {
                    IncrementFirstIntegerIfPresent(record, "70", layerNames.Count);
                    inLayerTable = true;
                }

                patchedPairs.AddRange(record);
                continue;
            }

            if (inLayerTable && IsEntityStart(pair, "ENDTAB"))
            {
                foreach (var layerName in layerNames)
                {
                    patchedPairs.AddRange(BuildLayerRecord(
                        layerName,
                        handleGenerator.Next(),
                        layerTableOwner,
                        sourceLayerRecords));
                }

                injected = true;
                inLayerTable = false;
                patchedPairs.Add(pair);
                index++;
                continue;
            }

            patchedPairs.Add(pair);
            index++;
        }

        return injected ? patchedPairs : pairs.ToList();
    }

    private static List<DxfPair> InjectSymbolTableRecords(
        IReadOnlyList<DxfPair> pairs,
        string tableName,
        IReadOnlyList<string> recordNames,
        IReadOnlyDictionary<string, IReadOnlyList<DxfPair>> sourceRecords,
        string? tableOwner,
        DxfHandleGenerator handleGenerator)
    {
        if (recordNames.Count == 0)
        {
            return pairs.ToList();
        }

        var addedPairCount = recordNames
            .Select(name => sourceRecords.TryGetValue(name, out var sourceRecord) ? sourceRecord.Count : 0)
            .Sum();
        var patchedPairs = new List<DxfPair>(pairs.Count + addedPairCount);
        var inRequestedTable = false;
        var injected = false;

        for (var index = 0; index < pairs.Count;)
        {
            var pair = pairs[index];
            if (IsEntityStart(pair, "TABLE"))
            {
                var record = ReadRecord(pairs, ref index);
                if (string.Equals(FirstGroupValue(record, "2"), tableName, StringComparison.OrdinalIgnoreCase))
                {
                    IncrementFirstIntegerIfPresent(record, "70", recordNames.Count);
                    inRequestedTable = true;
                }

                patchedPairs.AddRange(record);
                continue;
            }

            if (inRequestedTable && IsEntityStart(pair, "ENDTAB"))
            {
                foreach (var recordName in recordNames)
                {
                    if (sourceRecords.TryGetValue(recordName, out var sourceRecord))
                    {
                        patchedPairs.AddRange(BuildSymbolTableRecordFromSource(
                            tableName,
                            recordName,
                            sourceRecord,
                            handleGenerator.Next(),
                            tableOwner));
                    }
                }

                injected = true;
                inRequestedTable = false;
                patchedPairs.Add(pair);
                index++;
                continue;
            }

            patchedPairs.Add(pair);
            index++;
        }

        return injected ? patchedPairs : pairs.ToList();
    }

    private static IReadOnlyList<DxfPair> BuildSymbolTableRecordFromSource(
        string tableName,
        string recordName,
        IReadOnlyList<DxfPair> sourceRecord,
        string handle,
        string? owner)
    {
        var recordKind = sourceRecord.Count == 0 ? tableName : sourceRecord[0].Value;
        var record = new List<DxfPair> { new("0", recordKind), new("5", handle) };
        if (!string.IsNullOrWhiteSpace(owner))
        {
            record.Add(new DxfPair("330", owner));
        }

        foreach (var pair in sourceRecord.Skip(1))
        {
            if (IsSourceIdentityPair(pair))
            {
                continue;
            }

            record.Add(pair.Code == "2"
                ? pair with { Value = recordName }
                : pair);
        }

        return record;
    }

    private static IReadOnlyList<DxfPair> BuildLayerRecord(
        string layerName,
        string handle,
        string? owner,
        IReadOnlyDictionary<string, IReadOnlyList<DxfPair>> sourceLayerRecords)
    {
        return sourceLayerRecords.TryGetValue(layerName, out var sourceRecord)
            ? BuildLayerRecordFromSource(layerName, sourceRecord, handle, owner)
            : BuildDefaultLayerRecord(layerName, handle, owner);
    }

    private static IReadOnlyList<DxfPair> BuildLayerRecordFromSource(
        string layerName,
        IReadOnlyList<DxfPair> sourceRecord,
        string handle,
        string? owner)
    {
        var record = new List<DxfPair> { new("0", "LAYER"), new("5", handle) };
        if (!string.IsNullOrWhiteSpace(owner))
        {
            record.Add(new DxfPair("330", owner));
        }

        foreach (var pair in sourceRecord.Skip(1))
        {
            if (IsSourceIdentityPair(pair))
            {
                continue;
            }

            record.Add(pair.Code == "2"
                ? pair with { Value = layerName }
                : pair);
        }

        EnsureLayerRecordCompatibilityTail(record);
        return record;
    }

    private static IReadOnlyList<DxfPair> BuildDefaultLayerRecord(string layerName, string handle, string? owner)
    {
        var record = new List<DxfPair>
        {
            new("0", "LAYER"),
            new("5", handle)
        };
        if (!string.IsNullOrWhiteSpace(owner))
        {
            record.Add(new DxfPair("330", owner));
        }

        record.AddRange(
        [
            new DxfPair("100", "AcDbSymbolTableRecord"),
            new DxfPair("100", "AcDbLayerTableRecord"),
            new DxfPair("2", layerName),
            new DxfPair("70", "0"),
            new DxfPair("62", "7"),
            new DxfPair("6", "Continuous"),
            new DxfPair("370", "-3"),
            new DxfPair("390", "F"),
            new DxfPair("347", "98"),
            new DxfPair("348", "0")
        ]);
        return record;
    }

    private static void EnsureLayerRecordCompatibilityTail(List<DxfPair> record)
    {
        if (!record.Any(pair => pair.Code == "370"))
        {
            record.Add(new DxfPair("370", "-3"));
        }

        if (!record.Any(pair => pair.Code == "390"))
        {
            record.Add(new DxfPair("390", "F"));
        }

        if (!record.Any(pair => pair.Code == "347"))
        {
            record.Add(new DxfPair("347", "98"));
        }

        if (!record.Any(pair => pair.Code == "348"))
        {
            record.Add(new DxfPair("348", "0"));
        }
    }

    private static List<DxfPair> InjectEntityRecords(
        IReadOnlyList<DxfPair> pairs,
        IReadOnlyList<IReadOnlyList<DxfPair>> entityRecords)
    {
        if (entityRecords.Count == 0)
        {
            return pairs.ToList();
        }

        var patchedPairs = new List<DxfPair>(pairs.Count + entityRecords.Sum(record => record.Count));
        string? currentSection = null;
        var injected = false;

        for (var index = 0; index < pairs.Count; index++)
        {
            var pair = pairs[index];
            if (IsEntityStart(pair, "SECTION"))
            {
                patchedPairs.Add(pair);
                if (index + 1 < pairs.Count)
                {
                    index++;
                    currentSection = string.Equals(pairs[index].Code, "2", StringComparison.Ordinal)
                        ? pairs[index].Value.Trim()
                        : null;
                    patchedPairs.Add(pairs[index]);
                }

                continue;
            }

            if (string.Equals(currentSection, "ENTITIES", StringComparison.OrdinalIgnoreCase) &&
                IsEntityStart(pair, "ENDSEC"))
            {
                foreach (var record in entityRecords)
                {
                    patchedPairs.AddRange(record);
                }

                injected = true;
                currentSection = null;
                patchedPairs.Add(pair);
                continue;
            }

            if (IsEntityStart(pair, "ENDSEC"))
            {
                currentSection = null;
            }

            patchedPairs.Add(pair);
        }

        return injected ? patchedPairs : pairs.ToList();
    }

    private static List<DxfPair> UpdateHandSeed(IReadOnlyList<DxfPair> pairs, string nextAvailableHandle)
    {
        var patchedPairs = pairs.ToList();
        for (var index = 0; index + 1 < patchedPairs.Count; index++)
        {
            if (string.Equals(patchedPairs[index].Code, "9", StringComparison.Ordinal) &&
                string.Equals(patchedPairs[index].Value.Trim(), "$HANDSEED", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(patchedPairs[index + 1].Code, "5", StringComparison.Ordinal))
            {
                patchedPairs[index + 1] = patchedPairs[index + 1] with { Value = nextAvailableHandle };
                return patchedPairs;
            }
        }

        return patchedPairs;
    }

    // ----- DXF pair helpers --------------------------------------------------------------

    private static List<DxfPair> ReadDxfPairs(string dxfPath)
    {
        var lines = File.ReadAllLines(dxfPath, Encoding.Latin1);
        var pairs = new List<DxfPair>(lines.Length / 2);
        for (var index = 0; index + 1 < lines.Length; index += 2)
        {
            pairs.Add(new DxfPair(lines[index].Trim(), lines[index + 1]));
        }

        return pairs;
    }

    private static void WriteDxfPairs(string dxfPath, IReadOnlyList<DxfPair> pairs)
    {
        using var writer = new StreamWriter(dxfPath, append: false, Encoding.Latin1);
        foreach (var pair in pairs)
        {
            writer.WriteLine(pair.Code);
            writer.WriteLine(pair.Value);
        }
    }

    private static List<DxfPair> ReadRecord(IReadOnlyList<DxfPair> pairs, ref int index)
    {
        var record = new List<DxfPair> { pairs[index] };
        index++;
        while (index < pairs.Count && !string.Equals(pairs[index].Code, "0", StringComparison.Ordinal))
        {
            record.Add(pairs[index]);
            index++;
        }

        return record;
    }

    private static bool IsEntityStart(DxfPair pair, string entityKind)
    {
        return string.Equals(pair.Code, "0", StringComparison.Ordinal) &&
            string.Equals(pair.Value, entityKind, StringComparison.OrdinalIgnoreCase);
    }

    private static string? FirstGroupValue(IReadOnlyList<DxfPair> pairs, string code)
    {
        return pairs
            .Where(pair => string.Equals(pair.Code, code, StringComparison.Ordinal))
            .Select(pair => pair.Value.Trim())
            .FirstOrDefault();
    }

    private static void ReplacePointIfPresent(
        List<DxfPair> record,
        string xCode,
        string yCode,
        IReadOnlyList<AdjustedCompressionStepDto> steps)
    {
        var xIndex = IndexOfGroupCode(record, xCode);
        var yIndex = IndexOfGroupCode(record, yCode);
        if (xIndex < 0 ||
            yIndex < 0 ||
            !TryParseDecimal(record[xIndex].Value, out var x) ||
            !TryParseDecimal(record[yIndex].Value, out var y))
        {
            return;
        }

        var mapped = ApplyCompressionPoint(x, y, steps);
        record[xIndex] = record[xIndex] with { Value = FormatDecimal(mapped.X) };
        record[yIndex] = record[yIndex] with { Value = FormatDecimal(mapped.Y) };
    }

    private static void ReplaceRepeatedPointPairs(
        List<DxfPair> record,
        string xCode,
        string yCode,
        IReadOnlyList<AdjustedCompressionStepDto> steps)
    {
        var xIndices = IndicesOfGroupCode(record, xCode);
        var yIndices = IndicesOfGroupCode(record, yCode);
        var count = Math.Min(xIndices.Count, yIndices.Count);

        for (var index = 0; index < count; index++)
        {
            var xIndex = xIndices[index];
            var yIndex = yIndices[index];
            if (!TryParseDecimal(record[xIndex].Value, out var x) ||
                !TryParseDecimal(record[yIndex].Value, out var y))
            {
                continue;
            }

            var mapped = ApplyCompressionPoint(x, y, steps);
            record[xIndex] = record[xIndex] with { Value = FormatDecimal(mapped.X) };
            record[yIndex] = record[yIndex] with { Value = FormatDecimal(mapped.Y) };
        }
    }

    private static int IndexOfGroupCode(IReadOnlyList<DxfPair> record, string code)
    {
        for (var index = 0; index < record.Count; index++)
        {
            if (string.Equals(record[index].Code, code, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }

    private static IReadOnlyList<int> IndicesOfGroupCode(IReadOnlyList<DxfPair> record, string code)
    {
        var indices = new List<int>();
        for (var index = 0; index < record.Count; index++)
        {
            if (string.Equals(record[index].Code, code, StringComparison.Ordinal))
            {
                indices.Add(index);
            }
        }

        return indices;
    }

    private static void IncrementFirstIntegerIfPresent(List<DxfPair> record, string code, int increment)
    {
        var index = IndexOfGroupCode(record, code);
        if (index < 0 ||
            !int.TryParse(record[index].Value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return;
        }

        record[index] = record[index] with
        {
            Value = (value + increment).ToString(CultureInfo.InvariantCulture)
        };
    }

    private static IReadOnlyList<string> MissingSymbolNames(
        IReadOnlySet<string> existingNames,
        IEnumerable<string> sourceNames)
    {
        return sourceNames
            .Where(name => !existingNames.Contains(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlySet<string> ReadSymbolTableNames(IReadOnlyList<DxfPair> pairs, string tableName)
        => ReadSymbolTableRecordMap(pairs, tableName).Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static IReadOnlyDictionary<string, IReadOnlyList<DxfPair>> ReadSymbolTableRecordMap(
        IReadOnlyList<DxfPair> pairs,
        string tableName)
    {
        var records = new Dictionary<string, IReadOnlyList<DxfPair>>(StringComparer.OrdinalIgnoreCase);
        var inRequestedTable = false;

        for (var index = 0; index < pairs.Count;)
        {
            var pair = pairs[index];
            if (IsEntityStart(pair, "TABLE"))
            {
                var tableHeader = ReadRecord(pairs, ref index);
                inRequestedTable = string.Equals(
                    FirstGroupValue(tableHeader, "2"),
                    tableName,
                    StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (inRequestedTable && IsEntityStart(pair, "ENDTAB"))
            {
                inRequestedTable = false;
                index++;
                continue;
            }

            if (inRequestedTable && IsEntityStart(pair, tableName))
            {
                var record = ReadRecord(pairs, ref index);
                var recordName = FirstGroupValue(record, "2");
                if (!string.IsNullOrWhiteSpace(recordName))
                {
                    records[recordName] = record;
                }

                continue;
            }

            index++;
        }

        return records;
    }

    private static IReadOnlyList<IReadOnlyList<DxfPair>> ReadEntityRecordsFromEntitiesSection(IReadOnlyList<DxfPair> pairs)
    {
        var records = new List<IReadOnlyList<DxfPair>>();
        string? currentSection = null;

        for (var index = 0; index < pairs.Count;)
        {
            var pair = pairs[index];
            if (IsEntityStart(pair, "SECTION"))
            {
                index++;
                currentSection = index < pairs.Count && string.Equals(pairs[index].Code, "2", StringComparison.Ordinal)
                    ? pairs[index].Value.Trim()
                    : null;
                index++;
                continue;
            }

            if (IsEntityStart(pair, "ENDSEC"))
            {
                currentSection = null;
                index++;
                continue;
            }

            if (string.Equals(currentSection, "ENTITIES", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(pair.Code, "0", StringComparison.Ordinal))
            {
                records.Add(ReadRecord(pairs, ref index));
                continue;
            }

            index++;
        }

        return records;
    }

    private static string? ResolveTableHandle(IReadOnlyList<DxfPair> pairs, string tableName)
    {
        for (var index = 0; index < pairs.Count;)
        {
            if (!IsEntityStart(pairs[index], "TABLE"))
            {
                index++;
                continue;
            }

            var record = ReadRecord(pairs, ref index);
            if (string.Equals(FirstGroupValue(record, "2"), tableName, StringComparison.OrdinalIgnoreCase))
            {
                return FirstGroupValue(record, "5");
            }
        }

        return null;
    }

    private static string? ResolveModelSpaceOwnerHandle(IReadOnlyList<DxfPair> pairs)
    {
        for (var index = 0; index < pairs.Count;)
        {
            if (!IsEntityStart(pairs[index], "BLOCK_RECORD"))
            {
                index++;
                continue;
            }

            var record = ReadRecord(pairs, ref index);
            var blockName = FirstGroupValue(record, "2");
            if (string.Equals(blockName, "*MODEL_SPACE", StringComparison.OrdinalIgnoreCase))
            {
                return FirstGroupValue(record, "5");
            }
        }

        for (var index = 0; index < pairs.Count;)
        {
            if (!string.Equals(pairs[index].Code, "0", StringComparison.Ordinal))
            {
                index++;
                continue;
            }

            var record = ReadRecord(pairs, ref index);
            var owner = FirstGroupValue(record, "330");
            if (!string.IsNullOrWhiteSpace(owner))
            {
                return owner;
            }
        }

        return null;
    }

    private static void AddPoint(
        ICollection<DxfPair> record,
        string xCode,
        string yCode,
        string zCode,
        DxfPoint point)
    {
        record.Add(new DxfPair(xCode, FormatDouble(point.X)));
        record.Add(new DxfPair(yCode, FormatDouble(point.Y)));
        record.Add(new DxfPair(zCode, FormatDouble(point.Z)));
    }

    private static string ResolveLayerName(string? layerName)
        => string.IsNullOrWhiteSpace(layerName) ? "0" : layerName;

    private static bool TryParseDecimal(string value, out decimal result)
        => decimal.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result);

    private static string FormatDecimal(decimal value)
        => value.ToString("0.###############", CultureInfo.InvariantCulture);

    private static string FormatDouble(double value)
        => value.ToString("0.###############", CultureInfo.InvariantCulture);

    private sealed record SitePlanInjection(
        IReadOnlyList<IReadOnlyList<DxfPair>> EntityRecords,
        IReadOnlySet<string> LayerNames,
        IReadOnlyDictionary<string, IReadOnlyList<DxfPair>> LayerRecords,
        IReadOnlyDictionary<string, IReadOnlyList<DxfPair>> LineTypeRecords,
        IReadOnlyDictionary<string, IReadOnlyList<DxfPair>> StyleRecords);

    private sealed class DxfHandleGenerator
    {
        private long nextHandle;

        public DxfHandleGenerator(IReadOnlyList<DxfPair> pairs)
        {
            nextHandle = pairs
                .Where(pair => string.Equals(pair.Code, "5", StringComparison.Ordinal))
                .Select(pair => pair.Value.Trim())
                .Select(value => long.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed)
                    ? parsed
                    : 0L)
                .DefaultIfEmpty(0L)
                .Max() + 1L;
        }

        public string Next()
        {
            var handle = nextHandle;
            nextHandle++;
            return handle.ToString("X", CultureInfo.InvariantCulture);
        }

        public string NextAvailableHandle => nextHandle.ToString("X", CultureInfo.InvariantCulture);
    }
}
