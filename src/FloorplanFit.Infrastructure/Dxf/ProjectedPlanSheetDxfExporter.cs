using System.Globalization;
using System.Text;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Infrastructure.Dxf;

public sealed class ProjectedPlanSheetDxfExporter : IProjectedPlanSheetExporter
{
    private const string BinaryDxfSentinel = "AutoCAD Binary DXF\r\n\u001A\0";

    public Task ExportAsync(
        string sourceFilePath,
        string outputFilePath,
        SheetAdjustmentProjectionTransform transform,
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

        if (IsBinaryDxf(sourceFilePath))
        {
            var binaryPairs = ReadBinaryDxfPairs(sourceFilePath);
            var projectedBinaryPairs = ProjectCoordinatePairs(binaryPairs, transform, cancellationToken);
            WriteBinaryDxfPairs(outputFilePath, projectedBinaryPairs);
            return Task.CompletedTask;
        }

        var pairs = ReadDxfPairs(sourceFilePath);
        var projectedPairs = ProjectCoordinatePairs(pairs, transform, cancellationToken);

        WriteDxfPairs(outputFilePath, projectedPairs);
        return Task.CompletedTask;
    }

    private static IReadOnlyList<DxfPair> ReadBinaryDxfPairs(string dxfPath)
    {
        using var reader = new BinaryReader(File.OpenRead(dxfPath), Encoding.Latin1);
        var sentinel = Encoding.ASCII.GetBytes(BinaryDxfSentinel);
        var actualSentinel = reader.ReadBytes(sentinel.Length);
        if (!actualSentinel.SequenceEqual(sentinel))
        {
            throw new InvalidDataException("The DXF file is not an AutoCAD binary DXF.");
        }

        var pairs = new List<DxfPair>();
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var code = reader.ReadInt16();
            var value = ReadBinaryDxfValue(reader, code);
            pairs.Add(new DxfPair(code.ToString(CultureInfo.InvariantCulture), value));
            if (code == 0 && string.Equals(value, "EOF", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
        }

        return pairs;
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
        return Convert.ToHexString(reader.ReadBytes(length));
    }

    private static void WriteBinaryDxfPairs(string dxfPath, IReadOnlyList<DxfPair> pairs)
    {
        using var writer = new BinaryWriter(File.Create(dxfPath), Encoding.Latin1);
        writer.Write(Encoding.ASCII.GetBytes(BinaryDxfSentinel));

        foreach (var pair in pairs)
        {
            var code = short.Parse(pair.Code, CultureInfo.InvariantCulture);
            writer.Write(code);
            WriteBinaryDxfValue(writer, code, pair.Value);
        }
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

    private static List<DxfPair> ProjectCoordinatePairs(
        IReadOnlyList<DxfPair> pairs,
        SheetAdjustmentProjectionTransform transform,
        CancellationToken cancellationToken)
    {
        var projectedPairs = pairs.ToList();
        var dimensionBlockNames = CollectDimensionBlockNames(projectedPairs, cancellationToken);
        var currentSection = string.Empty;
        var currentEntity = string.Empty;
        var currentBlockName = string.Empty;
        for (var index = 0; index + 1 < projectedPairs.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pair = projectedPairs[index];
            if (pair.Code == "0")
            {
                currentEntity = pair.Value.Trim().ToUpperInvariant();
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

            var projected = ProjectPoint(x, y, transform);
            projectedPairs[index] = xPair with { Value = FormatDouble(projected.X) };
            projectedPairs[index + 1] = yPair with { Value = FormatDouble(projected.Y) };
            index++;
        }

        // ponytail: project modelspace entities only; metadata/header/object rewrites made AutoCAD reject real dependent sheets.
        return projectedPairs;
    }

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

    private static bool IsTextEntity(string entityType)
        => entityType is "TEXT" or "MTEXT";

    private static bool IsArcAngleCode(string code)
        => code is "50" or "51";

    private static bool IsInsertScaleCode(string code)
        => code is "41" or "42" or "43";

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

    private sealed record DxfPair(string Code, string Value);
}
