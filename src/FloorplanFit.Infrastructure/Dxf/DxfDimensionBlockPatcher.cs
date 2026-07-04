using System.Globalization;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Infrastructure.Dxf;

internal static class DxfDimensionBlockPatcher
{
    public static List<DxfPair> PatchGeometryBlocks(
        IReadOnlyList<DxfPair> sourcePairs,
        IReadOnlyList<DimensionDto> dimensions,
        CancellationToken cancellationToken)
    {
        if (dimensions.Count == 0)
        {
            return sourcePairs.ToList();
        }

        var sourceDimensions = ReadDimensionRecords(sourcePairs);
        var sourceDimensionByHandle = sourceDimensions
            .Where(record => !string.IsNullOrWhiteSpace(record.Handle))
            .ToDictionary(record => record.Handle!, StringComparer.OrdinalIgnoreCase);
        var twinBlocksByHandle = BuildTwinBlocksByHandle(sourceDimensions);
        var blockPatches = BuildBlockPatches(dimensions, sourceDimensionByHandle, twinBlocksByHandle, cancellationToken);

        if (blockPatches.Count == 0)
        {
            return sourcePairs.ToList();
        }

        var patchedPairs = new List<DxfPair>(sourcePairs.Count);
        string? currentBlockName = null;
        var currentBlockEntitySortOrder = 0;

        for (var index = 0; index < sourcePairs.Count;)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pair = sourcePairs[index];
            if (IsEntityStart(pair, "BLOCK"))
            {
                var record = ReadRecord(sourcePairs, ref index);
                currentBlockName = FirstGroupValue(record, "2");
                currentBlockEntitySortOrder = 0;
                patchedPairs.AddRange(record);
                continue;
            }

            if (IsEntityStart(pair, "ENDBLK"))
            {
                currentBlockName = null;
                currentBlockEntitySortOrder = 0;
                patchedPairs.Add(pair);
                index++;
                continue;
            }

            if (currentBlockName is not null && string.Equals(pair.Code, "0", StringComparison.Ordinal))
            {
                var record = ReadRecord(sourcePairs, ref index);
                currentBlockEntitySortOrder++;

                if (blockPatches.TryGetValue(currentBlockName, out var dimension))
                {
                    record = PatchBlockEntityRecord(record, dimension, currentBlockEntitySortOrder);
                }

                patchedPairs.AddRange(record);
                continue;
            }

            patchedPairs.Add(pair);
            index++;
        }

        return patchedPairs;
    }

    private static Dictionary<string, DimensionDto> BuildBlockPatches(
        IReadOnlyList<DimensionDto> dimensions,
        IReadOnlyDictionary<string, DxfDimensionRecord> sourceDimensionByHandle,
        IReadOnlyDictionary<string, IReadOnlyList<string>> twinBlocksByHandle,
        CancellationToken cancellationToken)
    {
        var blockPatches = new Dictionary<string, DimensionDto>(StringComparer.OrdinalIgnoreCase);

        foreach (var dimension in dimensions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sourceHandle = ResolveSourceHandle(dimension);
            if (string.IsNullOrWhiteSpace(sourceHandle) ||
                !sourceDimensionByHandle.TryGetValue(sourceHandle, out var sourceRecord))
            {
                continue;
            }

            var targetBlocks = twinBlocksByHandle.TryGetValue(sourceHandle, out var twinBlocks)
                ? twinBlocks
                : string.IsNullOrWhiteSpace(sourceRecord.BlockName)
                    ? []
                    : [sourceRecord.BlockName!];

            foreach (var blockName in targetBlocks)
            {
                if (!string.IsNullOrWhiteSpace(blockName))
                {
                    blockPatches[blockName] = dimension;
                }
            }
        }

        return blockPatches;
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> BuildTwinBlocksByHandle(IReadOnlyList<DxfDimensionRecord> dimensions)
    {
        return dimensions
            .GroupBy(record => BuildDimensionRecordDeduplicationSignature(record.Pairs), StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .SelectMany(group =>
            {
                var blocks = group
                    .Select(record => record.BlockName)
                    .Where(blockName => !string.IsNullOrWhiteSpace(blockName))
                    .Cast<string>()
                    .ToArray();
                return group
                    .Where(record => !string.IsNullOrWhiteSpace(record.Handle))
                    .Select(record => new KeyValuePair<string, IReadOnlyList<string>>(record.Handle!, blocks));
            })
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static string? ResolveSourceHandle(DimensionDto dimension)
    {
        if (!string.IsNullOrWhiteSpace(dimension.SourceHandle))
        {
            return dimension.SourceHandle;
        }

        const string prefix = "DIMENSION:";
        return dimension.SourceEntityRef.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? dimension.SourceEntityRef[prefix.Length..]
            : null;
    }

    private static List<DxfPair> PatchBlockEntityRecord(
        List<DxfPair> record,
        DimensionDto dimension,
        int sortOrder)
    {
        var entityKind = record[0].Value;
        return entityKind.ToUpperInvariant() switch
        {
            "LINE" => PatchLineRecord(record, dimension.LinePrimitives.FirstOrDefault(primitive => primitive.SortOrder == sortOrder)),
            "TEXT" => PatchTextRecord(record, dimension.TextPrimitives.FirstOrDefault(primitive => primitive.SortOrder == sortOrder), isMText: false),
            "MTEXT" => PatchTextRecord(record, dimension.TextPrimitives.FirstOrDefault(primitive => primitive.SortOrder == sortOrder), isMText: true),
            "INSERT" => PatchInsertRecord(record, dimension.InsertPrimitives.FirstOrDefault(primitive => primitive.SortOrder == sortOrder)),
            "CIRCLE" => PatchCircleRecord(record, dimension.CirclePrimitives.FirstOrDefault(primitive => primitive.SortOrder == sortOrder)),
            "ARC" => PatchArcRecord(record, dimension.ArcPrimitives.FirstOrDefault(primitive => primitive.SortOrder == sortOrder)),
            "SOLID" => PatchSolidRecord(record, dimension.SolidPrimitives.FirstOrDefault(primitive => primitive.SortOrder == sortOrder)),
            _ => record
        };
    }

    private static List<DxfPair> PatchLineRecord(List<DxfPair> record, DimensionLinePrimitiveDto? primitive)
    {
        if (primitive is null)
        {
            return record;
        }

        ReplaceFirst(record, "10", FormatDecimal(primitive.StartX));
        ReplaceFirst(record, "20", FormatDecimal(primitive.StartY));
        ReplaceFirst(record, "11", FormatDecimal(primitive.EndX));
        ReplaceFirst(record, "21", FormatDecimal(primitive.EndY));
        return record;
    }

    private static List<DxfPair> PatchTextRecord(List<DxfPair> record, DimensionTextPrimitiveDto? primitive, bool isMText)
    {
        if (primitive is null)
        {
            return record;
        }

        var existingText = FirstGroupValue(record, "1") ?? string.Empty;
        ReplaceFirst(record, "1", FormatTextValue(existingText, primitive.Text, isMText));
        ReplaceFirst(record, "10", FormatDecimal(primitive.X));
        ReplaceFirst(record, "20", FormatDecimal(primitive.Y));
        ReplaceFirst(record, "40", FormatDecimal(primitive.Height));
        ReplaceFirstIfPresent(record, "50", FormatDecimal(primitive.RotationDegrees));
        return record;
    }

    private static List<DxfPair> PatchInsertRecord(List<DxfPair> record, DimensionInsertPrimitiveDto? primitive)
    {
        if (primitive is null)
        {
            return record;
        }

        ReplaceFirst(record, "2", primitive.Name);
        ReplaceFirst(record, "10", FormatDecimal(primitive.X));
        ReplaceFirst(record, "20", FormatDecimal(primitive.Y));
        ReplaceFirstIfPresent(record, "30", FormatDecimal(primitive.Z));
        ReplaceFirstIfPresent(record, "41", FormatDecimal(primitive.ScaleX));
        ReplaceFirstIfPresent(record, "42", FormatDecimal(primitive.ScaleY));
        ReplaceFirstIfPresent(record, "43", FormatDecimal(primitive.ScaleZ));
        ReplaceFirstIfPresent(record, "50", FormatDecimal(primitive.RotationDegrees));
        return record;
    }

    private static List<DxfPair> PatchCircleRecord(List<DxfPair> record, DimensionCirclePrimitiveDto? primitive)
    {
        if (primitive is null)
        {
            return record;
        }

        ReplaceFirst(record, "10", FormatDecimal(primitive.CenterX));
        ReplaceFirst(record, "20", FormatDecimal(primitive.CenterY));
        ReplaceFirst(record, "40", FormatDecimal(primitive.Radius));
        return record;
    }

    private static List<DxfPair> PatchArcRecord(List<DxfPair> record, DimensionArcPrimitiveDto? primitive)
    {
        if (primitive is null)
        {
            return record;
        }

        ReplaceFirst(record, "10", FormatDecimal(primitive.CenterX));
        ReplaceFirst(record, "20", FormatDecimal(primitive.CenterY));
        ReplaceFirst(record, "40", FormatDecimal(primitive.Radius));
        ReplaceFirst(record, "50", FormatDecimal(primitive.StartAngleDegrees));
        ReplaceFirst(record, "51", FormatDecimal(primitive.EndAngleDegrees));
        return record;
    }

    private static List<DxfPair> PatchSolidRecord(List<DxfPair> record, DimensionSolidPrimitiveDto? primitive)
    {
        if (primitive is null)
        {
            return record;
        }

        ReplaceNth(record, "10", 0, FormatDecimal(primitive.Point1X));
        ReplaceNth(record, "20", 0, FormatDecimal(primitive.Point1Y));
        ReplaceNth(record, "11", 0, FormatDecimal(primitive.Point2X));
        ReplaceNth(record, "21", 0, FormatDecimal(primitive.Point2Y));
        ReplaceNth(record, "12", 0, FormatDecimal(primitive.Point3X));
        ReplaceNth(record, "22", 0, FormatDecimal(primitive.Point3Y));
        ReplaceNth(record, "13", 0, FormatDecimal(primitive.Point4X));
        ReplaceNth(record, "23", 0, FormatDecimal(primitive.Point4Y));
        return record;
    }

    private static IReadOnlyList<DxfDimensionRecord> ReadDimensionRecords(IReadOnlyList<DxfPair> pairs)
    {
        var records = new List<DxfDimensionRecord>();

        for (var index = 0; index < pairs.Count;)
        {
            var pair = pairs[index];
            if (!IsEntityStart(pair, "DIMENSION"))
            {
                index++;
                continue;
            }

            var recordPairs = ReadRecord(pairs, ref index);
            records.Add(new DxfDimensionRecord(
                FirstGroupValue(recordPairs, "5"),
                FirstGroupValue(recordPairs, "2"),
                recordPairs));
        }

        return records;
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

    private static string BuildDimensionRecordDeduplicationSignature(IReadOnlyList<DxfPair> recordPairs)
    {
        return string.Join(
            "|",
            DimensionDeduplicationGroupCodes.Select(code =>
                string.Join(
                    ",",
                    recordPairs
                        .Where(pair => string.Equals(pair.Code, code, StringComparison.Ordinal))
                        .Select(pair => pair.Value))));
    }

    private static string? FirstGroupValue(IReadOnlyList<DxfPair> pairs, string code)
    {
        return pairs
            .Where(pair => string.Equals(pair.Code, code, StringComparison.Ordinal))
            .Select(pair => pair.Value.Trim())
            .FirstOrDefault();
    }

    private static void ReplaceFirst(List<DxfPair> record, string code, string value)
    {
        ReplaceNth(record, code, occurrence: 0, value);
    }

    private static void ReplaceFirstIfPresent(List<DxfPair> record, string code, string value)
    {
        if (record.Any(pair => string.Equals(pair.Code, code, StringComparison.Ordinal)))
        {
            ReplaceFirst(record, code, value);
        }
    }

    private static void ReplaceNth(List<DxfPair> record, string code, int occurrence, string value)
    {
        var currentOccurrence = 0;
        for (var index = 0; index < record.Count; index++)
        {
            if (!string.Equals(record[index].Code, code, StringComparison.Ordinal))
            {
                continue;
            }

            if (currentOccurrence == occurrence)
            {
                record[index] = record[index] with { Value = value };
                return;
            }

            currentOccurrence++;
        }
    }

    private static string FormatTextValue(string existingText, string newText, bool isMText)
    {
        if (!isMText || string.IsNullOrWhiteSpace(existingText) || newText.StartsWith('\\'))
        {
            return newText;
        }

        var alignmentPrefixEnd = existingText.IndexOf(';');
        if (alignmentPrefixEnd > 0 && existingText.StartsWith("\\A", StringComparison.OrdinalIgnoreCase))
        {
            return string.Concat(existingText.AsSpan(0, alignmentPrefixEnd + 1), newText);
        }

        return newText;
    }

    private static string FormatDecimal(decimal value)
    {
        return value.ToString("0.###############", CultureInfo.InvariantCulture);
    }

    private sealed record DxfDimensionRecord(string? Handle, string? BlockName, IReadOnlyList<DxfPair> Pairs);

    private static readonly string[] DimensionDeduplicationGroupCodes =
    [
        "8",
        "70",
        "1",
        "10",
        "20",
        "30",
        "11",
        "21",
        "31",
        "13",
        "23",
        "33",
        "14",
        "24",
        "34",
        "42",
        "50",
        "51",
        "52",
        "53"
    ];
}
