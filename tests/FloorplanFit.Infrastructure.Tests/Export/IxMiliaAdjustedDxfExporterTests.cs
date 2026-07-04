using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Infrastructure.Dxf;
using FloorplanFit.Infrastructure.Tests.TestSupport;

namespace FloorplanFit.Infrastructure.Tests.Export;

public sealed class IxMiliaAdjustedDxfExporterTests
{
    [Fact]
    public async Task ExportAsync_writes_adjusted_native_dimension_and_round_trips_through_extractor()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SEMINOLE2000.dxf");
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-adjusted-dxf-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var outputPath = Path.Combine(tempRoot, "SEMINOLE2000-adjusted.dxf");

        try
        {
            var extractor = new IxMiliaDimensionExtractor();
            var dimensions = await extractor.ExtractAsync(sourcePath, CancellationToken.None);
            var sourceDimension = Assert.Single(dimensions, item => item.GeometryBlockName == "*D169");

            var adjustedDimension = new DimensionDto(
                Guid.NewGuid(),
                sourceDimension.SourceEntityRef,
                sourceDimension.SourceLayer,
                sourceDimension.SourceEntityKind,
                sourceDimension.GeometryBlockName,
                "6'-0\"",
                sourceDimension.DisplayTextSource,
                "6'-0\"",
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
                RenderTextX = (sourceDimension.RenderTextX ?? 0m) + 10m,
                RenderTextY = (sourceDimension.RenderTextY ?? 0m) + 5m,
                RenderTextHeight = sourceDimension.RenderTextHeight,
                RenderTextRotationDegrees = sourceDimension.RenderTextRotationDegrees,
                RenderTextStyleName = sourceDimension.RenderTextStyleName,
                RenderTextHorizontalAlignment = sourceDimension.RenderTextHorizontalAlignment,
                RenderTextVerticalAlignment = sourceDimension.RenderTextVerticalAlignment,
                RenderTextAttachmentPoint = sourceDimension.RenderTextAttachmentPoint,
                LineSegments = sourceDimension.LineSegments
                    .Select((segment, index) => new DimensionLineSegmentDto(
                        index == 0 ? segment.StartX + 12m : segment.StartX,
                        segment.StartY,
                        index == 0 ? segment.EndX + 12m : segment.EndX,
                        segment.EndY))
                    .ToArray(),
                LinePrimitives = sourceDimension.LinePrimitives
                    .Select((primitive, index) => new DimensionLinePrimitiveDto(
                        primitive.PrimitiveKey,
                        primitive.SortOrder,
                        index == 0 ? primitive.StartX + 12m : primitive.StartX,
                        primitive.StartY,
                        index == 0 ? primitive.EndX + 12m : primitive.EndX,
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
                        "6'-0\"",
                        primitive.X + 10m,
                        primitive.Y + 5m,
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

            var exporter = new IxMiliaAdjustedDxfExporter();

            await exporter.ExportAsync(sourcePath, outputPath, [adjustedDimension], CancellationToken.None);

            Assert.True(File.Exists(outputPath));

            var adjustedDimensions = await extractor.ExtractAsync(outputPath, CancellationToken.None);
            var reloaded = Assert.Single(adjustedDimensions, item => item.GeometryBlockName == sourceDimension.GeometryBlockName);

            Assert.Equal("6'-0\"", reloaded.DisplayText);
            Assert.Equal((sourceDimension.RenderTextX ?? 0m) + 10m, reloaded.RenderTextX);
            Assert.Equal((sourceDimension.RenderTextY ?? 0m) + 5m, reloaded.RenderTextY);
            Assert.Equal(sourceDimension.LineSegments[0].StartX + 12m, reloaded.LineSegments[0].StartX);
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
    public async Task ExportAsync_suppresses_exact_source_duplicate_dimension_twins()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SEMINOLE2000.dxf");
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-adjusted-dxf-twins-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var outputPath = Path.Combine(tempRoot, "SEMINOLE2000-adjusted-twin-diagnostic.dxf");

        try
        {
            var extractor = new IxMiliaDimensionExtractor();
            var dimensions = await extractor.ExtractAsync(sourcePath, CancellationToken.None);
            var sourceDimension = Assert.Single(dimensions, item => item.GeometryBlockName == "*D169");
            var sourceTwin = Assert.Single(dimensions, item => item.GeometryBlockName == "*D498");

            Assert.Equal(sourceDimension.DisplayText, sourceTwin.DisplayText);
            Assert.Equal(sourceDimension.RenderTextX, sourceTwin.RenderTextX);
            Assert.Equal(sourceDimension.RenderTextY, sourceTwin.RenderTextY);

            var adjustedDimension = CreateAdjustedDimension(sourceDimension, "6'-0\"", textOffsetX: 10m, textOffsetY: 5m, firstLineOffsetX: 12m);
            var exporter = new IxMiliaAdjustedDxfExporter();

            await exporter.ExportAsync(sourcePath, outputPath, [adjustedDimension], CancellationToken.None);

            var adjustedDimensions = await extractor.ExtractAsync(outputPath, CancellationToken.None);
            var reloaded = Assert.Single(adjustedDimensions, item => item.GeometryBlockName == "*D169");

            Assert.DoesNotContain(adjustedDimensions, item => item.GeometryBlockName == "*D498");
            Assert.Equal("6'-0\"", reloaded.DisplayText);
            Assert.Equal((sourceDimension.RenderTextX ?? 0m) + 10m, reloaded.RenderTextX);
            Assert.Equal((sourceDimension.RenderTextY ?? 0m) + 5m, reloaded.RenderTextY);
            Assert.Equal(sourceDimension.LineSegments[0].StartX + 12m, reloaded.LineSegments[0].StartX);
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
    public async Task ExportAsync_preserves_native_dimension_metadata_for_block_rendered_adjustments()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SEMINOLE2000.dxf");
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-adjusted-dxf-native-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var outputPath = Path.Combine(tempRoot, "SEMINOLE2000-adjusted-native-diagnostic.dxf");

        try
        {
            var extractor = new IxMiliaDimensionExtractor();
            var dimensions = await extractor.ExtractAsync(sourcePath, CancellationToken.None);
            var sourceDimension = Assert.Single(dimensions, item => item.GeometryBlockName == "*D169");
            var adjustedDimension = CreateAdjustedDimension(sourceDimension, "6'-0\"", textOffsetX: 10m, textOffsetY: 5m, firstLineOffsetX: 12m);
            var exporter = new IxMiliaAdjustedDxfExporter();

            await exporter.ExportAsync(sourcePath, outputPath, [adjustedDimension], CancellationToken.None);

            var sourceRecord = ReadEntityRecord(sourcePath, "DIMENSION", "2", "*D169");
            var outputRecord = ReadEntityRecord(outputPath, "DIMENSION", "2", "*D169");

            Assert.Equal(GetGroupValues(sourceRecord, "1"), GetGroupValues(outputRecord, "1"));
            Assert.Equal(GetGroupValues(sourceRecord, "10"), GetGroupValues(outputRecord, "10"));
            Assert.Equal(GetGroupValues(sourceRecord, "20"), GetGroupValues(outputRecord, "20"));
            Assert.Equal(GetGroupValues(sourceRecord, "11"), GetGroupValues(outputRecord, "11"));
            Assert.Equal(GetGroupValues(sourceRecord, "21"), GetGroupValues(outputRecord, "21"));
            Assert.Equal(GetGroupValues(sourceRecord, "13"), GetGroupValues(outputRecord, "13"));
            Assert.Equal(GetGroupValues(sourceRecord, "23"), GetGroupValues(outputRecord, "23"));
            Assert.Equal(GetGroupValues(sourceRecord, "14"), GetGroupValues(outputRecord, "14"));
            Assert.Equal(GetGroupValues(sourceRecord, "24"), GetGroupValues(outputRecord, "24"));
            Assert.Equal(GetGroupValues(sourceRecord, "42"), GetGroupValues(outputRecord, "42"));
            Assert.Equal(GetGroupValues(sourceRecord, "50"), GetGroupValues(outputRecord, "50"));
            Assert.Equal(GetGroupValues(sourceRecord, "53"), GetGroupValues(outputRecord, "53"));
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
    public async Task ExportAsync_preserves_source_dxf_sections_and_removes_duplicate_native_dimensions()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SEMINOLE2000.dxf");
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-adjusted-dxf-source-preserving-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var outputPath = Path.Combine(tempRoot, "SEMINOLE2000-adjusted-source-preserving.dxf");

        try
        {
            var extractor = new IxMiliaDimensionExtractor();
            var dimensions = await extractor.ExtractAsync(sourcePath, CancellationToken.None);
            var sourceDimension = Assert.Single(dimensions, item => item.GeometryBlockName == "*D169");
            var adjustedDimension = CreateAdjustedDimension(sourceDimension, "6'-0\"", textOffsetX: 10m, textOffsetY: 5m, firstLineOffsetX: 12m);
            var exporter = new IxMiliaAdjustedDxfExporter();

            await exporter.ExportAsync(sourcePath, outputPath, [adjustedDimension], CancellationToken.None);

            Assert.Contains("ACDSDATA", ReadSectionNames(outputPath));
            Assert.Equal(164, CountEntityRecords(outputPath, "DIMENSION"));
            Assert.Equal(0, CountDuplicateDimensionSignatures(outputPath));
            Assert.Equal(0, CountDimAssocRecordsOwnedByRemovedDimensions(sourcePath, outputPath));
            Assert.Equal(0, CountDictionaryEntriesReferencingMissingDimAssoc(outputPath));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static DimensionDto CreateAdjustedDimension(
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

    private static IReadOnlyList<(string Code, string Value)> ReadEntityRecord(
        string dxfPath,
        string entityKind,
        string identityCode,
        string identityValue)
    {
        var lines = File.ReadAllLines(dxfPath, System.Text.Encoding.Latin1);

        for (var index = 0; index + 1 < lines.Length; index += 2)
        {
            var code = lines[index].Trim();
            var value = lines[index + 1].Trim();
            if (!string.Equals(code, "0", StringComparison.Ordinal) ||
                !string.Equals(value, entityKind, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var record = new List<(string Code, string Value)> { (code, value) };
            for (var cursor = index + 2; cursor + 1 < lines.Length; cursor += 2)
            {
                var currentCode = lines[cursor].Trim();
                var currentValue = lines[cursor + 1].Trim();
                if (string.Equals(currentCode, "0", StringComparison.Ordinal))
                {
                    break;
                }

                record.Add((currentCode, currentValue));
            }

            if (GetGroupValues(record, identityCode).Contains(identityValue, StringComparer.OrdinalIgnoreCase))
            {
                return record;
            }
        }

        throw new InvalidOperationException($"Could not find {entityKind} with group {identityCode}={identityValue} in {dxfPath}.");
    }

    private static IReadOnlyList<string> ReadSectionNames(string dxfPath)
    {
        var pairs = ReadDxfPairs(dxfPath);
        var sections = new List<string>();

        for (var index = 0; index + 1 < pairs.Count; index++)
        {
            if (string.Equals(pairs[index].Code, "0", StringComparison.Ordinal) &&
                string.Equals(pairs[index].Value, "SECTION", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(pairs[index + 1].Code, "2", StringComparison.Ordinal))
            {
                sections.Add(pairs[index + 1].Value);
            }
        }

        return sections;
    }

    private static int CountEntityRecords(string dxfPath, string entityKind)
    {
        return ReadEntityRecords(dxfPath, entityKind).Count;
    }

    private static int CountDuplicateDimensionSignatures(string dxfPath)
    {
        return ReadEntityRecords(dxfPath, "DIMENSION")
            .GroupBy(BuildDimensionSignature)
            .Count(group => group.Count() > 1);
    }

    private static int CountDimAssocRecordsOwnedByRemovedDimensions(string sourcePath, string outputPath)
    {
        var sourceDimensionHandles = ReadEntityRecords(sourcePath, "DIMENSION")
            .SelectMany(record => GetGroupValues(record, "5"))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var outputDimensionHandles = ReadEntityRecords(outputPath, "DIMENSION")
            .SelectMany(record => GetGroupValues(record, "5"))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        sourceDimensionHandles.ExceptWith(outputDimensionHandles);

        return ReadEntityRecords(outputPath, "DIMASSOC")
            .Count(record => GetGroupValues(record, "330").Any(sourceDimensionHandles.Contains));
    }

    private static int CountDictionaryEntriesReferencingMissingDimAssoc(string dxfPath)
    {
        var dimAssocHandles = ReadEntityRecords(dxfPath, "DIMASSOC")
            .SelectMany(record => GetGroupValues(record, "5"))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var count = 0;
        foreach (var dictionary in ReadEntityRecords(dxfPath, "DICTIONARY"))
        {
            for (var index = 0; index + 1 < dictionary.Count; index++)
            {
                if (string.Equals(dictionary[index].Code, "3", StringComparison.Ordinal) &&
                    string.Equals(dictionary[index].Value, "ACAD_DIMASSOC", StringComparison.OrdinalIgnoreCase) &&
                    (string.Equals(dictionary[index + 1].Code, "350", StringComparison.Ordinal) ||
                     string.Equals(dictionary[index + 1].Code, "360", StringComparison.Ordinal)) &&
                    !dimAssocHandles.Contains(dictionary[index + 1].Value))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static IReadOnlyList<IReadOnlyList<(string Code, string Value)>> ReadEntityRecords(string dxfPath, string entityKind)
    {
        var pairs = ReadDxfPairs(dxfPath);
        var records = new List<IReadOnlyList<(string Code, string Value)>>();

        for (var index = 0; index < pairs.Count;)
        {
            var pair = pairs[index];
            if (!string.Equals(pair.Code, "0", StringComparison.Ordinal) ||
                !string.Equals(pair.Value, entityKind, StringComparison.OrdinalIgnoreCase))
            {
                index++;
                continue;
            }

            var record = new List<(string Code, string Value)> { pair };
            index++;
            while (index < pairs.Count && !string.Equals(pairs[index].Code, "0", StringComparison.Ordinal))
            {
                record.Add(pairs[index]);
                index++;
            }

            records.Add(record);
        }

        return records;
    }

    private static string BuildDimensionSignature(IReadOnlyList<(string Code, string Value)> record)
    {
        string[] codes =
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
        return string.Join(
            "|",
            codes.Select(code => string.Join(",", GetGroupValues(record, code))));
    }

    private static IReadOnlyList<(string Code, string Value)> ReadDxfPairs(string dxfPath)
    {
        var lines = File.ReadAllLines(dxfPath, System.Text.Encoding.Latin1);
        var pairs = new List<(string Code, string Value)>(lines.Length / 2);
        for (var index = 0; index + 1 < lines.Length; index += 2)
        {
            pairs.Add((lines[index].Trim(), lines[index + 1].Trim()));
        }

        return pairs;
    }

    private static IReadOnlyList<string> GetGroupValues(IReadOnlyList<(string Code, string Value)> record, string code)
    {
        return record
            .Where(item => string.Equals(item.Code, code, StringComparison.Ordinal))
            .Select(item => item.Value)
            .ToArray();
    }
}
