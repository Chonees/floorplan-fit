using System.Globalization;
using System.Text;
using FloorplanFit.Application.Abstractions;
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
        var operationAuditAccumulators = BuildOperationAuditAccumulators(recipePlan);
        var dimensionBlockNames = CollectDimensionBlockNames(projectedPairs, cancellationToken);
        ThrowIfUnsupportedRecipeEntities(projectedPairs, dimensionBlockNames, recipePlan?.EffectiveRecipe, cancellationToken);
        ReplaceRecipeCrossingCircularCurvesWithPolylines(projectedPairs, recipePlan?.EffectiveRecipe, cancellationToken);
        ThrowIfUnsupportedRecipeCurveCrossings(projectedPairs, dimensionBlockNames, recipePlan?.EffectiveRecipe, cancellationToken);
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

            var projected = ProjectPoint(x, y, transform, recipePlan?.EffectiveRecipe);
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
        operationAudits = operationAuditAccumulators
            .Select(item => item.ToDto())
            .ToArray();
        outlineAudit = BuildOutlineCongruenceAudit(recipePlan, projectedPairs);
        return projectedPairs;
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

    private sealed record DxfPair(string Code, string Value);
}
