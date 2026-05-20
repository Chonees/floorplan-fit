using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.Measurement;
using IxMilia.Dxf;
using IxMilia.Dxf.Blocks;
using IxMilia.Dxf.Entities;

namespace FloorplanFit.Infrastructure.Dxf;

public sealed class IxMiliaDimensionExtractor : IDimensionExtractor
{
    private const string GeometryBlockTextSource = "GeometryBlock";
    private const string DimensionTextOverrideSource = "DimensionTextOverride";
    private const string GeneratedFallbackSource = "GeneratedFallback";
    private const string ExcludedLayer = "ELECTRICAL WIRING";
    private static readonly string[] HandleFieldNames =
    [
        "<IxMilia.Dxf.IDxfItemInternal.Handle>k__BackingField",
        "<Handle>k__BackingField"
    ];

    public Task<IReadOnlyList<DetectedDimension>> ExtractAsync(string managedFilePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(managedFilePath))
        {
            throw new ArgumentException("A DXF file path is required.", nameof(managedFilePath));
        }

        var dxf = DxfFile.Load(managedFilePath);
        var sourceUnit = DetectSourceUnit(dxf);
        var toMillimetersFactor = ToMillimetersFactor(sourceUnit);
        var dimensions = new List<DetectedDimension>();
        var dimensionIndex = 0;

        foreach (var dimension in dxf.Entities.OfType<DxfDimensionBase>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.Equals(dimension.Layer, ExcludedLayer, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            dimensionIndex++;
            var sourceHandle = GetSourceHandle(dimension);
            var measurement = ResolveMeasurement(dimension);
            var geometryBlockRenderData = ResolveGeometryBlockRenderData(dxf, dimension.BlockName);
            var displayText = ResolveDisplayText(geometryBlockRenderData, dimension, measurement, sourceUnit, out var textSource);
            var definitionPoint1 = GetPoint(dimension, "DefinitionPoint1");
            var definitionPoint2 = GetPoint(dimension, "DefinitionPoint2");
            var definitionPoint3 = GetPoint(dimension, "DefinitionPoint3");

            dimensions.Add(new DetectedDimension(
                SourceEntityRef: BuildSourceEntityRef(sourceHandle, dimensionIndex),
                SourceLayer: dimension.Layer ?? string.Empty,
                SourceEntityKind: "DIMENSION",
                GeometryBlockName: dimension.BlockName,
                DisplayText: displayText,
                DisplayTextSource: textSource,
                RawTextOverride: NormalizeRawTextOverride(dimension.Text),
                MeasurementSourceUnits: measurement,
                MeasurementMillimeters: measurement * toMillimetersFactor,
                SourceUnit: sourceUnit.ToString(),
                DimType: (int)dimension.DimensionType,
                Angle: GetAngle(dimension),
                ObliqueAngle: GetObliqueAngle(dimension),
                DefPointX: ToDecimal(definitionPoint1.X),
                DefPointY: ToDecimal(definitionPoint1.Y),
                DefPointZ: ToDecimal(definitionPoint1.Z),
                DefPoint2X: ToDecimal(definitionPoint2.X),
                DefPoint2Y: ToDecimal(definitionPoint2.Y),
                DefPoint2Z: ToDecimal(definitionPoint2.Z),
                DefPoint3X: ToDecimal(definitionPoint3.X),
                DefPoint3Y: ToDecimal(definitionPoint3.Y),
                DefPoint3Z: ToDecimal(definitionPoint3.Z),
                Confidence: 0.99m,
                DetectionNotes: $"Detected native DIMENSION on layer {dimension.Layer ?? string.Empty}.")
            {
                SourceHandle = sourceHandle,
                RenderTextX = geometryBlockRenderData?.RenderTextX,
                RenderTextY = geometryBlockRenderData?.RenderTextY,
                RenderTextHeight = geometryBlockRenderData?.RenderTextHeight,
                RenderTextRotationDegrees = geometryBlockRenderData?.RenderTextRotationDegrees,
                RenderTextStyleName = geometryBlockRenderData?.RenderTextStyleName,
                RenderTextHorizontalAlignment = geometryBlockRenderData?.RenderTextHorizontalAlignment,
                RenderTextVerticalAlignment = geometryBlockRenderData?.RenderTextVerticalAlignment,
                RenderTextAttachmentPoint = geometryBlockRenderData?.RenderTextAttachmentPoint,
                LineSegments = geometryBlockRenderData?.LineSegments ?? [],
                LinePrimitives = geometryBlockRenderData?.LinePrimitives ?? [],
                TextPrimitives = geometryBlockRenderData?.TextPrimitives ?? [],
                InsertPrimitives = geometryBlockRenderData?.InsertPrimitives ?? [],
                CirclePrimitives = geometryBlockRenderData?.CirclePrimitives ?? [],
                ArcPrimitives = geometryBlockRenderData?.ArcPrimitives ?? [],
                SolidPrimitives = geometryBlockRenderData?.SolidPrimitives ?? []
            });
        }

        return Task.FromResult<IReadOnlyList<DetectedDimension>>(dimensions);
    }

    private static string BuildSourceEntityRef(string? sourceHandle, int dimensionIndex)
    {
        return !string.IsNullOrWhiteSpace(sourceHandle)
            ? $"DIMENSION:{sourceHandle}"
            : $"DIMENSION:{dimensionIndex.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string ResolveDisplayText(
        GeometryBlockRenderData? geometryBlockRenderData,
        DxfDimensionBase dimension,
        decimal measurement,
        LengthUnit sourceUnit,
        out string displayTextSource)
    {
        if (!string.IsNullOrWhiteSpace(geometryBlockRenderData?.DisplayText))
        {
            displayTextSource = GeometryBlockTextSource;
            return geometryBlockRenderData.DisplayText;
        }

        var rawOverride = dimension.Text ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(rawOverride) && !string.Equals(rawOverride.Trim(), "<>", StringComparison.Ordinal))
        {
            displayTextSource = DimensionTextOverrideSource;
            return NormalizeText(rawOverride);
        }

        displayTextSource = GeneratedFallbackSource;
        return FormatMeasurementFallback(measurement, sourceUnit);
    }

    private static GeometryBlockRenderData? ResolveGeometryBlockRenderData(DxfFile dxf, string? geometryBlockName)
    {
        var block = FindBlock(dxf, geometryBlockName);
        if (block is null)
        {
            return null;
        }

        var textParts = new List<string>();
        var lineSegments = new List<DetectedDimensionLineSegment>();
        var linePrimitives = new List<DetectedDimensionLinePrimitive>();
        var textPrimitives = new List<DetectedDimensionTextPrimitive>();
        var insertPrimitives = new List<DetectedDimensionInsertPrimitive>();
        var circlePrimitives = new List<DetectedDimensionCirclePrimitive>();
        var arcPrimitives = new List<DetectedDimensionArcPrimitive>();
        var solidPrimitives = new List<DetectedDimensionSolidPrimitive>();
        decimal? renderTextX = null;
        decimal? renderTextY = null;
        decimal? renderTextHeight = null;
        decimal? renderTextRotationDegrees = null;
        string? renderTextStyleName = null;
        string? renderTextHorizontalAlignment = null;
        string? renderTextVerticalAlignment = null;
        string? renderTextAttachmentPoint = null;

        var primitiveSortOrder = 0;
        foreach (var entity in block.Entities)
        {
            primitiveSortOrder++;
            switch (entity)
            {
                case DxfMText mText:
                    if (!string.IsNullOrWhiteSpace(mText.Text))
                    {
                        textParts.Add(NormalizeText(mText.Text));
                    }

                    if (renderTextX is null && !string.IsNullOrWhiteSpace(mText.Text))
                    {
                        renderTextX = ToDecimal(mText.InsertionPoint.X);
                        renderTextY = ToDecimal(mText.InsertionPoint.Y);
                        renderTextHeight = mText.InitialTextHeight > 0d ? ToDecimal(mText.InitialTextHeight) : null;
                        renderTextRotationDegrees = ToDecimal(mText.RotationAngle);
                        renderTextStyleName = mText.TextStyleName;
                        renderTextAttachmentPoint = mText.AttachmentPoint.ToString();
                    }

                    textPrimitives.Add(new DetectedDimensionTextPrimitive(
                        BuildPrimitiveKey(block.Name, "MTEXT", GetSourceHandle(mText), primitiveSortOrder),
                        primitiveSortOrder,
                        NormalizeText(mText.Text ?? string.Empty),
                        ToDecimal(mText.InsertionPoint.X),
                        ToDecimal(mText.InsertionPoint.Y),
                        mText.InitialTextHeight > 0d ? ToDecimal(mText.InitialTextHeight) : 0m,
                        ToDecimal(mText.RotationAngle))
                    {
                        SourceHandle = GetSourceHandle(mText),
                        SourceLayer = mText.Layer,
                        StyleName = mText.TextStyleName,
                        AttachmentPoint = mText.AttachmentPoint.ToString()
                    });

                    break;
                case DxfText text:
                    if (!string.IsNullOrWhiteSpace(text.Value))
                    {
                        textParts.Add(NormalizeText(text.Value));
                    }

                    if (renderTextX is null && !string.IsNullOrWhiteSpace(text.Value))
                    {
                        renderTextX = ToDecimal(text.Location.X);
                        renderTextY = ToDecimal(text.Location.Y);
                        renderTextHeight = text.TextHeight > 0d ? ToDecimal(text.TextHeight) : null;
                        renderTextRotationDegrees = ToDecimal(text.Rotation);
                        renderTextStyleName = text.TextStyleName;
                        renderTextHorizontalAlignment = text.HorizontalTextJustification.ToString();
                        renderTextVerticalAlignment = text.VerticalTextJustification.ToString();
                    }

                    textPrimitives.Add(new DetectedDimensionTextPrimitive(
                        BuildPrimitiveKey(block.Name, "TEXT", GetSourceHandle(text), primitiveSortOrder),
                        primitiveSortOrder,
                        NormalizeText(text.Value ?? string.Empty),
                        ToDecimal(text.Location.X),
                        ToDecimal(text.Location.Y),
                        text.TextHeight > 0d ? ToDecimal(text.TextHeight) : 0m,
                        ToDecimal(text.Rotation))
                    {
                        SourceHandle = GetSourceHandle(text),
                        SourceLayer = text.Layer,
                        StyleName = text.TextStyleName,
                        HorizontalAlignment = text.HorizontalTextJustification.ToString(),
                        VerticalAlignment = text.VerticalTextJustification.ToString()
                    });

                    break;
                case DxfLine line:
                    lineSegments.Add(new DetectedDimensionLineSegment(
                        ToDecimal(line.P1.X),
                        ToDecimal(line.P1.Y),
                        ToDecimal(line.P2.X),
                        ToDecimal(line.P2.Y)));
                    linePrimitives.Add(new DetectedDimensionLinePrimitive(
                        BuildPrimitiveKey(block.Name, "LINE", GetSourceHandle(line), primitiveSortOrder),
                        primitiveSortOrder,
                        ToDecimal(line.P1.X),
                        ToDecimal(line.P1.Y),
                        ToDecimal(line.P2.X),
                        ToDecimal(line.P2.Y))
                    {
                        SourceHandle = GetSourceHandle(line),
                        SourceLayer = line.Layer
                    });
                    break;
                case DxfInsert insert:
                    insertPrimitives.Add(new DetectedDimensionInsertPrimitive(
                        BuildPrimitiveKey(block.Name, "INSERT", GetSourceHandle(insert), primitiveSortOrder),
                        primitiveSortOrder,
                        insert.Name ?? string.Empty,
                        ToDecimal(insert.Location.X),
                        ToDecimal(insert.Location.Y),
                        ToDecimal(insert.Location.Z))
                    {
                        SourceHandle = GetSourceHandle(insert),
                        SourceLayer = insert.Layer,
                        RotationDegrees = ToDecimal(insert.Rotation),
                        ScaleX = ToDecimal(insert.XScaleFactor),
                        ScaleY = ToDecimal(insert.YScaleFactor),
                        ScaleZ = ToDecimal(insert.ZScaleFactor)
                    });
                    break;
                case DxfArc arc:
                    arcPrimitives.Add(new DetectedDimensionArcPrimitive(
                        BuildPrimitiveKey(block.Name, "ARC", GetSourceHandle(arc), primitiveSortOrder),
                        primitiveSortOrder,
                        ToDecimal(arc.Center.X),
                        ToDecimal(arc.Center.Y),
                        ToDecimal(arc.Radius),
                        ToDecimal(arc.StartAngle),
                        ToDecimal(arc.EndAngle))
                    {
                        SourceHandle = GetSourceHandle(arc),
                        SourceLayer = arc.Layer
                    });
                    break;
                case DxfCircle circle:
                    circlePrimitives.Add(new DetectedDimensionCirclePrimitive(
                        BuildPrimitiveKey(block.Name, "CIRCLE", GetSourceHandle(circle), primitiveSortOrder),
                        primitiveSortOrder,
                        ToDecimal(circle.Center.X),
                        ToDecimal(circle.Center.Y),
                        ToDecimal(circle.Radius))
                    {
                        SourceHandle = GetSourceHandle(circle),
                        SourceLayer = circle.Layer
                    });
                    break;
                case DxfSolid solid:
                    solidPrimitives.Add(new DetectedDimensionSolidPrimitive(
                        BuildPrimitiveKey(block.Name, "SOLID", GetSourceHandle(solid), primitiveSortOrder),
                        primitiveSortOrder,
                        ToDecimal(solid.FirstCorner.X),
                        ToDecimal(solid.FirstCorner.Y),
                        ToDecimal(solid.SecondCorner.X),
                        ToDecimal(solid.SecondCorner.Y),
                        ToDecimal(solid.ThirdCorner.X),
                        ToDecimal(solid.ThirdCorner.Y),
                        ToDecimal(solid.FourthCorner.X),
                        ToDecimal(solid.FourthCorner.Y))
                    {
                        SourceHandle = GetSourceHandle(solid),
                        SourceLayer = solid.Layer
                    });
                    break;
            }
        }

        var displayText = textParts.Count == 0
            ? null
            : string.Join(" ", textParts.Where(part => !string.IsNullOrWhiteSpace(part)));

        return new GeometryBlockRenderData(
            displayText,
            renderTextX,
            renderTextY,
            renderTextHeight,
            renderTextRotationDegrees,
            renderTextStyleName,
            renderTextHorizontalAlignment,
            renderTextVerticalAlignment,
            renderTextAttachmentPoint,
            lineSegments,
            linePrimitives,
            textPrimitives,
            insertPrimitives,
            circlePrimitives,
            arcPrimitives,
            solidPrimitives);
    }

    private static string BuildPrimitiveKey(string blockName, string primitiveKind, string? sourceHandle, int sortOrder)
    {
        return !string.IsNullOrWhiteSpace(sourceHandle)
            ? $"{blockName}:{primitiveKind}:{sourceHandle}"
            : $"{blockName}:{primitiveKind}:{sortOrder.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string? GetSourceHandle(DxfEntity entity)
    {
        for (var currentType = entity.GetType(); currentType is not null; currentType = currentType.BaseType)
        {
            foreach (var fieldName in HandleFieldNames)
            {
                var field = currentType.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (field?.GetValue(entity) is { } value)
                {
                    var handle = value.ToString();
                    if (!string.IsNullOrWhiteSpace(handle))
                    {
                        return handle;
                    }
                }
            }
        }

        return null;
    }

    private static DxfBlock? FindBlock(DxfFile dxf, string? geometryBlockName)
    {
        if (string.IsNullOrWhiteSpace(geometryBlockName))
        {
            return null;
        }

        return dxf.Blocks.FirstOrDefault(block =>
            string.Equals(block.Name, geometryBlockName, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeText(string value)
    {
        return string.Join(
                " ",
                value
                    .Replace("\\A1;", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("\\A0;", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("\\P", " ", StringComparison.OrdinalIgnoreCase)
                    .Replace("{", " ", StringComparison.Ordinal)
                    .Replace("}", " ", StringComparison.Ordinal)
                    .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .Trim();
    }

    private static string NormalizeRawTextOverride(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), "<>", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        return NormalizeText(value);
    }

    private static string FormatMeasurementFallback(decimal measurement, LengthUnit sourceUnit)
    {
        return sourceUnit switch
        {
            LengthUnit.Inch or LengthUnit.Foot => FormatArchitecturalInches(measurement),
            _ => measurement.ToString("0.###", CultureInfo.InvariantCulture)
        };
    }

    private static string FormatArchitecturalInches(decimal totalInches)
    {
        var rounded = decimal.Round(totalInches, 0, MidpointRounding.AwayFromZero);
        var feet = decimal.ToInt32(decimal.Truncate(rounded / 12m));
        var inches = decimal.ToInt32(rounded % 12m);
        return feet > 0
            ? $"{feet}'-{inches}\""
            : $"{inches}\"";
    }

    private static decimal ToDecimal(double value)
    {
        return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
    }

    private static decimal ResolveMeasurement(DxfDimensionBase dimension)
    {
        if (dimension.ActualMeasurement > 0d)
        {
            return ToDecimal(dimension.ActualMeasurement);
        }

        return dimension switch
        {
            DxfAlignedDimension aligned => ToDecimal(Distance(aligned.DefinitionPoint1, aligned.DefinitionPoint2)),
            DxfRotatedDimension rotated => ToDecimal(ProjectedDistance(
                rotated.DefinitionPoint1,
                rotated.DefinitionPoint2,
                rotated.RotationAngle)),
            DxfOrdinateDimension ordinate => ToDecimal(ProjectOrdinateDistance(ordinate)),
            DxfRadialDimension radial => ToDecimal(Distance(radial.DefinitionPoint1, radial.DefinitionPoint2)),
            DxfDiameterDimension diameter => ToDecimal(Distance(diameter.DefinitionPoint1, diameter.DefinitionPoint2)),
            _ => 0m
        };
    }

    private static decimal GetAngle(DxfDimensionBase dimension)
    {
        if (TryGetDoubleProperty(dimension, "RotationAngle", out var rotationAngle))
        {
            return ToDecimal(rotationAngle);
        }

        if (TryGetDoubleProperty(dimension, "HorizontalDirectionAngle", out var horizontalDirectionAngle))
        {
            return ToDecimal(horizontalDirectionAngle);
        }

        return 0m;
    }

    private static decimal GetObliqueAngle(DxfDimensionBase dimension)
    {
        return TryGetDoubleProperty(dimension, "ExtensionLineAngle", out var extensionLineAngle)
            ? ToDecimal(extensionLineAngle)
            : 0m;
    }

    private static DxfPoint GetPoint(DxfDimensionBase dimension, string propertyName)
    {
        var property = dimension.GetType().GetProperty(propertyName);
        return property?.GetValue(dimension) is DxfPoint point
            ? point
            : DxfPoint.Origin;
    }

    private static bool TryGetDoubleProperty(DxfDimensionBase dimension, string propertyName, out double value)
    {
        var property = dimension.GetType().GetProperty(propertyName);
        if (property?.PropertyType == typeof(double) && property.GetValue(dimension) is double doubleValue)
        {
            value = doubleValue;
            return true;
        }

        value = 0d;
        return false;
    }

    private static double Distance(DxfPoint start, DxfPoint end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var dz = end.Z - start.Z;
        return Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
    }

    private static double ProjectedDistance(DxfPoint start, DxfPoint end, double rotationAngleDegrees)
    {
        var angleRadians = rotationAngleDegrees * (Math.PI / 180d);
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        return Math.Abs((dx * Math.Cos(angleRadians)) + (dy * Math.Sin(angleRadians)));
    }

    private static double ProjectOrdinateDistance(DxfOrdinateDimension ordinate)
    {
        return ordinate.IsOrdinateXType
            ? Math.Abs(ordinate.DefinitionPoint2.X - ordinate.DefinitionPoint1.X)
            : Math.Abs(ordinate.DefinitionPoint2.Y - ordinate.DefinitionPoint1.Y);
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

    private static decimal ToMillimetersFactor(LengthUnit sourceUnit)
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

    private sealed record GeometryBlockRenderData(
        string? DisplayText,
        decimal? RenderTextX,
        decimal? RenderTextY,
        decimal? RenderTextHeight,
        decimal? RenderTextRotationDegrees,
        string? RenderTextStyleName,
        string? RenderTextHorizontalAlignment,
        string? RenderTextVerticalAlignment,
        string? RenderTextAttachmentPoint,
        IReadOnlyList<DetectedDimensionLineSegment> LineSegments,
        IReadOnlyList<DetectedDimensionLinePrimitive> LinePrimitives,
        IReadOnlyList<DetectedDimensionTextPrimitive> TextPrimitives,
        IReadOnlyList<DetectedDimensionInsertPrimitive> InsertPrimitives,
        IReadOnlyList<DetectedDimensionCirclePrimitive> CirclePrimitives,
        IReadOnlyList<DetectedDimensionArcPrimitive> ArcPrimitives,
        IReadOnlyList<DetectedDimensionSolidPrimitive> SolidPrimitives);
}
