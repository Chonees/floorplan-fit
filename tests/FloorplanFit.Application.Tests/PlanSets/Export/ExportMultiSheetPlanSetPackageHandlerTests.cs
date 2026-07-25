using System.Security.Cryptography;
using System.Text.Json;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Export;
using FloorplanFit.Application.PlanSets.ExportAudit;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets.Export;

public sealed class ExportMultiSheetPlanSetPackageHandlerTests : IDisposable
{
    private readonly string tempRoot = Path.Combine(
        Path.GetTempPath(),
        $"floorplan-fit-atomic-package-{Guid.NewGuid():N}");

    [Fact]
    public async Task HandleAsync_publishes_folder_only_with_staged_canonical_verification_and_final_storage()
    {
        Directory.CreateDirectory(tempRoot);
        var canonicalPath = Path.Combine(tempRoot, "My unsafe plan!.dxf");
        await File.WriteAllTextAsync(canonicalPath, "canonical-source");
        var projection = CreateProjection(Guid.NewGuid(), Guid.NewGuid());
        var projectionRepository = new FakeSheetAdjustmentProjectionRepository(projection);
        var packageDirectory = Path.Combine(tempRoot, "My unsafe plan!-plan-set");
        MultiSheetExportAuditDto? verificationInput = null;
        var writer = new CapturingPlanSetExportManifestWriter("exports/plan-sets/package/manifest.json")
        {
            OnBuildVerification = audit => verificationInput = audit
        };
        var electricalSourcePath = CreateElectricalSourceFile();
        var handler = new ExportMultiSheetPlanSetPackageHandler(
            projectionRepository,
            new FakePlanSheetSourceReader(new PlanSheetSourceDto(
                projection.DependentSheetId,
                "ElectricalPlan",
                "Electrical",
                Guid.NewGuid(),
                electricalSourcePath)),
            CreateProjectedSheetHandler(projectionRepository, new CapturingProjectedPlanSheetExporter(), [(projection, electricalSourcePath)]),
            new CreateMultiSheetExportAuditHandler(
                projectionRepository,
                new FakePlanSheetReader(),
                new CapturingPlanSetExportRepository(),
                new CapturingPlanSetAuditEventRepository(),
                writer,
                new CapturingUnitOfWork(),
                new FakeClock(new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc))));

        var response = await handler.HandleAsync(
            new ExportMultiSheetPlanSetPackageRequest(
                projection.PlanSetVersionId,
                Guid.NewGuid(),
                projection.CanonicalAdjustmentId,
                canonicalPath,
                packageDirectory,
                [projection.Id])
            {
                DeleteCanonicalSourceAfterSuccess = true
            },
            CancellationToken.None);

        Assert.Equal(
            [
                "manifest.json",
                "My-unsafe-plan-audit.txt",
                "My-unsafe-plan-comparison.json",
                "My-unsafe-plan-electrical.dxf",
                "My-unsafe-plan-floorplan.dxf"
            ],
            Directory.GetFiles(packageDirectory).Select(Path.GetFileName).Order().ToArray());
        Assert.All(Directory.GetFiles(packageDirectory, "*.dxf"), path => Assert.True(new FileInfo(path).Length > 0));
        Assert.False(File.Exists(Path.Combine(packageDirectory, "My-unsafe-plan-comparison.dxf")));
        using (var comparison = JsonDocument.Parse(await File.ReadAllTextAsync(
                   Path.Combine(packageDirectory, "My-unsafe-plan-comparison.json"))))
        {
            Assert.Equal("final-output-congruence", comparison.RootElement.GetProperty("stage").GetString());
            var congruence = comparison.RootElement.GetProperty("sheets")[0].GetProperty("finalOutputCongruence");
            Assert.Equal(
                Path.Combine(packageDirectory, "My-unsafe-plan-floorplan.dxf"),
                congruence.GetProperty("FloorOutputPath").GetString());
            Assert.Equal(
                Path.Combine(packageDirectory, "My-unsafe-plan-electrical.dxf"),
                congruence.GetProperty("ElectricalOutputPath").GetString());
        }
        Assert.DoesNotContain(
            ".staging-",
            await File.ReadAllTextAsync(Path.Combine(packageDirectory, "My-unsafe-plan-comparison.json")),
            StringComparison.Ordinal);

        Assert.False(string.IsNullOrWhiteSpace(await File.ReadAllTextAsync(
            Path.Combine(packageDirectory, "My-unsafe-plan-audit.txt"))));
        Assert.False(File.Exists(canonicalPath));
        Assert.NotNull(verificationInput);
        var canonicalSheet = Assert.Single(verificationInput.Sheets, sheet => sheet.SheetKind == "CanonicalFloorPlan");
        Assert.Equal(Path.Combine(packageDirectory, "My-unsafe-plan-floorplan.dxf"), canonicalSheet.StoragePath);
        Assert.Contains(".staging-", canonicalSheet.VerificationPath, StringComparison.Ordinal);
        Assert.EndsWith("My-unsafe-plan-floorplan.dxf", canonicalSheet.VerificationPath, StringComparison.Ordinal);
        Assert.Equal(
            ["Audit", "Comparison", "ElectricalPlan", "FloorPlan", "Manifest"],
            response.Artifacts.Select(artifact => artifact.Role).Order().ToArray());
        Assert.DoesNotContain(response.Artifacts, artifact => artifact.Role == "ComparisonReview");
        Assert.All(response.Artifacts, artifact => Assert.StartsWith(packageDirectory, artifact.Path, StringComparison.Ordinal));
        using (var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(
                   Path.Combine(packageDirectory, "manifest.json"))))
        {
            Assert.Equal(
                Path.Combine(packageDirectory, "manifest.json"),
                manifest.RootElement.GetProperty("PackageManifestPath").GetString());
            var artifacts = manifest.RootElement.GetProperty("Artifacts").EnumerateArray().ToArray();
            Assert.Equal(5, artifacts.Length);
            Assert.All(artifacts, artifact =>
                Assert.StartsWith(packageDirectory, artifact.GetProperty("Path").GetString(), StringComparison.Ordinal));
        }
        Assert.DoesNotContain(
            ".staging-",
            await File.ReadAllTextAsync(Path.Combine(packageDirectory, "manifest.json")),
            StringComparison.Ordinal);
        Assert.Equal(
            Path.Combine(packageDirectory, "My-unsafe-plan-floorplan.dxf"),
            Assert.Single(response.Sheets, sheet => sheet.SheetKind == "CanonicalFloorPlan").StoragePath);
    }

    [Fact]
    public async Task HandleAsync_suffixes_real_sheet_name_collisions_deterministically()
    {
        Directory.CreateDirectory(tempRoot);
        var canonicalPath = Path.Combine(tempRoot, "X.dxf");
        await File.WriteAllTextAsync(canonicalPath, "canonical-source");
        var planSetVersionId = Guid.NewGuid();
        var adjustmentId = Guid.NewGuid();
        var first = CreateProjection(planSetVersionId, adjustmentId, SheetAdjustmentProjectionStatus.ReadyForExport, Guid.NewGuid());
        var second = CreateProjection(planSetVersionId, adjustmentId, SheetAdjustmentProjectionStatus.ReadyForExport, Guid.NewGuid());
        var projections = new FakeSheetAdjustmentProjectionRepository(first, second);
        var packageDirectory = Path.Combine(tempRoot, "X-plan-set");
        var firstSourcePath = CreateElectricalSourceFile();
        var secondSourcePath = CreateElectricalSourceFile();
        var handler = new ExportMultiSheetPlanSetPackageHandler(
            projections,
            new FakePlanSheetSourceReader(
                new PlanSheetSourceDto(first.DependentSheetId, "ElectricalPlan", "Electrical A", Guid.NewGuid(), firstSourcePath),
                new PlanSheetSourceDto(second.DependentSheetId, "ElectricalPlan", "Electrical B", Guid.NewGuid(), secondSourcePath)),
            CreateProjectedSheetHandler(
                projections,
                new CapturingProjectedPlanSheetExporter(),
                [(first, firstSourcePath), (second, secondSourcePath)]),
            new CreateMultiSheetExportAuditHandler(
                projections,
                new FakePlanSheetReader(),
                new CapturingPlanSetExportRepository(),
                new CapturingPlanSetAuditEventRepository(),
                new CapturingPlanSetExportManifestWriter("manifest.json"),
                new CapturingUnitOfWork(),
                new FakeClock(DateTime.UtcNow)));

        await handler.HandleAsync(
            new ExportMultiSheetPlanSetPackageRequest(
                planSetVersionId,
                Guid.NewGuid(),
                adjustmentId,
                canonicalPath,
                packageDirectory,
                [first.Id, second.Id]),
            CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(packageDirectory, "X-electrical.dxf")));
        Assert.True(File.Exists(Path.Combine(packageDirectory, "X-electrical-2.dxf")));
        Assert.True(File.Exists(canonicalPath));
    }

    [Fact]
    public async Task HandleAsync_publishes_complete_package_from_sibling_staging()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var canonicalAdjustmentId = Guid.NewGuid();
        var projection = CreateProjection(planSetVersionId, canonicalAdjustmentId);
        var projectionRepository = new FakeSheetAdjustmentProjectionRepository(projection);
        var projectedSheetExporter = new CapturingProjectedPlanSheetExporter();
        var auditRepository = new CapturingPlanSetExportRepository();
        var packageDirectory = CreatePackageDirectory();
        var electricalSourcePath = CreateElectricalSourceFile();
        var expectedOutputPath = Path.Combine(packageDirectory, "floor-plan-adjusted-electrical.dxf");
        var packageHandler = new ExportMultiSheetPlanSetPackageHandler(
            projectionRepository,
            new FakePlanSheetSourceReader(
                new PlanSheetSourceDto(
                    projection.DependentSheetId,
                    "ElectricalPlan",
                    "Electrical",
                    Guid.NewGuid(),
                    electricalSourcePath)),
            CreateProjectedSheetHandler(projectionRepository, projectedSheetExporter, [(projection, electricalSourcePath)]),
            new CreateMultiSheetExportAuditHandler(
                projectionRepository,
                new FakePlanSheetReader(),
                auditRepository,
                new CapturingPlanSetAuditEventRepository(),
                new CapturingPlanSetExportManifestWriter("exports/plan-sets/package/manifest.json"),
                new CapturingUnitOfWork(),
                new FakeClock(new DateTime(2026, 6, 30, 23, 59, 0, DateTimeKind.Utc))));

        var response = await packageHandler.HandleAsync(
            new ExportMultiSheetPlanSetPackageRequest(
                planSetVersionId,
                canonicalFloorPlanVersionId,
                canonicalAdjustmentId,
                CreateCanonicalPath(),
                packageDirectory,
                [projection.Id]),
            CancellationToken.None);

        var exportCall = Assert.Single(projectedSheetExporter.Calls);
        Assert.Equal(electricalSourcePath, exportCall.SourceFilePath);
        Assert.NotEqual(expectedOutputPath, exportCall.OutputFilePath);
        Assert.Contains(".staging-", exportCall.OutputFilePath, StringComparison.Ordinal);
        Assert.True(File.Exists(expectedOutputPath));
        Assert.Empty(FindSiblingStagingDirectories(packageDirectory));

        var dependentSheet = Assert.Single(response.Sheets, sheet => sheet.ProjectionId == projection.Id);
        Assert.Equal("ProjectedAutomatically", dependentSheet.Status);
        Assert.Equal(expectedOutputPath, dependentSheet.StoragePath);
        Assert.Equal("exports/plan-sets/package/manifest.json", response.PackageManifestPath);

        var savedAudit = Assert.Single(auditRepository.Items);
        Assert.Contains(
            savedAudit.Sheets,
            sheet => sheet.SheetProjectionId == projection.Id &&
                     sheet.StoragePath == expectedOutputPath);
    }

    [Fact]
    public async Task HandleAsync_keeps_final_hidden_until_verification_and_workspace_manifest_then_renames()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var canonicalAdjustmentId = Guid.NewGuid();
        var projection = CreateProjection(planSetVersionId, canonicalAdjustmentId);
        var projectionRepository = new FakeSheetAdjustmentProjectionRepository(projection);
        var packageDirectory = CreatePackageDirectory();
        var expectedOutputPath = Path.Combine(packageDirectory, "floor-plan-adjusted-electrical.dxf");
        var order = new List<string>();
        var writer = new CapturingPlanSetExportManifestWriter("exports/plan-sets/package/manifest.json")
        {
            OnBuildVerification = audit =>
            {
                order.Add("verify");
                Assert.False(Directory.Exists(packageDirectory));
                var dependent = Assert.Single(audit.Sheets, sheet => sheet.ProjectionId == projection.Id);
                Assert.Equal(expectedOutputPath, dependent.StoragePath);
                Assert.NotNull(dependent.VerificationPath);
                Assert.Contains(".staging-", dependent.VerificationPath, StringComparison.Ordinal);
                Assert.True(File.Exists(dependent.VerificationPath));
            },
            OnWrite = audit =>
            {
                order.Add("manifest");
                Assert.False(Directory.Exists(packageDirectory));
                var serialized = JsonSerializer.Serialize(audit);
                Assert.False(serialized.Contains(".staging-", StringComparison.Ordinal));
                Assert.False(serialized.Contains("VerificationPath", StringComparison.Ordinal));
            },
            OnPackageWrite = (audit, stagingDirectory) =>
            {
                order.Add("package-artifacts");
                Assert.False(Directory.Exists(packageDirectory));
                Assert.Contains(".staging-", stagingDirectory, StringComparison.Ordinal);
                Assert.All(audit.Artifacts, artifact =>
                    Assert.StartsWith(packageDirectory, artifact.Path, StringComparison.Ordinal));
            }
        };
        var exportRepository = new CapturingPlanSetExportRepository
        {
            OnAdd = export =>
            {
                order.Add("persist");
                Assert.True(Directory.Exists(packageDirectory));
                Assert.All(
                    export.Sheets,
                    sheet => Assert.False((sheet.StoragePath ?? string.Empty).Contains(".staging-", StringComparison.Ordinal)));
            }
        };
        var electricalSourcePath = CreateElectricalSourceFile();
        var packageHandler = new ExportMultiSheetPlanSetPackageHandler(
            projectionRepository,
            new FakePlanSheetSourceReader(
                new PlanSheetSourceDto(
                    projection.DependentSheetId,
                    "ElectricalPlan",
                    "Electrical",
                    Guid.NewGuid(),
                    electricalSourcePath)),
            CreateProjectedSheetHandler(projectionRepository, new CapturingProjectedPlanSheetExporter(), [(projection, electricalSourcePath)]),
            new CreateMultiSheetExportAuditHandler(
                projectionRepository,
                new FakePlanSheetReader(),
                exportRepository,
                new CapturingPlanSetAuditEventRepository(),
                writer,
                new CapturingUnitOfWork(),
                new FakeClock(new DateTime(2026, 6, 30, 23, 59, 0, DateTimeKind.Utc))));

        var response = await packageHandler.HandleAsync(
            new ExportMultiSheetPlanSetPackageRequest(
                planSetVersionId,
                canonicalFloorPlanVersionId,
                canonicalAdjustmentId,
                CreateCanonicalPath(),
                packageDirectory,
                [projection.Id]),
            CancellationToken.None);

        Assert.Equal(new[] { "verify", "manifest", "package-artifacts", "persist" }, order);
        Assert.True(File.Exists(expectedOutputPath));
        Assert.False(JsonSerializer.Serialize(response).Contains(".staging-", StringComparison.Ordinal));
    }

    [Fact]
    public async Task HandleAsync_without_explicit_projection_ids_exports_ready_projections_and_audits_manual_or_missing_sheets()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var canonicalAdjustmentId = Guid.NewGuid();
        var electricalSheetId = Guid.NewGuid();
        var roofSheetId = Guid.NewGuid();
        var facadeSheetId = Guid.NewGuid();
        var readyProjection = CreateProjection(
            planSetVersionId,
            canonicalAdjustmentId,
            SheetAdjustmentProjectionStatus.ReadyForExport,
            electricalSheetId);
        var manualProjection = CreateProjection(
            planSetVersionId,
            canonicalAdjustmentId,
            SheetAdjustmentProjectionStatus.RequiresManualConfirmation,
            roofSheetId);
        var projectionRepository = new FakeSheetAdjustmentProjectionRepository(readyProjection, manualProjection);
        var projectedSheetExporter = new CapturingProjectedPlanSheetExporter();
        var packageDirectory = CreatePackageDirectory();
        var electricalSourcePath = CreateElectricalSourceFile();
        var expectedOutputPath = Path.Combine(packageDirectory, "floor-plan-adjusted-electrical.dxf");
        var packageHandler = new ExportMultiSheetPlanSetPackageHandler(
            projectionRepository,
            new FakePlanSheetSourceReader(
                new PlanSheetSourceDto(electricalSheetId, "ElectricalPlan", "Electrical", Guid.NewGuid(), electricalSourcePath)),
            CreateProjectedSheetHandler(projectionRepository, projectedSheetExporter, [(readyProjection, electricalSourcePath)]),
            new CreateMultiSheetExportAuditHandler(
                projectionRepository,
                new FakePlanSheetReader(
                    planSetVersionId,
                    [
                        CreateSheet(electricalSheetId, "ElectricalPlan"),
                        CreateSheet(roofSheetId, "RoofPlan"),
                        CreateSheet(facadeSheetId, "FacadeElevation")
                    ]),
                new CapturingPlanSetExportRepository(),
                new CapturingPlanSetAuditEventRepository(),
                new CapturingPlanSetExportManifestWriter("exports/plan-sets/package/manifest.json"),
                new CapturingUnitOfWork(),
                new FakeClock(new DateTime(2026, 6, 30, 23, 59, 0, DateTimeKind.Utc))));

        var response = await packageHandler.HandleAsync(
            new ExportMultiSheetPlanSetPackageRequest(
                planSetVersionId,
                canonicalFloorPlanVersionId,
                canonicalAdjustmentId,
                CreateCanonicalPath(),
                packageDirectory,
                []),
            CancellationToken.None);

        var exportCall = Assert.Single(projectedSheetExporter.Calls);
        Assert.Equal(electricalSourcePath, exportCall.SourceFilePath);
        Assert.Contains(".staging-", exportCall.OutputFilePath, StringComparison.Ordinal);
        Assert.True(File.Exists(expectedOutputPath));

        Assert.Equal(4, response.Summary.TotalSheetCount);
        Assert.Equal(1, response.Summary.AutomaticallyProjectedSheetCount);
        Assert.Equal(2, response.Summary.ManualConfirmationRequiredSheetCount);

        var electrical = Assert.Single(response.Sheets, sheet => sheet.SheetId == electricalSheetId);
        Assert.Equal("ProjectedAutomatically", electrical.Status);
        Assert.Equal(expectedOutputPath, electrical.StoragePath);

        var roof = Assert.Single(response.Sheets, sheet => sheet.SheetId == roofSheetId);
        Assert.Equal("RequiresManualConfirmation", roof.Status);
        Assert.Null(roof.StoragePath);

        var facade = Assert.Single(response.Sheets, sheet => sheet.SheetId == facadeSheetId);
        Assert.Equal("MissingProjection", facade.Status);
    }

    [Fact]
    public async Task HandleAsync_without_required_electrical_flag_keeps_latest_manual_projection_in_audit()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var canonicalAdjustmentId = Guid.NewGuid();
        var electricalSheetId = Guid.NewGuid();
        var oldReadyProjection = CreateProjection(
            planSetVersionId,
            canonicalAdjustmentId,
            SheetAdjustmentProjectionStatus.ReadyForExport,
            electricalSheetId,
            new DateTime(2026, 6, 30, 23, 50, 0, DateTimeKind.Utc));
        var latestManualProjection = CreateProjection(
            planSetVersionId,
            canonicalAdjustmentId,
            SheetAdjustmentProjectionStatus.RequiresManualConfirmation,
            electricalSheetId,
            new DateTime(2026, 6, 30, 23, 55, 0, DateTimeKind.Utc));
        var projectionRepository = new FakeSheetAdjustmentProjectionRepository(oldReadyProjection, latestManualProjection);
        var projectedSheetExporter = new CapturingProjectedPlanSheetExporter();
        var packageHandler = new ExportMultiSheetPlanSetPackageHandler(
            projectionRepository,
            new FakePlanSheetSourceReader(
                new PlanSheetSourceDto(electricalSheetId, "ElectricalPlan", "Electrical", Guid.NewGuid(), "library/electrical.dxf")),
            new ExportProjectedPlanSheetHandler(projectionRepository, projectedSheetExporter),
            new CreateMultiSheetExportAuditHandler(
                projectionRepository,
                new FakePlanSheetReader(planSetVersionId, [CreateSheet(electricalSheetId, "ElectricalPlan")]),
                new CapturingPlanSetExportRepository(),
                new CapturingPlanSetAuditEventRepository(),
                new CapturingPlanSetExportManifestWriter("exports/plan-sets/package/manifest.json"),
                new CapturingUnitOfWork(),
                new FakeClock(new DateTime(2026, 6, 30, 23, 59, 0, DateTimeKind.Utc))));

        var response = await packageHandler.HandleAsync(
            new ExportMultiSheetPlanSetPackageRequest(
                planSetVersionId,
                canonicalFloorPlanVersionId,
                canonicalAdjustmentId,
                CreateCanonicalPath(),
                CreatePackageDirectory(),
                []),
            CancellationToken.None);

        Assert.Empty(projectedSheetExporter.Calls);
        var electrical = Assert.Single(response.Sheets, sheet => sheet.SheetId == electricalSheetId);
        Assert.Equal(latestManualProjection.Id, electrical.ProjectionId);
        Assert.Equal("RequiresManualConfirmation", electrical.Status);
        Assert.Null(electrical.StoragePath);
    }

    [Fact]
    public async Task HandleAsync_requiring_ready_electrical_fails_closed_when_latest_electrical_is_not_ready()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var canonicalAdjustmentId = Guid.NewGuid();
        var electricalSheetId = Guid.NewGuid();
        var oldReadyProjection = CreateProjection(
            planSetVersionId,
            canonicalAdjustmentId,
            SheetAdjustmentProjectionStatus.ReadyForExport,
            electricalSheetId,
            new DateTime(2026, 6, 30, 23, 50, 0, DateTimeKind.Utc));
        var latestManualProjection = CreateProjection(
            planSetVersionId,
            canonicalAdjustmentId,
            SheetAdjustmentProjectionStatus.RequiresManualConfirmation,
            electricalSheetId,
            new DateTime(2026, 6, 30, 23, 55, 0, DateTimeKind.Utc));
        var projectionRepository = new FakeSheetAdjustmentProjectionRepository(oldReadyProjection, latestManualProjection);
        var projectedSheetExporter = new CapturingProjectedPlanSheetExporter();
        var exportRepository = new CapturingPlanSetExportRepository();
        var packageDirectory = CreatePackageDirectory();
        var canonicalPath = CreateCanonicalPath();
        var packageHandler = new ExportMultiSheetPlanSetPackageHandler(
            projectionRepository,
            new FakePlanSheetSourceReader(
                new PlanSheetSourceDto(electricalSheetId, "ElectricalPlan", "Electrical", Guid.NewGuid(), "library/electrical.dxf")),
            new ExportProjectedPlanSheetHandler(projectionRepository, projectedSheetExporter),
            new CreateMultiSheetExportAuditHandler(
                projectionRepository,
                new FakePlanSheetReader(planSetVersionId, [CreateSheet(electricalSheetId, "ElectricalPlan")]),
                exportRepository,
                new CapturingPlanSetAuditEventRepository(),
                new CapturingPlanSetExportManifestWriter("exports/plan-sets/package/manifest.json"),
                new CapturingUnitOfWork(),
                new FakeClock(new DateTime(2026, 6, 30, 23, 59, 0, DateTimeKind.Utc))));

        var error = await Assert.ThrowsAsync<ProjectedPlanSheetManualReviewRequiredException>(() =>
            packageHandler.HandleAsync(
                new ExportMultiSheetPlanSetPackageRequest(
                    planSetVersionId,
                    canonicalFloorPlanVersionId,
                    canonicalAdjustmentId,
                    canonicalPath,
                    packageDirectory,
                    [])
                {
                    RequireReadyElectricalPlan = true
                },
                CancellationToken.None));

        Assert.Contains("ElectricalPlan", error.Message, StringComparison.Ordinal);
        Assert.Contains("RequiresManualConfirmation", error.Message, StringComparison.Ordinal);
        Assert.Contains("Needs review", error.Message, StringComparison.Ordinal);
        Assert.Empty(projectedSheetExporter.Calls);
        Assert.False(Directory.Exists(packageDirectory));
        Assert.True(File.Exists(canonicalPath));
        Assert.Empty(FindSiblingStagingDirectories(packageDirectory));
        var failure = AssertFailure(exportRepository, PlanSetExportFailureStage.DependentSheetGeneration);
        Assert.Contains("Needs review", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_requiring_ready_electrical_fails_closed_when_no_electrical_projection_exists()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalAdjustmentId = Guid.NewGuid();
        var roofSheetId = Guid.NewGuid();
        var roofProjection = CreateProjection(
            planSetVersionId,
            canonicalAdjustmentId,
            SheetAdjustmentProjectionStatus.ReadyForExport,
            roofSheetId);
        var projectionRepository = new FakeSheetAdjustmentProjectionRepository(roofProjection);
        var projectedSheetExporter = new CapturingProjectedPlanSheetExporter();
        var exportRepository = new CapturingPlanSetExportRepository();
        var packageDirectory = CreatePackageDirectory();
        var canonicalPath = CreateCanonicalPath();
        var packageHandler = new ExportMultiSheetPlanSetPackageHandler(
            projectionRepository,
            new FakePlanSheetSourceReader(
                new PlanSheetSourceDto(roofSheetId, "RoofPlan", "Roof", Guid.NewGuid(), "library/roof.dxf")),
            new ExportProjectedPlanSheetHandler(projectionRepository, projectedSheetExporter),
            new CreateMultiSheetExportAuditHandler(
                projectionRepository,
                new FakePlanSheetReader(planSetVersionId, [CreateSheet(roofSheetId, "RoofPlan")]),
                exportRepository,
                new CapturingPlanSetAuditEventRepository(),
                new CapturingPlanSetExportManifestWriter("exports/plan-sets/package/manifest.json"),
                new CapturingUnitOfWork(),
                new FakeClock(new DateTime(2026, 6, 30, 23, 59, 0, DateTimeKind.Utc))));

        var error = await Assert.ThrowsAsync<ProjectedPlanSheetManualReviewRequiredException>(() =>
            packageHandler.HandleAsync(
                new ExportMultiSheetPlanSetPackageRequest(
                    planSetVersionId,
                    Guid.NewGuid(),
                    canonicalAdjustmentId,
                    canonicalPath,
                    packageDirectory,
                    [])
                {
                    RequireReadyElectricalPlan = true
                },
                CancellationToken.None));

        Assert.Contains("ElectricalPlan", error.Message, StringComparison.Ordinal);
        Assert.Empty(projectedSheetExporter.Calls);
        Assert.False(Directory.Exists(packageDirectory));
        Assert.True(File.Exists(canonicalPath));
        Assert.Empty(FindSiblingStagingDirectories(packageDirectory));
        AssertFailure(exportRepository, PlanSetExportFailureStage.DependentSheetGeneration);
    }

    [Fact]
    public async Task HandleAsync_requiring_ready_electrical_publishes_when_latest_electrical_is_ready()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalAdjustmentId = Guid.NewGuid();
        var electricalSheetId = Guid.NewGuid();
        var readyProjection = CreateProjection(
            planSetVersionId,
            canonicalAdjustmentId,
            SheetAdjustmentProjectionStatus.ReadyForExport,
            electricalSheetId);
        var projectionRepository = new FakeSheetAdjustmentProjectionRepository(readyProjection);
        var projectedSheetExporter = new CapturingProjectedPlanSheetExporter();
        var packageDirectory = CreatePackageDirectory();
        var electricalSourcePath = CreateElectricalSourceFile();
        var expectedOutputPath = Path.Combine(packageDirectory, "floor-plan-adjusted-electrical.dxf");
        var packageHandler = new ExportMultiSheetPlanSetPackageHandler(
            projectionRepository,
            new FakePlanSheetSourceReader(
                new PlanSheetSourceDto(electricalSheetId, "ElectricalPlan", "Electrical", Guid.NewGuid(), electricalSourcePath)),
            CreateProjectedSheetHandler(projectionRepository, projectedSheetExporter, [(readyProjection, electricalSourcePath)]),
            new CreateMultiSheetExportAuditHandler(
                projectionRepository,
                new FakePlanSheetReader(planSetVersionId, [CreateSheet(electricalSheetId, "ElectricalPlan")]),
                new CapturingPlanSetExportRepository(),
                new CapturingPlanSetAuditEventRepository(),
                new CapturingPlanSetExportManifestWriter("exports/plan-sets/package/manifest.json"),
                new CapturingUnitOfWork(),
                new FakeClock(new DateTime(2026, 6, 30, 23, 59, 0, DateTimeKind.Utc))));

        var response = await packageHandler.HandleAsync(
            new ExportMultiSheetPlanSetPackageRequest(
                planSetVersionId,
                Guid.NewGuid(),
                canonicalAdjustmentId,
                CreateCanonicalPath(),
                packageDirectory,
                [])
            {
                RequireReadyElectricalPlan = true
            },
            CancellationToken.None);

        var exportCall = Assert.Single(projectedSheetExporter.Calls);
        Assert.Equal(electricalSourcePath, exportCall.SourceFilePath);
        Assert.True(File.Exists(expectedOutputPath));
        var electrical = Assert.Single(response.Sheets, sheet => sheet.SheetId == electricalSheetId);
        Assert.Equal("ProjectedAutomatically", electrical.Status);
    }

    [Fact]
    public async Task HandleAsync_auto_discovery_breaks_equal_timestamps_by_projection_id_before_assigning_suffixes()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalAdjustmentId = Guid.NewGuid();
        var firstSheetId = Guid.NewGuid();
        var secondSheetId = Guid.NewGuid();
        var timestamp = new DateTime(2026, 6, 30, 23, 58, 0, DateTimeKind.Utc);
        var selectedFirstId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var secondId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        var second = CreateProjection(
            planSetVersionId,
            canonicalAdjustmentId,
            SheetAdjustmentProjectionStatus.ReadyForExport,
            secondSheetId,
            timestamp,
            secondId);
        var selectedFirst = CreateProjection(
            planSetVersionId,
            canonicalAdjustmentId,
            SheetAdjustmentProjectionStatus.ReadyForExport,
            firstSheetId,
            timestamp,
            selectedFirstId);
        var supersededFirst = CreateProjection(
            planSetVersionId,
            canonicalAdjustmentId,
            SheetAdjustmentProjectionStatus.ReadyForExport,
            firstSheetId,
            timestamp,
            Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var projections = new FakeSheetAdjustmentProjectionRepository(second, selectedFirst, supersededFirst);
        var exporter = new CapturingProjectedPlanSheetExporter();
        var packageDirectory = CreatePackageDirectory();
        var firstSourcePath = CreateElectricalSourceFile();
        var secondSourcePath = CreateElectricalSourceFile();
        var handler = new ExportMultiSheetPlanSetPackageHandler(
            projections,
            new FakePlanSheetSourceReader(
                new PlanSheetSourceDto(firstSheetId, "ElectricalPlan", "Electrical A", Guid.NewGuid(), firstSourcePath),
                new PlanSheetSourceDto(secondSheetId, "ElectricalPlan", "Electrical B", Guid.NewGuid(), secondSourcePath)),
            CreateProjectedSheetHandler(
                projections,
                exporter,
                [(selectedFirst, firstSourcePath), (second, secondSourcePath)]),
            new CreateMultiSheetExportAuditHandler(
                projections,
                new FakePlanSheetReader(planSetVersionId,
                    [CreateSheet(firstSheetId, "ElectricalPlan"), CreateSheet(secondSheetId, "ElectricalPlan")]),
                new CapturingPlanSetExportRepository(),
                new CapturingPlanSetAuditEventRepository(),
                new CapturingPlanSetExportManifestWriter("manifest.json"),
                new CapturingUnitOfWork(),
                new FakeClock(DateTime.UtcNow)));

        var response = await handler.HandleAsync(
            new ExportMultiSheetPlanSetPackageRequest(
                planSetVersionId,
                Guid.NewGuid(),
                canonicalAdjustmentId,
                CreateCanonicalPath(),
                packageDirectory,
                []),
            CancellationToken.None);

        Assert.Equal(selectedFirstId, Assert.Single(response.Sheets, sheet => sheet.SheetId == firstSheetId).ProjectionId);
        Assert.Collection(
            exporter.Calls,
            call =>
            {
                Assert.Equal(firstSourcePath, call.SourceFilePath);
                Assert.Equal("floor-plan-adjusted-electrical.dxf", Path.GetFileName(call.OutputFilePath));
            },
            call =>
            {
                Assert.Equal(secondSourcePath, call.SourceFilePath);
                Assert.Equal("floor-plan-adjusted-electrical-2.dxf", Path.GetFileName(call.OutputFilePath));
            });
    }

    [Fact]
    public async Task HandleAsync_automatic_discovery_does_not_publish_when_electrical_export_requires_manual_review()
    {
        var projection = CreateProjection(Guid.NewGuid(), Guid.NewGuid());
        var packageDirectory = CreatePackageDirectory();
        var exportRepository = new CapturingPlanSetExportRepository();
        var exporter = new CapturingProjectedPlanSheetExporter
        {
            ManualReviewError = new ProjectedPlanSheetManualReviewRequiredException(
                "Electrical overlay contains unsupported geometry and requires manual review.")
        };
        var handler = CreatePackageHandler(
            projection,
            exporter,
            exportRepository,
            new CapturingPlanSetExportManifestWriter("unused/manifest.json"));
        var request = CreateRequest(projection, packageDirectory) with
        {
            DependentProjectionIds = []
        };

        var error = await Assert.ThrowsAsync<ProjectedPlanSheetManualReviewRequiredException>(() =>
            handler.HandleAsync(request, CancellationToken.None));

        Assert.Contains("unsupported geometry", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(Directory.Exists(packageDirectory));
        Assert.True(File.Exists(request.CanonicalFloorPlanExportPath));
        Assert.Empty(FindSiblingStagingDirectories(packageDirectory));
        var failure = AssertFailure(exportRepository, PlanSetExportFailureStage.DependentSheetGeneration);
        Assert.Contains("unsupported geometry", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleAsync_keeps_package_audit_when_projected_sheet_requires_manual_review_during_export()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var canonicalAdjustmentId = Guid.NewGuid();
        var projection = CreateProjection(planSetVersionId, canonicalAdjustmentId);
        var projectionRepository = new FakeSheetAdjustmentProjectionRepository(projection);
        var projectedSheetExporter = new CapturingProjectedPlanSheetExporter
        {
            ManualReviewError = new ProjectedPlanSheetManualReviewRequiredException(
                "CIRCLE crosses a canonical recipe pinch line.")
        };
        var electricalSourcePath = CreateElectricalSourceFile();
        var packageHandler = new ExportMultiSheetPlanSetPackageHandler(
            projectionRepository,
            new FakePlanSheetSourceReader(
                new PlanSheetSourceDto(
                    projection.DependentSheetId,
                    "ElectricalPlan",
                    "Electrical",
                    Guid.NewGuid(),
                    electricalSourcePath)),
            CreateProjectedSheetHandler(
                projectionRepository,
                projectedSheetExporter,
                [(projection, electricalSourcePath)],
                unitOfWork: new CapturingUnitOfWork()),
            new CreateMultiSheetExportAuditHandler(
                projectionRepository,
                new FakePlanSheetReader(),
                new CapturingPlanSetExportRepository(),
                new CapturingPlanSetAuditEventRepository(),
                new CapturingPlanSetExportManifestWriter("exports/plan-sets/package/manifest.json"),
                new CapturingUnitOfWork(),
                new FakeClock(new DateTime(2026, 6, 30, 23, 59, 0, DateTimeKind.Utc))));

        var response = await packageHandler.HandleAsync(
            new ExportMultiSheetPlanSetPackageRequest(
                planSetVersionId,
                canonicalFloorPlanVersionId,
                canonicalAdjustmentId,
                CreateCanonicalPath(),
                CreatePackageDirectory(),
                [projection.Id]),
            CancellationToken.None);

        var dependentSheet = Assert.Single(response.Sheets, sheet => sheet.ProjectionId == projection.Id);
        Assert.Equal("RequiresManualConfirmation", dependentSheet.Status);
        Assert.Null(dependentSheet.StoragePath);
        Assert.Contains("pinch line", dependentSheet.Warning, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("RequiresManualConfirmation", response.Status);
    }

    [Fact]
    public async Task HandleAsync_audits_explicit_manual_projection_without_trying_to_export_it()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var canonicalAdjustmentId = Guid.NewGuid();
        var projection = CreateProjection(
            planSetVersionId,
            canonicalAdjustmentId,
            SheetAdjustmentProjectionStatus.RequiresManualConfirmation,
            Guid.NewGuid());
        var projectionRepository = new FakeSheetAdjustmentProjectionRepository(projection);
        var projectedSheetExporter = new CapturingProjectedPlanSheetExporter();
        var packageHandler = new ExportMultiSheetPlanSetPackageHandler(
            projectionRepository,
            new FakePlanSheetSourceReader(
                new PlanSheetSourceDto(
                    projection.DependentSheetId,
                    "ElectricalPlan",
                    "Electrical",
                    Guid.NewGuid(),
                    "library/raw-dxf/electrical.dxf")),
            new ExportProjectedPlanSheetHandler(projectionRepository, projectedSheetExporter),
            new CreateMultiSheetExportAuditHandler(
                projectionRepository,
                new FakePlanSheetReader(),
                new CapturingPlanSetExportRepository(),
                new CapturingPlanSetAuditEventRepository(),
                new CapturingPlanSetExportManifestWriter("exports/plan-sets/package/manifest.json"),
                new CapturingUnitOfWork(),
                new FakeClock(new DateTime(2026, 6, 30, 23, 59, 0, DateTimeKind.Utc))));

        var response = await packageHandler.HandleAsync(
            new ExportMultiSheetPlanSetPackageRequest(
                planSetVersionId,
                canonicalFloorPlanVersionId,
                canonicalAdjustmentId,
                CreateCanonicalPath(),
                CreatePackageDirectory(),
                [projection.Id]),
            CancellationToken.None);

        Assert.Empty(projectedSheetExporter.Calls);
        var dependentSheet = Assert.Single(response.Sheets, sheet => sheet.ProjectionId == projection.Id);
        Assert.Equal("RequiresManualConfirmation", dependentSheet.Status);
        Assert.Null(dependentSheet.StoragePath);
        Assert.Equal("RequiresManualConfirmation", response.Status);
    }

    [Fact]
    public async Task HandleAsync_rejects_explicit_projection_from_another_canonical_adjustment_before_export()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var requestedCanonicalAdjustmentId = Guid.NewGuid();
        var projection = CreateProjection(planSetVersionId, Guid.NewGuid());
        var projectionRepository = new FakeSheetAdjustmentProjectionRepository(projection);
        var projectedSheetExporter = new CapturingProjectedPlanSheetExporter();
        var packageHandler = new ExportMultiSheetPlanSetPackageHandler(
            projectionRepository,
            new FakePlanSheetSourceReader(
                new PlanSheetSourceDto(
                    projection.DependentSheetId,
                    "ElectricalPlan",
                    "Electrical",
                    Guid.NewGuid(),
                    "library/raw-dxf/electrical.dxf")),
            new ExportProjectedPlanSheetHandler(projectionRepository, projectedSheetExporter),
            new CreateMultiSheetExportAuditHandler(
                projectionRepository,
                new FakePlanSheetReader(),
                new CapturingPlanSetExportRepository(),
                new CapturingPlanSetAuditEventRepository(),
                new CapturingPlanSetExportManifestWriter("exports/plan-sets/package/manifest.json"),
                new CapturingUnitOfWork(),
                new FakeClock(new DateTime(2026, 6, 30, 23, 59, 0, DateTimeKind.Utc))));

        await Assert.ThrowsAsync<ArgumentException>(() => packageHandler.HandleAsync(
            new ExportMultiSheetPlanSetPackageRequest(
                planSetVersionId,
                canonicalFloorPlanVersionId,
                requestedCanonicalAdjustmentId,
                CreateCanonicalPath(),
                CreatePackageDirectory(),
                [projection.Id]),
            CancellationToken.None));

        Assert.Empty(projectedSheetExporter.Calls);
    }

    [Fact]
    public async Task HandleAsync_rejects_explicit_projection_from_another_plan_set_before_export()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var canonicalAdjustmentId = Guid.NewGuid();
        var projection = CreateProjection(Guid.NewGuid(), canonicalAdjustmentId);
        var projectionRepository = new FakeSheetAdjustmentProjectionRepository(projection);
        var projectedSheetExporter = new CapturingProjectedPlanSheetExporter();
        var packageHandler = new ExportMultiSheetPlanSetPackageHandler(
            projectionRepository,
            new FakePlanSheetSourceReader(
                new PlanSheetSourceDto(
                    projection.DependentSheetId,
                    "ElectricalPlan",
                    "Electrical",
                    Guid.NewGuid(),
                    "library/raw-dxf/electrical.dxf")),
            new ExportProjectedPlanSheetHandler(projectionRepository, projectedSheetExporter),
            new CreateMultiSheetExportAuditHandler(
                projectionRepository,
                new FakePlanSheetReader(),
                new CapturingPlanSetExportRepository(),
                new CapturingPlanSetAuditEventRepository(),
                new CapturingPlanSetExportManifestWriter("exports/plan-sets/package/manifest.json"),
                new CapturingUnitOfWork(),
                new FakeClock(new DateTime(2026, 6, 30, 23, 59, 0, DateTimeKind.Utc))));

        await Assert.ThrowsAsync<ArgumentException>(() => packageHandler.HandleAsync(
            new ExportMultiSheetPlanSetPackageRequest(
                planSetVersionId,
                canonicalFloorPlanVersionId,
                canonicalAdjustmentId,
                CreateCanonicalPath(),
                CreatePackageDirectory(),
                [projection.Id]),
            CancellationToken.None));

        Assert.Empty(projectedSheetExporter.Calls);
    }

    [Fact]
    public async Task HandleAsync_exporter_failure_cleans_staging_and_persists_failed()
    {
        var projection = CreateProjection(Guid.NewGuid(), Guid.NewGuid());
        var packageDirectory = CreatePackageDirectory();
        var exportRepository = new CapturingPlanSetExportRepository();
        var exporter = new CapturingProjectedPlanSheetExporter
        {
            Failure = new IOException("Synthetic dependent-sheet exporter failure.")
        };
        var handler = CreatePackageHandler(
            projection,
            exporter,
            exportRepository,
            new CapturingPlanSetExportManifestWriter("unused/manifest.json"));

        var request = CreateRequest(projection, packageDirectory);
        var error = await Assert.ThrowsAsync<IOException>(() => handler.HandleAsync(
            request,
            CancellationToken.None));

        Assert.Contains("exporter failure", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(Directory.Exists(packageDirectory));
        Assert.True(File.Exists(request.CanonicalFloorPlanExportPath));
        Assert.Empty(FindSiblingStagingDirectories(packageDirectory));
        var failure = AssertFailure(exportRepository, PlanSetExportFailureStage.DependentSheetGeneration);
        Assert.False(failure.Canceled);
    }

    [Fact]
    public async Task HandleAsync_manifest_writer_failure_rolls_back_package_and_persists_failed()
    {
        var projection = CreateProjection(Guid.NewGuid(), Guid.NewGuid());
        var packageDirectory = CreatePackageDirectory();
        var exportRepository = new CapturingPlanSetExportRepository();
        var writer = new CapturingPlanSetExportManifestWriter("unused/manifest.json")
        {
            WriteError = new IOException("Synthetic workspace writer failure.")
        };
        var handler = CreatePackageHandler(
            projection,
            new CapturingProjectedPlanSheetExporter(),
            exportRepository,
            writer);

        var request = CreateRequest(projection, packageDirectory);
        var error = await Assert.ThrowsAsync<IOException>(() => handler.HandleAsync(
            request,
            CancellationToken.None));

        Assert.Contains("writer failure", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(Directory.Exists(packageDirectory));
        Assert.True(File.Exists(request.CanonicalFloorPlanExportPath));
        Assert.Empty(FindSiblingStagingDirectories(packageDirectory));
        var failure = AssertFailure(
            exportRepository,
            PlanSetExportFailureStage.VerificationAndWorkspacePublication);
        Assert.False(failure.Canceled);
    }

    [Fact]
    public async Task HandleAsync_package_artifact_failure_rolls_back_staging_and_preserves_owned_scratch()
    {
        var projection = CreateProjection(Guid.NewGuid(), Guid.NewGuid());
        var packageDirectory = CreatePackageDirectory();
        var exportRepository = new CapturingPlanSetExportRepository();
        var writer = new CapturingPlanSetExportManifestWriter("exports/plan-sets/package/manifest.json")
        {
            PackageWriteError = new IOException("Synthetic package artifact failure.")
        };
        var handler = CreatePackageHandler(
            projection,
            new CapturingProjectedPlanSheetExporter(),
            exportRepository,
            writer);

        var request = CreateRequest(projection, packageDirectory) with
        {
            DeleteCanonicalSourceAfterSuccess = true
        };
        var error = await Assert.ThrowsAsync<IOException>(() => handler.HandleAsync(
            request,
            CancellationToken.None));

        Assert.Contains("artifact failure", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(Directory.Exists(packageDirectory));
        Assert.True(File.Exists(request.CanonicalFloorPlanExportPath));
        Assert.Empty(FindSiblingStagingDirectories(packageDirectory));
        AssertFailure(exportRepository, PlanSetExportFailureStage.UserPackagePublication);
    }

    [Fact]
    public async Task HandleAsync_commit_failure_rolls_back_final_package_and_preserves_owned_scratch()
    {
        var projection = CreateProjection(Guid.NewGuid(), Guid.NewGuid());
        var packageDirectory = CreatePackageDirectory();
        var request = CreateRequest(projection, packageDirectory) with
        {
            DeleteCanonicalSourceAfterSuccess = true
        };
        var canonicalRepository = new CapturingCanonicalFloorPlanAdjustmentRepository(
            request.CanonicalFloorPlanExportPath);
        var unitOfWork = new FailingThenCapturingUnitOfWork(canonicalRepository);
        var handler = CreatePackageHandler(
            projection,
            new CapturingProjectedPlanSheetExporter(),
            new CapturingPlanSetExportRepository(),
            new CapturingPlanSetExportManifestWriter("exports/package/manifest.json"),
            canonicalRepository,
            unitOfWork);

        await Assert.ThrowsAsync<IOException>(() => handler.HandleAsync(request, CancellationToken.None));

        Assert.Equal(
            (request.CanonicalAdjustmentId, Path.Combine(packageDirectory, "floor-plan-adjusted-floorplan.dxf")),
            Assert.Single(canonicalRepository.Updates));
        Assert.False(Directory.Exists(packageDirectory));
        Assert.True(File.Exists(request.CanonicalFloorPlanExportPath));
        Assert.Equal(1, unitOfWork.RollbackCount);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        Assert.Equal(request.CanonicalFloorPlanExportPath, canonicalRepository.PersistedExportPath);
    }

    [Fact]
    public async Task HandleAsync_cancellation_cleans_staging_and_persists_failed()
    {
        var projection = CreateProjection(Guid.NewGuid(), Guid.NewGuid());
        var packageDirectory = CreatePackageDirectory();
        var exportRepository = new CapturingPlanSetExportRepository();
        var exporter = new CapturingProjectedPlanSheetExporter
        {
            Failure = new OperationCanceledException("Synthetic export cancellation.")
        };
        var handler = CreatePackageHandler(
            projection,
            exporter,
            exportRepository,
            new CapturingPlanSetExportManifestWriter("unused/manifest.json"));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.HandleAsync(
            CreateRequest(projection, packageDirectory),
            CancellationToken.None));

        Assert.False(Directory.Exists(packageDirectory));
        Assert.Empty(FindSiblingStagingDirectories(packageDirectory));
        var failure = AssertFailure(exportRepository, PlanSetExportFailureStage.DependentSheetGeneration);
        Assert.True(failure.Canceled);
    }

    [Fact]
    public async Task HandleAsync_existing_final_package_is_never_overwritten_or_deleted()
    {
        var projection = CreateProjection(Guid.NewGuid(), Guid.NewGuid());
        var packageDirectory = CreatePackageDirectory();
        Directory.CreateDirectory(packageDirectory);
        var sentinelPath = Path.Combine(packageDirectory, "existing.txt");
        File.WriteAllText(sentinelPath, "keep me");
        var exportRepository = new CapturingPlanSetExportRepository();
        var exporter = new CapturingProjectedPlanSheetExporter();
        var handler = CreatePackageHandler(
            projection,
            exporter,
            exportRepository,
            new CapturingPlanSetExportManifestWriter("unused/manifest.json"));

        var request = CreateRequest(projection, packageDirectory);
        await Assert.ThrowsAsync<IOException>(() => handler.HandleAsync(
            request,
            CancellationToken.None));

        Assert.Equal("keep me", File.ReadAllText(sentinelPath));
        Assert.True(File.Exists(request.CanonicalFloorPlanExportPath));
        Assert.Empty(exporter.Calls);
        AssertFailure(exportRepository, PlanSetExportFailureStage.UserPackagePublication);
    }

    private ExportMultiSheetPlanSetPackageHandler CreatePackageHandler(
        SheetAdjustmentProjection projection,
        IProjectedPlanSheetExporter exporter,
        CapturingPlanSetExportRepository exportRepository,
        IPlanSetExportManifestWriter manifestWriter,
        ICanonicalFloorPlanAdjustmentRepository? canonicalRepository = null,
        IUnitOfWork? unitOfWork = null)
    {
        var projectionRepository = new FakeSheetAdjustmentProjectionRepository(projection);
        var electricalSourcePath = CreateElectricalSourceFile();
        return new ExportMultiSheetPlanSetPackageHandler(
            projectionRepository,
            new FakePlanSheetSourceReader(
                new PlanSheetSourceDto(
                    projection.DependentSheetId,
                    "ElectricalPlan",
                    "Electrical",
                    Guid.NewGuid(),
                    electricalSourcePath)),
            CreateProjectedSheetHandler(projectionRepository, exporter, [(projection, electricalSourcePath)]),
            new CreateMultiSheetExportAuditHandler(
                projectionRepository,
                new FakePlanSheetReader(),
                exportRepository,
                new CapturingPlanSetAuditEventRepository(),
                manifestWriter,
                unitOfWork ?? new CapturingUnitOfWork(),
                new FakeClock(new DateTime(2026, 6, 30, 23, 59, 0, DateTimeKind.Utc)),
                canonicalFloorPlanAdjustmentRepository: canonicalRepository));
    }

    private ExportMultiSheetPlanSetPackageRequest CreateRequest(
        SheetAdjustmentProjection projection,
        string packageDirectory)
        => new(
            projection.PlanSetVersionId,
            Guid.NewGuid(),
            projection.CanonicalAdjustmentId,
            CreateCanonicalPath(),
            packageDirectory,
            [projection.Id]);

    private static PlanSetExportFailureDto AssertFailure(
        CapturingPlanSetExportRepository repository,
        PlanSetExportFailureStage expectedStage)
    {
        var export = Assert.Single(repository.Items);
        Assert.Equal(PlanSetExportStatus.Failed, export.Status);
        Assert.Null(export.PackageManifestPath);
        var failure = JsonSerializer.Deserialize<PlanSetExportFailureDto>(export.ConfidenceSummaryJson);
        Assert.NotNull(failure);
        Assert.Equal(PlanSetExportFailureDto.CurrentSchemaVersion, failure.SchemaVersion);
        Assert.Equal(expectedStage, failure.Stage);
        return failure;
    }

    private string CreateCanonicalPath()
    {
        Directory.CreateDirectory(tempRoot);
        var path = Path.Combine(tempRoot, "floor-plan-adjusted.dxf");
        File.WriteAllText(path, "canonical-source");
        return path;
    }

    private string CreatePackageDirectory()
    {
        Directory.CreateDirectory(tempRoot);
        return Path.Combine(tempRoot, $"package-{Guid.NewGuid():N}");
    }

    private string CreateElectricalSourceFile()
    {
        Directory.CreateDirectory(tempRoot);
        var path = Path.Combine(tempRoot, $"electrical-source-{Guid.NewGuid():N}.dxf");
        File.WriteAllText(path, "electrical-source");
        return path;
    }

    private static string ComputeSha256(string path)
        => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

    private static SheetRegistration CreateConfirmedRegistration(
        SheetAdjustmentProjection projection,
        string sourceFilePath)
    {
        var canonicalFloorPlanVersionId = projection.PlanSetVersionId;
        return new(
            projection.SheetRegistrationId,
            projection.PlanSetVersionId,
            projection.DependentSheetId,
            canonicalFloorPlanVersionId,
            SheetRegistrationMethod.WholeSheetSimilarity,
            new SheetRegistrationTransform(1m, 0m, 0m, 0m),
            0.9m,
            SheetRegistrationStatus.Confirmed,
            new DateTime(2026, 7, 4, 1, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 4, 1, 5, 0, DateTimeKind.Utc),
            warning: null,
            wholePlanRegistrationProof: new WholePlanRegistrationProof(
                WholePlanRegistrationProof.CurrentVersion,
                Passed: true,
                canonicalFloorPlanVersionId,
                projection.DependentSheetId,
                new string('a', 64),
                ComputeSha256(sourceFilePath),
                HorizontalCoverage: 0.94m,
                VerticalCoverage: 0.91m,
                RootMeanSquareResidual: 0.01m,
                MaximumResidual: 0.02m));
    }

    private static CanonicalFloorPlanAdjustment CreateExportCanonicalAdjustment(
        SheetAdjustmentProjection projection)
        => new(
            projection.CanonicalAdjustmentId,
            projection.PlanSetVersionId,
            projection.PlanSetVersionId,
            "site.dxf",
            "floor.dxf",
            "{}",
            JsonSerializer.Serialize(new AdjustmentRecipeSummaryDto("v1", 1m, 0m, 0m, [])),
            new DateTime(2026, 7, 4, 2, 0, 0, DateTimeKind.Utc));

    private static ExportProjectedPlanSheetHandler CreateProjectedSheetHandler(
        FakeSheetAdjustmentProjectionRepository projectionRepository,
        IProjectedPlanSheetExporter exporter,
        IReadOnlyList<(SheetAdjustmentProjection Projection, string SourceFilePath)> evidence,
        IUnitOfWork? unitOfWork = null)
        => new(
            projectionRepository,
            exporter,
            new FakeSheetRegistrationRepository(evidence
                .Select(item => CreateConfirmedRegistration(item.Projection, item.SourceFilePath))
                .ToArray()),
            new FakeExportCanonicalAdjustmentRepository(evidence
                .Select(item => CreateExportCanonicalAdjustment(item.Projection))
                .DistinctBy(adjustment => adjustment.Id)
                .ToArray()),
            unitOfWork);

    private sealed class FakeSheetRegistrationRepository : ISheetRegistrationRepository
    {
        private readonly Dictionary<Guid, SheetRegistration> registrations;

        public FakeSheetRegistrationRepository(params SheetRegistration[] registrations)
        {
            this.registrations = registrations.ToDictionary(item => item.Id);
        }

        public Task AddAsync(SheetRegistration registration, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<SheetRegistration?> GetByIdAsync(Guid registrationId, CancellationToken cancellationToken)
        {
            registrations.TryGetValue(registrationId, out var registration);
            return Task.FromResult(registration);
        }
    }

    private sealed class FakeExportCanonicalAdjustmentRepository : ICanonicalFloorPlanAdjustmentRepository
    {
        private readonly Dictionary<Guid, CanonicalFloorPlanAdjustment> adjustments;

        public FakeExportCanonicalAdjustmentRepository(params CanonicalFloorPlanAdjustment[] adjustments)
        {
            this.adjustments = adjustments.ToDictionary(item => item.Id);
        }

        public Task AddAsync(CanonicalFloorPlanAdjustment adjustment, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<CanonicalFloorPlanAdjustment?> GetByIdAsync(Guid adjustmentId, CancellationToken cancellationToken)
        {
            adjustments.TryGetValue(adjustmentId, out var adjustment);
            return Task.FromResult(adjustment);
        }
    }

    private static IReadOnlyList<string> FindSiblingStagingDirectories(string packageDirectory)
    {
        var parent = Path.GetDirectoryName(packageDirectory);
        if (string.IsNullOrWhiteSpace(parent) || !Directory.Exists(parent))
        {
            return [];
        }

        return Directory.GetDirectories(
            parent,
            $".{Path.GetFileName(packageDirectory)}.staging-*",
            SearchOption.TopDirectoryOnly);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static SheetAdjustmentProjection CreateProjection(Guid planSetVersionId, Guid canonicalAdjustmentId)
    {
        return CreateProjection(
            planSetVersionId,
            canonicalAdjustmentId,
            SheetAdjustmentProjectionStatus.ReadyForExport,
            Guid.NewGuid());
    }

    private static SheetAdjustmentProjection CreateProjection(
        Guid planSetVersionId,
        Guid canonicalAdjustmentId,
        SheetAdjustmentProjectionStatus status,
        Guid dependentSheetId,
        DateTime? createdAtUtc = null,
        Guid? projectionId = null)
    {
        return new SheetAdjustmentProjection(
            projectionId ?? Guid.NewGuid(),
            planSetVersionId,
            dependentSheetId,
            Guid.NewGuid(),
            canonicalAdjustmentId,
            SheetAdjustmentProjectionMethod.ElectricalWholeSheetSimilarity,
            new SheetAdjustmentProjectionTransform(1m, 0m, 10m, 20m),
            confidence: status is SheetAdjustmentProjectionStatus.ReadyForExport ? 0.9m : 0.6m,
            status,
            warning: status is SheetAdjustmentProjectionStatus.ReadyForExport ? null : "Needs review",
            canonicalCompressionStepCount: 0,
            createdAtUtc: createdAtUtc ?? new DateTime(2026, 6, 30, 23, 58, 0, DateTimeKind.Utc));
    }

    private static PlanSetSheetDto CreateSheet(Guid sheetId, string sheetType)
    {
        return new PlanSetSheetDto(
            sheetId,
            sheetType,
            sheetType,
            Guid.NewGuid(),
            null,
            IsCanonical: false,
            "Registered",
            "NotProjected");
    }

    private sealed class FakeSheetAdjustmentProjectionRepository : ISheetAdjustmentProjectionRepository
    {
        private readonly Dictionary<Guid, SheetAdjustmentProjection> projections;

        public FakeSheetAdjustmentProjectionRepository(params SheetAdjustmentProjection[] projections)
        {
            this.projections = projections.ToDictionary(item => item.Id);
        }

        public Task AddAsync(SheetAdjustmentProjection projection, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<SheetAdjustmentProjection?> GetByIdAsync(Guid projectionId, CancellationToken cancellationToken)
        {
            projections.TryGetValue(projectionId, out var projection);
            return Task.FromResult(projection);
        }

        public Task<IReadOnlyList<SheetAdjustmentProjection>> ListByPlanSetVersionAndCanonicalAdjustmentAsync(
            Guid planSetVersionId,
            Guid canonicalAdjustmentId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<SheetAdjustmentProjection>>(
                projections.Values
                    .Where(item => item.PlanSetVersionId == planSetVersionId &&
                                   item.CanonicalAdjustmentId == canonicalAdjustmentId)
                    .ToArray());
        }

        public Task UpdateAsync(SheetAdjustmentProjection projection, CancellationToken cancellationToken)
        {
            projections[projection.Id] = projection;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePlanSheetReader : IPlanSheetReader
    {
        private readonly Guid planSetVersionId;
        private readonly IReadOnlyList<PlanSetSheetDto> sheets;

        public FakePlanSheetReader()
            : this(Guid.NewGuid(), [])
        {
        }

        public FakePlanSheetReader(Guid planSetVersionId, IReadOnlyList<PlanSetSheetDto> sheets)
        {
            this.planSetVersionId = planSetVersionId;
            this.sheets = sheets;
        }

        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<PlanSetSheetDto>>> ListByPlanSetVersionIdsAsync(
            IReadOnlyCollection<Guid> planSetVersionIds,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<PlanSetSheetDto>>>(
                planSetVersionIds.Contains(planSetVersionId)
                    ? new Dictionary<Guid, IReadOnlyList<PlanSetSheetDto>>
                    {
                        [planSetVersionId] = sheets
                    }
                    : new Dictionary<Guid, IReadOnlyList<PlanSetSheetDto>>());
        }
    }

    private sealed class FakePlanSheetSourceReader : IPlanSheetSourceReader
    {
        private readonly IReadOnlyDictionary<Guid, PlanSheetSourceDto> sources;

        public FakePlanSheetSourceReader(params PlanSheetSourceDto[] sources)
        {
            this.sources = sources.ToDictionary(source => source.SheetId);
        }

        public Task<PlanSheetSourceDto?> GetBySheetIdAsync(Guid sheetId, CancellationToken cancellationToken)
        {
            sources.TryGetValue(sheetId, out var source);
            return Task.FromResult<PlanSheetSourceDto?>(source);
        }
    }

    private sealed class CapturingProjectedPlanSheetExporter : IProjectedPlanSheetExporter
    {
        public List<Call> Calls { get; } = [];

        public ProjectedPlanSheetManualReviewRequiredException? ManualReviewError { get; init; }

        public Exception? Failure { get; init; }

        public Task ExportAsync(
            string sourceFilePath,
            string outputFilePath,
            SheetAdjustmentProjectionTransform transform,
            CancellationToken cancellationToken)
        {
            Calls.Add(new Call(sourceFilePath, outputFilePath));
            if (Failure is not null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath)!);
                File.WriteAllText(outputFilePath, "partial");
                throw Failure;
            }

            if (ManualReviewError is not null)
            {
                throw ManualReviewError;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath)!);
            File.WriteAllText(outputFilePath, "complete");
            return Task.CompletedTask;
        }

        public sealed record Call(string SourceFilePath, string OutputFilePath);
    }

    private sealed class CapturingPlanSetExportRepository : IPlanSetExportRepository
    {
        public List<PlanSetExport> Items { get; } = [];

        public Action<PlanSetExport>? OnAdd { get; init; }

        public Task AddAsync(PlanSetExport export, CancellationToken cancellationToken)
        {
            OnAdd?.Invoke(export);
            Items.Add(export);
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingPlanSetAuditEventRepository : IPlanSetAuditEventRepository
    {
        public Task AddAsync(PlanSetAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingCanonicalFloorPlanAdjustmentRepository : ICanonicalFloorPlanAdjustmentRepository
    {
        public CapturingCanonicalFloorPlanAdjustmentRepository(string? persistedExportPath = null)
        {
            PersistedExportPath = persistedExportPath;
        }

        public List<(Guid AdjustmentId, string ExportPath)> Updates { get; } = [];

        public string? PersistedExportPath { get; private set; }

        public string? PendingExportPath { get; private set; }

        public Task AddAsync(CanonicalFloorPlanAdjustment adjustment, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task UpdateExportPathAsync(Guid adjustmentId, string finalPath, CancellationToken cancellationToken)
        {
            Updates.Add((adjustmentId, finalPath));
            PendingExportPath = finalPath;
            return Task.CompletedTask;
        }

        public void Commit()
        {
            if (PendingExportPath is not null)
            {
                PersistedExportPath = PendingExportPath;
                PendingExportPath = null;
            }
        }

        public void Rollback()
        {
            PendingExportPath = null;
        }
    }

    private sealed class CapturingPlanSetExportManifestWriter : IPlanSetExportManifestWriter
    {
        private readonly string manifestPath;

        public CapturingPlanSetExportManifestWriter(string manifestPath)
        {
            this.manifestPath = manifestPath;
        }

        public Exception? WriteError { get; init; }

        public Exception? PackageWriteError { get; init; }

        public Action<MultiSheetExportAuditDto>? OnBuildVerification { get; init; }

        public Action<MultiSheetExportAuditDto>? OnWrite { get; init; }

        public Action<MultiSheetExportAuditDto, string>? OnPackageWrite { get; init; }

        public PlanSetVerificationReportDto BuildVerificationReport(MultiSheetExportAuditDto audit)
        {
            OnBuildVerification?.Invoke(audit);
            return BuildSyntheticVerification(audit);
        }

        public Task<string> WriteAsync(MultiSheetExportAuditDto audit, CancellationToken cancellationToken)
        {
            OnWrite?.Invoke(audit);
            if (WriteError is not null)
            {
                throw WriteError;
            }

            return Task.FromResult(manifestPath);
        }

        public async Task WritePackageArtifactsAsync(
            MultiSheetExportAuditDto audit,
            string stagingDirectory,
            CancellationToken cancellationToken)
        {
            if (PackageWriteError is not null)
            {
                throw PackageWriteError;
            }

            OnPackageWrite?.Invoke(audit, stagingDirectory);

            var comparison = Assert.Single(audit.Artifacts, artifact => artifact.Role == "Comparison");
            var humanAudit = Assert.Single(audit.Artifacts, artifact => artifact.Role == "Audit");
            var manifest = Assert.Single(audit.Artifacts, artifact => artifact.Role == "Manifest");
            await File.WriteAllTextAsync(
                Path.Combine(stagingDirectory, Path.GetFileName(comparison.Path)),
                JsonSerializer.Serialize(new
                {
                    stage = "final-output-congruence",
                    sheets = audit.Sheets
                        .Where(sheet => sheet.SheetKind.Contains("Electrical", StringComparison.OrdinalIgnoreCase))
                        .Select(sheet => new
                        {
                            finalOutputCongruence = new
                            {
                                FloorOutputPath = audit.Sheets
                                    .Single(item => item.SheetKind == "CanonicalFloorPlan")
                                    .StoragePath,
                                ElectricalOutputPath = sheet.StoragePath
                            }
                        })
                }),
                cancellationToken);
            await File.WriteAllTextAsync(
                Path.Combine(stagingDirectory, Path.GetFileName(humanAudit.Path)),
                string.Join(Environment.NewLine, audit.HumanSummary),
                cancellationToken);
            await File.WriteAllTextAsync(
                Path.Combine(stagingDirectory, Path.GetFileName(manifest.Path)),
                JsonSerializer.Serialize(audit with { PackageManifestPath = manifest.Path }),
                cancellationToken);
        }
    }

    private static PlanSetVerificationReportDto BuildSyntheticVerification(MultiSheetExportAuditDto audit)
    {
        var dependentCount = Math.Max(0, audit.Sheets.Count - 1);
        var green = audit.Summary.ManualConfirmationRequiredSheetCount == 0;
        var check = new PlanSetVerificationCheckDto(
            green ? PlanSetVerificationCheckStatus.Passed : PlanSetVerificationCheckStatus.InsufficientData,
            dependentCount,
            green ? dependentCount : 0,
            0,
            green ? 0 : dependentCount);
        return new PlanSetVerificationReportDto(
            PlanSetVerificationReportDto.CurrentSchemaVersion,
            new PlanSetVerificationOutputDto(
                audit.Sheets.Count,
                green ? audit.Sheets.Count : Math.Max(0, audit.Sheets.Count - 1),
                green ? [] : [audit.Sheets.Last().SheetId]),
            check,
            new PlanSetVerificationOperationDto(0, 0, 0, 0),
            new PlanSetVerificationOperationDto(0, 0, 0, 0),
            check,
            check,
            check,
            check,
            green
                ? []
                : [new PlanSetVerificationReasonDto(
                    PlanSetVerificationReasonCode.MissingRequiredEvidence,
                    "test-double",
                    null,
                    "Synthetic blocked verification for an existing package test.")]);
    }

    private sealed class CapturingUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FailingThenCapturingUnitOfWork : IUnitOfWork
    {
        private readonly CapturingCanonicalFloorPlanAdjustmentRepository repository;
        private bool failNextSave = true;

        public FailingThenCapturingUnitOfWork(CapturingCanonicalFloorPlanAdjustmentRepository repository)
        {
            this.repository = repository;
        }

        public int RollbackCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            if (failNextSave)
            {
                failNextSave = false;
                throw new IOException("Synthetic transaction commit failure.");
            }

            repository.Commit();
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken)
        {
            RollbackCount++;
            repository.Rollback();
            return Task.CompletedTask;
        }
    }

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
