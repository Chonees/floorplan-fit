using System.Text.Json;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Export;
using FloorplanFit.Application.PlanSets.ExportAudit;
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
        var handler = new ExportMultiSheetPlanSetPackageHandler(
            projectionRepository,
            new FakePlanSheetSourceReader(new PlanSheetSourceDto(
                projection.DependentSheetId,
                "ElectricalPlan",
                "Electrical",
                Guid.NewGuid(),
                "library/raw-dxf/electrical.dxf")),
            new ExportProjectedPlanSheetHandler(projectionRepository, new CapturingProjectedPlanSheetExporter()),
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
            ["My-unsafe-plan-electrical.dxf", "My-unsafe-plan-floorplan.dxf"],
            Directory.GetFiles(packageDirectory, "*.dxf").Select(Path.GetFileName).Order().ToArray());
        Assert.All(Directory.GetFiles(packageDirectory, "*.dxf"), path => Assert.True(new FileInfo(path).Length > 0));
        Assert.False(File.Exists(canonicalPath));
        Assert.NotNull(verificationInput);
        var canonicalSheet = Assert.Single(verificationInput.Sheets, sheet => sheet.SheetKind == "CanonicalFloorPlan");
        Assert.Equal(Path.Combine(packageDirectory, "My-unsafe-plan-floorplan.dxf"), canonicalSheet.StoragePath);
        Assert.Contains(".staging-", canonicalSheet.VerificationPath, StringComparison.Ordinal);
        Assert.EndsWith("My-unsafe-plan-floorplan.dxf", canonicalSheet.VerificationPath, StringComparison.Ordinal);
        Assert.Equal(
            ["ElectricalPlan", "FloorPlan"],
            response.Artifacts.Select(artifact => artifact.Role).Order().ToArray());
        Assert.DoesNotContain(response.Artifacts, artifact => artifact.Role == "ComparisonReview");
        Assert.All(response.Artifacts, artifact => Assert.StartsWith(packageDirectory, artifact.Path, StringComparison.Ordinal));
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
        var handler = new ExportMultiSheetPlanSetPackageHandler(
            projections,
            new FakePlanSheetSourceReader(
                new PlanSheetSourceDto(first.DependentSheetId, "ElectricalPlan", "Electrical A", Guid.NewGuid(), "a.dxf"),
                new PlanSheetSourceDto(second.DependentSheetId, "ElectricalPlan", "Electrical B", Guid.NewGuid(), "b.dxf")),
            new ExportProjectedPlanSheetHandler(projections, new CapturingProjectedPlanSheetExporter()),
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
        var expectedOutputPath = Path.Combine(packageDirectory, "floor-plan-adjusted-electrical.dxf");
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
        Assert.Equal("library/raw-dxf/electrical.dxf", exportCall.SourceFilePath);
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
        var packageHandler = new ExportMultiSheetPlanSetPackageHandler(
            projectionRepository,
            new FakePlanSheetSourceReader(
                new PlanSheetSourceDto(
                    projection.DependentSheetId,
                    "ElectricalPlan",
                    "Electrical",
                    Guid.NewGuid(),
                    "library/raw-dxf/electrical.dxf")),
            new ExportProjectedPlanSheetHandler(projectionRepository, new CapturingProjectedPlanSheetExporter()),
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

        Assert.Equal(new[] { "verify", "manifest", "persist" }, order);
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
        var expectedOutputPath = Path.Combine(packageDirectory, "floor-plan-adjusted-electrical.dxf");
        var packageHandler = new ExportMultiSheetPlanSetPackageHandler(
            projectionRepository,
            new FakePlanSheetSourceReader(
                new PlanSheetSourceDto(electricalSheetId, "ElectricalPlan", "Electrical", Guid.NewGuid(), "library/electrical.dxf")),
            new ExportProjectedPlanSheetHandler(projectionRepository, projectedSheetExporter),
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
        Assert.Equal("library/electrical.dxf", exportCall.SourceFilePath);
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
    public async Task HandleAsync_without_explicit_projection_ids_uses_latest_projection_per_sheet()
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
        var handler = new ExportMultiSheetPlanSetPackageHandler(
            projections,
            new FakePlanSheetSourceReader(
                new PlanSheetSourceDto(firstSheetId, "ElectricalPlan", "Electrical A", Guid.NewGuid(), "a.dxf"),
                new PlanSheetSourceDto(secondSheetId, "ElectricalPlan", "Electrical B", Guid.NewGuid(), "b.dxf")),
            new ExportProjectedPlanSheetHandler(projections, exporter),
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
                Assert.Equal("a.dxf", call.SourceFilePath);
                Assert.Equal("floor-plan-adjusted-electrical.dxf", Path.GetFileName(call.OutputFilePath));
            },
            call =>
            {
                Assert.Equal("b.dxf", call.SourceFilePath);
                Assert.Equal("floor-plan-adjusted-electrical-2.dxf", Path.GetFileName(call.OutputFilePath));
            });
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
        var packageHandler = new ExportMultiSheetPlanSetPackageHandler(
            projectionRepository,
            new FakePlanSheetSourceReader(
                new PlanSheetSourceDto(
                    projection.DependentSheetId,
                    "ElectricalPlan",
                    "Electrical",
                    Guid.NewGuid(),
                    "library/raw-dxf/electrical.dxf")),
            new ExportProjectedPlanSheetHandler(
                projectionRepository,
                projectedSheetExporter,
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

    private static ExportMultiSheetPlanSetPackageHandler CreatePackageHandler(
        SheetAdjustmentProjection projection,
        IProjectedPlanSheetExporter exporter,
        CapturingPlanSetExportRepository exportRepository,
        IPlanSetExportManifestWriter manifestWriter,
        ICanonicalFloorPlanAdjustmentRepository? canonicalRepository = null,
        IUnitOfWork? unitOfWork = null)
    {
        var projectionRepository = new FakeSheetAdjustmentProjectionRepository(projection);
        return new ExportMultiSheetPlanSetPackageHandler(
            projectionRepository,
            new FakePlanSheetSourceReader(
                new PlanSheetSourceDto(
                    projection.DependentSheetId,
                    "ElectricalPlan",
                    "Electrical",
                    Guid.NewGuid(),
                    "library/raw-dxf/electrical.dxf")),
            new ExportProjectedPlanSheetHandler(projectionRepository, exporter),
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

        public Action<MultiSheetExportAuditDto>? OnBuildVerification { get; init; }

        public Action<MultiSheetExportAuditDto>? OnWrite { get; init; }

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
