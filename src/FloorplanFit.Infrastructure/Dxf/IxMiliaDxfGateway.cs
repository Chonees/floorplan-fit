using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.Measurement;
using IxMilia.Dxf;

namespace FloorplanFit.Infrastructure.Dxf;

public sealed class IxMiliaDxfGateway : IDxfGateway
{
    public Task<DetectedFloorPlanDocument> ReadFloorPlanAsync(string filePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("A DXF file path is required.", nameof(filePath));
        }

        var dxfFile = DxfFile.Load(filePath);
        var sourceUnit = DetectSourceUnit(dxfFile);
        var boundingBox = dxfFile.GetBoundingBox();

        var document = new DetectedFloorPlanDocument(
            Path.GetFileName(filePath),
            Path.GetFileNameWithoutExtension(filePath),
            sourceUnit,
            ToMillimetersFactor(sourceUnit),
            DxfAcadVersionStrings.VersionToString(dxfFile.Header.Version),
            BuildGeometryFingerprint(boundingBox));

        return Task.FromResult(document);
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
            _ => throw new InvalidOperationException("The DXF source unit could not be mapped to a millimeter conversion factor.")
        };
    }

    private static string BuildGeometryFingerprint(DxfBoundingBox boundingBox)
    {
        return FormattableString.Invariant(
            $"bbox:{FormatDouble(boundingBox.MinimumPoint.X)},{FormatDouble(boundingBox.MinimumPoint.Y)},{FormatDouble(boundingBox.MaximumPoint.X)},{FormatDouble(boundingBox.MaximumPoint.Y)}");
    }

    private static string FormatDouble(double value)
    {
        return value.ToString("G17", CultureInfo.InvariantCulture);
    }
}
