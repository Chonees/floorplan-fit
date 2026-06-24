using System.Globalization;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;

namespace FloorplanFit.Infrastructure.Dxf;

public static class SyntheticSitePlanDxfWriter
{
    public const string SetbacksLayer = "SETBACKS";
    public const string PropertyLayer = "2312-001-BM$0$C-PROP-SUBD";
    public const string EasementLayer = "E";
    public const string TextLayer = "TEXT";
    public const string ZeroLayer = "0";

    private const decimal InchesPerFoot = 12m;
    private const double PropertyMarginInches = 24d;

    public static string WriteToTempFile(decimal buildableWidthFeet, decimal buildableHeightFeet)
    {
        var fileName = FormattableString.Invariant(
            $"SYNTH ADJUST {Slug(buildableWidthFeet)}x{Slug(buildableHeightFeet)} FT {Guid.NewGuid():N}.dxf");
        var path = Path.Combine(Path.GetTempPath(), fileName);
        Write(path, buildableWidthFeet, buildableHeightFeet);
        return path;
    }

    public static void Write(string path, decimal buildableWidthFeet, decimal buildableHeightFeet)
    {
        if (buildableWidthFeet <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(buildableWidthFeet), "Buildable width must be positive.");
        }

        if (buildableHeightFeet <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(buildableHeightFeet), "Buildable height must be positive.");
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A DXF path is required.", nameof(path));
        }

        var widthInches = (double)(buildableWidthFeet * InchesPerFoot);
        var heightInches = (double)(buildableHeightFeet * InchesPerFoot);
        var min = PropertyMarginInches;
        var maxX = min + widthInches;
        var maxY = min + heightInches;

        var dxf = new DxfFile();
        dxf.Header.Version = DxfAcadVersion.R2013;
        dxf.Header.DefaultDrawingUnits = DxfUnits.Inches;
        dxf.Header.DrawingUnits = DxfDrawingUnits.English;
        AddLayers(dxf);

        AddRectangle(dxf, PropertyLayer, 0d, 0d, maxX + PropertyMarginInches, maxY + PropertyMarginInches);
        AddRectangle(dxf, SetbacksLayer, min, min, maxX, maxY);
        AddLine(dxf, EasementLayer, 0d, maxY + 12d, maxX + PropertyMarginInches, maxY + 12d);
        dxf.Entities.Add(new DxfText(
            new DxfPoint(min, maxY + 24d, 0d),
            10d,
            FormattableString.Invariant($"SIMULATED SITE PLAN {buildableWidthFeet:G29}' x {buildableHeightFeet:G29}'"))
        {
            Layer = TextLayer
        });
        dxf.Entities.Add(new DxfText(new DxfPoint(min, min - 16d, 0d), 8d, "SETBACK")
        {
            Layer = SetbacksLayer
        });

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        dxf.Save(path, asText: true);
    }

    private static void AddLayers(DxfFile dxf)
    {
        AddLayer(dxf, ZeroLayer, 7);
        AddLayer(dxf, TextLayer, 7);
        AddLayer(dxf, EasementLayer, 8);
        AddLayer(dxf, PropertyLayer, 7);
        AddLayer(dxf, SetbacksLayer, 31);
    }

    private static void AddLayer(DxfFile dxf, string name, byte color)
    {
        if (dxf.Layers.Any(layer => string.Equals(layer.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        dxf.Layers.Add(new DxfLayer(name, DxfColor.FromIndex(color)));
    }

    private static void AddRectangle(DxfFile dxf, string layer, double minX, double minY, double maxX, double maxY)
    {
        AddLine(dxf, layer, minX, minY, maxX, minY);
        AddLine(dxf, layer, maxX, minY, maxX, maxY);
        AddLine(dxf, layer, maxX, maxY, minX, maxY);
        AddLine(dxf, layer, minX, maxY, minX, minY);
    }

    private static void AddLine(DxfFile dxf, string layer, double startX, double startY, double endX, double endY)
        => dxf.Entities.Add(new DxfLine(
            new DxfPoint(startX, startY, 0d),
            new DxfPoint(endX, endY, 0d))
        {
            Layer = layer
        });

    private static string Slug(decimal value)
        => value.ToString("G29", CultureInfo.InvariantCulture).Replace('.', '_');
}
