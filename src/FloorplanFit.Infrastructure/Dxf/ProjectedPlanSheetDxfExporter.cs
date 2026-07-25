using System.Globalization;
using System.Text;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;
using FloorplanFit.Application.PlanSets.Projection;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;
using FloorplanFit.Infrastructure.Geometry;

namespace FloorplanFit.Infrastructure.Dxf;

public sealed class ProjectedPlanSheetDxfExporter : IProjectedPlanSheetExporter
{
    private const decimal CoordinateTolerance = 0.01m;

    private const string BinaryDxfSentinel = "AutoCAD Binary DXF\r\n\u001A\0";

    public async Task ExportAsync(
        string sourceFilePath,
        string outputFilePath,
        SheetAdjustmentProjectionTransform transform,
        CancellationToken cancellationToken)
    {
        await ExportWithAuditAsync(sourceFilePath, outputFilePath, transform, recipe: null, cancellationToken);
    }

    public async Task ExportAsync(
        string sourceFilePath,
        string outputFilePath,
        SheetAdjustmentProjectionTransform transform,
        ProjectedPlanSheetExportRecipe? recipe,
        CancellationToken cancellationToken)
    {
        await ExportWithAuditAsync(sourceFilePath, outputFilePath, transform, recipe, cancellationToken);
    }

    public Task<ProjectedPlanSheetExportAuditDto?> ExportWithAuditAsync(
        string sourceFilePath,
        string outputFilePath,
        SheetAdjustmentProjectionTransform transform,
        ProjectedPlanSheetExportRecipe? recipe,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFilePath);
        ArgumentNullException.ThrowIfNull(transform);

        var outputDirectory = Path.GetDirectoryName(outputFilePath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        if (!string.IsNullOrWhiteSpace(recipe?.CanonicalFloorPlanExportPath))
        {
            return ExportCanonicalBaseWithElectricalOverlay(
                sourceFilePath,
                outputFilePath,
                transform,
                recipe,
                cancellationToken);
        }

        if (IsBinaryDxf(sourceFilePath))
        {
            var binaryPairs = ReadBinaryDxfPairs(sourceFilePath, out var usesLegacyGroupCodes);
            var projectedBinaryPairs = ProjectCoordinatePairs(
                binaryPairs,
                transform,
                recipe,
                cancellationToken,
                out var operationAudits,
                out var outlineAudit);
            WriteBinaryDxfPairs(outputFilePath, projectedBinaryPairs, usesLegacyGroupCodes);
            return Task.FromResult<ProjectedPlanSheetExportAuditDto?>(
                BuildExportAudit(binaryPairs, projectedBinaryPairs, operationAudits, outlineAudit, outputFilePath));
        }

        var pairs = ReadDxfPairs(sourceFilePath);
        var projectedPairs = ProjectCoordinatePairs(
            pairs,
            transform,
            recipe,
            cancellationToken,
            out var textOperationAudits,
            out var textOutlineAudit);

        WriteDxfPairs(outputFilePath, projectedPairs);
        return Task.FromResult<ProjectedPlanSheetExportAuditDto?>(
            BuildExportAudit(pairs, projectedPairs, textOperationAudits, textOutlineAudit, outputFilePath));
    }

    private static Task<ProjectedPlanSheetExportAuditDto?> ExportCanonicalBaseWithElectricalOverlay(
        string electricalSourcePath,
        string outputFilePath,
        SheetAdjustmentProjectionTransform transform,
        ProjectedPlanSheetExportRecipe recipe,
        CancellationToken cancellationToken)
    {
        try
        {
            var canonicalPath = recipe.CanonicalFloorPlanExportPath!;
            if (!File.Exists(canonicalPath))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Adjusted canonical FloorPlan DXF was not found at '{canonicalPath}'.");
            }

            if (Path.GetFullPath(canonicalPath).Equals(Path.GetFullPath(outputFilePath), StringComparison.OrdinalIgnoreCase))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    "Electrical output must not overwrite the adjusted canonical FloorPlan DXF.");
            }

            var electricalPairs = ReadAnyDxfPairs(electricalSourcePath, out _);
            var projectedElectricalPairs = ProjectCoordinatePairs(
                electricalPairs,
                transform,
                recipe,
                cancellationToken,
                out var operationAudits,
                out var outlineAudit);
            var canonicalPairs = ReadAnyDxfPairs(canonicalPath, out var canonicalFormat);
            var composedPairs = ComposeCanonicalArchitectureWithElectricalOverlay(
                canonicalPairs,
                projectedElectricalPairs,
                electricalPairs,
                recipe,
                cancellationToken);

            WriteCompositionAtomically(outputFilePath, composedPairs, canonicalFormat);
            return Task.FromResult<ProjectedPlanSheetExportAuditDto?>(
                BuildExportAudit(electricalPairs, composedPairs, operationAudits, outlineAudit, outputFilePath));
        }
        catch (ProjectedPlanSheetManualReviewRequiredException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            IOException or
            UnauthorizedAccessException or
            InvalidDataException or
            FormatException or
            NotSupportedException or
            OverflowException)
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                $"Electrical canonical-base composition failed closed: {exception.Message}");
        }
    }

    private static IReadOnlyList<DxfPair> ReadBinaryDxfPairs(
        string dxfPath,
        out bool usesLegacyGroupCodes)
    {
        using var reader = new BinaryReader(File.OpenRead(dxfPath), Encoding.Latin1);
        var sentinel = Encoding.ASCII.GetBytes(BinaryDxfSentinel);
        var actualSentinel = reader.ReadBytes(sentinel.Length);
        if (actualSentinel.Length != sentinel.Length || !actualSentinel.SequenceEqual(sentinel))
        {
            throw new InvalidDataException("The DXF file is not an AutoCAD binary DXF.");
        }

        var pairs = new List<DxfPair>();
        usesLegacyGroupCodes = DetectLegacyBinaryDxfGroupCodes(reader);
        var firstCode = ReadBinaryDxfGroupCode(reader, usesLegacyGroupCodes);
        var firstValue = ReadBinaryDxfValue(reader, firstCode);
        if (firstCode != 0 || !string.Equals(firstValue, "SECTION", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The binary DXF does not start with the required 0/SECTION pair.");
        }

        pairs.Add(new DxfPair(firstCode.ToString(CultureInfo.InvariantCulture), firstValue));
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var code = ReadBinaryDxfGroupCode(reader, usesLegacyGroupCodes);
            var value = ReadBinaryDxfValue(reader, code);
            pairs.Add(new DxfPair(code.ToString(CultureInfo.InvariantCulture), value));
            if (code == 0 && string.Equals(value, "EOF", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
        }

        var lastPair = pairs[^1];
        if (lastPair.Code != "0" || !string.Equals(lastPair.Value, "EOF", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The binary DXF is missing the terminal 0/EOF pair.");
        }

        return pairs;
    }

    private static bool DetectLegacyBinaryDxfGroupCodes(BinaryReader reader)
    {
        var firstPairPosition = reader.BaseStream.Position;
        if (reader.BaseStream.Length - firstPairPosition < 2)
        {
            throw new InvalidDataException("The binary DXF is missing the required 0/SECTION pair.");
        }

        var firstCodeByte = reader.ReadByte();
        var secondByte = reader.ReadByte();
        reader.BaseStream.Position = firstPairPosition;
        if (firstCodeByte != 0)
        {
            throw new InvalidDataException("The binary DXF does not start with the required 0/SECTION pair.");
        }

        return secondByte != 0;
    }

    private static short ReadBinaryDxfGroupCode(BinaryReader reader, bool usesLegacyGroupCodes)
    {
        if (!usesLegacyGroupCodes)
        {
            return reader.ReadInt16();
        }

        var code = reader.ReadByte();
        return code == byte.MaxValue ? reader.ReadInt16() : (short)code;
    }

    private static string ReadBinaryDxfValue(BinaryReader reader, short code)
    {
        return GetBinaryDxfValueKind(code) switch
        {
            BinaryDxfValueKind.String => ReadNullTerminatedString(reader),
            BinaryDxfValueKind.Double => reader.ReadDouble().ToString("G17", CultureInfo.InvariantCulture),
            BinaryDxfValueKind.Int16 => reader.ReadInt16().ToString(CultureInfo.InvariantCulture),
            BinaryDxfValueKind.Int32 => reader.ReadInt32().ToString(CultureInfo.InvariantCulture),
            BinaryDxfValueKind.Int64 => reader.ReadInt64().ToString(CultureInfo.InvariantCulture),
            BinaryDxfValueKind.Boolean => reader.ReadByte() == 0 ? "0" : "1",
            BinaryDxfValueKind.BinaryChunk => ReadBinaryChunk(reader),
            _ => throw new InvalidDataException($"Unsupported binary DXF group code {code}.")
        };
    }

    private static string ReadNullTerminatedString(BinaryReader reader)
    {
        var bytes = new List<byte>();
        while (true)
        {
            var value = reader.ReadByte();
            if (value == 0)
            {
                return Encoding.Latin1.GetString(bytes.ToArray());
            }

            bytes.Add(value);
        }
    }

    private static string ReadBinaryChunk(BinaryReader reader)
    {
        var length = reader.ReadByte();
        var bytes = reader.ReadBytes(length);
        if (bytes.Length != length)
        {
            throw new InvalidDataException("The binary DXF chunk is truncated.");
        }

        return Convert.ToHexString(bytes);
    }

    private static void WriteBinaryDxfPairs(
        string dxfPath,
        IReadOnlyList<DxfPair> pairs,
        bool usesLegacyGroupCodes)
    {
        using var writer = new BinaryWriter(File.Create(dxfPath), Encoding.Latin1);
        writer.Write(Encoding.ASCII.GetBytes(BinaryDxfSentinel));

        foreach (var pair in pairs)
        {
            var code = short.Parse(pair.Code, CultureInfo.InvariantCulture);
            WriteBinaryDxfGroupCode(writer, code, usesLegacyGroupCodes);
            WriteBinaryDxfValue(writer, code, pair.Value);
        }
    }

    private static void WriteBinaryDxfGroupCode(
        BinaryWriter writer,
        short code,
        bool usesLegacyGroupCodes)
    {
        if (!usesLegacyGroupCodes)
        {
            writer.Write(code);
            return;
        }

        if (code < byte.MaxValue)
        {
            writer.Write((byte)code);
            return;
        }

        writer.Write(byte.MaxValue);
        writer.Write(code);
    }

    private static void WriteBinaryDxfValue(BinaryWriter writer, short code, string value)
    {
        switch (GetBinaryDxfValueKind(code))
        {
            case BinaryDxfValueKind.String:
                writer.Write(Encoding.Latin1.GetBytes(value));
                writer.Write((byte)0);
                break;

            case BinaryDxfValueKind.Double:
                writer.Write(double.Parse(value, CultureInfo.InvariantCulture));
                break;

            case BinaryDxfValueKind.Int16:
                writer.Write(short.Parse(value, CultureInfo.InvariantCulture));
                break;

            case BinaryDxfValueKind.Int32:
                writer.Write(int.Parse(value, CultureInfo.InvariantCulture));
                break;

            case BinaryDxfValueKind.Int64:
                writer.Write(long.Parse(value, CultureInfo.InvariantCulture));
                break;

            case BinaryDxfValueKind.Boolean:
                writer.Write(value.Trim() == "0" ? (byte)0 : (byte)1);
                break;

            case BinaryDxfValueKind.BinaryChunk:
                var bytes = Convert.FromHexString(value.Trim());
                if (bytes.Length > byte.MaxValue)
                {
                    throw new InvalidDataException("Binary DXF chunk is too large.");
                }

                writer.Write((byte)bytes.Length);
                writer.Write(bytes);
                break;
        }
    }

    private static BinaryDxfValueKind GetBinaryDxfValueKind(short code)
    {
        return code switch
        {
            >= 0 and <= 9 => BinaryDxfValueKind.String,
            >= 10 and <= 59 => BinaryDxfValueKind.Double,
            >= 60 and <= 79 => BinaryDxfValueKind.Int16,
            >= 90 and <= 99 => BinaryDxfValueKind.Int32,
            >= 100 and <= 109 => BinaryDxfValueKind.String,
            >= 110 and <= 149 => BinaryDxfValueKind.Double,
            >= 160 and <= 169 => BinaryDxfValueKind.Int64,
            >= 170 and <= 179 => BinaryDxfValueKind.Int16,
            >= 210 and <= 239 => BinaryDxfValueKind.Double,
            >= 270 and <= 289 => BinaryDxfValueKind.Int16,
            >= 290 and <= 299 => BinaryDxfValueKind.Boolean,
            >= 300 and <= 309 => BinaryDxfValueKind.String,
            >= 310 and <= 319 => BinaryDxfValueKind.BinaryChunk,
            >= 320 and <= 369 => BinaryDxfValueKind.String,
            >= 370 and <= 389 => BinaryDxfValueKind.Int16,
            >= 390 and <= 399 => BinaryDxfValueKind.String,
            >= 400 and <= 409 => BinaryDxfValueKind.Int16,
            >= 410 and <= 419 => BinaryDxfValueKind.String,
            >= 420 and <= 429 => BinaryDxfValueKind.Int32,
            >= 430 and <= 439 => BinaryDxfValueKind.String,
            >= 440 and <= 459 => BinaryDxfValueKind.Int32,
            >= 460 and <= 469 => BinaryDxfValueKind.Double,
            >= 470 and <= 481 => BinaryDxfValueKind.String,
            999 => BinaryDxfValueKind.String,
            >= 1000 and <= 1003 => BinaryDxfValueKind.String,
            1004 => BinaryDxfValueKind.BinaryChunk,
            1005 => BinaryDxfValueKind.String,
            >= 1010 and <= 1059 => BinaryDxfValueKind.Double,
            >= 1060 and <= 1070 => BinaryDxfValueKind.Int16,
            1071 => BinaryDxfValueKind.Int32,
            _ => throw new InvalidDataException($"Unsupported binary DXF group code {code}.")
        };
    }

    private static IReadOnlyList<DxfPair> ReadAnyDxfPairs(
        string dxfPath,
        out DxfStorageFormat storageFormat)
    {
        if (IsBinaryDxf(dxfPath))
        {
            var pairs = ReadBinaryDxfPairs(dxfPath, out var usesLegacyGroupCodes);
            storageFormat = new DxfStorageFormat(
                IsBinary: true,
                UsesLegacyGroupCodes: usesLegacyGroupCodes);
            return pairs;
        }

        storageFormat = new DxfStorageFormat(IsBinary: false, UsesLegacyGroupCodes: false);
        return ReadDxfPairs(dxfPath);
    }

    private static void WriteAnyDxfPairs(
        string dxfPath,
        IReadOnlyList<DxfPair> pairs,
        DxfStorageFormat storageFormat)
    {
        if (storageFormat.IsBinary)
        {
            WriteBinaryDxfPairs(dxfPath, pairs, storageFormat.UsesLegacyGroupCodes);
            return;
        }

        WriteDxfPairs(dxfPath, pairs);
    }

    private static void WriteCompositionAtomically(
        string outputFilePath,
        IReadOnlyList<DxfPair> pairs,
        DxfStorageFormat storageFormat)
    {
        var outputDirectory = Path.GetDirectoryName(outputFilePath) ?? Directory.GetCurrentDirectory();
        var temporaryPath = Path.Combine(
            outputDirectory,
            $".{Path.GetFileName(outputFilePath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            WriteAnyDxfPairs(temporaryPath, pairs, storageFormat);
            File.Move(temporaryPath, outputFilePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static List<DxfPair> ComposeCanonicalArchitectureWithElectricalOverlay(
        IReadOnlyList<DxfPair> canonicalPairs,
        IReadOnlyList<DxfPair> projectedElectricalPairs,
        IReadOnlyList<DxfPair> sourceElectricalPairs,
        ProjectedPlanSheetExportRecipe recipe,
        CancellationToken cancellationToken)
    {
        var electricalEntityRecords = ReadSectionRecords(projectedElectricalPairs, "ENTITIES");
        var overlayRecords = SelectElectricalOverlayRecords(electricalEntityRecords, cancellationToken);
        if (overlayRecords.Count == 0)
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                "Electrical overlay composition found no supported entities on ELECTRICAL layers.");
        }

        overlayRecords = ReconcileElectricalOverlayDiscipline(
            overlayRecords,
            sourceElectricalPairs,
            recipe,
            cancellationToken);

        var sourceBlocks = ReadBlockDefinitions(projectedElectricalPairs);
        var sourceObjectsByHandle = ReadObjectRecordsByHandle(projectedElectricalPairs);
        var referencedBlockNames = CollectReferencedBlockClosure(overlayRecords, sourceBlocks);
        var canonicalBlockNames = ReadBlockDefinitions(canonicalPairs).Keys
            .Concat(ReadSymbolTableRecordMap(canonicalPairs, "BLOCK_RECORD").Keys)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var blockNameMap = BuildElectricalBlockNameMap(referencedBlockNames, canonicalBlockNames);

        var importedBlockDefinitions = referencedBlockNames
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(name => new DxfBlockDefinition(
                blockNameMap[name],
                sourceBlocks[name].Records
                    .Select(record => NormalizeImportedAssociationMetadata(record, sourceObjectsByHandle))
                    .Select(record => RewriteBlockNames(record, blockNameMap))
                    .ToArray()))
            .ToArray();
        var rewrittenOverlayRecords = overlayRecords
            .Select(record => NormalizeImportedAssociationMetadata(record, sourceObjectsByHandle))
            .Select(record => RewriteBlockNames(record, blockNameMap))
            .ToArray();

        var resourceSourceRecords = rewrittenOverlayRecords
            .Concat(importedBlockDefinitions.SelectMany(block => block.Records))
            .ToArray();
        var missingTableRecords = CollectMissingElectricalSymbolTableRecords(
            canonicalPairs,
            projectedElectricalPairs,
            resourceSourceRecords);

        var canonicalUsesHandles = canonicalPairs.Any(pair => IsHandleDefinitionCode(pair.Code));
        if (!canonicalUsesHandles &&
            resourceSourceRecords.Any(record => record.Any(pair => IsHandleDefinitionCode(pair.Code))))
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                "Electrical overlay uses modern DXF handles but the adjusted canonical FloorPlan is handle-less; composition cannot be proven safe.");
        }

        var importedTableRecords = new Dictionary<string, IReadOnlyList<IReadOnlyList<DxfPair>>>(
            StringComparer.OrdinalIgnoreCase);
        IReadOnlyList<DxfBlockDefinition> finalBlocks;
        IReadOnlyList<IReadOnlyList<DxfPair>> finalOverlayRecords;
        DxfCompositionHandleAllocator? handleAllocator = null;

        if (!canonicalUsesHandles)
        {
            foreach (var (tableName, records) in missingTableRecords)
            {
                importedTableRecords[tableName] = records
                    .Select(SanitizeHandlelessSymbolTableRecord)
                    .ToArray();
            }

            finalBlocks = importedBlockDefinitions;
            finalOverlayRecords = rewrittenOverlayRecords;
        }
        else
        {
            var canonicalModelSpaceHandle = ResolveBlockRecordHandle(canonicalPairs, "*Model_Space");
            if (string.IsNullOrWhiteSpace(canonicalModelSpaceHandle))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    "Adjusted canonical FloorPlan has no resolvable *Model_Space owner handle.");
            }

            var sourceModelSpaceHandle = ResolveBlockRecordHandle(projectedElectricalPairs, "*Model_Space");
            handleAllocator = new DxfCompositionHandleAllocator(canonicalPairs);
            var importedBlockRecords = BuildMissingBlockRecordTableRecords(
                canonicalPairs,
                projectedElectricalPairs,
                referencedBlockNames,
                blockNameMap);

            var allImportedRecords = missingTableRecords.Values
                .SelectMany(records => records)
                .Concat(importedBlockRecords.Values)
                .Concat(importedBlockDefinitions.SelectMany(block => block.Records))
                .Concat(rewrittenOverlayRecords)
                .ToArray();
            var handleMap = BuildImportedHandleMap(allImportedRecords, handleAllocator);

            foreach (var (tableName, records) in missingTableRecords)
            {
                if (!string.Equals(tableName, "LAYER", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(tableName, "STYLE", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ProjectedPlanSheetManualReviewRequiredException(
                        $"Electrical symbol-table import is not supported for {tableName}; the canonical FloorPlan must already provide that resource.");
                }

                var tableOwner = ResolveTableHandle(canonicalPairs, tableName);
                if (string.IsNullOrWhiteSpace(tableOwner))
                {
                    throw new ProjectedPlanSheetManualReviewRequiredException(
                        $"Adjusted canonical FloorPlan is missing the {tableName} table owner required by the Electrical overlay.");
                }

                var preparedRecords = records
                    .Select(record => string.Equals(tableName, "LAYER", StringComparison.OrdinalIgnoreCase)
                        ? PrepareImportedLayerRecord(canonicalPairs, record)
                        : PrepareImportedStyleRecord(record))
                    .ToArray();
                var allowedCanonicalReferences = preparedRecords
                    .SelectMany(record => record)
                    .Where(pair => IsHandleReferenceCode(pair.Code) && pair.Code != "330")
                    .Select(pair => pair.Value.Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                importedTableRecords[tableName] = preparedRecords
                    .Select(record => TransformImportedRecord(
                        record,
                        tableOwner,
                        sourceModelSpaceHandle,
                        canonicalModelSpaceHandle,
                        handleMap,
                        handleAllocator,
                        allowedCanonicalReferences))
                    .ToArray();
            }

            var blockRecordTableOwner = ResolveTableHandle(canonicalPairs, "BLOCK_RECORD");
            if (importedBlockRecords.Count > 0 && string.IsNullOrWhiteSpace(blockRecordTableOwner))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    "Adjusted canonical FloorPlan is missing the BLOCK_RECORD table required by Electrical symbols.");
            }

            if (importedBlockRecords.Count > 0)
            {
                importedTableRecords["BLOCK_RECORD"] = importedBlockRecords
                    .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(item => TransformImportedRecord(
                        item.Value,
                        blockRecordTableOwner,
                        sourceModelSpaceHandle,
                        canonicalModelSpaceHandle,
                        handleMap,
                        handleAllocator))
                    .ToArray();
            }

            var finalBlockList = new List<DxfBlockDefinition>(importedBlockDefinitions.Length);
            foreach (var block in importedBlockDefinitions)
            {
                var sourceBlockName = blockNameMap
                    .Single(item => string.Equals(item.Value, block.Name, StringComparison.OrdinalIgnoreCase))
                    .Key;
                if (!importedBlockRecords.TryGetValue(sourceBlockName, out var blockRecord))
                {
                    throw new ProjectedPlanSheetManualReviewRequiredException(
                        $"Electrical block '{sourceBlockName}' has no BLOCK_RECORD resource.");
                }

                var sourceBlockRecordHandle = FirstPairValue(blockRecord, "5");
                if (string.IsNullOrWhiteSpace(sourceBlockRecordHandle) ||
                    !handleMap.TryGetValue(sourceBlockRecordHandle, out var destinationBlockOwner))
                {
                    throw new ProjectedPlanSheetManualReviewRequiredException(
                        $"Electrical block '{sourceBlockName}' has no remappable owner handle.");
                }

                finalBlockList.Add(new DxfBlockDefinition(
                    block.Name,
                    TransformImportedRecordSequence(
                        block.Records,
                        destinationBlockOwner,
                        sourceModelSpaceHandle,
                        canonicalModelSpaceHandle,
                        handleMap,
                        handleAllocator)));
            }

            finalBlocks = finalBlockList;
            finalOverlayRecords = TransformImportedRecordSequence(
                rewrittenOverlayRecords,
                canonicalModelSpaceHandle,
                sourceModelSpaceHandle,
                canonicalModelSpaceHandle,
                handleMap,
                handleAllocator);
        }

        var composedPairs = canonicalPairs.ToList();
        foreach (var (tableName, records) in importedTableRecords)
        {
            composedPairs = InjectTableRecords(composedPairs, tableName, records);
        }

        composedPairs = InjectBlockDefinitions(composedPairs, finalBlocks);
        composedPairs = InjectSectionRecords(composedPairs, "ENTITIES", finalOverlayRecords);
        if (handleAllocator is not null)
        {
            composedPairs = UpdateCompositionHandSeed(composedPairs, handleAllocator.NextAvailableHandle);
        }

        return composedPairs;
    }

    private static IReadOnlyList<IReadOnlyList<DxfPair>> SelectElectricalOverlayRecords(
        IReadOnlyList<IReadOnlyList<DxfPair>> entityRecords,
        CancellationToken cancellationToken)
    {
        var selected = new List<IReadOnlyList<DxfPair>>();
        for (var index = 0; index < entityRecords.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var record = entityRecords[index];
            var entityType = RecordType(record);
            var layerName = FirstPairValue(record, "8") ?? string.Empty;
            var isOverlay = IsElectricalOverlayLayer(layerName);
            if (isOverlay && !IsSupportedElectricalOverlayEntity(entityType))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Electrical overlay entity type {entityType} on layer '{layerName}' is not supported by safe composition.");
            }

            var hasOwnedSequence = HasOwnedEntitySequence(entityRecords, index, entityType);
            if (isOverlay)
            {
                selected.Add(record);
            }

            if (!hasOwnedSequence)
            {
                continue;
            }

            while (index + 1 < entityRecords.Count)
            {
                var child = entityRecords[++index];
                var childType = RecordType(child);
                var expectedChild = entityType == "INSERT" ? "ATTRIB" : "VERTEX";
                if (childType != expectedChild && childType != "SEQEND")
                {
                    throw new ProjectedPlanSheetManualReviewRequiredException(
                        $"Electrical {entityType} sequence contains unexpected child {childType}.");
                }

                if (isOverlay)
                {
                    selected.Add(child);
                }

                if (childType == "SEQEND")
                {
                    break;
                }
            }
        }

        return selected;
    }

    private static bool HasOwnedEntitySequence(
        IReadOnlyList<IReadOnlyList<DxfPair>> records,
        int parentIndex,
        string entityType)
    {
        if ((entityType != "INSERT" && entityType != "POLYLINE") || parentIndex + 1 >= records.Count)
        {
            return false;
        }

        var nextType = RecordType(records[parentIndex + 1]);
        return entityType == "INSERT" ? nextType == "ATTRIB" : nextType == "VERTEX";
    }

    private static bool IsElectricalOverlayLayer(string layerName)
        => layerName.StartsWith("ELECTRICAL", StringComparison.OrdinalIgnoreCase) &&
           !string.Equals(layerName, "ELECTRICAL WALLS", StringComparison.OrdinalIgnoreCase);

    private static bool IsSupportedElectricalOverlayEntity(string entityType)
        => entityType is
            "LINE" or
            "CIRCLE" or
            "ARC" or
            "ELLIPSE" or
            "LWPOLYLINE" or
            "POLYLINE" or
            "VERTEX" or
            "SEQEND" or
            "SPLINE" or
            "HATCH" or
            "INSERT" or
            "ATTRIB" or
            "TEXT" or
            "MTEXT" or
            "DIMENSION" or
            "POINT" or
            "SOLID" or
            "3DFACE";

    private static IReadOnlyList<IReadOnlyList<DxfPair>> ReconcileElectricalOverlayDiscipline(
        IReadOnlyList<IReadOnlyList<DxfPair>> overlayRecords,
        IReadOnlyList<DxfPair> sourceElectricalPairs,
        ProjectedPlanSheetExportRecipe recipe,
        CancellationToken cancellationToken)
    {
        if (recipe.CanonicalRecipe.Operations.Count == 0 && recipe.CanonicalRecipe.StretchActions.Count == 0)
        {
            // Affine-only composition moves the whole sheet rigidly, so devices and wires keep
            // size, count, and connectivity by construction; discipline reconciliation gates
            // only locally deforming recipes.
            return overlayRecords;
        }

        var reconciliation = recipe.OverlayReconciliation
            ?? throw new ProjectedPlanSheetManualReviewRequiredException(
                "Electrical overlay reconciliation evidence is required when the canonical recipe deforms locally; " +
                "commission explicit wall/room device hosts and wire routes before automatic export.");

        var deviceBindings = new Dictionary<string, ElectricalDeviceHostBinding>(StringComparer.OrdinalIgnoreCase);
        foreach (var binding in reconciliation.DeviceBindings)
        {
            ValidateDeviceHostBinding(binding, recipe.CanonicalRecipe);
            if (!deviceBindings.TryAdd(binding.DeviceHandle.Trim(), binding))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Electrical overlay reconciliation declares duplicate device bindings for handle '{binding.DeviceHandle}'.");
            }
        }

        var wireRoutes = new Dictionary<string, ElectricalWireRouteBinding>(StringComparer.OrdinalIgnoreCase);
        foreach (var route in reconciliation.WireRoutes)
        {
            if (string.IsNullOrWhiteSpace(route.CarrierHandle))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    "Electrical wire route evidence is missing its carrier handle.");
            }

            var startHandle = route.StartDeviceHandle?.Trim() ?? string.Empty;
            var endHandle = route.EndDeviceHandle?.Trim() ?? string.Empty;
            if (!deviceBindings.ContainsKey(startHandle) || !deviceBindings.ContainsKey(endHandle))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"UnsupportedWireRoute: route carrier '{route.CarrierHandle}' references endpoint devices " +
                    $"'{route.StartDeviceHandle}'/'{route.EndDeviceHandle}' without commissioned host bindings.");
            }

            if (!wireRoutes.TryAdd(route.CarrierHandle.Trim(), route))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Electrical overlay reconciliation declares duplicate wire routes for carrier '{route.CarrierHandle}'.");
            }
        }

        var staticCarrierHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var staticHandle in reconciliation.StaticCarrierHandles)
        {
            if (string.IsNullOrWhiteSpace(staticHandle) || !staticCarrierHandles.Add(staticHandle.Trim()))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Electrical overlay reconciliation declares a blank or duplicate static carrier handle '{staticHandle}'.");
            }

            if (wireRoutes.ContainsKey(staticHandle.Trim()) || deviceBindings.ContainsKey(staticHandle.Trim()))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Electrical overlay reconciliation declares '{staticHandle}' both as static and as a device/route; the evidence is contradictory.");
            }
        }

        var sourceRecordsByHandle = IndexEntityRecordsByHandle(sourceElectricalPairs);
        var deviceFinalPoints = new Dictionary<string, (double X, double Y)>(StringComparer.OrdinalIgnoreCase);
        var deviceOverridesByIndex = new Dictionary<int, IReadOnlyList<DxfPair>>();
        var seenDeviceHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenCarrierHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < overlayRecords.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var record = overlayRecords[index];
            if (RecordType(record) != "INSERT")
            {
                continue;
            }

            var handle = FirstPairValue(record, "5")?.Trim();
            var blockName = FirstPairValue(record, "2") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(handle))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Electrical overlay device INSERT '{blockName}' has no handle identity, so commissioned host reconciliation is impossible.");
            }

            seenDeviceHandles.Add(handle);
            if (!deviceBindings.TryGetValue(handle, out var binding))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Electrical overlay device INSERT '{handle}' ({blockName}) has no commissioned wall/room host binding; " +
                    "the house remains Setup required for automatic Electrical export.");
            }

            if (!string.Equals(blockName, binding.BlockName, StringComparison.OrdinalIgnoreCase))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Electrical device binding '{handle}' expects block '{binding.BlockName}' but the overlay INSERT is '{blockName}'; the evidence is stale.");
            }

            if (!sourceRecordsByHandle.TryGetValue(handle, out var sourceRecord))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Electrical device binding '{handle}' has no matching source entity; the evidence is stale.");
            }

            var basePoint = ReadRecordCoordinate(sourceRecord, "10", "20", handle);
            var finalPoint = ComputeReconciledDevicePoint(basePoint, binding, recipe);
            deviceFinalPoints[handle] = finalPoint;
            deviceOverridesByIndex[index] = OverrideRecordCoordinatePair(record, "10", "20", finalPoint, handle);
        }

        var reconciled = new List<IReadOnlyList<DxfPair>>(overlayRecords.Count);
        for (var index = 0; index < overlayRecords.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var record = overlayRecords[index];
            if (deviceOverridesByIndex.TryGetValue(index, out var deviceOverride))
            {
                reconciled.Add(deviceOverride);
                continue;
            }

            var entityType = RecordType(record);
            if (!IsElectricalCarrierEntity(entityType))
            {
                reconciled.Add(record);
                continue;
            }

            var handle = FirstPairValue(record, "5")?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(handle))
            {
                seenCarrierHandles.Add(handle);
            }

            if (!string.IsNullOrWhiteSpace(handle) && staticCarrierHandles.Contains(handle))
            {
                reconciled.Add(record);
                continue;
            }

            if (string.IsNullOrWhiteSpace(handle) || !wireRoutes.TryGetValue(handle, out var route))
            {
                var layerName = FirstPairValue(record, "8") ?? string.Empty;
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"UnsupportedWireRoute: electrical carrier {entityType} '{handle}' on layer '{layerName}' has neither " +
                    "commissioned route connectivity nor a static declaration.");
            }

            if (entityType != "LINE")
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"UnsupportedWireRoute: route carrier '{handle}' is {entityType}; only LINE carriers between commissioned devices are supported.");
            }

            var routeStartHandle = route.StartDeviceHandle?.Trim() ?? string.Empty;
            var routeEndHandle = route.EndDeviceHandle?.Trim() ?? string.Empty;
            if (!deviceFinalPoints.TryGetValue(routeStartHandle, out var startPoint) ||
                !deviceFinalPoints.TryGetValue(routeEndHandle, out var endPoint))
            {
                // A bound-but-absent endpoint device has no reconciled point, so the route has no
                // complete connectivity to regenerate from; reject explicitly instead of indexing.
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"UnsupportedWireRoute: route carrier '{handle}' references endpoint devices " +
                    $"'{route.StartDeviceHandle}'/'{route.EndDeviceHandle}' that are absent from the reconciled " +
                    "overlay devices, so the route has no complete connectivity to regenerate.");
            }

            var regenerated = OverrideRecordCoordinatePair(record, "10", "20", startPoint, handle);
            reconciled.Add(OverrideRecordCoordinatePair(regenerated, "11", "21", endPoint, handle));
        }

        foreach (var boundHandle in deviceBindings.Keys)
        {
            if (!seenDeviceHandles.Contains(boundHandle))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Electrical device binding '{boundHandle}' does not match any overlay device; the evidence is stale.");
            }
        }

        foreach (var routeHandle in wireRoutes.Keys)
        {
            if (!seenCarrierHandles.Contains(routeHandle))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Electrical wire route carrier '{routeHandle}' does not match any overlay carrier; the evidence is stale.");
            }
        }

        foreach (var staticHandle in staticCarrierHandles)
        {
            if (!seenCarrierHandles.Contains(staticHandle))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Electrical static carrier declaration '{staticHandle}' does not match any overlay carrier; the evidence is stale.");
            }
        }

        return reconciled;
    }

    private static void ValidateDeviceHostBinding(
        ElectricalDeviceHostBinding binding,
        AdjustmentRecipeSummaryDto canonicalRecipe)
    {
        if (string.IsNullOrWhiteSpace(binding.DeviceHandle) ||
            string.IsNullOrWhiteSpace(binding.BlockName) ||
            string.IsNullOrWhiteSpace(binding.HostSourceEntityRef))
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                "Electrical device binding requires a device handle, block name, and host source reference.");
        }

        if (!string.Equals(binding.HostKind, "Wall", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(binding.HostKind, "Room", StringComparison.OrdinalIgnoreCase))
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                $"Electrical device binding '{binding.DeviceHandle}' declares unsupported host kind '{binding.HostKind}'; only Wall and Room hosts are supported.");
        }

        if (string.Equals(binding.Role, "Fixed", StringComparison.OrdinalIgnoreCase))
        {
            if (binding.HostDeltaXSourceUnits != 0m || binding.HostDeltaYSourceUnits != 0m)
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Electrical device binding '{binding.DeviceHandle}' is Fixed but declares a host delta; the evidence is contradictory.");
            }

            return;
        }

        if (!string.Equals(binding.Role, "HostRigidMove", StringComparison.OrdinalIgnoreCase))
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                $"Electrical device binding '{binding.DeviceHandle}' declares unsupported role '{binding.Role}'; only Fixed and HostRigidMove are supported.");
        }

        EnsureHostDeltaMatchesRecipeAxis(
            binding.DeviceHandle,
            binding.HostDeltaXSourceUnits,
            horizontal: true,
            canonicalRecipe);
        EnsureHostDeltaMatchesRecipeAxis(
            binding.DeviceHandle,
            binding.HostDeltaYSourceUnits,
            horizontal: false,
            canonicalRecipe);
    }

    private static void EnsureHostDeltaMatchesRecipeAxis(
        string deviceHandle,
        decimal delta,
        bool horizontal,
        AdjustmentRecipeSummaryDto canonicalRecipe)
    {
        if (delta == 0m)
        {
            return;
        }

        var axisName = horizontal ? "Width" : "Height";
        var totalAxisDelta = canonicalRecipe.Operations
            .Where(operation => horizontal
                ? IsHorizontalRecipeOperation(operation)
                : IsVerticalRecipeOperation(operation))
            .Sum(operation => Math.Abs(operation.DeltaSourceUnits)) +
            canonicalRecipe.StretchActions
                .Where(action => string.Equals(action.AxisTag, axisName, StringComparison.OrdinalIgnoreCase))
                .Sum(action => Math.Abs(action.DeltaSourceUnits));
        if (totalAxisDelta <= 0m || Math.Abs(delta) > totalAxisDelta)
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                $"Electrical device binding '{deviceHandle}' declares a {axisName} host delta of {delta} that the canonical recipe's {axisName} change cannot justify.");
        }
    }

    private static (double X, double Y) ComputeReconciledDevicePoint(
        (decimal X, decimal Y) sourcePoint,
        ElectricalDeviceHostBinding binding,
        ProjectedPlanSheetExportRecipe recipe)
    {
        var affineOnlyRecipe = recipe.CanonicalRecipe with
        {
            Operations = [],
            StretchActions = []
        };
        var affine = ElectricalRecipeProjection.ProjectPoint(
            sourcePoint.X,
            sourcePoint.Y,
            recipe.RegistrationTransform,
            affineOnlyRecipe,
            outlineNormalization: null);

        return (
            (double)(affine.X + (binding.HostDeltaXSourceUnits * recipe.CanonicalRecipe.FloorToSiteScale)),
            (double)(affine.Y + (binding.HostDeltaYSourceUnits * recipe.CanonicalRecipe.FloorToSiteScale)));
    }

    private static Dictionary<string, IReadOnlyList<DxfPair>> IndexEntityRecordsByHandle(
        IReadOnlyList<DxfPair> pairs)
    {
        var records = ReadSectionRecords(pairs, "ENTITIES");
        var byHandle = new Dictionary<string, IReadOnlyList<DxfPair>>(StringComparer.OrdinalIgnoreCase);
        foreach (var record in records)
        {
            var handle = FirstPairValue(record, "5")?.Trim();
            if (string.IsNullOrWhiteSpace(handle))
            {
                continue;
            }

            if (!byHandle.TryAdd(handle, record))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Electrical source declares duplicate entity handle '{handle}'; reconciliation cannot bind devices safely.");
            }
        }

        return byHandle;
    }

    private static (decimal X, decimal Y) ReadRecordCoordinate(
        IReadOnlyList<DxfPair> record,
        string xCode,
        string yCode,
        string handle)
    {
        decimal? x = null;
        decimal? y = null;
        foreach (var pair in record)
        {
            if (x is null && pair.Code == xCode && TryParseDecimal(pair.Value, out var parsedX))
            {
                x = parsedX;
            }
            else if (y is null && pair.Code == yCode && TryParseDecimal(pair.Value, out var parsedY))
            {
                y = parsedY;
            }
        }

        if (x is null || y is null)
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                $"Electrical entity '{handle}' is missing the {xCode}/{yCode} coordinate pair required for reconciliation.");
        }

        return (x.Value, y.Value);
    }

    private static IReadOnlyList<DxfPair> OverrideRecordCoordinatePair(
        IReadOnlyList<DxfPair> record,
        string xCode,
        string yCode,
        (double X, double Y) point,
        string handle)
    {
        var overridden = new List<DxfPair>(record.Count);
        var wroteX = false;
        var wroteY = false;
        foreach (var pair in record)
        {
            if (!wroteX && pair.Code == xCode)
            {
                overridden.Add(new DxfPair(xCode, FormatDouble(point.X)));
                wroteX = true;
            }
            else if (!wroteY && pair.Code == yCode)
            {
                overridden.Add(new DxfPair(yCode, FormatDouble(point.Y)));
                wroteY = true;
            }
            else
            {
                overridden.Add(pair);
            }
        }

        if (!wroteX || !wroteY)
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                $"Electrical entity '{handle}' is missing the {xCode}/{yCode} coordinate pair required for reconciliation.");
        }

        return overridden;
    }

    private static bool IsElectricalCarrierEntity(string entityType)
        => entityType is "LINE" or "LWPOLYLINE" or "POLYLINE" or "ARC" or "CIRCLE" or "ELLIPSE" or "SPLINE";

    private static Dictionary<string, DxfBlockDefinition> ReadBlockDefinitions(
        IReadOnlyList<DxfPair> pairs)
    {
        var records = ReadSectionRecords(pairs, "BLOCKS");
        var blocks = new Dictionary<string, DxfBlockDefinition>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < records.Count; index++)
        {
            if (RecordType(records[index]) != "BLOCK")
            {
                continue;
            }

            var blockName = FirstPairValue(records[index], "2");
            if (string.IsNullOrWhiteSpace(blockName))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    "DXF BLOCK record is missing its name.");
            }

            var blockRecords = new List<IReadOnlyList<DxfPair>> { records[index] };
            while (++index < records.Count)
            {
                blockRecords.Add(records[index]);
                if (RecordType(records[index]) == "ENDBLK")
                {
                    break;
                }
            }

            if (RecordType(blockRecords[^1]) != "ENDBLK" ||
                !blocks.TryAdd(blockName, new DxfBlockDefinition(blockName, blockRecords)))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"DXF block definition '{blockName}' is incomplete or duplicated.");
            }
        }

        return blocks;
    }

    private static HashSet<string> CollectReferencedBlockClosure(
        IReadOnlyList<IReadOnlyList<DxfPair>> overlayRecords,
        IReadOnlyDictionary<string, DxfBlockDefinition> sourceBlocks)
    {
        var pending = new Queue<string>(CollectReferencedBlockNames(overlayRecords));
        var closure = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (pending.TryDequeue(out var blockName))
        {
            if (!closure.Add(blockName))
            {
                continue;
            }

            if (!sourceBlocks.TryGetValue(blockName, out var block))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Electrical overlay references missing block definition '{blockName}'.");
            }

            foreach (var nestedName in CollectReferencedBlockNames(block.Records))
            {
                pending.Enqueue(nestedName);
            }
        }

        return closure;
    }

    private static IEnumerable<string> CollectReferencedBlockNames(
        IEnumerable<IReadOnlyList<DxfPair>> records)
    {
        foreach (var record in records)
        {
            var recordType = RecordType(record);
            if (recordType != "INSERT" && recordType != "DIMENSION")
            {
                continue;
            }

            var blockName = FirstPairValue(record, "2");
            if (!string.IsNullOrWhiteSpace(blockName))
            {
                yield return blockName;
            }
        }
    }

    private static Dictionary<string, string> BuildElectricalBlockNameMap(
        IEnumerable<string> sourceBlockNames,
        ISet<string> canonicalBlockNames)
    {
        var reserved = new HashSet<string>(canonicalBlockNames, StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sourceName in sourceBlockNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
        {
            var destinationName = sourceName;
            if (reserved.Contains(destinationName))
            {
                var safeName = new string(sourceName
                    .Select(character => char.IsLetterOrDigit(character) || character is '_' or '-' ? character : '_')
                    .ToArray());
                var suffix = 1;
                destinationName = $"FF_ELECTRICAL_{safeName}";
                while (reserved.Contains(destinationName))
                {
                    destinationName = $"FF_ELECTRICAL_{safeName}_{suffix++}";
                }
            }

            reserved.Add(destinationName);
            result.Add(sourceName, destinationName);
        }

        return result;
    }

    private static IReadOnlyList<DxfPair> RewriteBlockNames(
        IReadOnlyList<DxfPair> record,
        IReadOnlyDictionary<string, string> blockNameMap)
    {
        var entityType = RecordType(record);
        var rewritten = record.ToList();
        for (var index = 1; index < rewritten.Count; index++)
        {
            var pair = rewritten[index];
            var isBlockName = entityType switch
            {
                "INSERT" or "DIMENSION" or "BLOCK_RECORD" => pair.Code == "2",
                "BLOCK" => pair.Code is "2" or "3",
                _ => false
            };
            if (isBlockName && blockNameMap.TryGetValue(pair.Value.Trim(), out var destinationName))
            {
                rewritten[index] = pair with { Value = destinationName };
            }
        }

        return rewritten;
    }

    private static Dictionary<string, IReadOnlyList<IReadOnlyList<DxfPair>>> CollectMissingElectricalSymbolTableRecords(
        IReadOnlyList<DxfPair> canonicalPairs,
        IReadOnlyList<DxfPair> electricalPairs,
        IReadOnlyList<IReadOnlyList<DxfPair>> importedResourceRecords)
    {
        VerifyNamedResourcesAlreadyExist(
            canonicalPairs,
            "LTYPE",
            CollectPairValues(importedResourceRecords, "6"),
            ["BYLAYER", "BYBLOCK", "CONTINUOUS"]);
        var requiredStyles = CollectPairValues(importedResourceRecords, "7");
        VerifyNamedResourcesAlreadyExist(
            canonicalPairs,
            "DIMSTYLE",
            importedResourceRecords
                .Where(record => RecordType(record) == "DIMENSION")
                .Select(record => FirstPairValue(record, "3"))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase),
            []);
        VerifyNamedResourcesAlreadyExist(
            canonicalPairs,
            "APPID",
            CollectPairValues(importedResourceRecords, "1001"),
            []);

        var result = new Dictionary<string, IReadOnlyList<IReadOnlyList<DxfPair>>>(
            StringComparer.OrdinalIgnoreCase);
        var existingStyles = ReadSymbolTableRecordMap(canonicalPairs, "STYLE").Keys
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingStyles = requiredStyles
            .Where(name => !existingStyles.Contains(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (missingStyles.Length > 0)
        {
            var sourceStyles = ReadSymbolTableRecordMap(electricalPairs, "STYLE");
            var styleRecords = new List<IReadOnlyList<DxfPair>>(missingStyles.Length);
            foreach (var styleName in missingStyles)
            {
                if (!sourceStyles.TryGetValue(styleName, out var sourceRecord))
                {
                    throw new ProjectedPlanSheetManualReviewRequiredException(
                        $"Electrical overlay requires missing STYLE resource '{styleName}'.");
                }

                styleRecords.Add(sourceRecord);
            }

            result["STYLE"] = styleRecords;
        }

        var requiredLayers = CollectPairValues(importedResourceRecords, "8");
        var existingLayers = ReadSymbolTableRecordMap(canonicalPairs, "LAYER").Keys
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingLayers = requiredLayers
            .Where(name => !existingLayers.Contains(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (missingLayers.Length == 0)
        {
            return result;
        }

        var sourceLayers = ReadSymbolTableRecordMap(electricalPairs, "LAYER");
        var layerRecords = new List<IReadOnlyList<DxfPair>>(missingLayers.Length);
        foreach (var layerName in missingLayers)
        {
            if (!sourceLayers.TryGetValue(layerName, out var sourceRecord))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Electrical overlay requires missing LAYER resource '{layerName}'.");
            }

            var layerLineTypes = sourceRecord
                .Where(pair => pair.Code == "6")
                .Select(pair => pair.Value.Trim())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            VerifyNamedResourcesAlreadyExist(
                canonicalPairs,
                "LTYPE",
                layerLineTypes,
                ["BYLAYER", "BYBLOCK", "CONTINUOUS"]);
            layerRecords.Add(sourceRecord);
        }

        result["LAYER"] = layerRecords;
        return result;
    }

    private static void VerifyNamedResourcesAlreadyExist(
        IReadOnlyList<DxfPair> canonicalPairs,
        string tableName,
        IEnumerable<string> requiredNames,
        IEnumerable<string> builtInNames)
    {
        var required = requiredNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        required.ExceptWith(builtInNames);
        if (required.Count == 0)
        {
            return;
        }

        var existing = ReadSymbolTableRecordMap(canonicalPairs, tableName).Keys
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = required
            .Where(name => !existing.Contains(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (missing.Length > 0)
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                $"Electrical overlay requires {tableName} resource(s) absent from the canonical FloorPlan: {string.Join(", ", missing)}.");
        }
    }

    private static HashSet<string> CollectPairValues(
        IEnumerable<IReadOnlyList<DxfPair>> records,
        string code)
        => records
            .SelectMany(record => record)
            .Where(pair => pair.Code == code && !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => pair.Value.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, IReadOnlyList<DxfPair>> BuildMissingBlockRecordTableRecords(
        IReadOnlyList<DxfPair> canonicalPairs,
        IReadOnlyList<DxfPair> electricalPairs,
        IEnumerable<string> sourceBlockNames,
        IReadOnlyDictionary<string, string> blockNameMap)
    {
        var canonicalRecords = ReadSymbolTableRecordMap(canonicalPairs, "BLOCK_RECORD");
        var sourceRecords = ReadSymbolTableRecordMap(electricalPairs, "BLOCK_RECORD");
        var result = new Dictionary<string, IReadOnlyList<DxfPair>>(StringComparer.OrdinalIgnoreCase);
        foreach (var sourceName in sourceBlockNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
        {
            var destinationName = blockNameMap[sourceName];
            if (canonicalRecords.ContainsKey(destinationName))
            {
                continue;
            }

            if (!sourceRecords.TryGetValue(sourceName, out var sourceRecord))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Electrical block '{sourceName}' has no BLOCK_RECORD table entry.");
            }

            result.Add(sourceName, RewriteBlockNames(sourceRecord, blockNameMap));
        }

        return result;
    }

    private static Dictionary<string, string> BuildImportedHandleMap(
        IEnumerable<IReadOnlyList<DxfPair>> records,
        DxfCompositionHandleAllocator handleAllocator)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var record in records)
        {
            var sourceHandle = ReadRecordHandle(record);
            if (string.IsNullOrWhiteSpace(sourceHandle))
            {
                continue;
            }

            if (!result.TryAdd(sourceHandle, handleAllocator.Next()))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Electrical resource handle '{sourceHandle}' is duplicated.");
            }
        }

        return result;
    }

    private static IReadOnlyList<IReadOnlyList<DxfPair>> TransformImportedRecordSequence(
        IReadOnlyList<IReadOnlyList<DxfPair>> sourceRecords,
        string defaultOwner,
        string? sourceModelSpaceHandle,
        string canonicalModelSpaceHandle,
        IReadOnlyDictionary<string, string> handleMap,
        DxfCompositionHandleAllocator handleAllocator)
    {
        var transformed = new List<IReadOnlyList<DxfPair>>(sourceRecords.Count);
        string? sequenceOwner = null;
        string? expectedChildType = null;
        for (var index = 0; index < sourceRecords.Count; index++)
        {
            var sourceRecord = sourceRecords[index];
            var entityType = RecordType(sourceRecord);
            if (sequenceOwner is null && (entityType is "ATTRIB" or "VERTEX" or "SEQEND"))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Electrical {entityType} record has no owning INSERT or POLYLINE sequence.");
            }

            if (sequenceOwner is not null && entityType != expectedChildType && entityType != "SEQEND")
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Electrical owned entity sequence expected {expectedChildType} or SEQEND but found {entityType}.");
            }

            var transformedRecord = TransformImportedRecord(
                sourceRecord,
                sequenceOwner ?? defaultOwner,
                sourceModelSpaceHandle,
                canonicalModelSpaceHandle,
                handleMap,
                handleAllocator);
            transformed.Add(transformedRecord);

            if (sequenceOwner is null && HasOwnedEntitySequence(sourceRecords, index, entityType))
            {
                sequenceOwner = ReadRecordHandle(transformedRecord)
                    ?? throw new ProjectedPlanSheetManualReviewRequiredException(
                        $"Electrical {entityType} sequence parent has no remapped handle.");
                expectedChildType = entityType == "INSERT" ? "ATTRIB" : "VERTEX";
            }
            else if (sequenceOwner is not null && entityType == "SEQEND")
            {
                sequenceOwner = null;
                expectedChildType = null;
            }
        }

        if (sequenceOwner is not null)
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                "Electrical owned entity sequence is missing SEQEND.");
        }

        return transformed;
    }

    private static IReadOnlyList<DxfPair> NormalizeImportedAssociationMetadata(
        IReadOnlyList<DxfPair> sourceRecord,
        IReadOnlyDictionary<string, IReadOnlyList<DxfPair>> sourceObjectsByHandle)
    {
        var normalized = new List<DxfPair>(sourceRecord.Count);
        for (var index = 0; index < sourceRecord.Count;)
        {
            var pair = sourceRecord[index];
            if (pair.Code != "102" || !pair.Value.TrimStart().StartsWith("{", StringComparison.Ordinal))
            {
                normalized.Add(pair);
                index++;
                continue;
            }

            var groupEnd = FindApplicationGroupEnd(sourceRecord, index);
            var group = sourceRecord
                .Skip(index)
                .Take(groupEnd - index + 1)
                .ToArray();
            var groupName = pair.Value.Trim();
            if (string.Equals(groupName, "{ACAD_REACTORS", StringComparison.OrdinalIgnoreCase))
            {
                if (group.Skip(1).SkipLast(1).Any(item => item.Code != "330"))
                {
                    throw new ProjectedPlanSheetManualReviewRequiredException(
                        $"Electrical {RecordType(sourceRecord)} has a nonstandard ACAD_REACTORS payload that cannot be detached safely.");
                }

                index = groupEnd + 1;
                continue;
            }

            if (RecordType(sourceRecord) == "DIMENSION" &&
                string.Equals(groupName, "{ACAD_XDICTIONARY", StringComparison.OrdinalIgnoreCase) &&
                IsStandardDimensionAssociationDictionary(sourceRecord, group, sourceObjectsByHandle))
            {
                index = groupEnd + 1;
                continue;
            }

            normalized.AddRange(group);
            index = groupEnd + 1;
        }

        return normalized;
    }

    private static int FindApplicationGroupEnd(IReadOnlyList<DxfPair> record, int startIndex)
    {
        var depth = 0;
        for (var index = startIndex; index < record.Count; index++)
        {
            if (record[index].Code != "102")
            {
                continue;
            }

            var value = record[index].Value.Trim();
            if (value.StartsWith("{", StringComparison.Ordinal))
            {
                depth++;
            }
            else if (value == "}" && --depth == 0)
            {
                return index;
            }
        }

        throw new ProjectedPlanSheetManualReviewRequiredException(
            $"Electrical {RecordType(record)} contains an unterminated DXF application group.");
    }

    private static bool IsStandardDimensionAssociationDictionary(
        IReadOnlyList<DxfPair> dimensionRecord,
        IReadOnlyList<DxfPair> applicationGroup,
        IReadOnlyDictionary<string, IReadOnlyList<DxfPair>> sourceObjectsByHandle)
    {
        var payload = applicationGroup.Skip(1).SkipLast(1).ToArray();
        if (payload.Length != 1 || payload[0].Code != "360")
        {
            return false;
        }

        var dictionaryHandle = payload[0].Value.Trim();
        if (!sourceObjectsByHandle.TryGetValue(dictionaryHandle, out var dictionaryRecord) ||
            RecordType(dictionaryRecord) != "DICTIONARY" ||
            !string.Equals(
                FirstPairValue(dictionaryRecord, "330"),
                ReadRecordHandle(dimensionRecord),
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        for (var index = 1; index + 1 < dictionaryRecord.Count; index++)
        {
            if (dictionaryRecord[index].Code != "3" ||
                !string.Equals(dictionaryRecord[index].Value.Trim(), "ACAD_DIMASSOC", StringComparison.OrdinalIgnoreCase) ||
                dictionaryRecord[index + 1].Code is not ("350" or "360"))
            {
                continue;
            }

            var associationHandle = dictionaryRecord[index + 1].Value.Trim();
            return sourceObjectsByHandle.TryGetValue(associationHandle, out var associationRecord) &&
                   RecordType(associationRecord) == "DIMASSOC";
        }

        return false;
    }

    private static IReadOnlyList<DxfPair> TransformImportedRecord(
        IReadOnlyList<DxfPair> sourceRecord,
        string? ownerOverride,
        string? sourceModelSpaceHandle,
        string canonicalModelSpaceHandle,
        IReadOnlyDictionary<string, string> handleMap,
        DxfCompositionHandleAllocator handleAllocator,
        IReadOnlySet<string>? allowedDestinationHandleReferences = null)
    {
        var record = sourceRecord.ToList();
        var entityType = RecordType(record);
        var handleDefinitionCode = RecordHandleDefinitionCode(record);
        var sourceHandle = FirstPairValue(record, handleDefinitionCode);
        var destinationHandle = !string.IsNullOrWhiteSpace(sourceHandle) && handleMap.TryGetValue(sourceHandle, out var mappedHandle)
            ? mappedHandle
            : handleAllocator.Next();
        var transformed = new List<DxfPair>(record.Count + 2)
        {
            record[0],
            new DxfPair(handleDefinitionCode, destinationHandle)
        };
        if (!string.IsNullOrWhiteSpace(ownerOverride))
        {
            transformed.Add(new DxfPair("330", ownerOverride));
        }

        var sourceOwnerSkipped = false;
        var insideApplicationGroup = false;
        for (var index = 1; index < record.Count; index++)
        {
            var pair = record[index];
            if (pair.Code == handleDefinitionCode)
            {
                continue;
            }

            if (pair.Code == "102")
            {
                insideApplicationGroup = pair.Value.Trim() != "}";
                transformed.Add(pair);
                continue;
            }

            if (pair.Code == "330" && !insideApplicationGroup && !sourceOwnerSkipped)
            {
                sourceOwnerSkipped = true;
                continue;
            }

            if (entityType == "HATCH" && pair.Code == "330" && !insideApplicationGroup)
            {
                continue;
            }

            if (IsHandleReferenceCode(pair.Code))
            {
                if (allowedDestinationHandleReferences?.Contains(pair.Value.Trim()) == true)
                {
                    transformed.Add(pair);
                    continue;
                }

                if (handleMap.TryGetValue(pair.Value.Trim(), out var destinationReference))
                {
                    transformed.Add(pair with { Value = destinationReference });
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(sourceModelSpaceHandle) &&
                    string.Equals(pair.Value.Trim(), sourceModelSpaceHandle, StringComparison.OrdinalIgnoreCase))
                {
                    transformed.Add(pair with { Value = canonicalModelSpaceHandle });
                    continue;
                }

                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Electrical {entityType} references unresolved DXF handle '{pair.Value.Trim()}' through group {pair.Code}.");
            }

            transformed.Add(entityType == "HATCH" && pair.Code is "71" or "97"
                ? pair with { Value = "0" }
                : pair);
        }

        return transformed;
    }

    private static IReadOnlyList<DxfPair> PrepareImportedLayerRecord(
        IReadOnlyList<DxfPair> canonicalPairs,
        IReadOnlyList<DxfPair> sourceRecord)
    {
        if (RecordType(sourceRecord) != "LAYER")
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                $"Expected a LAYER resource but found {RecordType(sourceRecord)}.");
        }

        if (!ReadSymbolTableRecordMap(canonicalPairs, "LAYER").TryGetValue("0", out var canonicalLayerZero))
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                "Adjusted canonical FloorPlan has no layer 0 reference template for safe Electrical layer import.");
        }

        var canonicalReferencesByCode = canonicalLayerZero
            .Where(pair => IsHandleReferenceCode(pair.Code) && pair.Code != "330")
            .GroupBy(pair => pair.Code, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(pair => pair.Value.Trim()).ToArray(),
                StringComparer.Ordinal);
        var referenceOffsets = new Dictionary<string, int>(StringComparer.Ordinal);
        var prepared = new List<DxfPair>(sourceRecord.Count);
        foreach (var pair in sourceRecord)
        {
            if (!IsHandleReferenceCode(pair.Code) || pair.Code == "330")
            {
                prepared.Add(pair);
                continue;
            }

            if (!canonicalReferencesByCode.TryGetValue(pair.Code, out var canonicalValues))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Electrical LAYER resource uses unsupported handle reference group {pair.Code}; canonical layer 0 has no safe equivalent.");
            }

            var offset = referenceOffsets.TryGetValue(pair.Code, out var currentOffset)
                ? currentOffset
                : 0;
            if (offset >= canonicalValues.Length)
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Electrical LAYER resource has more group {pair.Code} references than canonical layer 0 can safely normalize.");
            }

            prepared.Add(pair with { Value = canonicalValues[offset] });
            referenceOffsets[pair.Code] = offset + 1;
        }

        return prepared;
    }

    private static IReadOnlyList<DxfPair> PrepareImportedStyleRecord(IReadOnlyList<DxfPair> sourceRecord)
    {
        if (RecordType(sourceRecord) != "STYLE")
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                $"Expected a STYLE resource but found {RecordType(sourceRecord)}.");
        }

        if (sourceRecord.Any(pair => IsHandleReferenceCode(pair.Code) && pair.Code != "330"))
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                "Electrical STYLE resource contains unsupported object references.");
        }

        return sourceRecord;
    }

    private static IReadOnlyList<DxfPair> SanitizeHandlelessSymbolTableRecord(IReadOnlyList<DxfPair> sourceRecord)
    {
        if (sourceRecord.Any(pair => IsHandleReferenceCode(pair.Code)))
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                $"Handle-less canonical FloorPlan cannot safely import {RecordType(sourceRecord)} resources with handle references.");
        }

        return sourceRecord
            .Where(pair => !IsHandleDefinitionCode(pair.Code))
            .ToArray();
    }

    private static bool IsHandleDefinitionCode(string code)
        => code is "5" or "105";

    private static string RecordHandleDefinitionCode(IReadOnlyList<DxfPair> record)
        => RecordType(record) == "DIMSTYLE" ? "105" : "5";

    private static string? ReadRecordHandle(IReadOnlyList<DxfPair> record)
        => FirstPairValue(record, RecordHandleDefinitionCode(record));

    private static bool IsHandleReferenceCode(string code)
    {
        if (!int.TryParse(code, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericCode))
        {
            return false;
        }

        return numericCode is >= 330 and <= 369 or
            >= 390 and <= 399 or
            480 or
            481 or
            1005;
    }

    private static IReadOnlyList<IReadOnlyList<DxfPair>> ReadSectionRecords(
        IReadOnlyList<DxfPair> pairs,
        string sectionName)
    {
        var records = new List<IReadOnlyList<DxfPair>>();
        var inRequestedSection = false;
        for (var index = 0; index < pairs.Count;)
        {
            if (IsDxfStart(pairs[index], "SECTION") &&
                index + 1 < pairs.Count &&
                pairs[index + 1].Code == "2")
            {
                inRequestedSection = string.Equals(
                    pairs[index + 1].Value.Trim(),
                    sectionName,
                    StringComparison.OrdinalIgnoreCase);
                index += 2;
                continue;
            }

            if (IsDxfStart(pairs[index], "ENDSEC"))
            {
                inRequestedSection = false;
                index++;
                continue;
            }

            if (!inRequestedSection || pairs[index].Code != "0")
            {
                index++;
                continue;
            }

            records.Add(ReadRawRecord(pairs, ref index));
        }

        return records;
    }

    private static Dictionary<string, IReadOnlyList<DxfPair>> ReadSymbolTableRecordMap(
        IReadOnlyList<DxfPair> pairs,
        string tableName)
    {
        var records = new Dictionary<string, IReadOnlyList<DxfPair>>(StringComparer.OrdinalIgnoreCase);
        var inRequestedTable = false;
        for (var index = 0; index < pairs.Count;)
        {
            if (IsDxfStart(pairs[index], "TABLE"))
            {
                var tableRecord = ReadRawRecord(pairs, ref index);
                inRequestedTable = string.Equals(
                    FirstPairValue(tableRecord, "2"),
                    tableName,
                    StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (IsDxfStart(pairs[index], "ENDTAB"))
            {
                inRequestedTable = false;
                index++;
                continue;
            }

            if (!inRequestedTable || pairs[index].Code != "0")
            {
                index++;
                continue;
            }

            var record = ReadRawRecord(pairs, ref index);
            var name = FirstPairValue(record, "2");
            if (!string.IsNullOrWhiteSpace(name) && !records.TryAdd(name, record))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"DXF {tableName} table contains duplicate record '{name}'.");
            }
        }

        return records;
    }

    private static Dictionary<string, IReadOnlyList<DxfPair>> ReadObjectRecordsByHandle(
        IReadOnlyList<DxfPair> pairs)
    {
        var records = new Dictionary<string, IReadOnlyList<DxfPair>>(StringComparer.OrdinalIgnoreCase);
        foreach (var record in ReadSectionRecords(pairs, "OBJECTS"))
        {
            var handle = ReadRecordHandle(record);
            if (string.IsNullOrWhiteSpace(handle))
            {
                continue;
            }

            if (!records.TryAdd(handle, record))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Electrical OBJECTS section contains duplicate handle '{handle}'.");
            }
        }

        return records;
    }

    private static string? ResolveTableHandle(IReadOnlyList<DxfPair> pairs, string tableName)
    {
        for (var index = 0; index < pairs.Count;)
        {
            if (!IsDxfStart(pairs[index], "TABLE"))
            {
                index++;
                continue;
            }

            var tableRecord = ReadRawRecord(pairs, ref index);
            if (string.Equals(FirstPairValue(tableRecord, "2"), tableName, StringComparison.OrdinalIgnoreCase))
            {
                return FirstPairValue(tableRecord, "5");
            }
        }

        return null;
    }

    private static string? ResolveBlockRecordHandle(IReadOnlyList<DxfPair> pairs, string blockName)
        => ReadSymbolTableRecordMap(pairs, "BLOCK_RECORD")
            .TryGetValue(blockName, out var record)
                ? FirstPairValue(record, "5")
                : null;

    private static IReadOnlyList<DxfPair> ReadRawRecord(IReadOnlyList<DxfPair> pairs, ref int index)
    {
        var record = new List<DxfPair> { pairs[index++] };
        while (index < pairs.Count && pairs[index].Code != "0")
        {
            record.Add(pairs[index++]);
        }

        return record;
    }

    private static string RecordType(IReadOnlyList<DxfPair> record)
        => record.Count == 0 ? string.Empty : record[0].Value.Trim().ToUpperInvariant();

    private static string? FirstPairValue(IReadOnlyList<DxfPair> record, string code)
        => record
            .Where(pair => pair.Code == code)
            .Select(pair => pair.Value.Trim())
            .FirstOrDefault();

    private static bool IsDxfStart(DxfPair pair, string value)
        => pair.Code == "0" && string.Equals(pair.Value.Trim(), value, StringComparison.OrdinalIgnoreCase);

    private static List<DxfPair> InjectTableRecords(
        IReadOnlyList<DxfPair> pairs,
        string tableName,
        IReadOnlyList<IReadOnlyList<DxfPair>> records)
    {
        if (records.Count == 0)
        {
            return pairs.ToList();
        }

        var result = new List<DxfPair>(pairs.Count + records.Sum(record => record.Count));
        var inRequestedTable = false;
        var foundTable = false;
        var injected = false;
        for (var index = 0; index < pairs.Count;)
        {
            if (IsDxfStart(pairs[index], "TABLE"))
            {
                var tableRecord = ReadRawRecord(pairs, ref index).ToList();
                inRequestedTable = string.Equals(
                    FirstPairValue(tableRecord, "2"),
                    tableName,
                    StringComparison.OrdinalIgnoreCase);
                if (inRequestedTable)
                {
                    foundTable = true;
                    IncrementTableCount(tableRecord, records.Count);
                }

                result.AddRange(tableRecord);
                continue;
            }

            if (inRequestedTable && IsDxfStart(pairs[index], "ENDTAB"))
            {
                foreach (var record in records)
                {
                    result.AddRange(record);
                }

                injected = true;
                inRequestedTable = false;
            }

            result.Add(pairs[index++]);
        }

        if (!foundTable || !injected)
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                $"Adjusted canonical FloorPlan is missing a writable {tableName} symbol table.");
        }

        return result;
    }

    private static void IncrementTableCount(List<DxfPair> tableRecord, int addedCount)
    {
        var countIndex = tableRecord.FindIndex(pair => pair.Code == "70");
        if (countIndex < 0 ||
            !int.TryParse(tableRecord[countIndex].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count))
        {
            return;
        }

        tableRecord[countIndex] = tableRecord[countIndex] with
        {
            Value = checked(count + addedCount).ToString(CultureInfo.InvariantCulture)
        };
    }

    private static List<DxfPair> InjectBlockDefinitions(
        IReadOnlyList<DxfPair> pairs,
        IReadOnlyList<DxfBlockDefinition> blocks)
        => blocks.Count == 0
            ? pairs.ToList()
            : InjectSectionRecords(
                pairs,
                "BLOCKS",
                blocks.SelectMany(block => block.Records).ToArray());

    private static List<DxfPair> InjectSectionRecords(
        IReadOnlyList<DxfPair> pairs,
        string sectionName,
        IReadOnlyList<IReadOnlyList<DxfPair>> records)
    {
        if (records.Count == 0)
        {
            return pairs.ToList();
        }

        var result = new List<DxfPair>(pairs.Count + records.Sum(record => record.Count));
        var currentSection = string.Empty;
        var injected = false;
        for (var index = 0; index < pairs.Count; index++)
        {
            var pair = pairs[index];
            if (IsDxfStart(pair, "SECTION") && index + 1 < pairs.Count && pairs[index + 1].Code == "2")
            {
                currentSection = pairs[index + 1].Value.Trim();
            }
            else if (IsDxfStart(pair, "ENDSEC"))
            {
                if (string.Equals(currentSection, sectionName, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var record in records)
                    {
                        result.AddRange(record);
                    }

                    injected = true;
                }

                currentSection = string.Empty;
            }

            result.Add(pair);
        }

        if (!injected)
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                $"Adjusted canonical FloorPlan is missing its {sectionName} section.");
        }

        return result;
    }

    private static List<DxfPair> UpdateCompositionHandSeed(
        IReadOnlyList<DxfPair> pairs,
        string nextAvailableHandle)
    {
        var result = pairs.ToList();
        for (var index = 0; index + 1 < result.Count; index++)
        {
            if (result[index].Code == "9" &&
                string.Equals(result[index].Value.Trim(), "$HANDSEED", StringComparison.OrdinalIgnoreCase) &&
                result[index + 1].Code == "5")
            {
                result[index + 1] = result[index + 1] with { Value = nextAvailableHandle };
                break;
            }
        }

        return result;
    }

    private static List<DxfPair> ProjectCoordinatePairs(
        IReadOnlyList<DxfPair> pairs,
        SheetAdjustmentProjectionTransform transform,
        ProjectedPlanSheetExportRecipe? recipe,
        CancellationToken cancellationToken,
        out IReadOnlyList<ProjectedPlanSheetOperationAuditDto> operationAudits,
        out ProjectedPlanSheetOutlineCongruenceAuditDto? outlineAudit)
    {
        var projectedPairs = pairs.ToList();
        var recipePlan = BuildRecipeProjectionPlan(projectedPairs, recipe);
        var cadStretchPlan = BuildElectricalCadStretchProjectionPlan(
            projectedPairs,
            recipePlan?.EffectiveRecipe,
            cancellationToken);
        var operationAuditAccumulators = BuildOperationAuditAccumulators(recipePlan);
        var dimensionBlockNames = CollectDimensionBlockNames(projectedPairs, cancellationToken);
        if (cadStretchPlan is null)
        {
            ThrowIfUnsupportedRecipeEntities(projectedPairs, dimensionBlockNames, recipePlan?.EffectiveRecipe, cancellationToken);
            ReplaceRecipeCrossingCircularCurvesWithPolylines(projectedPairs, recipePlan?.EffectiveRecipe, cancellationToken);
            ThrowIfUnsupportedRecipeCurveCrossings(projectedPairs, dimensionBlockNames, recipePlan?.EffectiveRecipe, cancellationToken);
        }
        var currentSection = string.Empty;
        var currentEntity = string.Empty;
        var currentBlockName = string.Empty;
        var currentEntityKey = string.Empty;
        decimal? textInsertionSourceX = null;
        decimal? textInsertionSourceY = null;
        double? textInsertionProjectedX = null;
        double? textInsertionProjectedY = null;
        for (var index = 0; index + 1 < projectedPairs.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pair = projectedPairs[index];
            if (pair.Code == "0")
            {
                currentEntity = pair.Value.Trim().ToUpperInvariant();
                currentEntityKey = $"{currentSection}:{currentBlockName}:{currentEntity}:{index}";
                textInsertionSourceX = null;
                textInsertionSourceY = null;
                textInsertionProjectedX = null;
                textInsertionProjectedY = null;
                if (currentEntity == "SECTION" &&
                    index + 1 < projectedPairs.Count &&
                    projectedPairs[index + 1].Code == "2")
                {
                    currentSection = projectedPairs[index + 1].Value.Trim().ToUpperInvariant();
                }
                else if (currentEntity == "ENDSEC")
                {
                    currentSection = string.Empty;
                }
                else if (currentSection == "BLOCKS" && currentEntity == "BLOCK")
                {
                    currentBlockName = ResolveEntityPairValue(projectedPairs, index + 1, "2");
                }
                else if (currentSection == "BLOCKS" && currentEntity == "ENDBLK")
                {
                    currentBlockName = string.Empty;
                }

                continue;
            }

            var shouldProjectEntity =
                currentSection == "ENTITIES" ||
                (currentSection == "BLOCKS" && dimensionBlockNames.Contains(currentBlockName));
            if (!shouldProjectEntity)
            {
                continue;
            }

            if (IsCircleOrArc(currentEntity) &&
                pair.Code == "40" &&
                TryParseDecimal(pair.Value, out var radius))
            {
                projectedPairs[index] = pair with { Value = FormatDouble((double)(radius * transform.Scale)) };
                continue;
            }

            if (IsTextEntity(currentEntity) &&
                pair.Code == "40" &&
                TryParseDecimal(pair.Value, out var textHeight))
            {
                projectedPairs[index] = pair with { Value = FormatDouble((double)(textHeight * transform.Scale)) };
                continue;
            }

            if (currentEntity == "ARC" &&
                IsArcAngleCode(pair.Code) &&
                TryParseDecimal(pair.Value, out var arcAngle))
            {
                projectedPairs[index] = pair with
                {
                    Value = FormatDouble((double)(arcAngle + transform.RotationDegrees))
                };
                continue;
            }

            if (currentEntity == "INSERT" &&
                IsInsertScaleCode(pair.Code) &&
                TryParseDecimal(pair.Value, out var insertScale))
            {
                projectedPairs[index] = pair with { Value = FormatDouble((double)(insertScale * transform.Scale)) };
                continue;
            }

            if (currentEntity == "INSERT" &&
                pair.Code == "50" &&
                TryParseDecimal(pair.Value, out var insertRotation))
            {
                projectedPairs[index] = pair with
                {
                    Value = FormatDouble((double)(insertRotation + transform.RotationDegrees))
                };
                continue;
            }

            var xPair = projectedPairs[index];
            var yPair = projectedPairs[index + 1];
            if (!IsXCoordinateCode(xPair.Code) ||
                !IsMatchingYCoordinateCode(xPair.Code, yPair.Code) ||
                !TryParseDecimal(xPair.Value, out var x) ||
                !TryParseDecimal(yPair.Value, out var y))
            {
                continue;
            }

            if (currentEntity == "ELLIPSE" && xPair.Code == "11")
            {
                var projectedVector = ProjectVector(x, y, transform);
                projectedPairs[index] = xPair with { Value = FormatDouble(projectedVector.X) };
                projectedPairs[index + 1] = yPair with { Value = FormatDouble(projectedVector.Y) };
                index++;
                continue;
            }

            if (currentEntity == "MTEXT" && xPair.Code == "11")
            {
                var projectedVector = ProjectVector(x, y, transform);
                projectedPairs[index] = xPair with { Value = FormatDouble(projectedVector.X) };
                projectedPairs[index + 1] = yPair with { Value = FormatDouble(projectedVector.Y) };
                index++;
                continue;
            }

            if (currentEntity == "TEXT" &&
                recipePlan is not null &&
                xPair.Code == "11" &&
                textInsertionSourceX.HasValue &&
                textInsertionSourceY.HasValue &&
                textInsertionProjectedX.HasValue &&
                textInsertionProjectedY.HasValue)
            {
                var projectedVector = ProjectVector(
                    x - textInsertionSourceX.Value,
                    y - textInsertionSourceY.Value,
                    transform);
                projectedPairs[index] = xPair with { Value = FormatDouble(textInsertionProjectedX.Value + projectedVector.X) };
                projectedPairs[index + 1] = yPair with { Value = FormatDouble(textInsertionProjectedY.Value + projectedVector.Y) };
                index++;
                continue;
            }

            var projected = cadStretchPlan is null
                ? ProjectPoint(x, y, transform, recipePlan?.EffectiveRecipe)
                : ProjectCadStretchPoint(
                    x,
                    y,
                    index,
                    index + 1,
                    cadStretchPlan,
                    recipePlan!.EffectiveRecipe);
            RecordAppliedOperations(x, y, recipePlan, operationAuditAccumulators, currentEntityKey);
            if (currentEntity == "TEXT" && xPair.Code == "10" && recipePlan is not null)
            {
                textInsertionSourceX = x;
                textInsertionSourceY = y;
                textInsertionProjectedX = projected.X;
                textInsertionProjectedY = projected.Y;
            }

            projectedPairs[index] = xPair with { Value = FormatDouble(projected.X) };
            projectedPairs[index + 1] = yPair with { Value = FormatDouble(projected.Y) };
            index++;
        }

        // ponytail: project modelspace entities only; metadata/header/object rewrites made AutoCAD reject real dependent sheets.
        operationAudits = cadStretchPlan?.Audits ?? operationAuditAccumulators
            .Select(item => item.ToDto())
            .ToArray();
        outlineAudit = BuildOutlineCongruenceAudit(recipePlan, projectedPairs);
        return projectedPairs;
    }

    private static ElectricalCadStretchProjectionPlan? BuildElectricalCadStretchProjectionPlan(
        IReadOnlyList<DxfPair> pairs,
        ProjectedPlanSheetExportRecipe? recipe,
        CancellationToken cancellationToken)
    {
        if (recipe is null || recipe.CanonicalRecipe.StretchActions.Count == 0)
        {
            return null;
        }

        if (recipe.CanonicalRecipe.FloorToSiteScale <= 0m)
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                "Electrical CAD stretch export requires a positive canonical Floor-to-Site scale.");
        }

        var sourceEntities = ReadElectricalCadStretchEntities(
            pairs,
            recipe.RegistrationTransform,
            cancellationToken);
        var accumulated = new Dictionary<(int XPairIndex, int YPairIndex), (decimal DeltaX, decimal DeltaY)>();
        var audits = new List<ProjectedPlanSheetOperationAuditDto>();
        var actionIds = new HashSet<string>(StringComparer.Ordinal);
        var proofTolerance = recipe.WholePlanRegistrationProof?.MaximumResidual ?? 0m;

        for (var actionIndex = 0; actionIndex < recipe.CanonicalRecipe.StretchActions.Count; actionIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var actionDto = recipe.CanonicalRecipe.StretchActions[actionIndex];
            if (string.IsNullOrWhiteSpace(actionDto.ActionId) || !actionIds.Add(actionDto.ActionId))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Electrical CAD stretch recipe contains a duplicate or empty action id '{actionDto.ActionId}'.");
            }

            var matchTolerance = Math.Max(
                CoordinateTolerance,
                Math.Max(actionDto.CoordinateTolerance, proofTolerance));
            var resolved = ResolveElectricalCadStretchAction(actionDto, sourceEntities, matchTolerance);
            var result = CadStretchDeformationEngine.Apply(
                resolved.Action,
                resolved.Entities,
                resolved.Roles);
            if (!result.Succeeded)
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Electrical CAD stretch action '{actionDto.ActionId}' failed closed: {result.RejectionReason}");
            }

            foreach (var edit in result.Edits)
            {
                if (!resolved.RawEntitiesById.TryGetValue(edit.EntityId, out var rawEntity))
                {
                    throw new ProjectedPlanSheetManualReviewRequiredException(
                        $"Electrical CAD stretch action '{actionDto.ActionId}' produced an unbound Electrical entity edit '{edit.EntityId}'.");
                }

                foreach (var vertexEdit in edit.Vertices)
                {
                    if (vertexEdit.VertexIndex < 0 || vertexEdit.VertexIndex >= rawEntity.Points.Count)
                    {
                        throw new ProjectedPlanSheetManualReviewRequiredException(
                            $"Electrical CAD stretch action '{actionDto.ActionId}' produced invalid vertex {vertexEdit.VertexIndex} for its own entity '{edit.EntityId}'.");
                    }

                    var point = rawEntity.Points[vertexEdit.VertexIndex];
                    var key = (point.XPairIndex, point.YPairIndex);
                    accumulated.TryGetValue(key, out var prior);
                    accumulated[key] = (
                        prior.DeltaX + vertexEdit.DeltaX,
                        prior.DeltaY + vertexEdit.DeltaY);
                }
            }

            audits.Add(new ProjectedPlanSheetOperationAuditDto(
                actionDto.ActionId,
                actionIndex,
                "CadStretch",
                actionDto.AxisTag,
                actionDto.Edge,
                actionDto.CutCoordinate,
                actionDto.DeltaSourceUnits,
                result.Edits.Count,
                result.Edits.Sum(edit => edit.Vertices.Count),
                result.Audit.MeasuredDeltaSourceUnits,
                result.Audit.MeasuredDeltaSourceUnits,
                "Applied",
                $"Resolved Electrical's own target ids [{string.Join(", ", resolved.Action.TargetEntityIds)}] by registered geometry; stretched {result.Audit.StretchedEntityCount}, rigid-moved {result.Audit.RigidMovedEntityCount}, untouched {result.Audit.FixedEntityCount}; paired spacing {result.Audit.PairSpacingBeforeSourceUnits} -> {result.Audit.PairSpacingAfterSourceUnits}."));
        }

        return new ElectricalCadStretchProjectionPlan(accumulated, audits);
    }

    private static ResolvedElectricalCadStretchAction ResolveElectricalCadStretchAction(
        AdjustmentRecipeStretchActionDto action,
        IReadOnlyList<ElectricalCadDxfEntity> sourceEntities,
        decimal matchTolerance)
    {
        if (action.TargetSpans.Count != 2)
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                $"Electrical CAD stretch action '{action.ActionId}' requires exactly two canonical target spans.");
        }

        var rejectedCanonicalRole = action.CanonicalEntityRoles.FirstOrDefault(role =>
            string.Equals(role.Role, "Rejected", StringComparison.OrdinalIgnoreCase));
        if (rejectedCanonicalRole is not null)
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                rejectedCanonicalRole.Reason ??
                $"Canonical CAD stretch action '{action.ActionId}' already rejected '{rejectedCanonicalRole.EntityRef}'.");
        }

        var matchedTargets = action.TargetSpans
            .Select(span => ResolveElectricalTargetSpan(action, span, sourceEntities, matchTolerance))
            .ToArray();
        if (matchedTargets.Select(target => target.RawEntity.Ordinal).Distinct().Count() != 2)
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                $"Electrical CAD stretch action '{action.ActionId}' did not resolve two distinct Electrical wall-face entities.");
        }

        var targetByOrdinal = matchedTargets.ToDictionary(target => target.RawEntity.Ordinal);
        var targetIds = matchedTargets
            .Select(target => ElectricalCadEntityId(target.RawEntity.Ordinal))
            .ToArray();
        var entities = new List<CadStretchEntity>();
        var roles = new List<CadStretchEntityRole>();
        var rawEntitiesById = new Dictionary<string, ElectricalCadDxfEntity>(StringComparer.Ordinal);

        foreach (var sourceEntity in sourceEntities.Where(entity => entity.Points.Count > 0))
        {
            var entityId = ElectricalCadEntityId(sourceEntity.Ordinal);
            entities.Add(new CadStretchEntity(
                entityId,
                sourceEntity.Kind,
                sourceEntity.Points.Select(point => new CadStretchPoint(point.X, point.Y)).ToArray(),
                sourceEntity.SupportsVertexStretch));
            rawEntitiesById.Add(entityId, sourceEntity);

            if (targetByOrdinal.TryGetValue(sourceEntity.Ordinal, out var target))
            {
                roles.Add(new CadStretchEntityRole(
                    entityId,
                    CadStretchRole.Stretch,
                    [target.ClosingVertexIndex]));
                continue;
            }

            var side = ClassifyElectricalCadEntity(sourceEntity, action, matchTolerance);
            if (side == ElectricalCadCutSide.Crossing)
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"{sourceEntity.Kind} Electrical entity {sourceEntity.Ordinal} crosses or touches the unselected CAD stretch cut for action '{action.ActionId}'; no dependent output was written.");
            }

            if (side == ElectricalCadCutSide.Closing)
            {
                if (!sourceEntity.SupportsRigidMove)
                {
                    throw new ProjectedPlanSheetManualReviewRequiredException(
                        $"{sourceEntity.Kind} Electrical entity {sourceEntity.Ordinal} is on the closing side but cannot be translated safely for CAD stretch action '{action.ActionId}'.");
                }

                roles.Add(new CadStretchEntityRole(entityId, CadStretchRole.RigidMove, []));
            }
        }

        return new ResolvedElectricalCadStretchAction(
            new CadStretchAction(
                action.ActionId,
                action.AxisTag,
                action.Edge,
                action.DeltaSourceUnits,
                action.MaxDeltaSourceUnits,
                targetIds,
                matchTolerance),
            entities,
            roles,
            rawEntitiesById);
    }

    private static ResolvedElectricalTargetSpan ResolveElectricalTargetSpan(
        AdjustmentRecipeStretchActionDto action,
        AdjustmentRecipeTargetSpanDto span,
        IReadOnlyList<ElectricalCadDxfEntity> sourceEntities,
        decimal tolerance)
    {
        var matches = new List<ResolvedElectricalTargetSpan>();
        foreach (var sourceEntity in sourceEntities)
        {
            if (!sourceEntity.IsStructural ||
                !sourceEntity.SupportsVertexStretch ||
                sourceEntity.Points.Count != 2 ||
                sourceEntity.HasNonZeroBulge)
            {
                continue;
            }

            var first = sourceEntity.Points[0];
            var second = sourceEntity.Points[1];
            var directResidual = Math.Max(
                PointResidual(first, span.StartX, span.StartY),
                PointResidual(second, span.EndX, span.EndY));
            var reversedResidual = Math.Max(
                PointResidual(second, span.StartX, span.StartY),
                PointResidual(first, span.EndX, span.EndY));
            var direct = directResidual <= tolerance;
            var reversed = reversedResidual <= tolerance;
            if (!direct && !reversed)
            {
                continue;
            }

            var useReversed = reversed && (!direct || reversedResidual < directResidual);
            var closingVertexIndex = useReversed ? 1 - span.ClosingVertexIndex : span.ClosingVertexIndex;
            if (closingVertexIndex is < 0 or > 1)
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"Canonical target '{span.SourceEntityRef}' has invalid closing vertex {span.ClosingVertexIndex} for Electrical resolution.");
            }

            matches.Add(new ResolvedElectricalTargetSpan(
                sourceEntity,
                closingVertexIndex,
                useReversed ? reversedResidual : directResidual));
        }

        if (matches.Count != 1)
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                $"Electrical CAD stretch action '{action.ActionId}' expected exactly one registered structural match for canonical target '{span.SourceEntityRef}', found {matches.Count}; Floor entity ids were not reused.");
        }

        return matches[0];
    }

    private static IReadOnlyList<ElectricalCadDxfEntity> ReadElectricalCadStretchEntities(
        IReadOnlyList<DxfPair> pairs,
        SheetRegistrationTransform registration,
        CancellationToken cancellationToken)
    {
        var entities = new List<ElectricalCadDxfEntity>();
        var currentSection = string.Empty;
        var ordinal = 0;

        for (var index = 0; index < pairs.Count;)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pair = pairs[index];
            var value = pair.Value.Trim().ToUpperInvariant();
            if (pair.Code != "0")
            {
                index++;
                continue;
            }

            if (value == "SECTION" && index + 1 < pairs.Count)
            {
                index++;
                if (pairs[index].Code == "2")
                {
                    currentSection = pairs[index].Value.Trim().ToUpperInvariant();
                }

                index++;
                continue;
            }

            if (value == "ENDSEC")
            {
                currentSection = string.Empty;
                index++;
                continue;
            }

            var end = FindDxfRecordEnd(pairs, index + 1);
            if (currentSection != "ENTITIES")
            {
                index = end;
                continue;
            }

            var layer = ResolveEntityPairValue(pairs, index + 1, "8", end);
            if (value == "POLYLINE")
            {
                var points = new List<ElectricalCadPointBinding>();
                var hasNonZeroBulge = false;
                var cursor = end;
                while (cursor < pairs.Count && pairs[cursor].Code == "0")
                {
                    var childKind = pairs[cursor].Value.Trim().ToUpperInvariant();
                    var childEnd = FindDxfRecordEnd(pairs, cursor + 1);
                    if (childKind == "VERTEX")
                    {
                        AddElectricalCadPoint(
                            pairs,
                            cursor + 1,
                            childEnd,
                            "10",
                            "20",
                            registration,
                            points);
                        hasNonZeroBulge |= HasNonZeroBulge(pairs, cursor + 1, childEnd);
                        cursor = childEnd;
                        continue;
                    }

                    if (childKind == "SEQEND")
                    {
                        cursor = childEnd;
                    }

                    break;
                }

                entities.Add(new ElectricalCadDxfEntity(
                    ordinal++,
                    value,
                    layer,
                    points,
                    SupportsRigidMove: true,
                    SupportsVertexStretch: false,
                    HasNonZeroBulge: hasNonZeroBulge,
                    EnvelopeExpansion: 0m,
                    IsStructural: IsRegistrationAnchorLayer(layer)));
                index = cursor;
                continue;
            }

            entities.Add(BuildElectricalCadDxfEntity(
                ordinal++,
                value,
                layer,
                pairs,
                index + 1,
                end,
                registration));
            index = end;
        }

        return entities;
    }

    private static ElectricalCadDxfEntity BuildElectricalCadDxfEntity(
        int ordinal,
        string kind,
        string layer,
        IReadOnlyList<DxfPair> pairs,
        int start,
        int end,
        SheetRegistrationTransform registration)
    {
        var points = new List<ElectricalCadPointBinding>();
        var supportsRigidMove = true;
        var envelopeExpansion = 0m;

        switch (kind)
        {
            case "LINE":
                AddElectricalCadPoint(pairs, start, end, "10", "20", registration, points);
                AddElectricalCadPoint(pairs, start, end, "11", "21", registration, points);
                break;
            case "ARC":
            case "CIRCLE":
                AddElectricalCadPoint(pairs, start, end, "10", "20", registration, points);
                envelopeExpansion = Math.Abs((ReadFirstDxfDecimal(pairs, start, end, "40") ?? 0m) * registration.Scale);
                break;
            case "ELLIPSE":
                AddElectricalCadPoint(pairs, start, end, "10", "20", registration, points);
                var majorX = ReadFirstDxfDecimal(pairs, start, end, "11") ?? 0m;
                var majorY = ReadFirstDxfDecimal(pairs, start, end, "21") ?? 0m;
                var ratio = Math.Abs(ReadFirstDxfDecimal(pairs, start, end, "40") ?? 1m);
                var majorLength = (decimal)Math.Sqrt((double)((majorX * majorX) + (majorY * majorY)));
                envelopeExpansion = majorLength * Math.Abs(registration.Scale) * Math.Max(1m, ratio);
                break;
            case "TEXT":
                AddElectricalCadPoint(pairs, start, end, "10", "20", registration, points);
                AddElectricalCadPoint(pairs, start, end, "11", "21", registration, points);
                break;
            case "MTEXT":
            case "POINT":
            case "VERTEX":
            case "INSERT":
                AddElectricalCadPoint(pairs, start, end, "10", "20", registration, points);
                break;
            case "LWPOLYLINE":
            case "HATCH":
            case "SPLINE":
                AddRepeatedElectricalCadPoints(pairs, start, end, registration, points);
                break;
            case "SOLID":
            case "3DFACE":
            case "TRACE":
                foreach (var xCode in new[] { "10", "11", "12", "13" })
                {
                    AddElectricalCadPoint(
                        pairs,
                        start,
                        end,
                        xCode,
                        (int.Parse(xCode, CultureInfo.InvariantCulture) + 10).ToString(CultureInfo.InvariantCulture),
                        registration,
                        points);
                }
                break;
            case "DIMENSION":
                foreach (var xCode in new[] { "10", "11", "12", "13", "14", "15", "16" })
                {
                    AddElectricalCadPoint(
                        pairs,
                        start,
                        end,
                        xCode,
                        (int.Parse(xCode, CultureInfo.InvariantCulture) + 10).ToString(CultureInfo.InvariantCulture),
                        registration,
                        points);
                }
                break;
            default:
                supportsRigidMove = false;
                AddRepeatedElectricalCadPoints(pairs, start, end, registration, points);
                break;
        }

        var hasNonZeroBulge = HasNonZeroBulge(pairs, start, end);
        var supportsVertexStretch =
            kind == "LINE" && points.Count == 2 ||
            kind == "LWPOLYLINE" && points.Count == 2 && !hasNonZeroBulge;
        return new ElectricalCadDxfEntity(
            ordinal,
            kind,
            layer,
            points,
            supportsRigidMove,
            supportsVertexStretch,
            hasNonZeroBulge,
            envelopeExpansion,
            IsRegistrationAnchorLayer(layer));
    }

    private static void AddElectricalCadPoint(
        IReadOnlyList<DxfPair> pairs,
        int start,
        int end,
        string xCode,
        string yCode,
        SheetRegistrationTransform registration,
        ICollection<ElectricalCadPointBinding> points)
    {
        for (var index = start; index + 1 < end; index++)
        {
            if (pairs[index].Code != xCode ||
                pairs[index + 1].Code != yCode ||
                !TryParseDecimal(pairs[index].Value, out var x) ||
                !TryParseDecimal(pairs[index + 1].Value, out var y))
            {
                continue;
            }

            var canonical = RegisterElectricalPoint(x, y, registration);
            points.Add(new ElectricalCadPointBinding(
                canonical.X,
                canonical.Y,
                index,
                index + 1));
            return;
        }
    }

    private static void AddRepeatedElectricalCadPoints(
        IReadOnlyList<DxfPair> pairs,
        int start,
        int end,
        SheetRegistrationTransform registration,
        ICollection<ElectricalCadPointBinding> points)
    {
        for (var index = start; index + 1 < end; index++)
        {
            if (pairs[index].Code != "10" ||
                pairs[index + 1].Code != "20" ||
                !TryParseDecimal(pairs[index].Value, out var x) ||
                !TryParseDecimal(pairs[index + 1].Value, out var y))
            {
                continue;
            }

            var canonical = RegisterElectricalPoint(x, y, registration);
            points.Add(new ElectricalCadPointBinding(canonical.X, canonical.Y, index, index + 1));
            index++;
        }
    }

    private static int FindDxfRecordEnd(IReadOnlyList<DxfPair> pairs, int start)
    {
        var end = start;
        while (end < pairs.Count && pairs[end].Code != "0")
        {
            end++;
        }

        return end;
    }

    private static bool HasNonZeroBulge(IReadOnlyList<DxfPair> pairs, int start, int end)
        => pairs
            .Skip(start)
            .Take(end - start)
            .Any(pair => pair.Code == "42" && TryParseDecimal(pair.Value, out var bulge) && bulge != 0m);

    private static decimal? ReadFirstDxfDecimal(
        IReadOnlyList<DxfPair> pairs,
        int start,
        int end,
        string code)
    {
        for (var index = start; index < end; index++)
        {
            if (pairs[index].Code == code && TryParseDecimal(pairs[index].Value, out var value))
            {
                return value;
            }
        }

        return null;
    }

    private static ElectricalCadCutSide ClassifyElectricalCadEntity(
        ElectricalCadDxfEntity entity,
        AdjustmentRecipeStretchActionDto action,
        decimal tolerance)
    {
        var isWidth = string.Equals(action.AxisTag, "Width", StringComparison.OrdinalIgnoreCase);
        if (!isWidth && !string.Equals(action.AxisTag, "Height", StringComparison.OrdinalIgnoreCase))
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                $"Electrical CAD stretch action '{action.ActionId}' has unsupported axis '{action.AxisTag}'.");
        }

        var coordinates = entity.Points.Select(point => isWidth ? point.X : point.Y).ToArray();
        var min = coordinates.Min() - entity.EnvelopeExpansion;
        var max = coordinates.Max() + entity.EnvelopeExpansion;
        var minDistance = SignedElectricalClosingDistance(min, action.CutCoordinate, action.Edge);
        var maxDistance = SignedElectricalClosingDistance(max, action.CutCoordinate, action.Edge);
        var lower = Math.Min(minDistance, maxDistance);
        var upper = Math.Max(minDistance, maxDistance);
        if (lower > tolerance)
        {
            return ElectricalCadCutSide.Closing;
        }

        if (upper < -tolerance)
        {
            return ElectricalCadCutSide.Fixed;
        }

        return ElectricalCadCutSide.Crossing;
    }

    private static decimal SignedElectricalClosingDistance(
        decimal coordinate,
        decimal cutCoordinate,
        string edge)
    {
        if (string.Equals(edge, "Right", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(edge, "Top", StringComparison.OrdinalIgnoreCase))
        {
            return coordinate - cutCoordinate;
        }

        if (string.Equals(edge, "Left", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(edge, "Bottom", StringComparison.OrdinalIgnoreCase))
        {
            return cutCoordinate - coordinate;
        }

        throw new ProjectedPlanSheetManualReviewRequiredException(
            $"Electrical CAD stretch has unsupported closing edge '{edge}'.");
    }

    private static decimal PointResidual(
        ElectricalCadPointBinding point,
        decimal expectedX,
        decimal expectedY)
        => Math.Max(Math.Abs(point.X - expectedX), Math.Abs(point.Y - expectedY));

    private static string ElectricalCadEntityId(int ordinal)
        => $"ELECTRICAL:{ordinal}";

    private static (double X, double Y) ProjectCadStretchPoint(
        decimal electricalX,
        decimal electricalY,
        int xPairIndex,
        int yPairIndex,
        ElectricalCadStretchProjectionPlan stretchPlan,
        ProjectedPlanSheetExportRecipe recipe)
    {
        var canonical = RegisterElectricalPoint(
            electricalX,
            electricalY,
            recipe.RegistrationTransform);
        if (stretchPlan.DeltasByPair.TryGetValue((xPairIndex, yPairIndex), out var delta))
        {
            canonical = (canonical.X + delta.DeltaX, canonical.Y + delta.DeltaY);
        }

        return (
            (double)((canonical.X * recipe.CanonicalRecipe.FloorToSiteScale) + recipe.CanonicalRecipe.SiteOffsetX),
            (double)((canonical.Y * recipe.CanonicalRecipe.FloorToSiteScale) + recipe.CanonicalRecipe.SiteOffsetY));
    }

    private static RecipeProjectionPlan? BuildRecipeProjectionPlan(
        IReadOnlyList<DxfPair> pairs,
        ProjectedPlanSheetExportRecipe? recipe)
    {
        if (recipe is null)
        {
            return null;
        }

        if (recipe.RegistrationStatus is not SheetRegistrationStatus.Confirmed ||
            recipe.WholePlanRegistrationProof?.IsAuthoritative != true)
        {
            var legacySelection = TryCollectRegisteredAnchorBounds(
                pairs,
                recipe.RegistrationTransform,
                recipe.CanonicalSourceWidthInches,
                recipe.CanonicalSourceHeightInches);
            var hasCanonicalDimensions = recipe.CanonicalSourceWidthInches.HasValue ||
                                         recipe.CanonicalSourceHeightInches.HasValue;
            if (!legacySelection.Dominant.IsSelected &&
                (hasCanonicalDimensions ||
                 legacySelection.Dominant.Status == DominantOutlineSelectionStatus.Ambiguous))
            {
                ThrowDominantOutlineManualReview("Electrical source", legacySelection.Dominant);
            }

            throw new ProjectedPlanSheetManualReviewRequiredException(
                "Electrical recipe export requires a confirmed, current, passed whole-plan registration proof; missing, legacy, or invalid proof requires manual review.");
        }

        if ((recipe.CanonicalSourceWidthInches.HasValue && recipe.CanonicalSourceWidthInches.Value <= 0m) ||
            (recipe.CanonicalSourceHeightInches.HasValue && recipe.CanonicalSourceHeightInches.Value <= 0m))
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                "Electrical recipe export requires positive canonical source dimensions when dimensions are present; manual review is required.");
        }

        var authoritativeOperations = recipe.CanonicalRecipe.Operations
            .Select(operation => new OperationProjectionPlan(operation, operation, AnchorReason: null))
            .ToArray();
        var authoritativeRecipe = recipe with { OutlineNormalization = null };
        return new RecipeProjectionPlan(
            authoritativeRecipe,
            authoritativeOperations,
            SourceBounds: null,
            UsesAuthoritativeWholePlanProof: true);
    }

    private static decimal NormalizeAxis(
        decimal value,
        decimal min,
        decimal max,
        decimal scale,
        string anchor)
        => anchor switch
        {
            "Min" => min + ((value - min) * scale),
            "Max" => max - ((max - value) * scale),
            _ => ((min + max) / 2m) + ((value - ((min + max) / 2m)) * scale)
        };

    private static OperationProjectionPlan PlanOperation(
        AdjustmentRecipeOperationDto operation,
        RegisteredAnchorBounds? bounds,
        IDictionary<string, decimal> edgeOffsets)
    {
        if (bounds is null)
        {
            return new OperationProjectionPlan(operation, operation, null);
        }

        var edgeOffsetKey = $"{operation.AxisTag}:{operation.Edge}";
        edgeOffsets.TryGetValue(edgeOffsetKey, out var cumulativeEdgeOffset);
        var effectiveCoordinate = operation.Coordinate;
        string? reason = null;
        if (IsHorizontalRecipeOperation(operation) &&
            string.Equals(operation.Edge, "Right", StringComparison.OrdinalIgnoreCase) &&
            operation.Coordinate > bounds.MaxX)
        {
            effectiveCoordinate = bounds.MaxX - cumulativeEdgeOffset;
            reason = BuildEdgeAnchorReason(operation, "max X", bounds.MaxX);
        }
        else if (IsHorizontalRecipeOperation(operation) &&
                 string.Equals(operation.Edge, "Left", StringComparison.OrdinalIgnoreCase) &&
                 operation.Coordinate < bounds.MinX)
        {
            effectiveCoordinate = bounds.MinX + cumulativeEdgeOffset;
            reason = BuildEdgeAnchorReason(operation, "min X", bounds.MinX);
        }
        else if (IsVerticalRecipeOperation(operation) &&
                 string.Equals(operation.Edge, "Top", StringComparison.OrdinalIgnoreCase) &&
                 operation.Coordinate > bounds.MaxY)
        {
            effectiveCoordinate = bounds.MaxY - cumulativeEdgeOffset;
            reason = BuildEdgeAnchorReason(operation, "max Y", bounds.MaxY);
        }
        else if (IsVerticalRecipeOperation(operation) &&
                 string.Equals(operation.Edge, "Bottom", StringComparison.OrdinalIgnoreCase) &&
                 operation.Coordinate < bounds.MinY)
        {
            effectiveCoordinate = bounds.MinY + cumulativeEdgeOffset;
            reason = BuildEdgeAnchorReason(operation, "min Y", bounds.MinY);
        }

        if (reason is null)
        {
            return new OperationProjectionPlan(operation, operation, null);
        }

        var planned = new OperationProjectionPlan(
            operation,
            operation with { Coordinate = effectiveCoordinate },
            reason);
        edgeOffsets[edgeOffsetKey] = cumulativeEdgeOffset + operation.DeltaSourceUnits;
        return planned;
    }

    private static string BuildEdgeAnchorReason(
        AdjustmentRecipeOperationDto operation,
        string edgeName,
        decimal edgeCoordinate)
        => $"Applied using dependent-sheet edge anchor: canonical {operation.Edge} coordinate {operation.Coordinate} was outside registered Electrical anchor bounds; used {edgeName} {edgeCoordinate}.";

    private static RegisteredAnchorSelection TryCollectRegisteredAnchorBounds(
        IReadOnlyList<DxfPair> pairs,
        SheetRegistrationTransform registration,
        decimal? expectedWidth,
        decimal? expectedHeight)
    {
        var structuralSegments = new List<AxisAlignedStructuralSegment>();
        var wallBounds = new MutableRegisteredAnchorBounds();
        var allBounds = new MutableRegisteredAnchorBounds();
        var currentSection = string.Empty;
        string? polylineLayer = null;
        var polylinePoints = new List<RegisteredPoint>();
        var polylineClosed = false;

        for (var index = 0; index < pairs.Count;)
        {
            var pair = pairs[index];
            var value = pair.Value.Trim().ToUpperInvariant();
            if (pair.Code != "0")
            {
                index++;
                continue;
            }

            if (value == "SECTION" && index + 1 < pairs.Count)
            {
                index++;
                if (pairs[index].Code == "2")
                {
                    currentSection = pairs[index].Value.Trim().ToUpperInvariant();
                }

                index++;
                continue;
            }

            if (value == "ENDSEC")
            {
                currentSection = string.Empty;
                index++;
                continue;
            }

            var end = index + 1;
            while (end < pairs.Count && pairs[end].Code != "0")
            {
                end++;
            }

            if (currentSection != "ENTITIES")
            {
                index = end;
                continue;
            }

            var declaredLayer = ResolveEntityPairValue(pairs, index + 1, "8", end);
            var layer = string.IsNullOrWhiteSpace(declaredLayer) ? polylineLayer ?? string.Empty : declaredLayer;
            var isAnchorLayer = IsRegistrationAnchorLayer(layer);
            for (var coordinateIndex = index + 1; coordinateIndex + 1 < end; coordinateIndex++)
            {
                var xPair = pairs[coordinateIndex];
                var yPair = pairs[coordinateIndex + 1];
                if (!IsXCoordinateCode(xPair.Code) ||
                    !IsMatchingYCoordinateCode(xPair.Code, yPair.Code) ||
                    !TryParseDecimal(xPair.Value, out var x) ||
                    !TryParseDecimal(yPair.Value, out var y))
                {
                    continue;
                }

                var floor = RegisterElectricalPoint(x, y, registration);
                allBounds.Include(floor.X, floor.Y);
                if (isAnchorLayer && IsStructuralOutlineCoordinate(value, xPair.Code))
                {
                    wallBounds.Include(floor.X, floor.Y);
                }
            }

            if (value == "POLYLINE")
            {
                polylineLayer = declaredLayer;
                polylineClosed = IsClosedPolyline(pairs, index + 1, end);
                polylinePoints.Clear();
            }
            else if (value == "VERTEX")
            {
                if (IsRegistrationAnchorLayer(polylineLayer) &&
                    TryReadRegisteredPoint(pairs, index + 1, end, "10", "20", registration, out var vertex))
                {
                    polylinePoints.Add(vertex);
                }
            }
            else if (value == "SEQEND")
            {
                AddConnectedRegisteredSegments(polylinePoints, polylineClosed, structuralSegments);
                polylineLayer = null;
                polylineClosed = false;
                polylinePoints.Clear();
            }
            else if (isAnchorLayer)
            {
                AddRegisteredEntitySegments(
                    value,
                    pairs,
                    index + 1,
                    end,
                    registration,
                    structuralSegments);
            }

            index = end;
        }

        var legacyBounds = wallBounds.HasData
            ? wallBounds.ToImmutable(isStructural: true)
            : allBounds.HasData
                ? allBounds.ToImmutable(isStructural: false)
                : null;
        return new RegisteredAnchorSelection(
            DominantAxisAlignedOutlineSelector.Select(structuralSegments, expectedWidth, expectedHeight),
            legacyBounds);
    }

    private static void AddRegisteredEntitySegments(
        string entityType,
        IReadOnlyList<DxfPair> pairs,
        int start,
        int end,
        SheetRegistrationTransform registration,
        ICollection<AxisAlignedStructuralSegment> segments)
    {
        if (entityType == "LINE" &&
            TryReadRegisteredPoint(pairs, start, end, "10", "20", registration, out var lineStart) &&
            TryReadRegisteredPoint(pairs, start, end, "11", "21", registration, out var lineEnd))
        {
            segments.Add(new AxisAlignedStructuralSegment(lineStart.X, lineStart.Y, lineEnd.X, lineEnd.Y));
            return;
        }

        if (entityType is not ("LWPOLYLINE" or "3DFACE" or "SOLID"))
        {
            return;
        }

        var points = ReadRegisteredEntityPoints(pairs, start, end, registration);
        var isClosed = entityType is "3DFACE" or "SOLID" || IsClosedPolyline(pairs, start, end);
        AddConnectedRegisteredSegments(points, isClosed, segments);
    }

    private static IReadOnlyList<RegisteredPoint> ReadRegisteredEntityPoints(
        IReadOnlyList<DxfPair> pairs,
        int start,
        int end,
        SheetRegistrationTransform registration)
    {
        var points = new List<RegisteredPoint>();
        for (var index = start; index + 1 < end; index++)
        {
            var xCode = pairs[index].Code;
            var yCode = xCode switch
            {
                "10" => "20",
                "11" => "21",
                "12" => "22",
                "13" => "23",
                _ => null
            };
            if (yCode is null ||
                pairs[index + 1].Code != yCode ||
                !TryParseDecimal(pairs[index].Value, out var x) ||
                !TryParseDecimal(pairs[index + 1].Value, out var y))
            {
                continue;
            }

            var registered = RegisterElectricalPoint(x, y, registration);
            points.Add(new RegisteredPoint(registered.X, registered.Y));
        }

        return points;
    }

    private static bool TryReadRegisteredPoint(
        IReadOnlyList<DxfPair> pairs,
        int start,
        int end,
        string xCode,
        string yCode,
        SheetRegistrationTransform registration,
        out RegisteredPoint point)
    {
        var xValue = ResolveEntityPairValue(pairs, start, xCode, end);
        var yValue = ResolveEntityPairValue(pairs, start, yCode, end);
        if (TryParseDecimal(xValue, out var x) && TryParseDecimal(yValue, out var y))
        {
            var registered = RegisterElectricalPoint(x, y, registration);
            point = new RegisteredPoint(registered.X, registered.Y);
            return true;
        }

        point = default;
        return false;
    }

    private static void AddConnectedRegisteredSegments(
        IReadOnlyList<RegisteredPoint> points,
        bool isClosed,
        ICollection<AxisAlignedStructuralSegment> segments)
    {
        for (var index = 0; index + 1 < points.Count; index++)
        {
            AddRegisteredSegment(points[index], points[index + 1], segments);
        }

        if (isClosed && points.Count > 2)
        {
            AddRegisteredSegment(points[^1], points[0], segments);
        }
    }

    private static void AddRegisteredSegment(
        RegisteredPoint start,
        RegisteredPoint end,
        ICollection<AxisAlignedStructuralSegment> segments)
        => segments.Add(new AxisAlignedStructuralSegment(start.X, start.Y, end.X, end.Y));

    private static bool IsClosedPolyline(IReadOnlyList<DxfPair> pairs, int start, int end)
        => int.TryParse(ResolveEntityPairValue(pairs, start, "70", end), out var flags) &&
           (flags & 1) != 0;

    private static RegisteredAnchorBounds ToRegisteredAnchorBounds(DominantAxisAlignedOutline outline)
        => new(outline.MinX, outline.MinY, outline.MaxX, outline.MaxY, IsStructural: true);

    private static void ThrowDominantOutlineManualReview(
        string context,
        DominantAxisAlignedOutlineSelection selection)
        => throw new ProjectedPlanSheetManualReviewRequiredException(
            $"{context} dominant structural outline mismatch after registration: {selection.Reason} Canonical dimensions only validate structural evidence and cannot infer an outline scale; manual review is required.");

    private static bool IsStructuralOutlineCoordinate(string entityType, string xCode)
        => entityType is "LINE" or "LWPOLYLINE" or "POLYLINE" or "VERTEX" or "3DFACE" or "SOLID" &&
           xCode is "10" or "11" or "12" or "13";

    private static bool IsRegistrationAnchorLayer(string? layer)
        => !string.IsNullOrWhiteSpace(layer) &&
           (layer.Contains("WALL", StringComparison.OrdinalIgnoreCase) ||
            layer.Contains("EXTERIOR", StringComparison.OrdinalIgnoreCase) ||
            layer.Contains("STRUCT", StringComparison.OrdinalIgnoreCase));

    private static List<OperationAuditAccumulator> BuildOperationAuditAccumulators(
        RecipeProjectionPlan? recipePlan)
        => recipePlan is null
            ? []
            : recipePlan.Operations
                .Select((operation, index) => new OperationAuditAccumulator(operation.Original, index, operation.AnchorReason))
                .ToList();

    private static void RecordAppliedOperations(
        decimal electricalX,
        decimal electricalY,
        RecipeProjectionPlan? recipePlan,
        IReadOnlyList<OperationAuditAccumulator> accumulators,
        string entityKey)
    {
        if (recipePlan is null || accumulators.Count == 0)
        {
            return;
        }

        var registered = RegisterElectricalPoint(electricalX, electricalY, recipePlan.EffectiveRecipe.RegistrationTransform);
        var floor = NormalizeRegisteredPoint(registered, recipePlan.EffectiveRecipe.OutlineNormalization);
        for (var index = 0; index < recipePlan.Operations.Count; index++)
        {
            var operation = recipePlan.Operations[index].Effective;
            if (OperationApplies(floor.X, floor.Y, operation))
            {
                accumulators[index].Record(entityKey);
            }
        }
    }

    private static (decimal X, decimal Y) NormalizeRegisteredPoint(
        (decimal X, decimal Y) point,
        ProjectedPlanSheetOutlineNormalization? normalization)
    {
        if (normalization is null)
        {
            return point;
        }

        return (
            NormalizeAxis(
                point.X,
                normalization.SourceMinX,
                normalization.SourceMaxX,
                normalization.ScaleX,
                normalization.AnchorX),
            NormalizeAxis(
                point.Y,
                normalization.SourceMinY,
                normalization.SourceMaxY,
                normalization.ScaleY,
                normalization.AnchorY));
    }

    private static bool OperationApplies(
        decimal floorX,
        decimal floorY,
        FloorplanFit.Contracts.FloorPlans.AdjustmentRecipeOperationDto operation)
        => (IsHorizontalRecipeOperation(operation) &&
            string.Equals(operation.Edge, "Right", StringComparison.OrdinalIgnoreCase) &&
            floorX >= operation.Coordinate - CoordinateTolerance) ||
           (IsHorizontalRecipeOperation(operation) &&
            string.Equals(operation.Edge, "Left", StringComparison.OrdinalIgnoreCase) &&
            floorX <= operation.Coordinate + CoordinateTolerance) ||
           (IsVerticalRecipeOperation(operation) &&
            string.Equals(operation.Edge, "Top", StringComparison.OrdinalIgnoreCase) &&
            floorY >= operation.Coordinate - CoordinateTolerance) ||
           (IsVerticalRecipeOperation(operation) &&
            string.Equals(operation.Edge, "Bottom", StringComparison.OrdinalIgnoreCase) &&
            floorY <= operation.Coordinate + CoordinateTolerance);

    private static void ThrowIfUnsupportedRecipeEntities(
        IReadOnlyList<DxfPair> pairs,
        IReadOnlySet<string> dimensionBlockNames,
        ProjectedPlanSheetExportRecipe? recipe,
        CancellationToken cancellationToken)
    {
        if (recipe is null || recipe.CanonicalRecipe.Operations.Count == 0)
        {
            return;
        }

        var currentSection = string.Empty;
        var currentBlockName = string.Empty;
        for (var index = 0; index < pairs.Count;)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pair = pairs[index];
            var value = pair.Value.Trim().ToUpperInvariant();
            if (pair.Code != "0")
            {
                index++;
                continue;
            }

            if (value == "SECTION" && index + 1 < pairs.Count)
            {
                index++;
                if (pairs[index].Code == "2")
                {
                    currentSection = pairs[index].Value.Trim().ToUpperInvariant();
                }

                index++;
                continue;
            }

            if (value == "ENDSEC")
            {
                currentSection = string.Empty;
                index++;
                continue;
            }

            if (currentSection == "BLOCKS" && value == "BLOCK")
            {
                currentBlockName = ResolveEntityPairValue(pairs, index + 1, "2");
                index++;
                continue;
            }

            if (currentSection == "BLOCKS" && value == "ENDBLK")
            {
                currentBlockName = string.Empty;
                index++;
                continue;
            }

            var end = index + 1;
            while (end < pairs.Count && pairs[end].Code != "0")
            {
                cancellationToken.ThrowIfCancellationRequested();
                end++;
            }

            var shouldProjectEntity =
                currentSection == "ENTITIES" ||
                (currentSection == "BLOCKS" && dimensionBlockNames.Contains(currentBlockName));
            if (shouldProjectEntity &&
                !IsRecipeProjectionSupportedEntity(value) &&
                ContainsCoordinatePair(pairs, index + 1, end))
            {
                throw new ProjectedPlanSheetManualReviewRequiredException(
                    $"{value} is not supported for recipe-aware electrical export; manual electrical review is required.");
            }

            index = end;
        }
    }

    private static void ReplaceRecipeCrossingCircularCurvesWithPolylines(
        List<DxfPair> pairs,
        ProjectedPlanSheetExportRecipe? recipe,
        CancellationToken cancellationToken)
    {
        if (recipe is null || recipe.CanonicalRecipe.Operations.Count == 0)
        {
            return;
        }

        var currentSection = string.Empty;
        for (var index = 0; index < pairs.Count;)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pair = pairs[index];
            var value = pair.Value.Trim().ToUpperInvariant();
            if (pair.Code != "0")
            {
                index++;
                continue;
            }

            if (value == "SECTION" && index + 1 < pairs.Count)
            {
                index++;
                if (pairs[index].Code == "2")
                {
                    currentSection = pairs[index].Value.Trim().ToUpperInvariant();
                }

                index++;
                continue;
            }

            if (value == "ENDSEC")
            {
                currentSection = string.Empty;
                index++;
                continue;
            }

            var end = index + 1;
            while (end < pairs.Count && pairs[end].Code != "0")
            {
                cancellationToken.ThrowIfCancellationRequested();
                end++;
            }

            decimal centerX;
            decimal centerY;
            decimal radius;
            if (currentSection == "ENTITIES" &&
                value == "ARC" &&
                TryReadArc(pairs, index + 1, end, out centerX, out centerY, out radius, out var startAngle, out var endAngle) &&
                ArcCrossesRecipePinch(centerX, centerY, radius, startAngle, endAngle, recipe))
            {
                var polyline = BuildPolylineFromArc(pairs, index + 1, end, centerX, centerY, radius, startAngle, endAngle);
                pairs.RemoveRange(index, end - index);
                pairs.InsertRange(index, polyline);
                index += polyline.Count;
                continue;
            }

            if (currentSection == "ENTITIES" &&
                value == "CIRCLE" &&
                TryReadCurveCenterAndRadius(pairs, index + 1, end, value, out centerX, out centerY, out radius) &&
                CurveCrossesRecipePinch(centerX, centerY, radius, recipe))
            {
                var polyline = BuildPolylineFromCircle(pairs, index + 1, end, centerX, centerY, radius);
                pairs.RemoveRange(index, end - index);
                pairs.InsertRange(index, polyline);
                index += polyline.Count;
                continue;
            }

            index = end;
        }
    }

    private static void ThrowIfUnsupportedRecipeCurveCrossings(
        IReadOnlyList<DxfPair> pairs,
        IReadOnlySet<string> dimensionBlockNames,
        ProjectedPlanSheetExportRecipe? recipe,
        CancellationToken cancellationToken)
    {
        if (recipe is null || recipe.CanonicalRecipe.Operations.Count == 0)
        {
            return;
        }

        var currentSection = string.Empty;
        var currentBlockName = string.Empty;
        for (var index = 0; index < pairs.Count;)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pair = pairs[index];
            var value = pair.Value.Trim().ToUpperInvariant();
            if (pair.Code != "0")
            {
                index++;
                continue;
            }

            if (value == "SECTION" && index + 1 < pairs.Count)
            {
                index++;
                if (pairs[index].Code == "2")
                {
                    currentSection = pairs[index].Value.Trim().ToUpperInvariant();
                }

                index++;
                continue;
            }

            if (value == "ENDSEC")
            {
                currentSection = string.Empty;
                index++;
                continue;
            }

            if (currentSection == "BLOCKS" && value == "BLOCK")
            {
                currentBlockName = ResolveEntityPairValue(pairs, index + 1, "2");
                index++;
                continue;
            }

            if (currentSection == "BLOCKS" && value == "ENDBLK")
            {
                currentBlockName = string.Empty;
                index++;
                continue;
            }

            var end = index + 1;
            while (end < pairs.Count && pairs[end].Code != "0")
            {
                cancellationToken.ThrowIfCancellationRequested();
                end++;
            }

            var shouldInspectEntity =
                currentSection == "ENTITIES" ||
                (currentSection == "BLOCKS" && dimensionBlockNames.Contains(currentBlockName));
            if (shouldInspectEntity && IsGuardedCurveEntity(value))
            {
                decimal centerX;
                decimal centerY;
                decimal radius;
                if (value == "ARC" &&
                    TryReadArc(pairs, index + 1, end, out centerX, out centerY, out radius, out var startAngle, out var endAngle) &&
                    ArcCrossesRecipePinch(centerX, centerY, radius, startAngle, endAngle, recipe))
                {
                    throw new ProjectedPlanSheetManualReviewRequiredException(
                        $"{value} crosses a canonical recipe pinch line; manual electrical review is required before recipe-aware export.");
                }

                if (value != "ARC" &&
                    TryReadCurveCenterAndRadius(pairs, index + 1, end, value, out centerX, out centerY, out radius) &&
                    CurveCrossesRecipePinch(centerX, centerY, radius, recipe))
                {
                    throw new ProjectedPlanSheetManualReviewRequiredException(
                        $"{value} crosses a canonical recipe pinch line; manual electrical review is required before recipe-aware export.");
                }
            }

            index = end;
        }
    }

    private static bool TryReadArc(
        IReadOnlyList<DxfPair> pairs,
        int start,
        int end,
        out decimal centerX,
        out decimal centerY,
        out decimal radius,
        out decimal startAngle,
        out decimal endAngle)
    {
        centerX = 0m;
        centerY = 0m;
        radius = 0m;
        startAngle = 0m;
        endAngle = 0m;
        var hasX = false;
        var hasY = false;
        var hasRadius = false;
        var hasStart = false;
        var hasEnd = false;

        for (var index = start; index < end; index++)
        {
            var pair = pairs[index];
            if (pair.Code == "10" && TryParseDecimal(pair.Value, out centerX))
            {
                hasX = true;
            }
            else if (pair.Code == "20" && TryParseDecimal(pair.Value, out centerY))
            {
                hasY = true;
            }
            else if (pair.Code == "40" && TryParseDecimal(pair.Value, out radius))
            {
                hasRadius = true;
            }
            else if (pair.Code == "50" && TryParseDecimal(pair.Value, out startAngle))
            {
                hasStart = true;
            }
            else if (pair.Code == "51" && TryParseDecimal(pair.Value, out endAngle))
            {
                hasEnd = true;
            }
        }

        return hasX && hasY && hasRadius && hasStart && hasEnd && radius > 0m;
    }

    private static List<DxfPair> BuildPolylineFromArc(
        IReadOnlyList<DxfPair> pairs,
        int start,
        int end,
        decimal centerX,
        decimal centerY,
        decimal radius,
        decimal startAngle,
        decimal endAngle)
    {
        var points = SampleArcPoints(centerX, centerY, radius, startAngle, endAngle);
        return BuildPolylineFromPoints(pairs, start, end, points, closed: false);
    }

    private static List<DxfPair> BuildPolylineFromCircle(
        IReadOnlyList<DxfPair> pairs,
        int start,
        int end,
        decimal centerX,
        decimal centerY,
        decimal radius)
    {
        var points = SampleCirclePoints(centerX, centerY, radius);
        return BuildPolylineFromPoints(pairs, start, end, points, closed: true);
    }

    private static List<DxfPair> BuildPolylineFromPoints(
        IReadOnlyList<DxfPair> pairs,
        int start,
        int end,
        IReadOnlyList<(double X, double Y)> points,
        bool closed)
    {
        var polyline = new List<DxfPair>(8 + (points.Count * 2))
        {
            new("0", "LWPOLYLINE")
        };

        var copiedIdentityCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = start; index < end; index++)
        {
            var pair = pairs[index];
            if (IsPolylineIdentityCode(pair.Code) && copiedIdentityCodes.Add(pair.Code))
            {
                polyline.Add(pair);
            }
        }

        polyline.Add(new DxfPair("100", "AcDbEntity"));
        var copiedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = start; index < end; index++)
        {
            var pair = pairs[index];
            if (IsArcPolylineStyleCode(pair.Code) && copiedCodes.Add(pair.Code))
            {
                polyline.Add(pair);
            }
        }

        if (!copiedCodes.Contains("8"))
        {
            polyline.Add(new DxfPair("8", "0"));
        }

        polyline.Add(new DxfPair("100", "AcDbPolyline"));
        polyline.Add(new DxfPair("90", points.Count.ToString(CultureInfo.InvariantCulture)));
        polyline.Add(new DxfPair("70", closed ? "1" : "0"));
        foreach (var point in points)
        {
            polyline.Add(new DxfPair("10", FormatDouble(point.X)));
            polyline.Add(new DxfPair("20", FormatDouble(point.Y)));
        }

        return polyline;
    }

    private static bool IsPolylineIdentityCode(string code)
        => code is "5" or "330";

    private static bool IsArcPolylineStyleCode(string code)
        => code is "8" or "6" or "48" or "62" or "67" or "370" or "410" or "420" or "430" or "440";

    private static List<(double X, double Y)> SampleArcPoints(
        decimal centerX,
        decimal centerY,
        decimal radius,
        decimal startAngle,
        decimal endAngle)
    {
        var sweep = NormalizeArcSweepDegrees(startAngle, endAngle);
        var segments = Math.Max(8, (int)Math.Ceiling(sweep / 10d));
        var points = new List<(double X, double Y)>(segments + 1);
        for (var index = 0; index <= segments; index++)
        {
            var angle = ((double)startAngle + (sweep * index / segments)) * Math.PI / 180d;
            points.Add((
                NormalizeTiny((double)centerX + ((double)radius * Math.Cos(angle))),
                NormalizeTiny((double)centerY + ((double)radius * Math.Sin(angle)))));
        }

        return points;
    }

    private static List<(double X, double Y)> SampleCirclePoints(
        decimal centerX,
        decimal centerY,
        decimal radius)
    {
        const int segments = 36;
        var points = new List<(double X, double Y)>(segments);
        for (var index = 0; index < segments; index++)
        {
            var angle = index * 10d * Math.PI / 180d;
            points.Add((
                NormalizeTiny((double)centerX + ((double)radius * Math.Cos(angle))),
                NormalizeTiny((double)centerY + ((double)radius * Math.Sin(angle)))));
        }

        return points;
    }

    private static double NormalizeArcSweepDegrees(decimal startAngle, decimal endAngle)
    {
        var sweep = (double)(endAngle - startAngle);
        while (sweep <= 0d)
        {
            sweep += 360d;
        }

        while (sweep > 360d)
        {
            sweep -= 360d;
        }

        return sweep;
    }

    private static bool TryReadCurveCenterAndRadius(
        IReadOnlyList<DxfPair> pairs,
        int start,
        int end,
        string entityType,
        out decimal centerX,
        out decimal centerY,
        out decimal radius)
    {
        centerX = 0m;
        centerY = 0m;
        radius = 0m;
        var hasX = false;
        var hasY = false;
        var hasRadius = false;
        decimal majorX = 0m;
        decimal majorY = 0m;
        decimal ratio = 1m;
        var hasMajorX = false;
        var hasMajorY = false;

        for (var index = start; index < end; index++)
        {
            var pair = pairs[index];
            if (pair.Code == "10" && TryParseDecimal(pair.Value, out centerX))
            {
                hasX = true;
            }
            else if (pair.Code == "20" && TryParseDecimal(pair.Value, out centerY))
            {
                hasY = true;
            }
            else if (entityType == "ELLIPSE" && pair.Code == "40" && TryParseDecimal(pair.Value, out var parsedRatio))
            {
                ratio = parsedRatio;
            }
            else if (pair.Code == "40" && TryParseDecimal(pair.Value, out radius))
            {
                hasRadius = true;
            }
            else if (entityType == "ELLIPSE" && pair.Code == "11" && TryParseDecimal(pair.Value, out majorX))
            {
                hasMajorX = true;
            }
            else if (entityType == "ELLIPSE" && pair.Code == "21" && TryParseDecimal(pair.Value, out majorY))
            {
                hasMajorY = true;
            }
        }

        if (entityType != "ELLIPSE")
        {
            return hasX && hasY && hasRadius;
        }

        if (!hasX || !hasY || !hasMajorX || !hasMajorY)
        {
            return false;
        }

        // ponytail: ELLIPSE crossing uses a bounding radius; split/deform ellipses later only if real exports demand it.
        var majorRadius = Math.Sqrt(Math.Pow((double)majorX, 2d) + Math.Pow((double)majorY, 2d));
        radius = (decimal)(majorRadius * Math.Max(1d, Math.Abs((double)ratio)));
        return true;
    }

    private static bool ArcCrossesRecipePinch(
        decimal electricalCenterX,
        decimal electricalCenterY,
        decimal electricalRadius,
        decimal electricalStartAngle,
        decimal electricalEndAngle,
        ProjectedPlanSheetExportRecipe recipe)
    {
        var points = SampleArcPoints(
            electricalCenterX,
            electricalCenterY,
            electricalRadius,
            electricalStartAngle,
            electricalEndAngle);
        if (points.Count < 2)
        {
            return false;
        }

        var previous = NormalizeRegisteredPoint(
            RegisterElectricalPoint(
                (decimal)points[0].X,
                (decimal)points[0].Y,
                recipe.RegistrationTransform),
            recipe.OutlineNormalization);
        for (var index = 1; index < points.Count; index++)
        {
            var current = NormalizeRegisteredPoint(
                RegisterElectricalPoint(
                    (decimal)points[index].X,
                    (decimal)points[index].Y,
                    recipe.RegistrationTransform),
                recipe.OutlineNormalization);
            foreach (var operation in recipe.CanonicalRecipe.Operations)
            {
                if ((IsHorizontalRecipeOperation(operation) &&
                     SegmentCrossesCoordinate(previous.X, current.X, operation.Coordinate)) ||
                    (IsVerticalRecipeOperation(operation) &&
                     SegmentCrossesCoordinate(previous.Y, current.Y, operation.Coordinate)))
                {
                    return true;
                }
            }

            previous = current;
        }

        return false;
    }

    private static bool CurveCrossesRecipePinch(
        decimal electricalCenterX,
        decimal electricalCenterY,
        decimal electricalRadius,
        ProjectedPlanSheetExportRecipe recipe)
    {
        // ponytail: curve crossing uses full/bounding radius; inspect angles/axes later only if false positives matter.
        var center = NormalizeRegisteredPoint(
            RegisterElectricalPoint(
                electricalCenterX,
                electricalCenterY,
                recipe.RegistrationTransform),
            recipe.OutlineNormalization);
        var outlineScale = recipe.OutlineNormalization is null
            ? 1m
            : Math.Max(recipe.OutlineNormalization.ScaleX, recipe.OutlineNormalization.ScaleY);
        var radius = Math.Abs(electricalRadius * recipe.RegistrationTransform.Scale * outlineScale);

        foreach (var operation in recipe.CanonicalRecipe.Operations)
        {
            if ((IsHorizontalRecipeOperation(operation) &&
                 center.X - radius < operation.Coordinate &&
                 center.X + radius > operation.Coordinate) ||
                (IsVerticalRecipeOperation(operation) &&
                 center.Y - radius < operation.Coordinate &&
                 center.Y + radius > operation.Coordinate))
            {
                return true;
            }
        }

        return false;
    }

    private static bool SegmentCrossesCoordinate(decimal start, decimal end, decimal coordinate)
        => (start <= coordinate && end >= coordinate) ||
           (start >= coordinate && end <= coordinate);

    private static (decimal X, decimal Y) RegisterElectricalPoint(
        decimal x,
        decimal y,
        SheetRegistrationTransform registration)
    {
        var radians = (double)registration.RotationDegrees * Math.PI / 180d;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        var sourceX = (double)x;
        var sourceY = (double)y;
        var scale = (double)registration.Scale;

        return (
            (decimal)NormalizeTiny(((sourceX * cos) - (sourceY * sin)) * scale) + registration.TranslateX,
            (decimal)NormalizeTiny(((sourceX * sin) + (sourceY * cos)) * scale) + registration.TranslateY);
    }

    private static bool IsHorizontalRecipeOperation(FloorplanFit.Contracts.FloorPlans.AdjustmentRecipeOperationDto operation)
        => string.Equals(operation.AxisTag, "Width", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(operation.Kind, "HorizontalCompression", StringComparison.OrdinalIgnoreCase);

    private static bool IsVerticalRecipeOperation(FloorplanFit.Contracts.FloorPlans.AdjustmentRecipeOperationDto operation)
        => string.Equals(operation.AxisTag, "Height", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(operation.Kind, "VerticalCompression", StringComparison.OrdinalIgnoreCase);

    private static HashSet<string> CollectDimensionBlockNames(
        IReadOnlyList<DxfPair> pairs,
        CancellationToken cancellationToken)
    {
        var blockNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var currentSection = string.Empty;

        for (var index = 0; index < pairs.Count;)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pair = pairs[index];
            var value = pair.Value.Trim();
            if (pair.Code == "0" &&
                string.Equals(value, "SECTION", StringComparison.OrdinalIgnoreCase) &&
                index + 1 < pairs.Count)
            {
                index++;

                var sectionPair = pairs[index];
                if (sectionPair.Code == "2")
                {
                    currentSection = sectionPair.Value.Trim().ToUpperInvariant();
                }

                index++;
                continue;
            }

            if (pair.Code == "0" &&
                string.Equals(value, "ENDSEC", StringComparison.OrdinalIgnoreCase))
            {
                currentSection = string.Empty;
                index++;
                continue;
            }

            if (currentSection == "ENTITIES" &&
                pair.Code == "0" &&
                string.Equals(value, "DIMENSION", StringComparison.OrdinalIgnoreCase))
            {
                var entityEnd = index + 1;
                while (entityEnd < pairs.Count && pairs[entityEnd].Code != "0")
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    entityEnd++;
                }

                var blockName = ResolveEntityPairValue(pairs, index + 1, "2", entityEnd);
                if (!string.IsNullOrWhiteSpace(blockName))
                {
                    blockNames.Add(blockName);
                }

                index = entityEnd;
                continue;
            }

            index++;
        }

        return blockNames;
    }

    private static string ResolveEntityPairValue(
        IReadOnlyList<DxfPair> pairs,
        int start,
        string code,
        int? end = null)
    {
        var stop = end ?? pairs.Count;
        for (var index = start; index < stop && pairs[index].Code != "0"; index++)
        {
            if (pairs[index].Code == code)
            {
                return pairs[index].Value.Trim();
            }
        }

        return string.Empty;
    }

    private static bool IsCircleOrArc(string entityType)
        => entityType is "CIRCLE" or "ARC";

    private static bool IsGuardedCurveEntity(string entityType)
        => IsCircleOrArc(entityType) || entityType == "ELLIPSE";

    private static bool IsTextEntity(string entityType)
        => entityType is "TEXT" or "MTEXT";

    private static bool IsArcAngleCode(string code)
        => code is "50" or "51";

    private static bool IsInsertScaleCode(string code)
        => code is "41" or "42" or "43";

    private static bool IsRecipeProjectionSupportedEntity(string entityType)
        => entityType is "LINE" or "LWPOLYLINE" or "SPLINE" or "INSERT" or "TEXT" or "MTEXT" or
            "CIRCLE" or "ARC" or "ELLIPSE" or "DIMENSION" or "POINT" or "HATCH" or "3DFACE" or "SOLID";

    private static bool ContainsCoordinatePair(IReadOnlyList<DxfPair> pairs, int start, int end)
    {
        for (var index = start; index + 1 < end; index++)
        {
            if (IsXCoordinateCode(pairs[index].Code) &&
                IsMatchingYCoordinateCode(pairs[index].Code, pairs[index + 1].Code) &&
                TryParseDecimal(pairs[index].Value, out _) &&
                TryParseDecimal(pairs[index + 1].Value, out _))
            {
                return true;
            }
        }

        return false;
    }

    private static (double X, double Y) ProjectPoint(
        decimal x,
        decimal y,
        SheetAdjustmentProjectionTransform transform)
    {
        var scale = (double)transform.Scale;
        var radians = (double)transform.RotationDegrees * Math.PI / 180d;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        var sourceX = (double)x;
        var sourceY = (double)y;

        return (
            ((sourceX * cos) - (sourceY * sin)) * scale + (double)transform.TranslateX,
            ((sourceX * sin) + (sourceY * cos)) * scale + (double)transform.TranslateY);
    }

    private static (double X, double Y) ProjectPoint(
        decimal x,
        decimal y,
        SheetAdjustmentProjectionTransform transform,
        ProjectedPlanSheetExportRecipe? recipe)
    {
        if (recipe is null)
        {
            return ProjectPoint(x, y, transform);
        }

        var projected = ElectricalRecipeProjection.ProjectPoint(
            x,
            y,
            recipe.RegistrationTransform,
            recipe.CanonicalRecipe,
            recipe.OutlineNormalization);
        return ((double)projected.X, (double)projected.Y);
    }

    private static (double X, double Y) ProjectVector(
        decimal x,
        decimal y,
        SheetAdjustmentProjectionTransform transform)
    {
        var scale = (double)transform.Scale;
        var radians = (double)transform.RotationDegrees * Math.PI / 180d;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        var sourceX = (double)x;
        var sourceY = (double)y;

        return (
            ((sourceX * cos) - (sourceY * sin)) * scale,
            ((sourceX * sin) + (sourceY * cos)) * scale);
    }

    private static bool IsXCoordinateCode(string code)
    {
        return code.Length == 2 &&
               code[0] == '1' &&
               code[1] is >= '0' and <= '8';
    }

    private static bool IsMatchingYCoordinateCode(string xCode, string yCode)
    {
        return yCode.Length == 2 &&
               yCode[0] == '2' &&
               yCode[1] == xCode[1];
    }

    private static bool TryParseDecimal(string value, out decimal result)
    {
        return decimal.TryParse(
            value.Trim(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out result);
    }

    private static string FormatDouble(double value)
    {
        return Math.Abs(value) < 0.000000000001d
            ? "0"
            : value.ToString("0.############", CultureInfo.InvariantCulture);
    }

    private static double NormalizeTiny(double value)
        => Math.Abs(value) < 0.000000001d ? 0d : value;

    private static IReadOnlyList<DxfPair> ReadDxfPairs(string dxfPath)
    {
        var lines = File.ReadAllLines(dxfPath, Encoding.Latin1);
        var pairs = new List<DxfPair>(lines.Length / 2);
        for (var index = 0; index + 1 < lines.Length; index += 2)
        {
            pairs.Add(new DxfPair(lines[index].Trim(), lines[index + 1]));
        }

        return pairs;
    }

    private static bool IsBinaryDxf(string dxfPath)
    {
        var prefix = Encoding.ASCII.GetBytes("AutoCAD Binary DXF");
        Span<byte> buffer = stackalloc byte[prefix.Length];
        using var stream = File.OpenRead(dxfPath);
        return stream.Read(buffer) == prefix.Length && buffer.SequenceEqual(prefix);
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

    private static ProjectedPlanSheetExportAuditDto BuildExportAudit(
        IReadOnlyList<DxfPair> sourcePairs,
        IReadOnlyList<DxfPair> projectedPairs,
        IReadOnlyList<ProjectedPlanSheetOperationAuditDto> operationAudits,
        ProjectedPlanSheetOutlineCongruenceAuditDto? outlineAudit,
        string outputFilePath)
        => new(
            operationAudits,
            BuildDxfSafetyAudit(sourcePairs, projectedPairs, outputFilePath),
            outlineAudit);

    private static ProjectedPlanSheetOutlineCongruenceAuditDto? BuildOutlineCongruenceAudit(
        RecipeProjectionPlan? recipePlan,
        IReadOnlyList<DxfPair> projectedPairs)
    {
        if (recipePlan?.UsesAuthoritativeWholePlanProof == true)
        {
            return BuildRegistrationProofAuthorizationAudit();
        }

        if (recipePlan?.SourceBounds is null)
        {
            return null;
        }

        var source = recipePlan.SourceBounds;
        var expectedOutputWidth = ResolveFinalTargetWidth(recipePlan.EffectiveRecipe);
        var expectedOutputHeight = ResolveFinalTargetHeight(recipePlan.EffectiveRecipe);
        var outputSelection = TryCollectRegisteredAnchorBounds(
            projectedPairs,
            new SheetRegistrationTransform(1m, 0m, 0m, 0m),
            expectedOutputWidth,
            expectedOutputHeight);
        if (!outputSelection.Dominant.IsSelected &&
            (expectedOutputWidth.HasValue ||
             expectedOutputHeight.HasValue ||
             outputSelection.Dominant.Status == DominantOutlineSelectionStatus.Ambiguous))
        {
            ThrowDominantOutlineManualReview("Exported Electrical", outputSelection.Dominant);
        }

        var output = outputSelection.Dominant.Outline is null
            ? null
            : ToRegisteredAnchorBounds(outputSelection.Dominant.Outline);
        var canonical = BuildCanonicalOutline(recipePlan.EffectiveRecipe);
        var hasSourceStructuralOutline = source.IsStructural;
        var hasOutputStructuralOutline = output?.IsStructural == true;
        decimal? sourceWidthMismatch = canonical is null ? null : source.Width - canonical.Width;
        decimal? sourceHeightMismatch = canonical is null ? null : source.Height - canonical.Height;
        decimal? outputWidthMismatch = !hasOutputStructuralOutline || !expectedOutputWidth.HasValue
            ? null
            : output!.Width - expectedOutputWidth.Value;
        decimal? outputHeightMismatch = !hasOutputStructuralOutline || !expectedOutputHeight.HasValue
            ? null
            : output!.Height - expectedOutputHeight.Value;
        var status = canonical is null
            ? "InsufficientData"
            : !hasSourceStructuralOutline
                ? "InsufficientData"
                : !hasOutputStructuralOutline
                    ? "InsufficientData"
                    : "Congruent";
        var reason = canonical is null
            ? "Canonical FloorPlan source dimensions were unavailable; outline congruence cannot be trusted."
            : !hasSourceStructuralOutline
                ? "Electrical structural outline was not found on a structural layer; outline congruence cannot be trusted."
                : !hasOutputStructuralOutline
                    ? $"Exported Electrical dominant structural outline could not be selected: {outputSelection.Dominant.Reason}"
                    : "Electrical dominant structural outline matches the canonical FloorPlan dimensions within tolerance without inferred scaling.";

        return new ProjectedPlanSheetOutlineCongruenceAuditDto(
            status,
            reason,
            ToleranceInches: 0.05m,
            NormalizationApplied: false,
            CanonicalSourceOutline: canonical,
            ElectricalSourceOutline: ToOutlineDto(source),
            ElectricalNormalizedSourceOutline: ToOutlineDto(source),
            ElectricalExportOutline: hasOutputStructuralOutline ? ToOutlineDto(output!) : null,
            SourceWidthMismatchInches: sourceWidthMismatch,
            SourceHeightMismatchInches: sourceHeightMismatch,
            ExportWidthMismatchInches: outputWidthMismatch,
            ExportHeightMismatchInches: outputHeightMismatch,
            AnchorX: null,
            AnchorY: null,
            ScaleX: null,
            ScaleY: null);
    }

    private static ProjectedPlanSheetOutlineCongruenceAuditDto BuildRegistrationProofAuthorizationAudit()
        => new(
            "RegistrationProofAuthorized",
            "A confirmed, source-bound whole-plan registration proof authorizes the canonical coordinate frame. This stage does not measure source or exported outline bounds; final output congruence, operation audits, and DXF safety remain independently required.",
            ToleranceInches: WholePlanRegistrationAcceptancePolicy.MaximumResidualInches,
            NormalizationApplied: false,
            CanonicalSourceOutline: null,
            ElectricalSourceOutline: null,
            ElectricalNormalizedSourceOutline: null,
            ElectricalExportOutline: null,
            SourceWidthMismatchInches: null,
            SourceHeightMismatchInches: null,
            ExportWidthMismatchInches: null,
            ExportHeightMismatchInches: null,
            AnchorX: null,
            AnchorY: null,
            ScaleX: null,
            ScaleY: null);

    private static ProjectedPlanSheetOutlineDto? BuildCanonicalOutline(ProjectedPlanSheetExportRecipe recipe)
        => recipe.CanonicalSourceWidthInches.HasValue && recipe.CanonicalSourceHeightInches.HasValue
            ? new ProjectedPlanSheetOutlineDto(
                0m,
                0m,
                recipe.CanonicalSourceWidthInches.Value,
                recipe.CanonicalSourceHeightInches.Value,
                recipe.CanonicalSourceWidthInches.Value,
                recipe.CanonicalSourceHeightInches.Value)
            : null;

    private static ProjectedPlanSheetOutlineDto ToOutlineDto(RegisteredAnchorBounds bounds)
        => new(
            bounds.MinX,
            bounds.MinY,
            bounds.MaxX,
            bounds.MaxY,
            bounds.Width,
            bounds.Height);

    private static decimal? ResolveFinalTargetWidth(ProjectedPlanSheetExportRecipe recipe)
        => recipe.CanonicalSourceWidthInches.HasValue
            ? (recipe.CanonicalSourceWidthInches.Value - recipe.CanonicalRecipe.Operations
                .Where(operation => string.Equals(operation.AxisTag, "Width", StringComparison.OrdinalIgnoreCase))
                .Sum(operation => operation.DeltaSourceUnits)) * recipe.CanonicalRecipe.FloorToSiteScale
            : null;

    private static decimal? ResolveFinalTargetHeight(ProjectedPlanSheetExportRecipe recipe)
        => recipe.CanonicalSourceHeightInches.HasValue
            ? (recipe.CanonicalSourceHeightInches.Value - recipe.CanonicalRecipe.Operations
                .Where(operation => string.Equals(operation.AxisTag, "Height", StringComparison.OrdinalIgnoreCase))
                .Sum(operation => operation.DeltaSourceUnits)) * recipe.CanonicalRecipe.FloorToSiteScale
            : null;

    private static ProjectedPlanSheetDxfSafetyAuditDto BuildDxfSafetyAudit(
        IReadOnlyList<DxfPair> sourcePairs,
        IReadOnlyList<DxfPair> projectedPairs,
        string outputFilePath)
    {
        var before = CountDxfEntities(sourcePairs);
        var after = CountDxfEntities(projectedPairs);
        var outputExists = File.Exists(outputFilePath);

        return new ProjectedPlanSheetDxfSafetyAuditDto(
            outputExists,
            outputExists ? new FileInfo(outputFilePath).Length : 0,
            before.EntityCount,
            after.EntityCount,
            after.InsertCount,
            after.DimensionCount,
            after.EllipseCount,
            after.WireOrCurveCount,
            after.MissingHandleCount,
            after.MissingOwnerCount,
            UnsupportedCrossingEntityCount: 0);
    }

    private static DxfEntityStats CountDxfEntities(IReadOnlyList<DxfPair> pairs)
    {
        var currentSection = string.Empty;
        var stats = new MutableDxfEntityStats();

        for (var index = 0; index < pairs.Count;)
        {
            var pair = pairs[index];
            if (pair.Code != "0")
            {
                index++;
                continue;
            }

            var value = pair.Value.Trim().ToUpperInvariant();
            if (value == "SECTION" && index + 1 < pairs.Count)
            {
                currentSection = pairs[index + 1].Code == "2"
                    ? pairs[index + 1].Value.Trim().ToUpperInvariant()
                    : string.Empty;
                index += 2;
                continue;
            }

            if (value == "ENDSEC")
            {
                currentSection = string.Empty;
                index++;
                continue;
            }

            var end = index + 1;
            while (end < pairs.Count && pairs[end].Code != "0")
            {
                end++;
            }

            if ((currentSection == "ENTITIES" || currentSection == "BLOCKS") &&
                IsAuditableDxfEntity(value))
            {
                stats.Record(value, pairs, index + 1, end);
            }

            index = end;
        }

        return stats.ToImmutable();
    }

    private static bool IsAuditableDxfEntity(string entityType)
        => entityType is not ("BLOCK" or "ENDBLK");

    private sealed class MutableDxfEntityStats
    {
        private int entityCount;
        private int insertCount;
        private int dimensionCount;
        private int ellipseCount;
        private int wireOrCurveCount;
        private int missingHandleCount;
        private int missingOwnerCount;

        public void Record(string entityType, IReadOnlyList<DxfPair> pairs, int start, int end)
        {
            entityCount++;
            insertCount += entityType == "INSERT" ? 1 : 0;
            dimensionCount += entityType == "DIMENSION" ? 1 : 0;
            ellipseCount += entityType == "ELLIPSE" ? 1 : 0;
            wireOrCurveCount += entityType is "LWPOLYLINE" or "POLYLINE" or "SPLINE" or "ARC" ? 1 : 0;

            var hasHandle = false;
            var hasOwner = false;
            for (var index = start; index < end; index++)
            {
                hasHandle |= pairs[index].Code == "5";
                hasOwner |= pairs[index].Code == "330";
            }

            missingHandleCount += hasHandle ? 0 : 1;
            missingOwnerCount += hasOwner ? 0 : 1;
        }

        public DxfEntityStats ToImmutable()
            => new(
                entityCount,
                insertCount,
                dimensionCount,
                ellipseCount,
                wireOrCurveCount,
                missingHandleCount,
                missingOwnerCount);
    }

    private sealed class OperationAuditAccumulator
    {
        private readonly AdjustmentRecipeOperationDto operation;
        private readonly int index;
        private readonly string? appliedReason;
        private readonly HashSet<string> affectedEntities = new(StringComparer.Ordinal);
        private int affectedVertices;

        public OperationAuditAccumulator(
            AdjustmentRecipeOperationDto operation,
            int index,
            string? appliedReason)
        {
            this.operation = operation;
            this.index = index;
            this.appliedReason = appliedReason;
        }

        public void Record(string entityKey)
        {
            affectedEntities.Add(entityKey);
            affectedVertices++;
        }

        public ProjectedPlanSheetOperationAuditDto ToDto()
            => new(
                $"operation-{index}",
                index,
                operation.Kind,
                operation.AxisTag,
                operation.Edge,
                operation.Coordinate,
                operation.DeltaSourceUnits,
                affectedEntities.Count,
                affectedVertices,
                affectedVertices == 0 ? 0 : operation.DeltaSourceUnits,
                affectedVertices == 0 ? 0 : operation.DeltaSourceUnits,
                affectedVertices == 0 ? "NoGeometryAffected" : "Applied",
                affectedVertices == 0
                    ? appliedReason is null
                        ? "No registered electrical geometry fell on the affected side after Electrical-to-Floor registration; treated as a truly empty dependent-sheet zone."
                        : $"{appliedReason} No electrical geometry matched after edge-anchor fallback."
                    : appliedReason);
    }

    private enum ElectricalCadCutSide
    {
        Fixed,
        Closing,
        Crossing
    }

    private sealed record ElectricalCadPointBinding(
        decimal X,
        decimal Y,
        int XPairIndex,
        int YPairIndex);

    private sealed record ElectricalCadDxfEntity(
        int Ordinal,
        string Kind,
        string Layer,
        IReadOnlyList<ElectricalCadPointBinding> Points,
        bool SupportsRigidMove,
        bool SupportsVertexStretch,
        bool HasNonZeroBulge,
        decimal EnvelopeExpansion,
        bool IsStructural);

    private sealed record ResolvedElectricalTargetSpan(
        ElectricalCadDxfEntity RawEntity,
        int ClosingVertexIndex,
        decimal MatchResidual);

    private sealed record ResolvedElectricalCadStretchAction(
        CadStretchAction Action,
        IReadOnlyList<CadStretchEntity> Entities,
        IReadOnlyList<CadStretchEntityRole> Roles,
        IReadOnlyDictionary<string, ElectricalCadDxfEntity> RawEntitiesById);

    private sealed record ElectricalCadStretchProjectionPlan(
        IReadOnlyDictionary<(int XPairIndex, int YPairIndex), (decimal DeltaX, decimal DeltaY)> DeltasByPair,
        IReadOnlyList<ProjectedPlanSheetOperationAuditDto> Audits);

    private sealed record RecipeProjectionPlan(
        ProjectedPlanSheetExportRecipe EffectiveRecipe,
        IReadOnlyList<OperationProjectionPlan> Operations,
        RegisteredAnchorBounds? SourceBounds,
        bool UsesAuthoritativeWholePlanProof);

    private sealed record OperationProjectionPlan(
        AdjustmentRecipeOperationDto Original,
        AdjustmentRecipeOperationDto Effective,
        string? AnchorReason);

    private sealed record RegisteredAnchorSelection(
        DominantAxisAlignedOutlineSelection Dominant,
        RegisteredAnchorBounds? LegacyOperationBounds);

    private readonly record struct RegisteredPoint(decimal X, decimal Y);

    private sealed class MutableRegisteredAnchorBounds
    {
        public bool HasData { get; private set; }

        private decimal minX;
        private decimal minY;
        private decimal maxX;
        private decimal maxY;

        public void Include(decimal x, decimal y)
        {
            if (!HasData)
            {
                minX = maxX = x;
                minY = maxY = y;
                HasData = true;
                return;
            }

            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
        }

        public RegisteredAnchorBounds ToImmutable(bool isStructural)
            => new(minX, minY, maxX, maxY, isStructural);
    }

    private sealed record RegisteredAnchorBounds(
        decimal MinX,
        decimal MinY,
        decimal MaxX,
        decimal MaxY,
        bool IsStructural)
    {
        public decimal Width => MaxX - MinX;

        public decimal Height => MaxY - MinY;
    }

    private sealed record DxfEntityStats(
        int EntityCount,
        int InsertCount,
        int DimensionCount,
        int EllipseCount,
        int WireOrCurveCount,
        int MissingHandleCount,
        int MissingOwnerCount);

    private enum BinaryDxfValueKind
    {
        String,
        Double,
        Int16,
        Int32,
        Int64,
        Boolean,
        BinaryChunk
    }

    private sealed record DxfStorageFormat(bool IsBinary, bool UsesLegacyGroupCodes);

    private sealed record DxfBlockDefinition(
        string Name,
        IReadOnlyList<IReadOnlyList<DxfPair>> Records);

    private sealed class DxfCompositionHandleAllocator
    {
        private ulong nextHandle;

        public DxfCompositionHandleAllocator(IEnumerable<DxfPair> canonicalPairs)
        {
            var maximumHandle = canonicalPairs
                .Where(pair => IsHandleDefinitionCode(pair.Code))
                .Select(pair => ulong.TryParse(
                    pair.Value.Trim(),
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture,
                    out var value)
                        ? value
                        : 0UL)
                .DefaultIfEmpty(0UL)
                .Max();
            nextHandle = checked(maximumHandle + 1UL);
        }

        public string NextAvailableHandle
            => nextHandle.ToString("X", CultureInfo.InvariantCulture);

        public string Next()
        {
            var value = NextAvailableHandle;
            nextHandle = checked(nextHandle + 1UL);
            return value;
        }
    }

    private sealed record DxfPair(string Code, string Value);
}
