using System.Globalization;
using System.Text;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.PlanSets;
using FloorplanFit.Infrastructure.Dxf;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;

namespace FloorplanFit.Infrastructure.Tests.Dxf;

public sealed class ProjectedPlanSheetDxfExporterTests
{
    [Fact]
    public async Task ExportAsync_composes_adjusted_canonical_architecture_with_only_projected_electrical_overlay()
    {
        var canonicalPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-canonical.dxf");
        var electricalPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-electrical.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-composed.dxf");
        var canonicalDxf = string.Join(
            Environment.NewLine,
            [
                "0", "SECTION", "2", "TABLES",
                "0", "TABLE", "2", "LAYER", "70", "3",
                "0", "LAYER", "2", "0", "70", "0", "62", "7", "6", "CONTINUOUS",
                "0", "LAYER", "2", "ARCHITECTURAL", "70", "0", "62", "7", "6", "CONTINUOUS",
                "0", "LAYER", "2", "TITLE", "70", "0", "62", "7", "6", "CONTINUOUS",
                "0", "ENDTAB", "0", "ENDSEC",
                "0", "SECTION", "2", "BLOCKS", "0", "ENDSEC",
                "0", "SECTION", "2", "ENTITIES",
                "0", "LINE", "8", "ARCHITECTURAL", "10", "8", "20", "0", "11", "8", "21", "10",
                "0", "TEXT", "8", "TITLE", "10", "1", "20", "1", "40", "1", "1", "CANONICAL TITLE",
                "0", "ENDSEC", "0", "EOF"
            ]);
        var electricalDxf = string.Join(
            Environment.NewLine,
            [
                "0", "SECTION", "2", "TABLES",
                "0", "TABLE", "2", "LAYER", "70", "4",
                "0", "LAYER", "2", "0", "70", "0", "62", "7", "6", "CONTINUOUS",
                "0", "LAYER", "2", "ELECTRICAL", "70", "0", "62", "2", "6", "CONTINUOUS",
                "0", "LAYER", "2", "ELECTRICAL WALLS", "70", "0", "62", "8", "6", "CONTINUOUS",
                "0", "LAYER", "2", "TITLE", "70", "0", "62", "7", "6", "CONTINUOUS",
                "0", "ENDTAB", "0", "ENDSEC",
                "0", "SECTION", "2", "BLOCKS",
                "0", "BLOCK", "8", "0", "2", "SOCKET", "70", "0", "10", "0", "20", "0",
                "0", "CIRCLE", "8", "0", "10", "0", "20", "0", "40", "0.25",
                "0", "ENDBLK",
                "0", "ENDSEC",
                "0", "SECTION", "2", "ENTITIES",
                "0", "LINE", "8", "ELECTRICAL WALLS", "10", "10", "20", "0", "11", "10", "21", "10",
                "0", "INSERT", "8", "ELECTRICAL", "2", "SOCKET", "10", "10", "20", "5",
                "0", "TEXT", "8", "TITLE", "10", "2", "20", "2", "40", "1", "1", "DEPENDENT TITLE",
                "0", "ENDSEC", "0", "EOF"
            ]);
        await File.WriteAllTextAsync(canonicalPath, canonicalDxf, CancellationToken.None);
        await File.WriteAllTextAsync(electricalPath, electricalDxf, CancellationToken.None);

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();
            var recipe = new AdjustmentRecipeSummaryDto("v1", 1m, 0m, 0m, []);

            await exporter.ExportAsync(
                electricalPath,
                outputPath,
                new SheetAdjustmentProjectionTransform(1m, 0m, -2m, 0m),
                CreateProvenExportRecipe(
                    new SheetRegistrationTransform(1m, 0m, -2m, 0m),
                    recipe,
                    CanonicalFloorPlanExportPath: canonicalPath),
                CancellationToken.None);

            var output = await File.ReadAllLinesAsync(outputPath, CancellationToken.None);
            AssertEntityHasPair(output, "LINE", "ARCHITECTURAL", "10", "8");
            AssertEntityHasPair(output, "TEXT", "TITLE", "1", "CANONICAL TITLE");
            AssertEntityMissing(output, "LINE", "ELECTRICAL WALLS");
            AssertEntityMissing(output, "TEXT", "TITLE", "1", "DEPENDENT TITLE");
            AssertEntityHasPair(output, "INSERT", "ELECTRICAL", "10", "8");
            AssertEntityHasPair(output, "BLOCK", "0", "2", "SOCKET");
            AssertRecordHasPair(output, "LAYER", "2", "ELECTRICAL");
            Assert.Equal(canonicalDxf, await File.ReadAllTextAsync(canonicalPath, CancellationToken.None));
        }
        finally
        {
            File.Delete(canonicalPath);
            File.Delete(electricalPath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_remaps_imported_electrical_handles_and_owners_into_canonical_modelspace()
    {
        var canonicalPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-canonical-handles.dxf");
        var electricalPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-electrical-handles.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-composed-handles.dxf");
        await File.WriteAllTextAsync(
            canonicalPath,
            string.Join(
                Environment.NewLine,
                [
                    "0", "SECTION", "2", "HEADER", "9", "$HANDSEED", "5", "100", "0", "ENDSEC",
                    "0", "SECTION", "2", "TABLES",
                    "0", "TABLE", "5", "10", "2", "LAYER", "70", "2",
                    "0", "LAYER", "5", "11", "330", "10", "2", "0", "70", "0", "62", "7", "6", "CONTINUOUS",
                    "0", "LAYER", "5", "12", "330", "10", "2", "ARCHITECTURAL", "70", "0", "62", "7", "6", "CONTINUOUS",
                    "0", "ENDTAB",
                    "0", "TABLE", "5", "20", "2", "BLOCK_RECORD", "70", "1",
                    "0", "BLOCK_RECORD", "5", "21", "330", "20", "2", "*Model_Space", "70", "0",
                    "0", "ENDTAB", "0", "ENDSEC",
                    "0", "SECTION", "2", "BLOCKS",
                    "0", "BLOCK", "5", "22", "330", "21", "8", "0", "2", "*Model_Space", "70", "0", "10", "0", "20", "0", "3", "*Model_Space",
                    "0", "ENDBLK", "5", "23", "330", "21", "8", "0",
                    "0", "ENDSEC",
                    "0", "SECTION", "2", "ENTITIES",
                    "0", "LINE", "5", "24", "330", "21", "8", "ARCHITECTURAL", "10", "8", "20", "0", "11", "8", "21", "10",
                    "0", "ENDSEC", "0", "EOF"
                ]),
            CancellationToken.None);
        await File.WriteAllTextAsync(
            electricalPath,
            string.Join(
                Environment.NewLine,
                [
                    "0", "SECTION", "2", "HEADER", "9", "$HANDSEED", "5", "200", "0", "ENDSEC",
                    "0", "SECTION", "2", "TABLES",
                    "0", "TABLE", "5", "110", "2", "LAYER", "70", "2",
                    "0", "LAYER", "5", "111", "330", "110", "2", "0", "70", "0", "62", "7", "6", "CONTINUOUS",
                    "0", "LAYER", "5", "112", "330", "110", "2", "ELECTRICAL", "70", "0", "62", "2", "6", "CONTINUOUS",
                    "0", "ENDTAB",
                    "0", "TABLE", "5", "120", "2", "BLOCK_RECORD", "70", "2",
                    "0", "BLOCK_RECORD", "5", "121", "330", "120", "2", "*Model_Space", "70", "0",
                    "0", "BLOCK_RECORD", "5", "122", "330", "120", "2", "SOCKET", "70", "0",
                    "0", "ENDTAB", "0", "ENDSEC",
                    "0", "SECTION", "2", "BLOCKS",
                    "0", "BLOCK", "5", "123", "330", "121", "8", "0", "2", "*Model_Space", "70", "0", "10", "0", "20", "0", "3", "*Model_Space",
                    "0", "ENDBLK", "5", "124", "330", "121", "8", "0",
                    "0", "BLOCK", "5", "125", "330", "122", "8", "0", "2", "SOCKET", "70", "0", "10", "0", "20", "0", "3", "SOCKET",
                    "0", "CIRCLE", "5", "126", "330", "122", "8", "0", "10", "0", "20", "0", "40", "0.25",
                    "0", "ENDBLK", "5", "127", "330", "122", "8", "0",
                    "0", "ENDSEC",
                    "0", "SECTION", "2", "ENTITIES",
                    "0", "INSERT", "5", "130", "330", "121", "8", "ELECTRICAL", "2", "SOCKET", "10", "10", "20", "5",
                    "0", "ENDSEC", "0", "EOF"
                ]),
            CancellationToken.None);

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();
            await exporter.ExportAsync(
                electricalPath,
                outputPath,
                new SheetAdjustmentProjectionTransform(1m, 0m, -2m, 0m),
                CreateProvenExportRecipe(
                    new SheetRegistrationTransform(1m, 0m, -2m, 0m),
                    new AdjustmentRecipeSummaryDto("v1", 1m, 0m, 0m, []),
                    CanonicalFloorPlanExportPath: canonicalPath),
                CancellationToken.None);

            var output = await File.ReadAllLinesAsync(outputPath, CancellationToken.None);
            AssertEntityHasPair(output, "INSERT", "ELECTRICAL", "10", "8");
            AssertEntityHasPair(output, "INSERT", "ELECTRICAL", "330", "21");
            AssertRecordHasPair(output, "BLOCK_RECORD", "2", "SOCKET");

            var handles = Enumerable.Range(0, output.Length / 2)
                .Select(index => index * 2)
                .Where(index => output[index] == "5")
                .Select(index => output[index + 1])
                .ToArray();
            Assert.Equal(handles.Length, handles.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }
        finally
        {
            File.Delete(canonicalPath);
            File.Delete(electricalPath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_keeps_attrib_and_seqend_owned_by_the_remapped_insert()
    {
        var canonicalPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-canonical-sequence.dxf");
        var electricalPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-electrical-sequence.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-composed-sequence.dxf");
        await File.WriteAllTextAsync(canonicalPath, CreateModernCanonicalCompositionDxf(), CancellationToken.None);
        await File.WriteAllTextAsync(
            electricalPath,
            CreateModernElectricalCompositionDxf(
                [
                    "0", "INSERT", "5", "130", "330", "121", "8", "ELECTRICAL", "2", "DEVICE", "66", "1", "10", "10", "20", "5",
                    "0", "ATTRIB", "5", "131", "330", "130", "8", "ELECTRICAL", "10", "10", "20", "5", "40", "1", "1", "A", "2", "TAG",
                    "0", "SEQEND", "5", "132", "330", "130", "8", "ELECTRICAL"
                ],
                includeDeviceBlock: true),
            CancellationToken.None);

        try
        {
            await new ProjectedPlanSheetDxfExporter().ExportAsync(
                electricalPath,
                outputPath,
                new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
                CreateProvenExportRecipe(
                    new SheetRegistrationTransform(1m, 0m, 0m, 0m),
                    new AdjustmentRecipeSummaryDto("v1", 1m, 0m, 0m, []),
                    CanonicalFloorPlanExportPath: canonicalPath),
                CancellationToken.None);

            var output = await File.ReadAllLinesAsync(outputPath, CancellationToken.None);
            var insert = FindDxfRecord(output, "INSERT", ("8", "ELECTRICAL"));
            var insertHandle = ReadPairValue(insert, "5");
            Assert.False(string.IsNullOrWhiteSpace(insertHandle));
            Assert.Equal(insertHandle, ReadPairValue(FindDxfRecord(output, "ATTRIB", ("2", "TAG")), "330"));
            Assert.Equal(insertHandle, ReadPairValue(FindDxfRecord(output, "SEQEND", ("8", "ELECTRICAL")), "330"));
        }
        finally
        {
            File.Delete(canonicalPath);
            File.Delete(electricalPath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_deassociates_imported_hatch_without_dangling_boundary_counts()
    {
        var canonicalPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-canonical-hatch.dxf");
        var electricalPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-electrical-hatch.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-composed-hatch.dxf");
        await File.WriteAllTextAsync(canonicalPath, CreateModernCanonicalCompositionDxf(), CancellationToken.None);
        await File.WriteAllTextAsync(
            electricalPath,
            CreateModernElectricalCompositionDxf(
                [
                    "0", "HATCH", "5", "130", "330", "121", "8", "ELECTRICAL",
                    "10", "0", "20", "0", "30", "0", "2", "SOLID", "70", "1", "71", "1",
                    "91", "1", "92", "1", "93", "0", "97", "1", "330", "FEED", "75", "0", "76", "1", "98", "0"
                ]),
            CancellationToken.None);

        try
        {
            await new ProjectedPlanSheetDxfExporter().ExportAsync(
                electricalPath,
                outputPath,
                new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
                CreateProvenExportRecipe(
                    new SheetRegistrationTransform(1m, 0m, 0m, 0m),
                    new AdjustmentRecipeSummaryDto("v1", 1m, 0m, 0m, []),
                    CanonicalFloorPlanExportPath: canonicalPath),
                CancellationToken.None);

            var hatch = FindDxfRecord(
                await File.ReadAllLinesAsync(outputPath, CancellationToken.None),
                "HATCH",
                ("8", "ELECTRICAL"));
            Assert.Equal("0", ReadPairValue(hatch, "71"));
            Assert.Equal("0", ReadPairValue(hatch, "97"));
            Assert.DoesNotContain("FEED", hatch);
        }
        finally
        {
            File.Delete(canonicalPath);
            File.Delete(electricalPath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_detaches_source_database_reactors_from_imported_overlay()
    {
        var canonicalPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-canonical-reactor.dxf");
        var electricalPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-electrical-reactor.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-composed-reactor.dxf");
        await File.WriteAllTextAsync(canonicalPath, CreateModernCanonicalCompositionDxf(), CancellationToken.None);
        await File.WriteAllTextAsync(
            electricalPath,
            CreateModernElectricalCompositionDxf(
                [
                    "0", "CIRCLE", "5", "130",
                    "102", "{ACAD_REACTORS", "330", "DEAD", "102", "}",
                    "330", "121", "8", "ELECTRICAL", "10", "10", "20", "5", "40", "0.25"
                ]),
            CancellationToken.None);

        try
        {
            await new ProjectedPlanSheetDxfExporter().ExportAsync(
                electricalPath,
                outputPath,
                new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
                CreateProvenExportRecipe(
                    new SheetRegistrationTransform(1m, 0m, 0m, 0m),
                    new AdjustmentRecipeSummaryDto("v1", 1m, 0m, 0m, []),
                    CanonicalFloorPlanExportPath: canonicalPath),
                CancellationToken.None);

            var circle = FindDxfRecord(
                await File.ReadAllLinesAsync(outputPath, CancellationToken.None),
                "CIRCLE",
                ("8", "ELECTRICAL"));
            Assert.DoesNotContain("{ACAD_REACTORS", circle);
            Assert.DoesNotContain("DEAD", circle);
        }
        finally
        {
            File.Delete(canonicalPath);
            File.Delete(electricalPath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_detaches_standard_dimension_association_dictionary_from_imported_overlay()
    {
        var canonicalPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-canonical-dimassoc.dxf");
        var electricalPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-electrical-dimassoc.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-composed-dimassoc.dxf");
        await File.WriteAllTextAsync(canonicalPath, CreateModernCanonicalCompositionDxf(), CancellationToken.None);
        await File.WriteAllTextAsync(
            electricalPath,
            CreateModernElectricalCompositionDxf(
                [
                    "0", "DIMENSION", "5", "130",
                    "102", "{ACAD_XDICTIONARY", "360", "140", "102", "}",
                    "102", "{ACAD_REACTORS", "330", "141", "102", "}",
                    "330", "121", "8", "ELECTRICAL WIRING", "2", "DEVICE",
                    "10", "10", "20", "5", "11", "12", "21", "5", "70", "0"
                ],
                includeDeviceBlock: true,
                objectPairs:
                [
                    "0", "DICTIONARY", "5", "140", "330", "130", "100", "AcDbDictionary", "280", "1", "281", "1", "3", "ACAD_DIMASSOC", "360", "141",
                    "0", "DIMASSOC", "5", "141", "330", "140", "100", "AcDbDimAssoc", "330", "130", "90", "0"
                ]),
            CancellationToken.None);

        try
        {
            await new ProjectedPlanSheetDxfExporter().ExportAsync(
                electricalPath,
                outputPath,
                new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
                CreateProvenExportRecipe(
                    new SheetRegistrationTransform(1m, 0m, 0m, 0m),
                    new AdjustmentRecipeSummaryDto("v1", 1m, 0m, 0m, []),
                    CanonicalFloorPlanExportPath: canonicalPath),
                CancellationToken.None);

            var dimension = FindDxfRecord(
                await File.ReadAllLinesAsync(outputPath, CancellationToken.None),
                "DIMENSION",
                ("8", "ELECTRICAL WIRING"));
            Assert.DoesNotContain("{ACAD_XDICTIONARY", dimension);
            Assert.DoesNotContain("{ACAD_REACTORS", dimension);
            Assert.DoesNotContain("140", dimension);
            Assert.DoesNotContain("141", dimension);
        }
        finally
        {
            File.Delete(canonicalPath);
            File.Delete(electricalPath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_normalizes_imported_layer_object_references_to_canonical_layer_zero()
    {
        var canonicalPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-canonical-layer.dxf");
        var electricalPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-electrical-layer.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-composed-layer.dxf");
        await File.WriteAllTextAsync(
            canonicalPath,
            CreateModernCanonicalCompositionDxf(["347", "90", "390", "91"]),
            CancellationToken.None);
        await File.WriteAllTextAsync(
            electricalPath,
            CreateModernElectricalCompositionDxf(
                ["0", "CIRCLE", "5", "130", "330", "121", "8", "ELECTRICAL", "10", "10", "20", "5", "40", "0.25"],
                electricalLayerExtraPairs: ["347", "B0", "390", "B1"]),
            CancellationToken.None);

        try
        {
            await new ProjectedPlanSheetDxfExporter().ExportAsync(
                electricalPath,
                outputPath,
                new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
                CreateProvenExportRecipe(
                    new SheetRegistrationTransform(1m, 0m, 0m, 0m),
                    new AdjustmentRecipeSummaryDto("v1", 1m, 0m, 0m, []),
                    CanonicalFloorPlanExportPath: canonicalPath),
                CancellationToken.None);

            var electricalLayer = FindDxfRecord(
                await File.ReadAllLinesAsync(outputPath, CancellationToken.None),
                "LAYER",
                ("2", "ELECTRICAL"));
            Assert.Equal("90", ReadPairValue(electricalLayer, "347"));
            Assert.Equal("91", ReadPairValue(electricalLayer, "390"));
        }
        finally
        {
            File.Delete(canonicalPath);
            File.Delete(electricalPath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_imports_a_text_style_used_only_by_the_electrical_overlay()
    {
        var canonicalPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-canonical-style.dxf");
        var electricalPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-electrical-style.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-composed-style.dxf");
        await File.WriteAllTextAsync(
            canonicalPath,
            CreateModernCanonicalCompositionDxf(includeStandardStyleTable: true),
            CancellationToken.None);
        await File.WriteAllTextAsync(
            electricalPath,
            CreateModernElectricalCompositionDxf(
                [
                    "0", "TEXT", "5", "130", "330", "121", "8", "ELECTRICAL",
                    "7", "ROMANS", "10", "10", "20", "5", "40", "1", "1", "GFI"
                ],
                includeRomansStyle: true),
            CancellationToken.None);

        try
        {
            await new ProjectedPlanSheetDxfExporter().ExportAsync(
                electricalPath,
                outputPath,
                new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
                CreateProvenExportRecipe(
                    new SheetRegistrationTransform(1m, 0m, 0m, 0m),
                    new AdjustmentRecipeSummaryDto("v1", 1m, 0m, 0m, []),
                    CanonicalFloorPlanExportPath: canonicalPath),
                CancellationToken.None);

            var output = await File.ReadAllLinesAsync(outputPath, CancellationToken.None);
            AssertRecordHasPair(output, "STYLE", "2", "ROMANS");
            AssertEntityHasPair(output, "TEXT", "ELECTRICAL", "7", "ROMANS");
        }
        finally
        {
            File.Delete(canonicalPath);
            File.Delete(electricalPath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_fails_closed_when_overlay_metadata_references_an_unresolved_object()
    {
        var canonicalPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-canonical-metadata.dxf");
        var electricalPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-electrical-metadata.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-composed-metadata.dxf");
        await File.WriteAllTextAsync(canonicalPath, CreateModernCanonicalCompositionDxf(), CancellationToken.None);
        await File.WriteAllTextAsync(
            electricalPath,
            CreateModernElectricalCompositionDxf(
                [
                    "0", "CIRCLE", "5", "130", "330", "121", "8", "ELECTRICAL", "10", "10", "20", "5", "40", "0.25",
                    "102", "{CUSTOM_METADATA", "330", "DEAD", "102", "}"
                ]),
            CancellationToken.None);

        try
        {
            var error = await Assert.ThrowsAsync<ProjectedPlanSheetManualReviewRequiredException>(() =>
                new ProjectedPlanSheetDxfExporter().ExportAsync(
                    electricalPath,
                    outputPath,
                    new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
                    CreateProvenExportRecipe(
                        new SheetRegistrationTransform(1m, 0m, 0m, 0m),
                        new AdjustmentRecipeSummaryDto("v1", 1m, 0m, 0m, []),
                        CanonicalFloorPlanExportPath: canonicalPath),
                    CancellationToken.None));

            Assert.Contains("unresolved DXF handle", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            File.Delete(canonicalPath);
            File.Delete(electricalPath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_applies_electrical_recipe_projection_to_entity_points()
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
                    "8", "ELECTRICAL WALLS",
                    "10", "30",
                    "20", "40",
                    "11", "10",
                    "21", "10",
                    "0", "ENDSEC",
                    "0", "EOF"
                ]),
            CancellationToken.None);

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();
            var registration = new SheetRegistrationTransform(2m, 0m, 10m, 20m);
            var recipe = new AdjustmentRecipeSummaryDto(
                "v1",
                FloorToSiteScale: 3m,
                SiteOffsetX: 100m,
                SiteOffsetY: 200m,
                Operations:
                [
                    new AdjustmentRecipeOperationDto("HorizontalCompression", "Width", "Right", 50m, 2m),
                    new AdjustmentRecipeOperationDto("VerticalCompression", "Height", "Top", 80m, 5m)
                ]);

            await exporter.ExportAsync(
                sourcePath,
                outputPath,
                new SheetAdjustmentProjectionTransform(6m, 0m, 130m, 260m),
                CreateProvenExportRecipe(registration, recipe),
                CancellationToken.None);

            var output = await File.ReadAllLinesAsync(outputPath, CancellationToken.None);
            AssertHasPair(output, "10", "304");
            AssertHasPair(output, "20", "485");
            AssertHasPair(output, "11", "190");
            AssertHasPair(output, "21", "320");
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_anchors_out_of_bounds_top_recipe_to_electrical_wall_edge()
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
                    "8", "ELECTRICAL WALLS",
                    "10", "10",
                    "20", "90",
                    "11", "20",
                    "21", "70",
                    "0", "LINE",
                    "8", "ELECTRICAL WALLS",
                    "10", "30",
                    "20", "89.995",
                    "11", "40",
                    "21", "70",
                    "0", "ENDSEC",
                    "0", "EOF"
                ]),
            CancellationToken.None);

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();
            var recipe = new AdjustmentRecipeSummaryDto(
                "v1",
                FloorToSiteScale: 1m,
                SiteOffsetX: 0m,
                SiteOffsetY: 0m,
                Operations:
                [
                    new AdjustmentRecipeOperationDto("VerticalCompression", "Height", "Top", 100m, 5m),
                    new AdjustmentRecipeOperationDto("VerticalCompression", "Height", "Top", 101m, 5m)
                ]);

            var audit = await exporter.ExportWithAuditAsync(
                sourcePath,
                outputPath,
                new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
                CreateProvenExportRecipe(new SheetRegistrationTransform(1m, 0m, 0m, 0m), recipe),
                CancellationToken.None);

            var output = await File.ReadAllLinesAsync(outputPath, CancellationToken.None);
            AssertHasPair(output, "20", "80");
            AssertHasPair(output, "20", "79.995");
            AssertHasPair(output, "21", "70");
            Assert.All(audit!.Operations, operation =>
            {
                Assert.Equal("Applied", operation.Status);
                Assert.Contains("dependent-sheet edge anchor", operation.Reason, StringComparison.OrdinalIgnoreCase);
            });
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_requires_manual_review_when_dominant_outline_differs_from_canonical_without_evidenced_scale()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-source.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-projected.dxf");
        await WriteStructuralLinesDxfAsync(
            sourcePath,
            (0m, 0m, 470m, 0m),
            (470m, 0m, 470m, 930m),
            (470m, 930m, 0m, 930m),
            (0m, 930m, 0m, 0m));

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();
            var recipe = new AdjustmentRecipeSummaryDto(
                "v1",
                FloorToSiteScale: 1m,
                SiteOffsetX: 0m,
                SiteOffsetY: 0m,
                Operations:
                [
                    new AdjustmentRecipeOperationDto("HorizontalCompression", "Width", "Right", 469m, 3.6m)
                ]);

            var error = await Assert.ThrowsAsync<ProjectedPlanSheetManualReviewRequiredException>(() =>
                exporter.ExportAsync(
                    sourcePath,
                    outputPath,
                    new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
                    new ProjectedPlanSheetExportRecipe(
                        new SheetRegistrationTransform(1m, 0m, 0m, 0m),
                        recipe,
                        CanonicalSourceWidthInches: 468m,
                        CanonicalSourceHeightInches: 930m),
                    CancellationToken.None));

            Assert.Contains("mismatch", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("registration", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("manual review", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_uses_confirmed_whole_plan_proof_instead_of_locally_stronger_wrong_frame()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-source.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-projected.dxf");
        await WriteStructuralLinesDxfAsync(
            sourcePath,
            (0m, 0m, 100m, 0m),
            (368m, 0m, 468m, 0m),
            (468m, 0m, 468m, 100m),
            (468m, 830m, 468m, 930m),
            (368m, 930m, 468m, 930m),
            (0m, 930m, 100m, 930m),
            (0m, 830m, 0m, 930m),
            (0m, 0m, 0m, 100m),
            (5m, 200m, 461m, 200m),
            (461m, 200m, 461m, 676m),
            (461m, 676m, 5m, 676m),
            (5m, 676m, 5m, 200m));

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();
            var recipe = new AdjustmentRecipeSummaryDto(
                "v1",
                FloorToSiteScale: 1m,
                SiteOffsetX: 0m,
                SiteOffsetY: 0m,
                Operations:
                [
                    new AdjustmentRecipeOperationDto("HorizontalCompression", "Width", "Right", 464m, 4m)
                ]);

            var audit = await exporter.ExportWithAuditAsync(
                sourcePath,
                outputPath,
                new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
                CreateProvenExportRecipe(
                    new SheetRegistrationTransform(1m, 0m, 0m, 0m),
                    recipe,
                    CanonicalSourceWidthInches: 468m,
                    CanonicalSourceHeightInches: 930m),
                CancellationToken.None);

            Assert.True(File.Exists(outputPath));
            var operation = Assert.Single(audit!.Operations);
            Assert.Equal(464m, operation.Coordinate);
            Assert.Null(operation.Reason);
            Assert.Equal("RegistrationProofAuthorized", audit.OutlineCongruence!.Status);
            Assert.False(audit.OutlineCongruence.NormalizationApplied);
            Assert.Null(audit.OutlineCongruence.ElectricalSourceOutline);
            Assert.Null(audit.OutlineCongruence.ElectricalExportOutline);
            Assert.Null(audit.OutlineCongruence.SourceWidthMismatchInches);
            Assert.Null(audit.OutlineCongruence.ExportWidthMismatchInches);

            var lines = ReadLineSegments(await File.ReadAllLinesAsync(outputPath));
            Assert.Contains(lines, line => line == (368m, 0m, 464m, 0m));
            Assert.Contains(lines, line => line == (5m, 200m, 461m, 200m));
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExportAsync_fails_closed_without_valid_passed_whole_plan_proof(bool invalidProof)
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-source.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-projected.dxf");
        await WriteStructuralLinesDxfAsync(
            sourcePath,
            (0m, 0m, 100m, 0m),
            (368m, 0m, 468m, 0m),
            (468m, 0m, 468m, 100m),
            (468m, 830m, 468m, 930m),
            (368m, 930m, 468m, 930m),
            (0m, 930m, 100m, 930m),
            (0m, 830m, 0m, 930m),
            (0m, 0m, 0m, 100m),
            (5m, 200m, 461m, 200m),
            (461m, 200m, 461m, 676m),
            (461m, 676m, 5m, 676m),
            (5m, 676m, 5m, 200m));

        try
        {
            var proof = invalidProof
                ? CreatePassedWholePlanProof() with
                {
                    HorizontalCoverage = 0m,
                    VerticalCoverage = 0m,
                    RootMeanSquareResidual = 100m,
                    MaximumResidual = 100m
                }
                : null;
            var recipe = new AdjustmentRecipeSummaryDto("v1", 1m, 0m, 0m, []);

            await Assert.ThrowsAsync<ProjectedPlanSheetManualReviewRequiredException>(() =>
                new ProjectedPlanSheetDxfExporter().ExportAsync(
                    sourcePath,
                    outputPath,
                    new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
                    new ProjectedPlanSheetExportRecipe(
                        new SheetRegistrationTransform(1m, 0m, 0m, 0m),
                        recipe,
                        CanonicalSourceWidthInches: 468m,
                        CanonicalSourceHeightInches: 930m,
                        RegistrationStatus: SheetRegistrationStatus.Confirmed,
                        WholePlanRegistrationProof: proof),
                    CancellationToken.None));

            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_validates_dominant_outline_for_affine_only_recipe()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-source.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-projected.dxf");
        await WriteStructuralLinesDxfAsync(
            sourcePath,
            (0m, 0m, 101m, 0m),
            (101m, 0m, 101m, 200m),
            (101m, 200m, 0m, 200m),
            (0m, 200m, 0m, 0m));

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();
            var recipe = new AdjustmentRecipeSummaryDto(
                "v1",
                FloorToSiteScale: 1m,
                SiteOffsetX: 0m,
                SiteOffsetY: 0m,
                Operations: []);

            await Assert.ThrowsAsync<ProjectedPlanSheetManualReviewRequiredException>(() =>
                exporter.ExportAsync(
                    sourcePath,
                    outputPath,
                    new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
                    new ProjectedPlanSheetExportRecipe(
                        new SheetRegistrationTransform(1m, 0m, 0m, 0m),
                        recipe,
                        CanonicalSourceWidthInches: 100m,
                        CanonicalSourceHeightInches: 200m),
                    CancellationToken.None));

            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_requires_manual_review_when_dominant_axis_pairs_are_spatially_disconnected()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-source.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-projected.dxf");
        await WriteStructuralLinesDxfAsync(
            sourcePath,
            (0m, 0m, 0m, 200m),
            (100m, 0m, 100m, 200m),
            (0m, 300m, 100m, 300m),
            (0m, 500m, 100m, 500m));

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();
            var recipe = new AdjustmentRecipeSummaryDto(
                "v1",
                FloorToSiteScale: 1m,
                SiteOffsetX: 0m,
                SiteOffsetY: 0m,
                Operations: []);

            await Assert.ThrowsAsync<ProjectedPlanSheetManualReviewRequiredException>(() =>
                exporter.ExportAsync(
                    sourcePath,
                    outputPath,
                    new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
                    new ProjectedPlanSheetExportRecipe(
                        new SheetRegistrationTransform(1m, 0m, 0m, 0m),
                        recipe,
                        CanonicalSourceWidthInches: 100m,
                        CanonicalSourceHeightInches: 200m),
                    CancellationToken.None));

            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_requires_manual_review_when_non_touching_interior_stubs_cannot_form_an_outline()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-source.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-projected.dxf");
        await WriteStructuralLinesDxfAsync(
            sourcePath,
            (0m, 40m, 0m, 60m),
            (100m, 40m, 100m, 60m),
            (40m, 0m, 60m, 0m),
            (40m, 200m, 60m, 200m));

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();
            var recipe = new AdjustmentRecipeSummaryDto(
                "v1",
                FloorToSiteScale: 1m,
                SiteOffsetX: 0m,
                SiteOffsetY: 0m,
                Operations: []);

            await Assert.ThrowsAsync<ProjectedPlanSheetManualReviewRequiredException>(() =>
                exporter.ExportAsync(
                    sourcePath,
                    outputPath,
                    new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
                    new ProjectedPlanSheetExportRecipe(
                        new SheetRegistrationTransform(1m, 0m, 0m, 0m),
                        recipe,
                        CanonicalSourceWidthInches: 100m,
                        CanonicalSourceHeightInches: 200m),
                    CancellationToken.None));

            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_does_not_promote_weaker_expected_size_pair_through_tolerance_chain()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-source.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-projected.dxf");
        await WriteStructuralLinesDxfAsync(
            sourcePath,
            (0m, 0m, 0m, 100.04m),
            (10m, 0m, 10m, 100.04m),
            (30m, 0m, 30m, 100.08m),
            (40m, 0m, 40m, 100.08m),
            (60m, 0m, 60m, 100m),
            (75m, 0m, 75m, 100m),
            (0m, -10m, 75m, -10m),
            (0m, 110m, 75m, 110m));

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();
            var recipe = new AdjustmentRecipeSummaryDto(
                "v1",
                FloorToSiteScale: 1m,
                SiteOffsetX: 0m,
                SiteOffsetY: 0m,
                Operations: []);

            await Assert.ThrowsAsync<ProjectedPlanSheetManualReviewRequiredException>(() =>
                exporter.ExportAsync(
                    sourcePath,
                    outputPath,
                    new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
                    new ProjectedPlanSheetExportRecipe(
                        new SheetRegistrationTransform(1m, 0m, 0m, 0m),
                        recipe,
                        CanonicalSourceWidthInches: 15m,
                        CanonicalSourceHeightInches: 120m),
                    CancellationToken.None));

            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_uses_proof_authorized_canonical_coordinate_instead_of_short_outboard_fragments()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-source.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-projected.dxf");
        await WriteStructuralLinesDxfAsync(
            sourcePath,
            (0m, 0m, 100m, 0m),
            (100m, 0m, 100m, 200m),
            (100m, 200m, 0m, 200m),
            (0m, 200m, 0m, 0m),
            (-0.5m, 0m, -0.5m, 30m),
            (100.5m, 0m, 100.5m, 30m));

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();
            var recipe = new AdjustmentRecipeSummaryDto(
                "v1",
                FloorToSiteScale: 1m,
                SiteOffsetX: 0m,
                SiteOffsetY: 0m,
                Operations:
                [
                    new AdjustmentRecipeOperationDto("HorizontalCompression", "Width", "Right", 90m, 10m)
                ]);

            var audit = await exporter.ExportWithAuditAsync(
                sourcePath,
                outputPath,
                new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
                CreateProvenExportRecipe(
                    new SheetRegistrationTransform(1m, 0m, 0m, 0m),
                    recipe,
                    CanonicalSourceWidthInches: 100m,
                    CanonicalSourceHeightInches: 200m),
                CancellationToken.None);

            var outline = audit!.OutlineCongruence!;
            Assert.Equal("RegistrationProofAuthorized", outline.Status);
            Assert.Null(outline.ElectricalSourceOutline);
            Assert.False(outline.NormalizationApplied);
            Assert.Null(outline.ElectricalExportOutline);
            var lines = ReadLineSegments(await File.ReadAllLinesAsync(outputPath));
            Assert.Contains(lines, line => line == (0m, 0m, 90m, 0m));
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_requires_manual_review_for_ambiguous_dominant_wall_pairs()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-source.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-projected.dxf");
        await WriteStructuralLinesDxfAsync(
            sourcePath,
            (0m, 0m, 100m, 0m),
            (100m, 0m, 100m, 200m),
            (100m, 200m, 0m, 200m),
            (0m, 200m, 0m, 0m),
            (10m, 0m, 110m, 0m),
            (110m, 0m, 110m, 200m),
            (110m, 200m, 10m, 200m),
            (10m, 200m, 10m, 0m));

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();
            var recipe = new AdjustmentRecipeSummaryDto(
                "v1",
                FloorToSiteScale: 1m,
                SiteOffsetX: 0m,
                SiteOffsetY: 0m,
                Operations:
                [
                    new AdjustmentRecipeOperationDto("HorizontalCompression", "Width", "Right", 90m, 1m)
                ]);

            var error = await Assert.ThrowsAsync<ProjectedPlanSheetManualReviewRequiredException>(() =>
                exporter.ExportAsync(
                    sourcePath,
                    outputPath,
                    new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
                    new ProjectedPlanSheetExportRecipe(
                        new SheetRegistrationTransform(1m, 0m, 0m, 0m),
                        recipe,
                        CanonicalSourceWidthInches: 100m,
                        CanonicalSourceHeightInches: 200m),
                    CancellationToken.None));

            Assert.Contains("ambiguous", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_applies_electrical_recipe_projection_to_point_entities()
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
                    "0", "POINT",
                    "8", "ELECTRICAL",
                    "10", "60",
                    "20", "10",
                    "0", "ENDSEC",
                    "0", "EOF"
                ]),
            CancellationToken.None);

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();
            var recipe = new AdjustmentRecipeSummaryDto(
                "v1",
                FloorToSiteScale: 2m,
                SiteOffsetX: 100m,
                SiteOffsetY: 200m,
                Operations:
                [
                    new AdjustmentRecipeOperationDto("HorizontalCompression", "Width", "Right", 50m, 2m)
                ]);

            await exporter.ExportAsync(
                sourcePath,
                outputPath,
                new SheetAdjustmentProjectionTransform(2m, 0m, 100m, 200m),
                CreateProvenExportRecipe(new SheetRegistrationTransform(1m, 0m, 0m, 0m), recipe),
                CancellationToken.None);

            var lines = await File.ReadAllLinesAsync(outputPath, CancellationToken.None);

            AssertEntityHasPair(lines, "POINT", "ELECTRICAL", "10", "216");
            AssertEntityHasPair(lines, "POINT", "ELECTRICAL", "20", "220");
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_applies_electrical_recipe_projection_to_hatch_boundary_points()
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
                    "0", "HATCH",
                    "8", "ELECTRICAL HATCH",
                    "10", "60",
                    "20", "10",
                    "11", "20",
                    "21", "10",
                    "0", "ENDSEC",
                    "0", "EOF"
                ]),
            CancellationToken.None);

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();
            var recipe = new AdjustmentRecipeSummaryDto(
                "v1",
                FloorToSiteScale: 2m,
                SiteOffsetX: 100m,
                SiteOffsetY: 200m,
                Operations:
                [
                    new AdjustmentRecipeOperationDto("HorizontalCompression", "Width", "Right", 50m, 2m)
                ]);

            await exporter.ExportAsync(
                sourcePath,
                outputPath,
                new SheetAdjustmentProjectionTransform(2m, 0m, 100m, 200m),
                CreateProvenExportRecipe(new SheetRegistrationTransform(1m, 0m, 0m, 0m), recipe),
                CancellationToken.None);

            var lines = await File.ReadAllLinesAsync(outputPath, CancellationToken.None);

            AssertEntityHasPair(lines, "HATCH", "ELECTRICAL HATCH", "10", "216");
            AssertEntityHasPair(lines, "HATCH", "ELECTRICAL HATCH", "20", "220");
            AssertEntityHasPair(lines, "HATCH", "ELECTRICAL HATCH", "11", "140");
            AssertEntityHasPair(lines, "HATCH", "ELECTRICAL HATCH", "21", "220");
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_applies_electrical_recipe_projection_to_3dface_vertices()
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
                    "0", "3DFACE",
                    "8", "ELECTRICAL FACE",
                    "10", "60",
                    "20", "10",
                    "30", "18.75",
                    "11", "20",
                    "21", "10",
                    "31", "18.75",
                    "0", "ENDSEC",
                    "0", "EOF"
                ]),
            CancellationToken.None);

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();
            var recipe = new AdjustmentRecipeSummaryDto(
                "v1",
                FloorToSiteScale: 2m,
                SiteOffsetX: 100m,
                SiteOffsetY: 200m,
                Operations:
                [
                    new AdjustmentRecipeOperationDto("HorizontalCompression", "Width", "Right", 50m, 2m)
                ]);

            await exporter.ExportAsync(
                sourcePath,
                outputPath,
                new SheetAdjustmentProjectionTransform(2m, 0m, 100m, 200m),
                CreateProvenExportRecipe(new SheetRegistrationTransform(1m, 0m, 0m, 0m), recipe),
                CancellationToken.None);

            var lines = await File.ReadAllLinesAsync(outputPath, CancellationToken.None);

            AssertEntityHasPair(lines, "3DFACE", "ELECTRICAL FACE", "10", "216");
            AssertEntityHasPair(lines, "3DFACE", "ELECTRICAL FACE", "20", "220");
            AssertEntityHasPair(lines, "3DFACE", "ELECTRICAL FACE", "30", "18.75");
            AssertEntityHasPair(lines, "3DFACE", "ELECTRICAL FACE", "11", "140");
            AssertEntityHasPair(lines, "3DFACE", "ELECTRICAL FACE", "21", "220");
            AssertEntityHasPair(lines, "3DFACE", "ELECTRICAL FACE", "31", "18.75");
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_applies_electrical_recipe_projection_to_solid_vertices()
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
                    "0", "SOLID",
                    "8", "TEXT LBLS",
                    "100", "AcDbTrace",
                    "10", "60",
                    "20", "10",
                    "30", "18.75",
                    "11", "20",
                    "21", "10",
                    "31", "18.75",
                    "12", "60",
                    "22", "20",
                    "32", "18.75",
                    "13", "20",
                    "23", "20",
                    "33", "18.75",
                    "0", "ENDSEC",
                    "0", "EOF"
                ]),
            CancellationToken.None);

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();
            var recipe = new AdjustmentRecipeSummaryDto(
                "v1",
                FloorToSiteScale: 2m,
                SiteOffsetX: 100m,
                SiteOffsetY: 200m,
                Operations:
                [
                    new AdjustmentRecipeOperationDto("HorizontalCompression", "Width", "Right", 50m, 2m)
                ]);

            await exporter.ExportAsync(
                sourcePath,
                outputPath,
                new SheetAdjustmentProjectionTransform(2m, 0m, 100m, 200m),
                CreateProvenExportRecipe(new SheetRegistrationTransform(1m, 0m, 0m, 0m), recipe),
                CancellationToken.None);

            var lines = await File.ReadAllLinesAsync(outputPath, CancellationToken.None);

            AssertEntityHasPair(lines, "SOLID", "TEXT LBLS", "10", "216");
            AssertEntityHasPair(lines, "SOLID", "TEXT LBLS", "20", "220");
            AssertEntityHasPair(lines, "SOLID", "TEXT LBLS", "30", "18.75");
            AssertEntityHasPair(lines, "SOLID", "TEXT LBLS", "11", "140");
            AssertEntityHasPair(lines, "SOLID", "TEXT LBLS", "21", "220");
            AssertEntityHasPair(lines, "SOLID", "TEXT LBLS", "31", "18.75");
            AssertEntityHasPair(lines, "SOLID", "TEXT LBLS", "12", "216");
            AssertEntityHasPair(lines, "SOLID", "TEXT LBLS", "22", "240");
            AssertEntityHasPair(lines, "SOLID", "TEXT LBLS", "32", "18.75");
            AssertEntityHasPair(lines, "SOLID", "TEXT LBLS", "13", "140");
            AssertEntityHasPair(lines, "SOLID", "TEXT LBLS", "23", "240");
            AssertEntityHasPair(lines, "SOLID", "TEXT LBLS", "33", "18.75");
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_converts_recipe_crossing_circle_to_closed_polyline()
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
                    "5", "C100",
                    "330", "1F",
                    "8", "ELECTRICAL WALLS",
                    "62", "3",
                    "10", "49",
                    "20", "10",
                    "40", "5",
                    "0", "ENDSEC",
                    "0", "EOF"
                ]),
            CancellationToken.None);

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();
            var recipe = new AdjustmentRecipeSummaryDto(
                "v1",
                FloorToSiteScale: 1m,
                SiteOffsetX: 0m,
                SiteOffsetY: 0m,
                Operations:
                [
                    new AdjustmentRecipeOperationDto("HorizontalCompression", "Width", "Right", 50m, 2m)
                ]);

            await exporter.ExportAsync(
                sourcePath,
                outputPath,
                new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
                CreateProvenExportRecipe(
                    new SheetRegistrationTransform(1m, 0m, 0m, 0m),
                    recipe),
                CancellationToken.None);

            var lines = await File.ReadAllLinesAsync(outputPath, CancellationToken.None);

            AssertEntityMissing(lines, "CIRCLE", "ELECTRICAL WALLS");
            AssertEntityHasPair(lines, "LWPOLYLINE", "ELECTRICAL WALLS", "5", "C100");
            AssertEntityHasPair(lines, "LWPOLYLINE", "ELECTRICAL WALLS", "330", "1F");
            AssertEntityHasPair(lines, "LWPOLYLINE", "ELECTRICAL WALLS", "62", "3");
            AssertEntityHasPair(lines, "LWPOLYLINE", "ELECTRICAL WALLS", "90", "36");
            AssertEntityHasPair(lines, "LWPOLYLINE", "ELECTRICAL WALLS", "70", "1");
            AssertEntityHasPair(lines, "LWPOLYLINE", "ELECTRICAL WALLS", "10", "52");
            AssertEntityHasPair(lines, "LWPOLYLINE", "ELECTRICAL WALLS", "10", "44");
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_converts_recipe_crossing_arc_to_polyline()
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
                    "5", "A100",
                    "330", "1F",
                    "8", "ELECTRICAL WIRING",
                    "62", "7",
                    "10", "50",
                    "20", "10",
                    "40", "5",
                    "50", "0",
                    "51", "180",
                    "0", "ENDSEC",
                    "0", "EOF"
                ]),
            CancellationToken.None);

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();
            var recipe = new AdjustmentRecipeSummaryDto(
                "v1",
                FloorToSiteScale: 1m,
                SiteOffsetX: 0m,
                SiteOffsetY: 0m,
                Operations:
                [
                    new AdjustmentRecipeOperationDto("HorizontalCompression", "Width", "Right", 50m, 2m)
                ]);

            await exporter.ExportAsync(
                sourcePath,
                outputPath,
                new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
                CreateProvenExportRecipe(
                    new SheetRegistrationTransform(1m, 0m, 0m, 0m),
                    recipe),
                CancellationToken.None);

            var lines = await File.ReadAllLinesAsync(outputPath, CancellationToken.None);

            AssertEntityMissing(lines, "ARC", "ELECTRICAL WIRING");
            AssertEntityHasPair(lines, "LWPOLYLINE", "ELECTRICAL WIRING", "5", "A100");
            AssertEntityHasPair(lines, "LWPOLYLINE", "ELECTRICAL WIRING", "330", "1F");
            AssertEntityHasPair(lines, "LWPOLYLINE", "ELECTRICAL WIRING", "62", "7");
            AssertEntityHasPair(lines, "LWPOLYLINE", "ELECTRICAL WIRING", "90", "19");
            AssertEntityHasPair(lines, "LWPOLYLINE", "ELECTRICAL WIRING", "10", "53");
            AssertEntityHasPair(lines, "LWPOLYLINE", "ELECTRICAL WIRING", "10", "45");
            AssertHasPair(lines, "0", "EOF");
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_rejects_recipe_projection_when_ellipse_crosses_pinch_line()
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
                    "0", "ELLIPSE",
                    "8", "ELECTRICAL WALLS",
                    "10", "49",
                    "20", "10",
                    "11", "5",
                    "21", "0",
                    "40", "0.5",
                    "0", "ENDSEC",
                    "0", "EOF"
                ]),
            CancellationToken.None);

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();
            var recipe = new AdjustmentRecipeSummaryDto(
                "v1",
                FloorToSiteScale: 1m,
                SiteOffsetX: 0m,
                SiteOffsetY: 0m,
                Operations:
                [
                    new AdjustmentRecipeOperationDto("HorizontalCompression", "Width", "Right", 50m, 2m)
                ]);

            var error = await Assert.ThrowsAsync<ProjectedPlanSheetManualReviewRequiredException>(() =>
                exporter.ExportAsync(
                    sourcePath,
                    outputPath,
                    new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
                    CreateProvenExportRecipe(
                        new SheetRegistrationTransform(1m, 0m, 0m, 0m),
                        recipe),
                    CancellationToken.None));

            Assert.Contains("ELLIPSE crosses a canonical recipe pinch line", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_rejects_recipe_projection_for_unknown_coordinate_entities()
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
                    "0", "LEADER",
                    "8", "ELECTRICAL WALLS",
                    "10", "49",
                    "20", "10",
                    "0", "ENDSEC",
                    "0", "EOF"
                ]),
            CancellationToken.None);

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();
            var recipe = new AdjustmentRecipeSummaryDto(
                "v1",
                FloorToSiteScale: 1m,
                SiteOffsetX: 0m,
                SiteOffsetY: 0m,
                Operations:
                [
                    new AdjustmentRecipeOperationDto("HorizontalCompression", "Width", "Right", 50m, 2m)
                ]);

            var error = await Assert.ThrowsAsync<ProjectedPlanSheetManualReviewRequiredException>(() =>
                exporter.ExportAsync(
                    sourcePath,
                    outputPath,
                    new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
                    CreateProvenExportRecipe(
                        new SheetRegistrationTransform(1m, 0m, 0m, 0m),
                        recipe),
                    CancellationToken.None));

            Assert.Contains("LEADER is not supported for recipe-aware electrical export", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_rejects_recipe_projection_when_dimension_block_curve_crosses_pinch_line()
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
                    "0", "CIRCLE",
                    "8", "DIMS",
                    "10", "49",
                    "20", "10",
                    "40", "5",
                    "0", "ENDBLK",
                    "0", "ENDSEC",
                    "0", "SECTION",
                    "2", "ENTITIES",
                    "0", "DIMENSION",
                    "8", "DIMS",
                    "2", "*D1",
                    "10", "1",
                    "20", "2",
                    "0", "ENDSEC",
                    "0", "EOF"
                ]),
            CancellationToken.None);

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();
            var recipe = new AdjustmentRecipeSummaryDto(
                "v1",
                FloorToSiteScale: 1m,
                SiteOffsetX: 0m,
                SiteOffsetY: 0m,
                Operations:
                [
                    new AdjustmentRecipeOperationDto("HorizontalCompression", "Width", "Right", 50m, 2m)
                ]);

            var error = await Assert.ThrowsAsync<ProjectedPlanSheetManualReviewRequiredException>(() =>
                exporter.ExportAsync(
                    sourcePath,
                    outputPath,
                    new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
                    CreateProvenExportRecipe(
                        new SheetRegistrationTransform(1m, 0m, 0m, 0m),
                        recipe),
                    CancellationToken.None));

            Assert.Contains("CIRCLE crosses a canonical recipe pinch line", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

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
    public async Task ExportAsync_recipe_projection_moves_text_without_deforming_alignment_or_mtext_direction()
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
                    "10", "60",
                    "20", "10",
                    "11", "40",
                    "21", "10",
                    "40", "1",
                    "1", "PANEL A",
                    "0", "MTEXT",
                    "8", "NOTES",
                    "10", "60",
                    "20", "20",
                    "11", "1",
                    "21", "0",
                    "40", "1",
                    "1", "ROOM NOTE",
                    "0", "ENDSEC",
                    "0", "EOF"
                ]),
            CancellationToken.None);

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();
            var recipe = new AdjustmentRecipeSummaryDto(
                "v1",
                FloorToSiteScale: 2m,
                SiteOffsetX: 100m,
                SiteOffsetY: 200m,
                Operations:
                [
                    new AdjustmentRecipeOperationDto("HorizontalCompression", "Width", "Right", 50m, 2m)
                ]);

            await exporter.ExportAsync(
                sourcePath,
                outputPath,
                new SheetAdjustmentProjectionTransform(2m, 0m, 100m, 200m),
                CreateProvenExportRecipe(new SheetRegistrationTransform(1m, 0m, 0m, 0m), recipe),
                CancellationToken.None);

            var lines = await File.ReadAllLinesAsync(outputPath, CancellationToken.None);

            AssertEntityHasPair(lines, "TEXT", "NOTES", "10", "216");
            AssertEntityHasPair(lines, "TEXT", "NOTES", "11", "176");
            AssertEntityHasPair(lines, "MTEXT", "NOTES", "10", "216");
            AssertEntityHasPair(lines, "MTEXT", "NOTES", "11", "2");
            AssertEntityHasPair(lines, "MTEXT", "NOTES", "21", "0");
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
    public async Task ExportAsync_rejects_truncated_binary_chunk()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-binary-source.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-binary-projected.dxf");
        using (var writer = new BinaryWriter(File.Create(sourcePath), Encoding.Latin1))
        {
            writer.Write(Encoding.ASCII.GetBytes("AutoCAD Binary DXF\r\n\u001A\0"));
            writer.Write((byte)0);
            writer.Write(Encoding.ASCII.GetBytes("SECTION"));
            writer.Write((byte)0);
            writer.Write((byte)2);
            writer.Write(Encoding.ASCII.GetBytes("ENTITIES"));
            writer.Write((byte)0);
            writer.Write(byte.MaxValue);
            writer.Write((short)310);
            writer.Write((byte)4);
            writer.Write(new byte[] { 0xAA, 0xBB });
        }

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();

            var error = await Assert.ThrowsAsync<InvalidDataException>(() =>
                exporter.ExportAsync(
                    sourcePath,
                    outputPath,
                    new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
                    CancellationToken.None));

            Assert.Contains("binary DXF chunk is truncated", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_rejects_binary_dxf_without_terminal_eof_pair()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-binary-source.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-binary-projected.dxf");
        using (var writer = new BinaryWriter(File.Create(sourcePath), Encoding.Latin1))
        {
            writer.Write(Encoding.ASCII.GetBytes("AutoCAD Binary DXF\r\n\u001A\0"));
            writer.Write((byte)0);
            writer.Write(Encoding.ASCII.GetBytes("SECTION"));
            writer.Write((byte)0);
            writer.Write((byte)2);
            writer.Write(Encoding.ASCII.GetBytes("ENTITIES"));
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write(Encoding.ASCII.GetBytes("ENDSEC"));
            writer.Write((byte)0);
        }

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();

            var error = await Assert.ThrowsAsync<InvalidDataException>(() =>
                exporter.ExportAsync(
                    sourcePath,
                    outputPath,
                    new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
                    CancellationToken.None));

            Assert.Contains("terminal 0/EOF pair", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(outputPath));
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

    [Fact]
    public async Task ExportWithAuditAsync_v2_resolves_electrical_targets_by_registered_geometry_and_uses_one_delta()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-source.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-projected.dxf");
        await WriteCadStretchElectricalFixtureAsync(sourcePath, includeCrossingArc: false);

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();
            var registration = new SheetRegistrationTransform(1m, 0m, 100m, 50m);
            var action = CreateElectricalCadStretchAction();
            var recipe = new AdjustmentRecipeSummaryDto(
                "v2",
                FloorToSiteScale: 1m,
                SiteOffsetX: 0m,
                SiteOffsetY: 0m,
                Operations: [])
            {
                StretchActions = [action]
            };

            var audit = await exporter.ExportWithAuditAsync(
                sourcePath,
                outputPath,
                new SheetAdjustmentProjectionTransform(1m, 0m, 100m, 50m),
                CreateProvenExportRecipe(registration, recipe),
                CancellationToken.None);

            var lines = await File.ReadAllLinesAsync(outputPath, CancellationToken.None);
            var segments = ReadLineSegments(lines);
            Assert.Contains((100m, 50m, 108m, 50m), segments);
            Assert.Contains((100m, 54m, 108m, 54m), segments);
            Assert.Contains((110m, 50m, 110m, 54m), segments);
            Assert.Contains((96m, 50m, 96m, 54m), segments);
            AssertEntityHasPair(lines, "INSERT", "ELECTRICAL", "10", "110");

            var actionAudit = Assert.Single(audit!.Operations);
            Assert.Equal(action.ActionId, actionAudit.OperationId);
            Assert.Equal("CadStretch", actionAudit.Kind);
            Assert.Equal(2m, actionAudit.ExpectedDeltaSourceUnits);
            Assert.Equal(2m, actionAudit.MeasuredMinDeltaSourceUnits);
            Assert.Equal(2m, actionAudit.MeasuredMaxDeltaSourceUnits);
            Assert.Equal("Applied", actionAudit.Status);
            Assert.Contains("registered geometry", actionAudit.Reason, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_v2_rejects_unselected_crossing_before_creating_output()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-source.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-projected.dxf");
        await WriteCadStretchElectricalFixtureAsync(sourcePath, includeCrossingArc: true);

        try
        {
            var exporter = new ProjectedPlanSheetDxfExporter();
            var registration = new SheetRegistrationTransform(1m, 0m, 100m, 50m);
            var recipe = new AdjustmentRecipeSummaryDto(
                "v2",
                FloorToSiteScale: 1m,
                SiteOffsetX: 0m,
                SiteOffsetY: 0m,
                Operations: [])
            {
                StretchActions = [CreateElectricalCadStretchAction()]
            };

            var error = await Assert.ThrowsAsync<ProjectedPlanSheetManualReviewRequiredException>(() =>
                exporter.ExportAsync(
                    sourcePath,
                    outputPath,
                    new SheetAdjustmentProjectionTransform(1m, 0m, 100m, 50m),
                    CreateProvenExportRecipe(registration, recipe),
                    CancellationToken.None));

            Assert.Contains("crosses", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ARC", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    private static Task WriteCadStretchElectricalFixtureAsync(string path, bool includeCrossingArc)
    {
        var pairs = new List<string>
        {
            "0", "SECTION",
            "2", "ENTITIES",
            "0", "LINE", "8", "ELECTRICAL WALLS", "10", "0", "20", "0", "11", "10", "21", "0",
            "0", "LINE", "8", "ELECTRICAL WALLS", "10", "0", "20", "4", "11", "10", "21", "4",
            "0", "LINE", "8", "ELECTRICAL WALLS", "10", "12", "20", "0", "11", "12", "21", "4",
            "0", "LINE", "8", "ELECTRICAL WALLS", "10", "-4", "20", "0", "11", "-4", "21", "4",
            "0", "INSERT", "8", "ELECTRICAL", "2", "SOCKET", "10", "12", "20", "2"
        };
        if (includeCrossingArc)
        {
            pairs.AddRange([
                "0", "ARC", "8", "ELECTRICAL WIRING",
                "10", "5", "20", "2", "40", "2", "50", "0", "51", "180"
            ]);
        }

        pairs.AddRange(["0", "ENDSEC", "0", "EOF"]);
        return File.WriteAllTextAsync(path, string.Join(Environment.NewLine, pairs), CancellationToken.None);
    }

    private static AdjustmentRecipeStretchActionDto CreateElectricalCadStretchAction()
        => new(
            ActionId: "paired-wall-width-right",
            AxisTag: "Width",
            Edge: "Right",
            CutCoordinate: 105m,
            DeltaSourceUnits: 2m,
            MaxDeltaSourceUnits: 4m,
            CoordinateTolerance: 0.05m,
            CanonicalSourceBounds: new AdjustmentRecipeBoundsDto(90m, 40m, 120m, 60m),
            TargetSpans:
            [
                new AdjustmentRecipeTargetSpanDto("FLOOR-LINE:900", Guid.NewGuid(), 0, 100m, 50m, 110m, 50m, 1),
                new AdjustmentRecipeTargetSpanDto("FLOOR-LINE:901", Guid.NewGuid(), 0, 100m, 54m, 110m, 54m, 1)
            ],
            CanonicalEntityRoles: []);

    private static Task WriteStructuralLinesDxfAsync(
        string path,
        params (decimal X1, decimal Y1, decimal X2, decimal Y2)[] lines)
    {
        var pairs = new List<string> { "0", "SECTION", "2", "ENTITIES" };
        foreach (var line in lines)
        {
            pairs.AddRange(
            [
                "0", "LINE",
                "8", "ELECTRICAL WALLS",
                "10", line.X1.ToString(CultureInfo.InvariantCulture),
                "20", line.Y1.ToString(CultureInfo.InvariantCulture),
                "11", line.X2.ToString(CultureInfo.InvariantCulture),
                "21", line.Y2.ToString(CultureInfo.InvariantCulture)
            ]);
        }

        pairs.AddRange(["0", "ENDSEC", "0", "EOF"]);
        return File.WriteAllTextAsync(path, string.Join(Environment.NewLine, pairs), CancellationToken.None);
    }

    [Fact]
    public async Task ExportAsync_moves_reconciled_wall_hosted_device_by_commissioned_host_delta()
    {
        var fixture = await WriteComposedOverlayFixtureAsync(
            [
                "0", "INSERT", "5", "130", "330", "121", "8", "ELECTRICAL", "2", "DEVICE", "10", "4", "20", "5"
            ]);
        try
        {
            await ExportComposedOverlayAsync(
                fixture,
                new ElectricalOverlayReconciliation(
                    [WallBinding("130", "HostRigidMove", deltaX: -1m)],
                    []));

            var output = await File.ReadAllLinesAsync(fixture.OutputPath, CancellationToken.None);
            var device = FindDxfRecord(output, "INSERT", ("8", "ELECTRICAL"));
            Assert.Equal("3", ReadPairValue(device, "10"));
            Assert.Equal("5", ReadPairValue(device, "20"));
            Assert.Equal("DEVICE", ReadPairValue(device, "2"));
            Assert.Equal(1, CountRecords(output, "INSERT"));
        }
        finally
        {
            DeleteComposedOverlayFixture(fixture);
        }
    }

    [Fact]
    public async Task ExportAsync_keeps_fixed_device_unmoved_when_recipe_cut_crosses_it()
    {
        var fixture = await WriteComposedOverlayFixtureAsync(
            [
                "0", "INSERT", "5", "130", "330", "121", "8", "ELECTRICAL", "2", "DEVICE", "10", "10", "20", "5"
            ]);
        try
        {
            await ExportComposedOverlayAsync(
                fixture,
                new ElectricalOverlayReconciliation(
                    [WallBinding("130", "Fixed")],
                    []));

            var output = await File.ReadAllLinesAsync(fixture.OutputPath, CancellationToken.None);
            var device = FindDxfRecord(output, "INSERT", ("8", "ELECTRICAL"));
            Assert.Equal("10", ReadPairValue(device, "10"));
            Assert.Equal("5", ReadPairValue(device, "20"));
        }
        finally
        {
            DeleteComposedOverlayFixture(fixture);
        }
    }

    [Fact]
    public async Task ExportAsync_fails_closed_when_deforming_composition_lacks_overlay_reconciliation()
    {
        var fixture = await WriteComposedOverlayFixtureAsync(
            [
                "0", "INSERT", "5", "130", "330", "121", "8", "ELECTRICAL", "2", "DEVICE", "10", "4", "20", "5"
            ]);
        try
        {
            var error = await Assert.ThrowsAsync<ProjectedPlanSheetManualReviewRequiredException>(() =>
                ExportComposedOverlayAsync(fixture, reconciliation: null));

            Assert.Contains("reconciliation", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(fixture.OutputPath));
        }
        finally
        {
            DeleteComposedOverlayFixture(fixture);
        }
    }

    [Fact]
    public async Task ExportAsync_rejects_stale_device_binding_block_before_output()
    {
        var fixture = await WriteComposedOverlayFixtureAsync(
            [
                "0", "INSERT", "5", "130", "330", "121", "8", "ELECTRICAL", "2", "DEVICE", "10", "4", "20", "5"
            ]);
        try
        {
            var error = await Assert.ThrowsAsync<ProjectedPlanSheetManualReviewRequiredException>(() =>
                ExportComposedOverlayAsync(
                    fixture,
                    new ElectricalOverlayReconciliation(
                        [WallBinding("130", "Fixed", blockName: "SOCKET")],
                        [])));

            Assert.Contains("130", error.Message, StringComparison.Ordinal);
            Assert.Contains("SOCKET", error.Message, StringComparison.Ordinal);
            Assert.False(File.Exists(fixture.OutputPath));
        }
        finally
        {
            DeleteComposedOverlayFixture(fixture);
        }
    }

    [Fact]
    public async Task ExportAsync_rejects_unbound_overlay_device_before_output()
    {
        var fixture = await WriteComposedOverlayFixtureAsync(
            [
                "0", "INSERT", "5", "130", "330", "121", "8", "ELECTRICAL", "2", "DEVICE", "10", "4", "20", "5",
                "0", "INSERT", "5", "140", "330", "121", "8", "ELECTRICAL", "2", "DEVICE", "10", "5", "20", "5"
            ]);
        try
        {
            var error = await Assert.ThrowsAsync<ProjectedPlanSheetManualReviewRequiredException>(() =>
                ExportComposedOverlayAsync(
                    fixture,
                    new ElectricalOverlayReconciliation(
                        [WallBinding("130", "Fixed")],
                        [])));

            Assert.Contains("140", error.Message, StringComparison.Ordinal);
            Assert.Contains("host", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(fixture.OutputPath));
        }
        finally
        {
            DeleteComposedOverlayFixture(fixture);
        }
    }

    [Fact]
    public async Task ExportAsync_rejects_duplicate_device_bindings_before_output()
    {
        var fixture = await WriteComposedOverlayFixtureAsync(
            [
                "0", "INSERT", "5", "130", "330", "121", "8", "ELECTRICAL", "2", "DEVICE", "10", "4", "20", "5"
            ]);
        try
        {
            var error = await Assert.ThrowsAsync<ProjectedPlanSheetManualReviewRequiredException>(() =>
                ExportComposedOverlayAsync(
                    fixture,
                    new ElectricalOverlayReconciliation(
                        [WallBinding("130", "Fixed"), WallBinding("130", "Fixed")],
                        [])));

            Assert.Contains("duplicate", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(fixture.OutputPath));
        }
        finally
        {
            DeleteComposedOverlayFixture(fixture);
        }
    }

    [Fact]
    public async Task ExportAsync_regenerates_wire_routes_from_final_device_endpoints()
    {
        var fixture = await WriteComposedOverlayFixtureAsync(
            [
                "0", "INSERT", "5", "130", "330", "121", "8", "ELECTRICAL", "2", "DEVICE", "10", "2", "20", "2",
                "0", "INSERT", "5", "140", "330", "121", "8", "ELECTRICAL", "2", "DEVICE", "10", "10", "20", "2",
                "0", "INSERT", "5", "170", "330", "121", "8", "ELECTRICAL", "2", "DEVICE", "10", "2", "20", "3",
                "0", "INSERT", "5", "180", "330", "121", "8", "ELECTRICAL", "2", "DEVICE", "10", "10", "20", "3",
                "0", "LINE", "5", "150", "330", "121", "8", "ELECTRICAL", "10", "2", "20", "2", "11", "10", "21", "2",
                "0", "LINE", "5", "160", "330", "121", "8", "ELECTRICAL", "10", "2", "20", "3", "11", "10", "21", "3"
            ]);
        try
        {
            await ExportComposedOverlayAsync(
                fixture,
                new ElectricalOverlayReconciliation(
                    [
                        WallBinding("130", "Fixed"),
                        WallBinding("140", "HostRigidMove", deltaX: -1m),
                        WallBinding("170", "Fixed"),
                        WallBinding("180", "Fixed")
                    ],
                    [
                        new ElectricalWireRouteBinding("150", "130", "140"),
                        new ElectricalWireRouteBinding("160", "170", "180")
                    ]));

            var output = await File.ReadAllLinesAsync(fixture.OutputPath, CancellationToken.None);
            var movedRoute = FindDxfRecord(output, "LINE", ("8", "ELECTRICAL"), ("20", "2"));
            Assert.Equal("2", ReadPairValue(movedRoute, "10"));
            Assert.Equal("9", ReadPairValue(movedRoute, "11"));
            Assert.Equal("2", ReadPairValue(movedRoute, "21"));
            var fixedRoute = FindDxfRecord(output, "LINE", ("8", "ELECTRICAL"), ("20", "3"));
            Assert.Equal("2", ReadPairValue(fixedRoute, "10"));
            Assert.Equal("10", ReadPairValue(fixedRoute, "11"));
            Assert.Equal("3", ReadPairValue(fixedRoute, "21"));
            var movedDevice = FindDxfRecord(output, "INSERT", ("10", "9"));
            Assert.Equal("2", ReadPairValue(movedDevice, "20"));
            Assert.Equal(4, CountRecords(output, "INSERT"));
        }
        finally
        {
            DeleteComposedOverlayFixture(fixture);
        }
    }

    [Fact]
    public async Task ExportAsync_rejects_uncommissioned_carrier_as_unsupported_wire_route()
    {
        var fixture = await WriteComposedOverlayFixtureAsync(
            [
                "0", "INSERT", "5", "130", "330", "121", "8", "ELECTRICAL", "2", "DEVICE", "10", "2", "20", "2",
                "0", "LINE", "5", "150", "330", "121", "8", "ELECTRICAL", "10", "2", "20", "2", "11", "10", "21", "2"
            ]);
        try
        {
            var error = await Assert.ThrowsAsync<ProjectedPlanSheetManualReviewRequiredException>(() =>
                ExportComposedOverlayAsync(
                    fixture,
                    new ElectricalOverlayReconciliation(
                        [WallBinding("130", "Fixed")],
                        [])));

            Assert.Contains("UnsupportedWireRoute", error.Message, StringComparison.Ordinal);
            Assert.Contains("150", error.Message, StringComparison.Ordinal);
            Assert.False(File.Exists(fixture.OutputPath));
        }
        finally
        {
            DeleteComposedOverlayFixture(fixture);
        }
    }

    [Fact]
    public async Task ExportAsync_rejects_non_line_route_carrier_as_unsupported_wire_route()
    {
        var fixture = await WriteComposedOverlayFixtureAsync(
            [
                "0", "INSERT", "5", "130", "330", "121", "8", "ELECTRICAL", "2", "DEVICE", "10", "2", "20", "2",
                "0", "INSERT", "5", "140", "330", "121", "8", "ELECTRICAL", "2", "DEVICE", "10", "4", "20", "3",
                "0", "ARC", "5", "150", "330", "121", "8", "ELECTRICAL", "10", "4", "20", "2", "40", "1", "50", "0", "51", "90"
            ]);
        try
        {
            var error = await Assert.ThrowsAsync<ProjectedPlanSheetManualReviewRequiredException>(() =>
                ExportComposedOverlayAsync(
                    fixture,
                    new ElectricalOverlayReconciliation(
                        [WallBinding("130", "Fixed"), WallBinding("140", "Fixed")],
                        [new ElectricalWireRouteBinding("150", "130", "140")])));

            Assert.Contains("UnsupportedWireRoute", error.Message, StringComparison.Ordinal);
            Assert.Contains("ARC", error.Message, StringComparison.Ordinal);
            Assert.False(File.Exists(fixture.OutputPath));
        }
        finally
        {
            DeleteComposedOverlayFixture(fixture);
        }
    }

    [Fact]
    public async Task ExportAsync_keeps_declared_static_carrier_projected_without_route()
    {
        var fixture = await WriteComposedOverlayFixtureAsync(
            [
                "0", "LINE", "5", "150", "330", "121", "8", "ELECTRICAL", "10", "2", "20", "2", "11", "10", "21", "2"
            ]);
        try
        {
            await ExportComposedOverlayAsync(
                fixture,
                new ElectricalOverlayReconciliation([], [])
                {
                    StaticCarrierHandles = ["150"]
                });

            var output = await File.ReadAllLinesAsync(fixture.OutputPath, CancellationToken.None);
            var staticCarrier = FindDxfRecord(output, "LINE", ("8", "ELECTRICAL"));
            Assert.Equal("2", ReadPairValue(staticCarrier, "10"));
            Assert.Equal("9", ReadPairValue(staticCarrier, "11"));
        }
        finally
        {
            DeleteComposedOverlayFixture(fixture);
        }
    }

    [Fact]
    public async Task ExportAsync_rejects_route_endpoint_without_device_binding()
    {
        var fixture = await WriteComposedOverlayFixtureAsync(
            [
                "0", "INSERT", "5", "130", "330", "121", "8", "ELECTRICAL", "2", "DEVICE", "10", "2", "20", "2",
                "0", "LINE", "5", "150", "330", "121", "8", "ELECTRICAL", "10", "2", "20", "2", "11", "10", "21", "2"
            ]);
        try
        {
            var error = await Assert.ThrowsAsync<ProjectedPlanSheetManualReviewRequiredException>(() =>
                ExportComposedOverlayAsync(
                    fixture,
                    new ElectricalOverlayReconciliation(
                        [WallBinding("130", "Fixed")],
                        [new ElectricalWireRouteBinding("150", "130", "999")])));

            Assert.Contains("999", error.Message, StringComparison.Ordinal);
            Assert.False(File.Exists(fixture.OutputPath));
        }
        finally
        {
            DeleteComposedOverlayFixture(fixture);
        }
    }

    [Fact]
    public async Task ExportAsync_rejects_route_endpoint_bound_but_absent_from_overlay_as_unsupported_wire_route()
    {
        var fixture = await WriteComposedOverlayFixtureAsync(
            [
                "0", "INSERT", "5", "130", "330", "121", "8", "ELECTRICAL", "2", "DEVICE", "10", "2", "20", "2",
                "0", "LINE", "5", "150", "330", "121", "8", "ELECTRICAL", "10", "2", "20", "2", "11", "10", "21", "2"
            ]);
        try
        {
            var error = await Assert.ThrowsAsync<ProjectedPlanSheetManualReviewRequiredException>(() =>
                ExportComposedOverlayAsync(
                    fixture,
                    new ElectricalOverlayReconciliation(
                        [WallBinding("130", "Fixed"), WallBinding("999", "Fixed")],
                        [new ElectricalWireRouteBinding("150", "130", "999")])));

            Assert.Contains("UnsupportedWireRoute", error.Message, StringComparison.Ordinal);
            Assert.Contains("150", error.Message, StringComparison.Ordinal);
            Assert.Contains("999", error.Message, StringComparison.Ordinal);
            Assert.False(File.Exists(fixture.OutputPath));
        }
        finally
        {
            DeleteComposedOverlayFixture(fixture);
        }
    }

    [Fact]
    public async Task ExportAsync_rejects_host_delta_without_matching_recipe_axis()
    {
        var fixture = await WriteComposedOverlayFixtureAsync(
            [
                "0", "INSERT", "5", "130", "330", "121", "8", "ELECTRICAL", "2", "DEVICE", "10", "4", "20", "5"
            ]);
        try
        {
            var error = await Assert.ThrowsAsync<ProjectedPlanSheetManualReviewRequiredException>(() =>
                ExportComposedOverlayAsync(
                    fixture,
                    new ElectricalOverlayReconciliation(
                        [WallBinding("130", "HostRigidMove", deltaY: -1m)],
                        [])));

            Assert.Contains("Height", error.Message, StringComparison.Ordinal);
            Assert.False(File.Exists(fixture.OutputPath));
        }
        finally
        {
            DeleteComposedOverlayFixture(fixture);
        }
    }

    private static async Task<(string CanonicalPath, string ElectricalPath, string OutputPath)> WriteComposedOverlayFixtureAsync(
        IReadOnlyList<string> electricalEntityPairs)
    {
        var canonicalPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-overlay-canonical.dxf");
        var electricalPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-overlay-electrical.dxf");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-overlay-composed.dxf");
        await File.WriteAllTextAsync(canonicalPath, CreateModernCanonicalCompositionDxf(), CancellationToken.None);
        await File.WriteAllTextAsync(
            electricalPath,
            CreateModernElectricalCompositionDxf(electricalEntityPairs, includeDeviceBlock: true),
            CancellationToken.None);
        return (canonicalPath, electricalPath, outputPath);
    }

    private static Task ExportComposedOverlayAsync(
        (string CanonicalPath, string ElectricalPath, string OutputPath) fixture,
        ElectricalOverlayReconciliation? reconciliation)
        => new ProjectedPlanSheetDxfExporter().ExportAsync(
            fixture.ElectricalPath,
            fixture.OutputPath,
            new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
            CreateProvenExportRecipe(
                new SheetRegistrationTransform(1m, 0m, 0m, 0m),
                CreateRightCutDeformingRecipe(),
                CanonicalFloorPlanExportPath: fixture.CanonicalPath,
                OverlayReconciliation: reconciliation),
            CancellationToken.None);

    private static AdjustmentRecipeSummaryDto CreateRightCutDeformingRecipe()
        => new(
            "v1",
            1m,
            0m,
            0m,
            [new AdjustmentRecipeOperationDto("HorizontalCompression", "Width", "Right", 6m, 1m)]);

    private static ElectricalDeviceHostBinding WallBinding(
        string handle,
        string role,
        decimal deltaX = 0m,
        decimal deltaY = 0m,
        string blockName = "DEVICE")
        => new(handle, blockName, "Wall", "LINE:7", role, deltaX, deltaY);

    private static void DeleteComposedOverlayFixture(
        (string CanonicalPath, string ElectricalPath, string OutputPath) fixture)
    {
        File.Delete(fixture.CanonicalPath);
        File.Delete(fixture.ElectricalPath);
        File.Delete(fixture.OutputPath);
    }

    private static int CountRecords(IReadOnlyList<string> lines, string recordType)
    {
        var count = 0;
        for (var index = 0; index + 1 < lines.Count; index += 2)
        {
            if (lines[index] == "0" &&
                string.Equals(lines[index + 1], recordType, StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }

    private static WholePlanRegistrationProof CreatePassedWholePlanProof()
        => new(
            WholePlanRegistrationProof.CurrentVersion,
            Passed: true,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new string('a', 64),
            new string('b', 64),
            HorizontalCoverage: 0.94m,
            VerticalCoverage: 0.91m,
            RootMeanSquareResidual: 0.01m,
            MaximumResidual: 0.02m);

    private static string CreateModernCanonicalCompositionDxf(
        IReadOnlyList<string>? layerZeroExtraPairs = null,
        bool includeStandardStyleTable = false)
    {
        var pairs = new List<string>
        {
            "0", "SECTION", "2", "HEADER", "9", "$HANDSEED", "5", "100", "0", "ENDSEC",
            "0", "SECTION", "2", "TABLES",
            "0", "TABLE", "5", "10", "2", "LAYER", "70", "2",
            "0", "LAYER", "5", "11", "330", "10", "2", "0", "70", "0", "62", "7", "6", "CONTINUOUS"
        };
        if (layerZeroExtraPairs is not null)
        {
            pairs.AddRange(layerZeroExtraPairs);
        }

        pairs.AddRange(
        [
            "0", "LAYER", "5", "12", "330", "10", "2", "ARCHITECTURAL", "70", "0", "62", "7", "6", "CONTINUOUS",
            "0", "ENDTAB"
        ]);
        if (includeStandardStyleTable)
        {
            pairs.AddRange(
            [
                "0", "TABLE", "5", "30", "2", "STYLE", "70", "1",
                "0", "STYLE", "5", "31", "330", "30", "2", "STANDARD", "70", "0", "40", "0", "41", "1", "50", "0", "71", "0", "42", "1", "3", "txt", "4", "",
                "0", "ENDTAB"
            ]);
        }

        pairs.AddRange(
        [
            "0", "TABLE", "5", "20", "2", "BLOCK_RECORD", "70", "1",
            "0", "BLOCK_RECORD", "5", "21", "330", "20", "2", "*Model_Space", "70", "0",
            "0", "ENDTAB", "0", "ENDSEC",
            "0", "SECTION", "2", "BLOCKS",
            "0", "BLOCK", "5", "22", "330", "21", "8", "0", "2", "*Model_Space", "70", "0", "10", "0", "20", "0", "3", "*Model_Space",
            "0", "ENDBLK", "5", "23", "330", "21", "8", "0",
            "0", "ENDSEC",
            "0", "SECTION", "2", "ENTITIES",
            "0", "LINE", "5", "24", "330", "21", "8", "ARCHITECTURAL", "10", "0", "20", "0", "11", "20", "21", "0",
            "0", "ENDSEC", "0", "EOF"
        ]);
        return string.Join(Environment.NewLine, pairs);
    }

    private static string CreateModernElectricalCompositionDxf(
        IReadOnlyList<string> entityPairs,
        IReadOnlyList<string>? electricalLayerExtraPairs = null,
        bool includeDeviceBlock = false,
        IReadOnlyList<string>? objectPairs = null,
        bool includeRomansStyle = false)
    {
        var pairs = new List<string>
        {
            "0", "SECTION", "2", "HEADER", "9", "$HANDSEED", "5", "200", "0", "ENDSEC",
            "0", "SECTION", "2", "TABLES",
            "0", "TABLE", "5", "110", "2", "LAYER", "70", "2",
            "0", "LAYER", "5", "111", "330", "110", "2", "0", "70", "0", "62", "7", "6", "CONTINUOUS",
            "0", "LAYER", "5", "112", "330", "110", "2", "ELECTRICAL", "70", "0", "62", "2", "6", "CONTINUOUS"
        };
        if (electricalLayerExtraPairs is not null)
        {
            pairs.AddRange(electricalLayerExtraPairs);
        }

        pairs.AddRange(["0", "ENDTAB"]);
        if (includeRomansStyle)
        {
            pairs.AddRange(
            [
                "0", "TABLE", "5", "150", "2", "STYLE", "70", "1",
                "0", "STYLE", "5", "151", "330", "150", "2", "ROMANS", "70", "0", "40", "0", "41", "1", "50", "0", "71", "0", "42", "1", "3", "romans", "4", "",
                "0", "ENDTAB"
            ]);
        }

        pairs.AddRange(
        [
            "0", "TABLE", "5", "120", "2", "BLOCK_RECORD", "70", includeDeviceBlock ? "2" : "1",
            "0", "BLOCK_RECORD", "5", "121", "330", "120", "2", "*Model_Space", "70", "0"
        ]);
        if (includeDeviceBlock)
        {
            pairs.AddRange(["0", "BLOCK_RECORD", "5", "122", "330", "120", "2", "DEVICE", "70", "0"]);
        }

        pairs.AddRange(
        [
            "0", "ENDTAB", "0", "ENDSEC",
            "0", "SECTION", "2", "BLOCKS",
            "0", "BLOCK", "5", "123", "330", "121", "8", "0", "2", "*Model_Space", "70", "0", "10", "0", "20", "0", "3", "*Model_Space",
            "0", "ENDBLK", "5", "124", "330", "121", "8", "0"
        ]);
        if (includeDeviceBlock)
        {
            pairs.AddRange(
            [
                "0", "BLOCK", "5", "125", "330", "122", "8", "0", "2", "DEVICE", "70", "0", "10", "0", "20", "0", "3", "DEVICE",
                "0", "CIRCLE", "5", "126", "330", "122", "8", "0", "10", "0", "20", "0", "40", "0.25",
                "0", "ENDBLK", "5", "127", "330", "122", "8", "0"
            ]);
        }

        pairs.AddRange(["0", "ENDSEC", "0", "SECTION", "2", "ENTITIES"]);
        pairs.AddRange(entityPairs);
        pairs.AddRange(["0", "ENDSEC"]);
        if (objectPairs is not null)
        {
            pairs.AddRange(["0", "SECTION", "2", "OBJECTS"]);
            pairs.AddRange(objectPairs);
            pairs.AddRange(["0", "ENDSEC"]);
        }

        pairs.AddRange(["0", "EOF"]);
        return string.Join(Environment.NewLine, pairs);
    }

    private static IReadOnlyList<string> FindDxfRecord(
        IReadOnlyList<string> lines,
        string recordType,
        params (string Code, string Value)[] requiredPairs)
    {
        for (var index = 0; index + 1 < lines.Count; index += 2)
        {
            if (lines[index] != "0" ||
                !string.Equals(lines[index + 1], recordType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var end = index + 2;
            while (end + 1 < lines.Count && lines[end] != "0")
            {
                end += 2;
            }

            if (requiredPairs.All(required => EntityHasPair(lines, index + 2, end, required.Code, required.Value)))
            {
                return lines.Skip(index).Take(end - index).ToArray();
            }
        }

        Assert.Fail($"Expected {recordType} record with {string.Join(", ", requiredPairs.Select(pair => $"{pair.Code}/{pair.Value}"))}.");
        return [];
    }

    private static string? ReadPairValue(IReadOnlyList<string> record, string code)
    {
        for (var index = 2; index + 1 < record.Count; index += 2)
        {
            if (record[index] == code)
            {
                return record[index + 1];
            }
        }

        return null;
    }

    private static ProjectedPlanSheetExportRecipe CreateProvenExportRecipe(
        SheetRegistrationTransform registrationTransform,
        AdjustmentRecipeSummaryDto canonicalRecipe,
        decimal? CanonicalSourceWidthInches = null,
        decimal? CanonicalSourceHeightInches = null,
        ProjectedPlanSheetOutlineNormalization? OutlineNormalization = null,
        string? CanonicalFloorPlanExportPath = null,
        ElectricalOverlayReconciliation? OverlayReconciliation = null)
        => new(
            registrationTransform,
            canonicalRecipe,
            CanonicalSourceWidthInches,
            CanonicalSourceHeightInches,
            OutlineNormalization,
            SheetRegistrationStatus.Confirmed,
            CreatePassedWholePlanProof(),
            CanonicalFloorPlanExportPath,
            OverlayReconciliation);

    private static IReadOnlyList<(decimal X1, decimal Y1, decimal X2, decimal Y2)> ReadLineSegments(
        IReadOnlyList<string> pairs)
    {
        var lines = new List<(decimal, decimal, decimal, decimal)>();
        for (var index = 0; index + 1 < pairs.Count; index += 2)
        {
            if (pairs[index] != "0" || pairs[index + 1] != "LINE")
            {
                continue;
            }

            decimal? x1 = null;
            decimal? y1 = null;
            decimal? x2 = null;
            decimal? y2 = null;
            for (var pairIndex = index + 2; pairIndex + 1 < pairs.Count && pairs[pairIndex] != "0"; pairIndex += 2)
            {
                if (!decimal.TryParse(
                        pairs[pairIndex + 1],
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out var value))
                {
                    continue;
                }

                switch (pairs[pairIndex])
                {
                    case "10": x1 = value; break;
                    case "20": y1 = value; break;
                    case "11": x2 = value; break;
                    case "21": y2 = value; break;
                }
            }

            if (x1.HasValue && y1.HasValue && x2.HasValue && y2.HasValue)
            {
                lines.Add((x1.Value, y1.Value, x2.Value, y2.Value));
            }
        }

        return lines;
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
        for (var index = 0; index + 1 < lines.Count; index += 2)
        {
            if (lines[index] != "9" || lines[index + 1] != variableName)
            {
                continue;
            }

            for (var pairIndex = index + 2; pairIndex + 1 < lines.Count; pairIndex += 2)
            {
                if (lines[pairIndex] is "9" or "0")
                {
                    break;
                }

                if (lines[pairIndex] == code && lines[pairIndex + 1] == value)
                {
                    return;
                }
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

    private static void AssertRecordHasPair(
        IReadOnlyList<string> lines,
        string recordType,
        string code,
        string value)
    {
        for (var index = 0; index + 1 < lines.Count; index += 2)
        {
            if (lines[index] != "0" ||
                !string.Equals(lines[index + 1], recordType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var end = index + 2;
            while (end + 1 < lines.Count && lines[end] != "0")
            {
                end += 2;
            }

            if (EntityHasPair(lines, index + 2, end, code, value))
            {
                return;
            }
        }

        Assert.Fail($"Expected {recordType} record to have pair {code}/{value}.");
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
