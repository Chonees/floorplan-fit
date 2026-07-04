using FloorplanFit.Domain.PlanSets;
using FloorplanFit.Infrastructure.Dxf;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;

namespace FloorplanFit.Infrastructure.Tests.Dxf;

public sealed class ProjectedPlanSheetDxfExporterTests
{
    [Fact]
    public async Task ExportAsync_applies_projection_transform_to_basic_dxf_points()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-source.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-projected.dxf");
        await File.WriteAllTextAsync(
            sourcePath,
            string.Join(
                Environment.NewLine,
                [
                    "0", "SECTION",
                    "2", "ENTITIES",
                    "0", "LINE",
                    "8", "WALLS",
                    "10", "1",
                    "20", "2",
                    "11", "3",
                    "21", "4",
                    "0", "TEXT",
                    "8", "NOTES",
                    "10", "5",
                    "20", "6",
                    "1", "ELECTRICAL",
                    "0", "ENDSEC",
                    "0", "EOF"
                ]),
            CancellationToken.None);

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();

            await exporter.ExportAsync(
                sourcePath,
                outputPath,
                new SheetAdjustmentProjectionTransform(
                    scale: 2m,
                    rotationDegrees: 0m,
                    translateX: 10m,
                    translateY: 20m),
                CancellationToken.None);

            var lines = await File.ReadAllLinesAsync(outputPath, CancellationToken.None);

            AssertHasPair(lines, "10", "12");
            AssertHasPair(lines, "20", "24");
            AssertHasPair(lines, "11", "16");
            AssertHasPair(lines, "21", "28");
            AssertHasPair(lines, "10", "20");
            AssertHasPair(lines, "20", "32");
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_scales_circle_and_arc_radius_for_dependent_sheet_symbols()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-source.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-projected.dxf");
        await File.WriteAllTextAsync(
            sourcePath,
            string.Join(
                Environment.NewLine,
                [
                    "0", "SECTION",
                    "2", "ENTITIES",
                    "0", "CIRCLE",
                    "8", "ELECTRICAL",
                    "10", "1",
                    "20", "2",
                    "40", "3",
                    "0", "ARC",
                    "8", "ROOF",
                    "10", "4",
                    "20", "5",
                    "40", "6",
                    "50", "0",
                    "51", "180",
                    "0", "ENDSEC",
                    "0", "EOF"
                ]),
            CancellationToken.None);

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();

            await exporter.ExportAsync(
                sourcePath,
                outputPath,
                new SheetAdjustmentProjectionTransform(
                    scale: 2m,
                    rotationDegrees: 0m,
                    translateX: 10m,
                    translateY: 20m),
                CancellationToken.None);

            var lines = await File.ReadAllLinesAsync(outputPath, CancellationToken.None);

            AssertHasPair(lines, "10", "12");
            AssertHasPair(lines, "20", "24");
            AssertHasPair(lines, "40", "6");
            AssertHasPair(lines, "10", "18");
            AssertHasPair(lines, "20", "30");
            AssertHasPair(lines, "40", "12");
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_scales_and_rotates_insert_block_references()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-source.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-projected.dxf");
        await File.WriteAllTextAsync(
            sourcePath,
            string.Join(
                Environment.NewLine,
                [
                    "0", "SECTION",
                    "2", "ENTITIES",
                    "0", "INSERT",
                    "8", "ELECTRICAL",
                    "2", "OUTLET_SYMBOL",
                    "10", "1",
                    "20", "2",
                    "41", "1.5",
                    "42", "0.5",
                    "50", "30",
                    "0", "ENDSEC",
                    "0", "EOF"
                ]),
            CancellationToken.None);

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();

            await exporter.ExportAsync(
                sourcePath,
                outputPath,
                new SheetAdjustmentProjectionTransform(
                    scale: 2m,
                    rotationDegrees: 15m,
                    translateX: 10m,
                    translateY: 20m),
                CancellationToken.None);

            var lines = await File.ReadAllLinesAsync(outputPath, CancellationToken.None);

            AssertHasPair(lines, "41", "3");
            AssertHasPair(lines, "42", "1");
            AssertHasPair(lines, "50", "45");
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_scales_text_and_mtext_height_for_dependent_sheet_labels()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-source.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-projected.dxf");
        await File.WriteAllTextAsync(
            sourcePath,
            string.Join(
                Environment.NewLine,
                [
                    "0", "SECTION",
                    "2", "ENTITIES",
                    "0", "TEXT",
                    "8", "NOTES",
                    "10", "1",
                    "20", "2",
                    "40", "0.125",
                    "1", "PANEL A",
                    "0", "MTEXT",
                    "8", "NOTES",
                    "10", "3",
                    "20", "4",
                    "40", "0.25",
                    "1", "ROOF NOTE",
                    "0", "ENDSEC",
                    "0", "EOF"
                ]),
            CancellationToken.None);

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();

            await exporter.ExportAsync(
                sourcePath,
                outputPath,
                new SheetAdjustmentProjectionTransform(
                    scale: 2m,
                    rotationDegrees: 0m,
                    translateX: 10m,
                    translateY: 20m),
                CancellationToken.None);

            var lines = await File.ReadAllLinesAsync(outputPath, CancellationToken.None);

            AssertHasPair(lines, "40", "0.25");
            AssertHasPair(lines, "40", "0.5");
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_rotates_arc_start_and_end_angles()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-source.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-projected.dxf");
        await File.WriteAllTextAsync(
            sourcePath,
            string.Join(
                Environment.NewLine,
                [
                    "0", "SECTION",
                    "2", "ENTITIES",
                    "0", "ARC",
                    "8", "ROOF",
                    "10", "1",
                    "20", "2",
                    "40", "3",
                    "50", "30",
                    "51", "120",
                    "0", "ENDSEC",
                    "0", "EOF"
                ]),
            CancellationToken.None);

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();

            await exporter.ExportAsync(
                sourcePath,
                outputPath,
                new SheetAdjustmentProjectionTransform(
                    scale: 1m,
                    rotationDegrees: 15m,
                    translateX: 0m,
                    translateY: 0m),
                CancellationToken.None);

            var lines = await File.ReadAllLinesAsync(outputPath, CancellationToken.None);

            AssertHasPair(lines, "50", "45");
            AssertHasPair(lines, "51", "135");
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_projects_binary_dxf_without_corrupting_autocad_file()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-binary-source.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-binary-projected.dxf");
        var dxf = new DxfFile();
        dxf.Entities.Add(new DxfLine(
            new DxfPoint(1d, 2d, 0d),
            new DxfPoint(3d, 4d, 0d)));
        dxf.Save(sourcePath, asText: false);

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();

            await exporter.ExportAsync(
                sourcePath,
                outputPath,
                new SheetAdjustmentProjectionTransform(
                    scale: 2m,
                    rotationDegrees: 0m,
                    translateX: 10m,
                    translateY: 20m),
                CancellationToken.None);

            var reloaded = DxfFile.Load(outputPath);
            var line = Assert.IsType<DxfLine>(Assert.Single(reloaded.Entities));

            Assert.Equal(12d, line.P1.X);
            Assert.Equal(24d, line.P1.Y);
            Assert.Equal(16d, line.P2.X);
            Assert.Equal(28d, line.P2.Y);
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_does_not_project_header_extents()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-source.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-projected.dxf");
        await File.WriteAllTextAsync(
            sourcePath,
            string.Join(
                Environment.NewLine,
                [
                    "0", "SECTION",
                    "2", "HEADER",
                    "9", "$EXTMIN",
                    "10", "1",
                    "20", "2",
                    "9", "$EXTMAX",
                    "10", "3",
                    "20", "4",
                    "0", "ENDSEC",
                    "0", "SECTION",
                    "2", "ENTITIES",
                    "0", "LINE",
                    "8", "ELECTRICAL",
                    "10", "1",
                    "20", "2",
                    "11", "3",
                    "21", "4",
                    "0", "ENDSEC",
                    "0", "EOF"
                ]),
            CancellationToken.None);

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();

            await exporter.ExportAsync(
                sourcePath,
                outputPath,
                new SheetAdjustmentProjectionTransform(
                    scale: 2m,
                    rotationDegrees: 0m,
                    translateX: 10m,
                    translateY: 20m),
                CancellationToken.None);

            var lines = await File.ReadAllLinesAsync(outputPath, CancellationToken.None);

            AssertHeaderPair(lines, "$EXTMIN", "10", "1");
            AssertHeaderPair(lines, "$EXTMIN", "20", "2");
            AssertHeaderPair(lines, "$EXTMAX", "10", "3");
            AssertHeaderPair(lines, "$EXTMAX", "20", "4");
            AssertHasPair(lines, "10", "12");
            AssertHasPair(lines, "20", "24");
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_preserves_electrical_curves()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-source.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-projected.dxf");
        await File.WriteAllTextAsync(
            sourcePath,
            string.Join(
                Environment.NewLine,
                [
                    "0", "SECTION",
                    "2", "ENTITIES",
                    "0", "ARC",
                    "8", "ELECTRICAL WIRING",
                    "10", "100",
                    "20", "200",
                    "40", "500",
                    "50", "0",
                    "51", "90",
                    "0", "SPLINE",
                    "8", "ELECTRICAL WIRING",
                    "10", "1",
                    "20", "2",
                    "0", "LINE",
                    "8", "ELECTRICAL WIRING",
                    "10", "1",
                    "20", "2",
                    "11", "3",
                    "21", "4",
                    "0", "ARC",
                    "8", "ELECTRICAL WALLS",
                    "10", "5",
                    "20", "6",
                    "40", "7",
                    "50", "0",
                    "51", "90",
                    "0", "ARC",
                    "8", "ELECTRICAL",
                    "62", "253",
                    "10", "9",
                    "20", "10",
                    "40", "11",
                    "50", "0",
                    "51", "90",
                    "0", "ARC",
                    "8", "ELECTRICAL",
                    "62", "256",
                    "10", "13",
                    "20", "14",
                    "40", "1",
                    "50", "0",
                    "51", "90",
                    "0", "LINE",
                    "8", "ELECTRICAL WALLS",
                    "10", "5",
                    "20", "6",
                    "11", "7",
                    "21", "8",
                    "0", "LWPOLYLINE",
                    "8", "ELECTRICAL WALLS",
                    "62", "251",
                    "90", "4",
                    "10", "1",
                    "20", "1",
                    "10", "2",
                    "20", "2",
                    "0", "ELLIPSE",
                    "8", "ELECTRICAL WALLS",
                    "62", "251",
                    "10", "3",
                    "20", "3",
                    "11", "1",
                    "21", "0",
                    "40", "0.678",
                    "0", "ENDSEC",
                    "0", "EOF"
                ]),
            CancellationToken.None);

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();

            await exporter.ExportAsync(
                sourcePath,
                outputPath,
                new SheetAdjustmentProjectionTransform(
                    scale: 2m,
                    rotationDegrees: 0m,
                    translateX: 10m,
                    translateY: 20m),
                CancellationToken.None);

            var lines = await File.ReadAllLinesAsync(outputPath, CancellationToken.None);

            AssertEntityHasPair(lines, "ARC", "ELECTRICAL WIRING", "10", "210");
            AssertEntityHasPair(lines, "SPLINE", "ELECTRICAL WIRING", "10", "12");
            AssertEntityHasPair(lines, "ARC", "ELECTRICAL WALLS", "10", "20");
            AssertEntityHasPair(lines, "LWPOLYLINE", "ELECTRICAL WALLS", "10", "12");
            AssertEntityHasPair(lines, "ELLIPSE", "ELECTRICAL WALLS", "10", "16");
            AssertEntityHasPair(lines, "ELLIPSE", "ELECTRICAL WALLS", "11", "2");
            AssertEntityHasPair(lines, "ELLIPSE", "ELECTRICAL WALLS", "21", "0");
            AssertEntityHasPair(lines, "ARC", "ELECTRICAL", "62", "253");
            AssertEntityHasPair(lines, "LINE", "ELECTRICAL WIRING", "10", "12");
            AssertEntityHasPair(lines, "LINE", "ELECTRICAL WALLS", "10", "20");
            AssertEntityHasPair(lines, "ARC", "ELECTRICAL", "62", "256");
            AssertEntityHasPair(lines, "ARC", "ELECTRICAL", "40", "2");
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_preserves_non_dimension_block_curves()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-source.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-projected.dxf");
        await File.WriteAllTextAsync(
            sourcePath,
            string.Join(
                Environment.NewLine,
                [
                    "0", "SECTION",
                    "2", "BLOCKS",
                    "0", "BLOCK",
                    "2", "CFANLT",
                    "0", "ARC",
                    "8", "ELECTRICAL",
                    "62", "253",
                    "10", "0",
                    "20", "0",
                    "40", "29.821",
                    "50", "0",
                    "51", "90",
                    "0", "CIRCLE",
                    "8", "ELECTRICAL",
                    "62", "256",
                    "10", "0",
                    "20", "0",
                    "40", "5",
                    "0", "ENDBLK",
                    "0", "ENDSEC",
                    "0", "SECTION",
                    "2", "ENTITIES",
                    "0", "INSERT",
                    "8", "ELECTRICAL",
                    "2", "CFANLT",
                    "10", "1",
                    "20", "2",
                    "0", "ENDSEC",
                    "0", "EOF"
                ]),
            CancellationToken.None);

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();

            await exporter.ExportAsync(
                sourcePath,
                outputPath,
                new SheetAdjustmentProjectionTransform(
                    scale: 2m,
                    rotationDegrees: 0m,
                    translateX: 10m,
                    translateY: 20m),
                CancellationToken.None);

            var lines = await File.ReadAllLinesAsync(outputPath, CancellationToken.None);

            AssertEntityHasPair(lines, "ARC", "ELECTRICAL", "62", "253");
            AssertEntityHasPair(lines, "ARC", "ELECTRICAL", "40", "29.821");
            AssertEntityHasPair(lines, "CIRCLE", "ELECTRICAL", "40", "5");
            AssertEntityHasPair(lines, "INSERT", "ELECTRICAL", "10", "12");
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_projects_dimension_graphic_blocks_with_dimension_entities()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-source.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-projected.dxf");
        await File.WriteAllTextAsync(
            sourcePath,
            string.Join(
                Environment.NewLine,
                [
                    "0", "SECTION",
                    "2", "BLOCKS",
                    "0", "BLOCK",
                    "2", "*D1",
                    "10", "0",
                    "20", "0",
                    "0", "LINE",
                    "8", "DIMS",
                    "10", "1",
                    "20", "2",
                    "11", "3",
                    "21", "4",
                    "0", "TEXT",
                    "8", "DIMS",
                    "10", "5",
                    "20", "6",
                    "40", "0.5",
                    "1", "2'-0\"",
                    "0", "ENDBLK",
                    "0", "BLOCK",
                    "2", "DOOR",
                    "0", "ARC",
                    "8", "ELECTRICAL WALLS",
                    "10", "7",
                    "20", "8",
                    "40", "9",
                    "0", "ENDBLK",
                    "0", "ENDSEC",
                    "0", "SECTION",
                    "2", "ENTITIES",
                    "0", "DIMENSION",
                    "8", "DIMS",
                    "2", "*D1",
                    "10", "1",
                    "20", "2",
                    "11", "3",
                    "21", "4",
                    "0", "INSERT",
                    "8", "ELECTRICAL WALLS",
                    "2", "DOOR",
                    "10", "1",
                    "20", "2",
                    "0", "ENDSEC",
                    "0", "EOF"
                ]),
            CancellationToken.None);

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();

            await exporter.ExportAsync(
                sourcePath,
                outputPath,
                new SheetAdjustmentProjectionTransform(
                    scale: 2m,
                    rotationDegrees: 0m,
                    translateX: 10m,
                    translateY: 20m),
                CancellationToken.None);

            var lines = await File.ReadAllLinesAsync(outputPath, CancellationToken.None);

            AssertEntityHasPair(lines, "DIMENSION", "DIMS", "10", "12");
            AssertEntityHasPair(lines, "LINE", "DIMS", "10", "12");
            AssertEntityHasPair(lines, "TEXT", "DIMS", "10", "20");
            AssertEntityHasPair(lines, "TEXT", "DIMS", "40", "1");
            AssertEntityHasPair(lines, "ARC", "ELECTRICAL WALLS", "10", "7");
            AssertEntityHasPair(lines, "INSERT", "ELECTRICAL WALLS", "10", "12");
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    private static void AssertHasPair(IReadOnlyList<string> lines, string code, string value)
    {
        for (var index = 0; index + 1 < lines.Count; index += 2)
        {
            if (lines[index] == code && lines[index + 1] == value)
            {
                return;
            }
        }

        Assert.Fail($"Expected DXF pair {code}/{value}.");
    }

    private static void AssertHeaderPair(IReadOnlyList<string> lines, string variableName, string code, string value)
    {
        for (var index = 0; index + 5 < lines.Count; index += 2)
        {
            if (lines[index] == "9" &&
                lines[index + 1] == variableName &&
                lines[index + 2] == code &&
                lines[index + 3] == value)
            {
                return;
            }
        }

        Assert.Fail($"Expected header variable {variableName} to keep pair {code}/{value}.");
    }

    private static void AssertEntityMissing(IReadOnlyList<string> lines, string entityType, string layerName)
    {
        Assert.False(
            HasEntity(lines, entityType, layerName),
            $"Expected no {entityType} entity on layer {layerName}.");
    }

    private static void AssertEntityMissing(
        IReadOnlyList<string> lines,
        string entityType,
        string layerName,
        string code,
        string value)
    {
        Assert.False(
            HasEntity(lines, entityType, layerName, code, value),
            $"Expected no {entityType} entity on layer {layerName} with pair {code}/{value}.");
    }

    private static void AssertEntityHasPair(
        IReadOnlyList<string> lines,
        string entityType,
        string layerName,
        string code,
        string value)
    {
        for (var index = 0; index + 1 < lines.Count; index += 2)
        {
            if (lines[index] != "0" ||
                !string.Equals(lines[index + 1], entityType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var end = index + 2;
            while (end + 1 < lines.Count && lines[end] != "0")
            {
                end += 2;
            }

            if (!EntityHasPair(lines, index + 2, end, "8", layerName))
            {
                continue;
            }

            if (EntityHasPair(lines, index + 2, end, code, value))
            {
                return;
            }
        }

        Assert.Fail($"Expected {entityType} entity on layer {layerName} to have pair {code}/{value}.");
    }

    private static bool HasEntity(
        IReadOnlyList<string> lines,
        string entityType,
        string layerName,
        string? requiredCode = null,
        string? requiredValue = null)
    {
        for (var index = 0; index + 1 < lines.Count; index += 2)
        {
            if (lines[index] != "0" ||
                !string.Equals(lines[index + 1], entityType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var end = index + 2;
            while (end + 1 < lines.Count && lines[end] != "0")
            {
                end += 2;
            }

            if (!EntityHasPair(lines, index + 2, end, "8", layerName))
            {
                continue;
            }

            if (requiredCode is null ||
                EntityHasPair(lines, index + 2, end, requiredCode, requiredValue ?? string.Empty))
            {
                return true;
            }
        }

        return false;
    }

    private static bool EntityHasPair(
        IReadOnlyList<string> lines,
        int start,
        int end,
        string code,
        string value)
    {
        for (var index = start; index + 1 < end; index += 2)
        {
            if (lines[index] == code &&
                string.Equals(lines[index + 1], value, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
