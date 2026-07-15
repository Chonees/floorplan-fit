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

    private static ProjectedPlanSheetExportRecipe CreateProvenExportRecipe(
        SheetRegistrationTransform registrationTransform,
        AdjustmentRecipeSummaryDto canonicalRecipe,
        decimal? CanonicalSourceWidthInches = null,
        decimal? CanonicalSourceHeightInches = null,
        ProjectedPlanSheetOutlineNormalization? OutlineNormalization = null)
        => new(
            registrationTransform,
            canonicalRecipe,
            CanonicalSourceWidthInches,
            CanonicalSourceHeightInches,
            OutlineNormalization,
            SheetRegistrationStatus.Confirmed,
            CreatePassedWholePlanProof());

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
