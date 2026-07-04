using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Infrastructure.Dxf;
using FloorplanFit.Infrastructure.Tests.TestSupport;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;

namespace FloorplanFit.Infrastructure.Tests.Dxf;

public sealed class IxMiliaAdjustedSitePlanExporterTests
{
    [Fact]
    public async Task ExportAsync_preserves_real_floor_plan_sections_when_combining_site_plan()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var floorPath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SEMINOLE2000.dxf");
        var sitePath = Path.Combine(solutionRoot, "PLANS", "originalsSitePlans", "158 DAWSON STREET.dxf");
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-adjusted-site-plan-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var outputPath = Path.Combine(tempRoot, "SEMINOLE2000-adjusted-to-site.dxf");
        var exporter = new IxMiliaAdjustedSitePlanExporter();

        try
        {
            var sourceSections = ReadSectionNames(floorPath);
            Assert.Contains("ACDSDATA", sourceSections);

            await exporter.ExportAsync(
                floorPath,
                sitePath,
                outputPath,
                new AdjustedSitePlanPlacementDto(
                    FloorToSiteScale: 1m,
                    SiteOffsetX: 0m,
                    SiteOffsetY: 0m,
                    CompressionSteps: []),
                CancellationToken.None);

            var outputSections = ReadSectionNames(outputPath);

            Assert.Equal(sourceSections, outputSections);
            Assert.Contains("ACDSDATA", File.ReadAllText(outputPath, System.Text.Encoding.Latin1));
            Assert.True(
                ReadHandSeed(outputPath) > ReadMaxHandle(outputPath),
                "The exported DXF must advance $HANDSEED beyond all injected handles so CAD readers do not allocate duplicates.");
            Assert.All(
                ReadLayerRecords(outputPath),
                layer => Assert.Contains(layer, pair => pair.Code == "390"));

            var reloaded = DxfFile.Load(outputPath);
            Assert.Contains(reloaded.Layers, layer => layer.Name.Contains("SETBACK", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExportAsync_patches_reactive_dimension_text_from_adjustment_preview()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var floorPath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SEMINOLE2000.dxf");
        var sitePath = CreateSitePlanFixture();
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-adjusted-site-plan-dimensions-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var outputPath = Path.Combine(tempRoot, "SEMINOLE2000-adjusted-to-site-dimensions.dxf");

        try
        {
            var extractor = new IxMiliaDimensionExtractor();
            var sourceDimensions = await extractor.ExtractAsync(floorPath, CancellationToken.None);
            var sourceDimension = Assert.Single(sourceDimensions, item => item.GeometryBlockName == "*D169");
            var adjustedDimension = CreateAdjustedDimensionPatch(
                sourceDimension,
                displayText: "6'-0\"",
                textOffsetX: 10m,
                textOffsetY: 5m,
                firstLineOffsetX: 12m);
            var exporter = new IxMiliaAdjustedSitePlanExporter();
            var placement = new AdjustedSitePlanPlacementDto(
                FloorToSiteScale: 1m,
                SiteOffsetX: 0m,
                SiteOffsetY: 0m,
                CompressionSteps: [],
                AdjustedDimensions: [adjustedDimension]);

            await exporter.ExportAsync(floorPath, sitePath, outputPath, placement, CancellationToken.None);

            var exportedDimensions = await extractor.ExtractAsync(outputPath, CancellationToken.None);
            var reloaded = Assert.Single(exportedDimensions, item => item.GeometryBlockName == sourceDimension.GeometryBlockName);

            Assert.Equal("6'-0\"", reloaded.DisplayText);
            Assert.Equal((sourceDimension.RenderTextX ?? 0m) + 10m, reloaded.RenderTextX);
            Assert.Equal((sourceDimension.RenderTextY ?? 0m) + 5m, reloaded.RenderTextY);
            Assert.Equal(sourceDimension.LineSegments[0].StartX + 12m, reloaded.LineSegments[0].StartX);
        }
        finally
        {
            File.Delete(sitePath);
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExportAsync_patches_floor_plan_compression_and_injects_inverse_transformed_site_plan()
    {
        var floorPath = CreateFloorPlanFixture();
        var sitePath = CreateSitePlanFixture();
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dxf");
        var exporter = new IxMiliaAdjustedSitePlanExporter();

        // Preview affine: previewPoint = floorSource * 1 + (110, 50).
        // One applied compression: width, right edge, cut at floor-source X=50, trim 2".
        var placement = new AdjustedSitePlanPlacementDto(
            FloorToSiteScale: 1m,
            SiteOffsetX: 110m,
            SiteOffsetY: 50m,
            CompressionSteps:
            [
                new AdjustedCompressionStepDto(
                    "Width",
                    "Right",
                    [new AdjustedCompressionMarkerDto(Coordinate: 50m, TrimSourceUnits: 2m)])
            ]);

        try
        {
            var result = await exporter.ExportAsync(floorPath, sitePath, outputPath, placement, CancellationToken.None);

            Assert.Equal(outputPath, result.OutputFilePath);
            Assert.True(result.InjectedSitePlanEntityCount >= 3);

            var lines = ReadLines(outputPath);

            // Floor plan walls: the span line (0,0)-(100,0) loses 2" on the right of the
            // cut; the right-side vertical wall shifts whole; the left wall stays put.
            AssertHasLine(lines, "WALLS", 0m, 0m, 98m, 0m);
            AssertHasLine(lines, "WALLS", 98m, 0m, 98m, 40m);
            AssertHasLine(lines, "WALLS", 0m, 0m, 0m, 40m);

            // Floor text beyond the cut shifts with the compression.
            AssertHasText(lines, "ROOM", 58m, 20m);

            // Site plan entities arrive inverse-transformed: q = (p - (110,50)) / 1.
            AssertHasLine(lines, "SETBACKS", 100m, 100m, 200m, 100m);
            AssertHasLine(lines, "2312-001-BM$0$C-PROP-SUBD", 90m, 90m, 210m, 90m);
            AssertHasArc(lines, "SETBACKS", 150m, 150m, 12m);
            AssertHasText(lines, "25' SETBACK", 110m, 110m);

            // Site plan layers must exist in the LAYER table of the merged file.
            var reloaded = DxfFile.Load(outputPath);
            Assert.Contains(reloaded.Layers, layer => layer.Name == "SETBACKS");
            Assert.Contains(reloaded.Layers, layer => layer.Name == "2312-001-BM$0$C-PROP-SUBD");
            Assert.Contains(reloaded.Entities.OfType<DxfLine>(), line => line.Layer == "SETBACKS");
        }
        finally
        {
            File.Delete(floorPath);
            File.Delete(sitePath);
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task ExportAsync_applies_inverse_scale_to_site_plan_lengths_and_text_heights()
    {
        var floorPath = CreateFloorPlanFixture();
        var sitePath = CreateSitePlanFixture();
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dxf");
        var exporter = new IxMiliaAdjustedSitePlanExporter();

        var placement = new AdjustedSitePlanPlacementDto(
            FloorToSiteScale: 2m,
            SiteOffsetX: 20m,
            SiteOffsetY: 10m,
            CompressionSteps: []);

        try
        {
            await exporter.ExportAsync(floorPath, sitePath, outputPath, placement, CancellationToken.None);
            var lines = ReadLines(outputPath);

            // q = (p - (20,10)) / 2 -> setback line (210,150)-(310,150) lands at (95,70)-(145,70).
            AssertHasLine(lines, "SETBACKS", 95m, 70m, 145m, 70m);
            // Arc radius halves with the inverse scale: center (260,200) r12 -> (120,95) r6.
            AssertHasArc(lines, "SETBACKS", 120m, 95m, 6m);
        }
        finally
        {
            File.Delete(floorPath);
            File.Delete(sitePath);
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task ExportAsync_preserves_site_plan_layer_and_entity_visual_metadata()
    {
        var floorPath = CreateFloorPlanFixture();
        var sitePath = CreateVisualSitePlanFixture();
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dxf");
        var exporter = new IxMiliaAdjustedSitePlanExporter();

        try
        {
            await exporter.ExportAsync(
                floorPath,
                sitePath,
                outputPath,
                new AdjustedSitePlanPlacementDto(
                    FloorToSiteScale: 1m,
                    SiteOffsetX: 0m,
                    SiteOffsetY: 0m,
                    CompressionSteps: []),
                CancellationToken.None);

            var layerRecord = Assert.Single(
                ReadLayerRecords(outputPath),
                record => record.Any(pair => pair.Code == "2" && pair.Value == "VISUAL-SITE"));
            Assert.Contains(layerRecord, pair => pair.Code == "62" && pair.Value == "6");
            Assert.Contains(layerRecord, pair => pair.Code == "6" && pair.Value == "VISUAL-PHANTOM");
            Assert.Contains(layerRecord, pair => pair.Code == "370" && pair.Value == "20");
            Assert.Contains(
                ReadTableRecords(outputPath, "LTYPE"),
                record => record.Any(pair => pair.Code == "2" && pair.Value == "VISUAL-PHANTOM"));
            Assert.Contains(
                ReadTableRecords(outputPath, "STYLE"),
                record => record.Any(pair => pair.Code == "2" && pair.Value == "HOUSE"));

            var visualLine = Assert.Single(
                ReadEntityRecords(outputPath),
                record => record[0].Value == "LINE" &&
                          record.Any(pair => pair.Code == "8" && pair.Value == "VISUAL-SITE"));
            Assert.Contains(visualLine, pair => pair.Code == "62" && pair.Value == "1");
            Assert.Contains(visualLine, pair => pair.Code == "370" && pair.Value == "13");

            var visualText = Assert.Single(
                ReadEntityRecords(outputPath),
                record => record[0].Value == "TEXT" &&
                          record.Any(pair => pair.Code == "1" && pair.Value == "VISUAL TITLE"));
            Assert.Contains(visualText, pair => pair.Code == "7" && pair.Value == "HOUSE");
            Assert.Contains(visualText, pair => pair.Code == "41" && pair.Value == "1.25");
            Assert.Contains(visualText, pair => pair.Code == "51" && pair.Value == "5");
        }
        finally
        {
            File.Delete(floorPath);
            File.Delete(sitePath);
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    private static string CreateFloorPlanFixture()
    {
        var dxf = new DxfFile();
        dxf.Header.Version = DxfAcadVersion.R2013;
        dxf.Layers.Add(new DxfLayer("WALLS", DxfColor.FromIndex(7)));
        dxf.Layers.Add(new DxfLayer("DOORS", DxfColor.FromIndex(3)));

        dxf.Entities.Add(new DxfLine(new DxfPoint(0, 0, 0), new DxfPoint(100, 0, 0)) { Layer = "WALLS" });
        dxf.Entities.Add(new DxfLine(new DxfPoint(100, 0, 0), new DxfPoint(100, 40, 0)) { Layer = "WALLS" });
        dxf.Entities.Add(new DxfLine(new DxfPoint(0, 0, 0), new DxfPoint(0, 40, 0)) { Layer = "WALLS" });
        dxf.Entities.Add(new DxfArc(new DxfPoint(30, 10, 0), 5, 0, 90) { Layer = "DOORS" });
        dxf.Entities.Add(new DxfText(new DxfPoint(60, 20, 0), 5, "ROOM") { Layer = "WALLS" });

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dxf");
        dxf.Save(path, asText: true);
        return path;
    }

    private static DimensionDto CreateAdjustedDimensionPatch(
        DetectedDimension sourceDimension,
        string displayText,
        decimal textOffsetX,
        decimal textOffsetY,
        decimal firstLineOffsetX)
    {
        return new DimensionDto(
            Guid.NewGuid(),
            sourceDimension.SourceEntityRef,
            sourceDimension.SourceLayer,
            sourceDimension.SourceEntityKind,
            sourceDimension.GeometryBlockName,
            displayText,
            sourceDimension.DisplayTextSource,
            displayText,
            sourceDimension.MeasurementSourceUnits,
            sourceDimension.MeasurementMillimeters,
            sourceDimension.SourceUnit,
            sourceDimension.DimType,
            sourceDimension.Angle,
            sourceDimension.ObliqueAngle,
            sourceDimension.DefPointX,
            sourceDimension.DefPointY,
            sourceDimension.DefPointZ,
            sourceDimension.DefPoint2X,
            sourceDimension.DefPoint2Y,
            sourceDimension.DefPoint2Z,
            sourceDimension.DefPoint3X,
            sourceDimension.DefPoint3Y,
            sourceDimension.DefPoint3Z,
            sourceDimension.Confidence,
            sourceDimension.DetectionNotes,
            1)
        {
            SourceHandle = sourceDimension.SourceHandle,
            RenderTextX = (sourceDimension.RenderTextX ?? 0m) + textOffsetX,
            RenderTextY = (sourceDimension.RenderTextY ?? 0m) + textOffsetY,
            RenderTextHeight = sourceDimension.RenderTextHeight,
            RenderTextRotationDegrees = sourceDimension.RenderTextRotationDegrees,
            RenderTextStyleName = sourceDimension.RenderTextStyleName,
            RenderTextHorizontalAlignment = sourceDimension.RenderTextHorizontalAlignment,
            RenderTextVerticalAlignment = sourceDimension.RenderTextVerticalAlignment,
            RenderTextAttachmentPoint = sourceDimension.RenderTextAttachmentPoint,
            LineSegments = sourceDimension.LineSegments
                .Select((segment, index) => new DimensionLineSegmentDto(
                    index == 0 ? segment.StartX + firstLineOffsetX : segment.StartX,
                    segment.StartY,
                    index == 0 ? segment.EndX + firstLineOffsetX : segment.EndX,
                    segment.EndY))
                .ToArray(),
            LinePrimitives = sourceDimension.LinePrimitives
                .Select((primitive, index) => new DimensionLinePrimitiveDto(
                    primitive.PrimitiveKey,
                    primitive.SortOrder,
                    index == 0 ? primitive.StartX + firstLineOffsetX : primitive.StartX,
                    primitive.StartY,
                    index == 0 ? primitive.EndX + firstLineOffsetX : primitive.EndX,
                    primitive.EndY)
                {
                    SourceHandle = primitive.SourceHandle,
                    SourceLayer = primitive.SourceLayer
                })
                .ToArray(),
            TextPrimitives = sourceDimension.TextPrimitives
                .Select(primitive => new DimensionTextPrimitiveDto(
                    primitive.PrimitiveKey,
                    primitive.SortOrder,
                    displayText,
                    primitive.X + textOffsetX,
                    primitive.Y + textOffsetY,
                    primitive.Height,
                    primitive.RotationDegrees)
                {
                    SourceHandle = primitive.SourceHandle,
                    SourceLayer = primitive.SourceLayer,
                    StyleName = primitive.StyleName,
                    HorizontalAlignment = primitive.HorizontalAlignment,
                    VerticalAlignment = primitive.VerticalAlignment,
                    AttachmentPoint = primitive.AttachmentPoint
                })
                .ToArray(),
            InsertPrimitives = sourceDimension.InsertPrimitives
                .Select(primitive => new DimensionInsertPrimitiveDto(
                    primitive.PrimitiveKey,
                    primitive.SortOrder,
                    primitive.Name,
                    primitive.X,
                    primitive.Y,
                    primitive.Z)
                {
                    SourceHandle = primitive.SourceHandle,
                    SourceLayer = primitive.SourceLayer,
                    RotationDegrees = primitive.RotationDegrees,
                    ScaleX = primitive.ScaleX,
                    ScaleY = primitive.ScaleY,
                    ScaleZ = primitive.ScaleZ
                })
                .ToArray(),
            CirclePrimitives = sourceDimension.CirclePrimitives
                .Select(primitive => new DimensionCirclePrimitiveDto(
                    primitive.PrimitiveKey,
                    primitive.SortOrder,
                    primitive.CenterX,
                    primitive.CenterY,
                    primitive.Radius)
                {
                    SourceHandle = primitive.SourceHandle,
                    SourceLayer = primitive.SourceLayer
                })
                .ToArray(),
            ArcPrimitives = sourceDimension.ArcPrimitives
                .Select(primitive => new DimensionArcPrimitiveDto(
                    primitive.PrimitiveKey,
                    primitive.SortOrder,
                    primitive.CenterX,
                    primitive.CenterY,
                    primitive.Radius,
                    primitive.StartAngleDegrees,
                    primitive.EndAngleDegrees)
                {
                    SourceHandle = primitive.SourceHandle,
                    SourceLayer = primitive.SourceLayer
                })
                .ToArray(),
            SolidPrimitives = sourceDimension.SolidPrimitives
                .Select(primitive => new DimensionSolidPrimitiveDto(
                    primitive.PrimitiveKey,
                    primitive.SortOrder,
                    primitive.Point1X,
                    primitive.Point1Y,
                    primitive.Point2X,
                    primitive.Point2Y,
                    primitive.Point3X,
                    primitive.Point3Y,
                    primitive.Point4X,
                    primitive.Point4Y)
                {
                    SourceHandle = primitive.SourceHandle,
                    SourceLayer = primitive.SourceLayer
                })
                .ToArray()
        };
    }

    private static string CreateSitePlanFixture()
    {
        var dxf = new DxfFile();
        dxf.Header.Version = DxfAcadVersion.R2013;
        dxf.Layers.Add(new DxfLayer("SETBACKS", DxfColor.FromIndex(31)));
        dxf.Layers.Add(new DxfLayer("2312-001-BM$0$C-PROP-SUBD", DxfColor.FromIndex(7)));

        dxf.Entities.Add(new DxfLine(new DxfPoint(210, 150, 0), new DxfPoint(310, 150, 0)) { Layer = "SETBACKS" });
        dxf.Entities.Add(new DxfLine(new DxfPoint(200, 140, 0), new DxfPoint(320, 140, 0)) { Layer = "2312-001-BM$0$C-PROP-SUBD" });
        dxf.Entities.Add(new DxfArc(new DxfPoint(260, 200, 0), 12, 10, 170) { Layer = "SETBACKS" });
        dxf.Entities.Add(new DxfText(new DxfPoint(220, 160, 0), 8, "25' SETBACK") { Layer = "SETBACKS" });

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dxf");
        dxf.Save(path, asText: true);
        return path;
    }

    private static string CreateVisualSitePlanFixture()
    {
        var dxf = new DxfFile();
        dxf.Header.Version = DxfAcadVersion.R2013;
        dxf.Layers.Add(new DxfLayer("VISUAL-SITE", DxfColor.FromIndex(6)));
        dxf.Entities.Add(new DxfLine(new DxfPoint(10, 20, 0), new DxfPoint(90, 20, 0))
        {
            Layer = "VISUAL-SITE"
        });
        dxf.Entities.Add(new DxfText(new DxfPoint(15, 30, 0), 4, "VISUAL TITLE")
        {
            Layer = "VISUAL-SITE"
        });

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dxf");
        dxf.Save(path, asText: true);
        PatchVisualSitePlanMetadata(path);
        return path;
    }

    private static void PatchVisualSitePlanMetadata(string path)
    {
        var lines = File.ReadAllLines(path, System.Text.Encoding.Latin1).ToList();
        InsertPairIntoRecord(lines, recordType: "LAYER", predicateCode: "2", predicateValue: "VISUAL-SITE", "370", "20");
        ReplacePairInRecord(lines, recordType: "LAYER", predicateCode: "2", predicateValue: "VISUAL-SITE", targetCode: "6", targetValue: "VISUAL-PHANTOM");
        InsertLTypeRecord(lines, "VISUAL-PHANTOM");
        InsertStyleRecord(lines, "HOUSE");
        InsertPairIntoRecord(lines, recordType: "LINE", predicateCode: "8", predicateValue: "VISUAL-SITE", "62", "1");
        InsertPairIntoRecord(lines, recordType: "LINE", predicateCode: "8", predicateValue: "VISUAL-SITE", "370", "13");
        InsertPairIntoRecord(lines, recordType: "TEXT", predicateCode: "1", predicateValue: "VISUAL TITLE", "7", "HOUSE");
        InsertPairIntoRecord(lines, recordType: "TEXT", predicateCode: "1", predicateValue: "VISUAL TITLE", "41", "1.25");
        InsertPairIntoRecord(lines, recordType: "TEXT", predicateCode: "1", predicateValue: "VISUAL TITLE", "51", "5");
        File.WriteAllLines(path, lines, System.Text.Encoding.Latin1);
    }

    private static void ReplacePairInRecord(
        List<string> lines,
        string recordType,
        string predicateCode,
        string predicateValue,
        string targetCode,
        string targetValue)
    {
        for (var index = 0; index + 1 < lines.Count; index += 2)
        {
            if (lines[index].Trim() != "0" ||
                !lines[index + 1].Trim().Equals(recordType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var recordEnd = index + 2;
            var matches = false;
            while (recordEnd + 1 < lines.Count && lines[recordEnd].Trim() != "0")
            {
                if (lines[recordEnd].Trim() == predicateCode &&
                    lines[recordEnd + 1].Trim() == predicateValue)
                {
                    matches = true;
                }

                recordEnd += 2;
            }

            if (!matches)
            {
                continue;
            }

            for (var cursor = index + 2; cursor + 1 < recordEnd; cursor += 2)
            {
                if (lines[cursor].Trim() == targetCode)
                {
                    lines[cursor + 1] = targetValue;
                    return;
                }
            }

            lines.Insert(recordEnd, targetValue);
            lines.Insert(recordEnd, targetCode);
            return;
        }

        throw new InvalidOperationException($"Could not replace {targetCode} in {recordType} record containing {predicateCode}={predicateValue}.");
    }

    private static void InsertLTypeRecord(List<string> lines, string linetypeName)
    {
        for (var index = 0; index + 1 < lines.Count; index += 2)
        {
            if (lines[index].Trim() != "0" ||
                !lines[index + 1].Trim().Equals("ENDTAB", StringComparison.OrdinalIgnoreCase) ||
                !IsInsideTable(lines, index, "LTYPE"))
            {
                continue;
            }

            var record = new[]
            {
                "0", "LTYPE",
                "5", "ABC",
                "100", "AcDbSymbolTableRecord",
                "100", "AcDbLinetypeTableRecord",
                "2", linetypeName,
                "70", "0",
                "3", linetypeName,
                "72", "65",
                "73", "0",
                "40", "0"
            };
            lines.InsertRange(index, record);
            return;
        }

        throw new InvalidOperationException("Could not find LTYPE table.");
    }

    private static void InsertStyleRecord(List<string> lines, string styleName)
    {
        for (var index = 0; index + 1 < lines.Count; index += 2)
        {
            if (lines[index].Trim() != "0" ||
                !lines[index + 1].Trim().Equals("ENDTAB", StringComparison.OrdinalIgnoreCase) ||
                !IsInsideTable(lines, index, "STYLE"))
            {
                continue;
            }

            var record = new[]
            {
                "0", "STYLE",
                "5", "ABD",
                "100", "AcDbSymbolTableRecord",
                "100", "AcDbTextStyleTableRecord",
                "2", styleName,
                "70", "0",
                "40", "0",
                "41", "1",
                "50", "0",
                "71", "0",
                "42", "0.2",
                "3", "txt",
                "4", ""
            };
            lines.InsertRange(index, record);
            return;
        }

        throw new InvalidOperationException("Could not find STYLE table.");
    }

    private static bool IsInsideTable(IReadOnlyList<string> lines, int endTabIndex, string tableName)
    {
        for (var cursor = endTabIndex - 2; cursor >= 0; cursor -= 2)
        {
            if (lines[cursor].Trim() == "2" &&
                lines[cursor + 1].Trim().Equals(tableName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (lines[cursor].Trim() == "0" &&
                lines[cursor + 1].Trim().Equals("TABLE", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return false;
    }

    private static void InsertPairIntoRecord(
        List<string> lines,
        string recordType,
        string predicateCode,
        string predicateValue,
        string code,
        string value)
    {
        for (var index = 0; index + 1 < lines.Count; index += 2)
        {
            if (lines[index].Trim() != "0" ||
                !lines[index + 1].Trim().Equals(recordType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var recordEnd = index + 2;
            var matches = false;
            while (recordEnd + 1 < lines.Count && lines[recordEnd].Trim() != "0")
            {
                if (lines[recordEnd].Trim() == predicateCode &&
                    lines[recordEnd + 1].Trim() == predicateValue)
                {
                    matches = true;
                }

                recordEnd += 2;
            }

            if (!matches)
            {
                continue;
            }

            lines.Insert(recordEnd, value);
            lines.Insert(recordEnd, code);
            return;
        }

        throw new InvalidOperationException($"Could not patch {recordType} record containing {predicateCode}={predicateValue}.");
    }

    private static IReadOnlyList<ParsedEntity> ReadLines(string path)
    {
        var lines = File.ReadAllLines(path);
        var entities = new List<ParsedEntity>();
        ParsedEntity? current = null;
        var inEntities = false;
        for (var index = 0; index + 1 < lines.Length; index += 2)
        {
            var code = lines[index].Trim();
            var value = lines[index + 1].Trim();
            if (code == "2" && value == "ENTITIES")
            {
                inEntities = true;
                continue;
            }

            if (!inEntities)
            {
                continue;
            }

            if (code == "0")
            {
                if (value == "ENDSEC")
                {
                    break;
                }

                current = new ParsedEntity(value);
                entities.Add(current);
                continue;
            }

            if (current is null)
            {
                continue;
            }

            switch (code)
            {
                case "8": current.Layer = value; break;
                case "1": current.Text = value; break;
                case "10": current.X1 = Parse(value); break;
                case "20": current.Y1 = Parse(value); break;
                case "11": current.X2 = Parse(value); break;
                case "21": current.Y2 = Parse(value); break;
                case "40": current.Size = Parse(value); break;
            }
        }

        return entities;
    }

    private static IReadOnlyList<string> ReadSectionNames(string dxfPath)
    {
        var lines = File.ReadAllLines(dxfPath, System.Text.Encoding.Latin1);
        var sections = new List<string>();

        for (var index = 0; index + 3 < lines.Length; index += 2)
        {
            var code = lines[index].Trim();
            var value = lines[index + 1].Trim();
            var nextCode = lines[index + 2].Trim();
            var nextValue = lines[index + 3].Trim();

            if (code == "0" &&
                value.Equals("SECTION", StringComparison.OrdinalIgnoreCase) &&
                nextCode == "2")
            {
                sections.Add(nextValue);
            }
        }

        return sections;
    }

    private static IReadOnlyList<IReadOnlyList<(string Code, string Value)>> ReadLayerRecords(string dxfPath)
        => ReadTableRecords(dxfPath, "LAYER");

    private static IReadOnlyList<IReadOnlyList<(string Code, string Value)>> ReadTableRecords(string dxfPath, string tableName)
    {
        var lines = File.ReadAllLines(dxfPath, System.Text.Encoding.Latin1);
        var records = new List<IReadOnlyList<(string Code, string Value)>>();
        var inRequestedTable = false;

        for (var index = 0; index + 1 < lines.Length;)
        {
            var code = lines[index].Trim();
            var value = lines[index + 1].Trim();
            if (code == "0" &&
                value.Equals("TABLE", StringComparison.OrdinalIgnoreCase) &&
                index + 3 < lines.Length &&
                lines[index + 2].Trim() == "2")
            {
                inRequestedTable = lines[index + 3].Trim().Equals(tableName, StringComparison.OrdinalIgnoreCase);
                index += 4;
                continue;
            }

            if (inRequestedTable && code == "0" && value.Equals("ENDTAB", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (!inRequestedTable || code != "0" || !value.Equals(tableName, StringComparison.OrdinalIgnoreCase))
            {
                index += 2;
                continue;
            }

            var record = new List<(string Code, string Value)>();
            for (; index + 1 < lines.Length; index += 2)
            {
                var currentCode = lines[index].Trim();
                var currentValue = lines[index + 1].Trim();
                if (record.Count > 0 && currentCode == "0")
                {
                    break;
                }

                record.Add((currentCode, currentValue));
            }

            records.Add(record);
        }

        return records;
    }

    private static IReadOnlyList<IReadOnlyList<(string Code, string Value)>> ReadEntityRecords(string dxfPath)
    {
        var lines = File.ReadAllLines(dxfPath, System.Text.Encoding.Latin1);
        var entities = new List<IReadOnlyList<(string Code, string Value)>>();
        var inEntities = false;

        for (var index = 0; index + 1 < lines.Length;)
        {
            var code = lines[index].Trim();
            var value = lines[index + 1].Trim();
            if (code == "2" && value.Equals("ENTITIES", StringComparison.OrdinalIgnoreCase))
            {
                inEntities = true;
                index += 2;
                continue;
            }

            if (!inEntities)
            {
                index += 2;
                continue;
            }

            if (code == "0" && value.Equals("ENDSEC", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (code != "0")
            {
                index += 2;
                continue;
            }

            var record = new List<(string Code, string Value)>();
            for (; index + 1 < lines.Length; index += 2)
            {
                var currentCode = lines[index].Trim();
                var currentValue = lines[index + 1].Trim();
                if (record.Count > 0 && currentCode == "0")
                {
                    break;
                }

                record.Add((currentCode, currentValue));
            }

            entities.Add(record);
        }

        return entities;
    }

    private static int ReadHandSeed(string dxfPath)
    {
        var lines = File.ReadAllLines(dxfPath, System.Text.Encoding.Latin1);
        for (var index = 0; index + 3 < lines.Length; index += 2)
        {
            if (lines[index].Trim() == "9" &&
                lines[index + 1].Trim().Equals("$HANDSEED", StringComparison.OrdinalIgnoreCase) &&
                lines[index + 2].Trim() == "5")
            {
                return int.Parse(lines[index + 3].Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }
        }

        throw new InvalidOperationException($"Could not find $HANDSEED in {dxfPath}.");
    }

    private static int ReadMaxHandle(string dxfPath)
    {
        var lines = File.ReadAllLines(dxfPath, System.Text.Encoding.Latin1);
        var maxHandle = 0;
        for (var index = 0; index + 1 < lines.Length; index += 2)
        {
            if (lines[index].Trim() == "5" &&
                !IsHandSeedValue(lines, index) &&
                int.TryParse(lines[index + 1].Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var handle))
            {
                maxHandle = Math.Max(maxHandle, handle);
            }
        }

        return maxHandle;
    }

    private static bool IsHandSeedValue(IReadOnlyList<string> lines, int codeIndex)
        => codeIndex >= 2 &&
           lines[codeIndex - 2].Trim() == "9" &&
           lines[codeIndex - 1].Trim().Equals("$HANDSEED", StringComparison.OrdinalIgnoreCase);

    private static decimal Parse(string value)
        => decimal.Parse(value, CultureInfo.InvariantCulture);

    private static void AssertHasLine(IReadOnlyList<ParsedEntity> entities, string layer, decimal x1, decimal y1, decimal x2, decimal y2)
        => Assert.Contains(entities, item =>
            item.Kind == "LINE" && item.Layer == layer &&
            Near(item.X1, x1) && Near(item.Y1, y1) && Near(item.X2, x2) && Near(item.Y2, y2));

    private static void AssertHasArc(IReadOnlyList<ParsedEntity> entities, string layer, decimal centerX, decimal centerY, decimal radius)
        => Assert.Contains(entities, item =>
            item.Kind == "ARC" && item.Layer == layer &&
            Near(item.X1, centerX) && Near(item.Y1, centerY) && Near(item.Size, radius));

    private static void AssertHasText(IReadOnlyList<ParsedEntity> entities, string text, decimal x, decimal y)
        => Assert.Contains(entities, item =>
            (item.Kind == "TEXT" || item.Kind == "MTEXT") && item.Text == text &&
            Near(item.X1, x) && Near(item.Y1, y));

    private static bool Near(decimal? actual, decimal expected)
        => actual.HasValue && Math.Abs(actual.Value - expected) < 0.000001m;

    private sealed class ParsedEntity(string kind)
    {
        public string Kind { get; } = kind;
        public string Layer { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public decimal? X1 { get; set; }
        public decimal? Y1 { get; set; }
        public decimal? X2 { get; set; }
        public decimal? Y2 { get; set; }
        public decimal? Size { get; set; }
    }
}
