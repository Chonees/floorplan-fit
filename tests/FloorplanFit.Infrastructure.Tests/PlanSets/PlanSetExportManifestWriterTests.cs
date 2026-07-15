using System.Text.Json;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Infrastructure.Runtime;
using FloorplanFit.Infrastructure.Storage;

namespace FloorplanFit.Infrastructure.Tests.PlanSets;

public sealed class PlanSetExportManifestWriterTests
{
    [Fact]
    public async Task WriteAsync_writes_manifest_with_per_sheet_status_confidence_and_warnings()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-manifest-{Guid.NewGuid():N}");

        try
        {
            var exportId = Guid.NewGuid();
            var audit = new MultiSheetExportAuditDto(
                exportId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "RequiresManualConfirmation",
                new ProjectionAuditSummaryDto(
                    TotalSheetCount: 3,
                    AutomaticallyProjectedSheetCount: 1,
                    ManualConfirmationRequiredSheetCount: 1,
                    LowestConfidence: 0.94m,
                    CanExportPackageAutomatically: false),
                [
                    new ExportedPlanSheetDto(
                        Guid.NewGuid(),
                        ProjectionId: null,
                        "FloorPlan",
                        "Exported",
                        @"C:\out\floor.dxf",
                        "CanonicalFloorPlanAdjustment",
                        Confidence: 1m,
                        Warning: null,
                        RuleSummary: "canonical"),
                    new ExportedPlanSheetDto(
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        "ElectricalPlan",
                        "ProjectedAutomatically",
                        @"C:\out\electrical.dxf",
                        "ElectricalWholeSheetSimilarity",
                        Confidence: 0.94m,
                        Warning: null,
                        RuleSummary: "electrical follows floor plan",
                        RecipeHandlingSummary: "ElectricalPlan: affine placement applied; local recipe requires review before DXF deformation: HorizontalCompression Right @50 delta 2."),
                    new ExportedPlanSheetDto(
                        Guid.NewGuid(),
                        ProjectionId: null,
                        "RoofPlan",
                        "MissingProjection",
                        StoragePath: null,
                        ProjectionMethod: null,
                        Confidence: null,
                        Warning: "No projection exists for this sheet and canonical adjustment.",
                        RuleSummary: null)
                ],
                PackageManifestPath: null,
                new DateTime(2026, 7, 1, 16, 0, 0, DateTimeKind.Utc),
                new PlanSetQualityReportDto(
                    Guid.NewGuid(),
                    RegistrationEventCount: 1,
                    ProjectionEventCount: 1,
                    LowestRegistrationConfidence: 0.82m,
                    LowestProjectionConfidence: 0.94m,
                    ManualRegistrationCount: 0,
                    ManualProjectionCount: 1,
                    Signals: []))
            {
                Verification = CreateBlockedVerification(),
                Artifacts =
                [
                    new PlanSetPackageArtifactDto("FloorPlan", @"C:\out\X-floorplan.dxf"),
                    new PlanSetPackageArtifactDto("ElectricalPlan", @"C:\out\X-electrical.dxf")
                ]
            };
            var writer = new PlanSetExportManifestWriter(new AppWorkspace(Path.Combine(tempRoot, "workspace")));

            var manifestPath = await writer.WriteAsync(audit, CancellationToken.None);

            Assert.True(File.Exists(manifestPath));
            Assert.EndsWith(Path.Combine("exports", "plan-sets", exportId.ToString("N"), "manifest.json"), manifestPath);

            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(manifestPath));
            var root = document.RootElement;
            Assert.Equal("RequiresManualConfirmation", root.GetProperty("Status").GetString());
            Assert.Equal(3, root.GetProperty("Summary").GetProperty("TotalSheetCount").GetInt32());

            var sheets = root.GetProperty("Sheets").EnumerateArray().ToArray();
            Assert.Contains(sheets, sheet =>
                sheet.GetProperty("SheetKind").GetString() == "ElectricalPlan" &&
                sheet.GetProperty("Status").GetString() == "ProjectedAutomatically" &&
                sheet.GetProperty("ProjectionMethod").GetString() == "ElectricalWholeSheetSimilarity" &&
                sheet.GetProperty("Confidence").GetDecimal() == 0.94m &&
                sheet.GetProperty("RecipeHandlingSummary").GetString()!.Contains("HorizontalCompression", StringComparison.Ordinal));
            Assert.Contains(sheets, sheet =>
                sheet.GetProperty("SheetKind").GetString() == "RoofPlan" &&
                sheet.GetProperty("Status").GetString() == "MissingProjection" &&
                sheet.GetProperty("Warning").GetString() == "No projection exists for this sheet and canonical adjustment.");
            Assert.Equal(1, root.GetProperty("QualityReport").GetProperty("ManualProjectionCount").GetInt32());
            Assert.Equal(MultiSheetExportAuditDto.CurrentSchemaVersion, root.GetProperty("schemaVersion").GetInt32());
            Assert.Equal("Blocked", root.GetProperty("Verification").GetProperty("Decision").GetString());
            Assert.Equal(
                PlanSetVerificationReportDto.CurrentSchemaVersion,
                root.GetProperty("Verification").GetProperty("schemaVersion").GetInt32());
            var artifacts = root.GetProperty("Artifacts").EnumerateArray().ToArray();
            Assert.Equal(2, artifacts.Length);
            Assert.Contains(artifacts, artifact =>
                artifact.GetProperty("Role").GetString() == "ElectricalPlan" &&
                artifact.GetProperty("Path").GetString() == @"C:\out\X-electrical.dxf");
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
    public async Task WriteAsync_publishes_audits_then_manifest_atomically()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-atomic-manifest-{Guid.NewGuid():N}");

        try
        {
            var audit = CreateWritableAudit();
            var workspaceRoot = Path.Combine(tempRoot, "workspace");
            var writer = new PlanSetExportManifestWriter(new AppWorkspace(workspaceRoot));

            var manifestPath = await writer.WriteAsync(audit, CancellationToken.None);

            Assert.True(File.Exists(manifestPath));
            var packageDirectory = Path.GetDirectoryName(manifestPath)!;
            var auditDirectory = Path.Combine(packageDirectory, "audit");
            Assert.Equal(9, Directory.GetFiles(auditDirectory, "*.json").Length);
            Assert.Empty(FindStagingDirectories(workspaceRoot, audit.ExportId));
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
    public async Task WriteAsync_existing_final_is_preserved()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-atomic-manifest-{Guid.NewGuid():N}");

        try
        {
            var audit = CreateWritableAudit();
            var workspaceRoot = Path.Combine(tempRoot, "workspace");
            var finalDirectory = Path.Combine(
                workspaceRoot,
                "exports",
                "plan-sets",
                audit.ExportId.ToString("N"));
            Directory.CreateDirectory(finalDirectory);
            var sentinelPath = Path.Combine(finalDirectory, "existing.txt");
            File.WriteAllText(sentinelPath, "do not replace");
            var writer = new PlanSetExportManifestWriter(new AppWorkspace(workspaceRoot));

            await Assert.ThrowsAsync<IOException>(() => writer.WriteAsync(audit, CancellationToken.None));

            Assert.Equal("do not replace", File.ReadAllText(sentinelPath));
            Assert.False(File.Exists(Path.Combine(finalDirectory, "manifest.json")));
            Assert.Empty(FindStagingDirectories(workspaceRoot, audit.ExportId));
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
    public async Task WriteAsync_cancellation_leaves_no_final_or_staging()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-atomic-manifest-{Guid.NewGuid():N}");

        try
        {
            var audit = CreateWritableAudit();
            var workspaceRoot = Path.Combine(tempRoot, "workspace");
            var finalDirectory = Path.Combine(
                workspaceRoot,
                "exports",
                "plan-sets",
                audit.ExportId.ToString("N"));
            var writer = new PlanSetExportManifestWriter(new AppWorkspace(workspaceRoot));
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                writer.WriteAsync(audit, cancellation.Token));

            Assert.False(Directory.Exists(finalDirectory));
            Assert.Empty(FindStagingDirectories(workspaceRoot, audit.ExportId));
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
    public void BuildVerificationReport_green_evidence_is_ready()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-verification-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            var floorPath = Path.Combine(tempRoot, "floor.dxf");
            var electricalPath = Path.Combine(tempRoot, "electrical.dxf");
            WriteRectangleDxf(floorPath);
            WriteRectangleDxf(electricalPath);
            var electricalSheetId = Guid.NewGuid();
            var dxfSafety = new ProjectedPlanSheetDxfSafetyAuditDto(
                OutputFileExists: true,
                OutputFileBytes: new FileInfo(electricalPath).Length,
                EntityCountBefore: 4,
                EntityCountAfter: 4,
                InsertCountAfter: 0,
                DimensionCountAfter: 0,
                EllipseCountAfter: 0,
                WireOrCurveCountAfter: 0,
                MissingHandleCountAfter: 0,
                MissingOwnerCountAfter: 0,
                UnsupportedCrossingEntityCount: 0);
            var outline = new ProjectedPlanSheetOutlineCongruenceAuditDto(
                "Congruent",
                "Comparable outlines match.",
                ToleranceInches: 0.05m,
                NormalizationApplied: false,
                CanonicalSourceOutline: null,
                ElectricalSourceOutline: null,
                ElectricalNormalizedSourceOutline: null,
                ElectricalExportOutline: null,
                SourceWidthMismatchInches: 0m,
                SourceHeightMismatchInches: 0m,
                ExportWidthMismatchInches: 0m,
                ExportHeightMismatchInches: 0m,
                AnchorX: null,
                AnchorY: null,
                ScaleX: 1m,
                ScaleY: 1m);
            var placement = new AdjustedSitePlanPlacementDto(1m, 0m, 0m, [], [])
            {
                InputAudit = new AdjustmentInputAuditDto(100m, 200m, 100m, 200m, 0m, 0m, "test"),
                FloorPlanImpactAudit = []
            };
            var audit = new MultiSheetExportAuditDto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "PendingVerification",
                new ProjectionAuditSummaryDto(2, 1, 0, 0.95m, false),
                [
                    new ExportedPlanSheetDto(
                        Guid.NewGuid(),
                        null,
                        "CanonicalFloorPlan",
                        "Exported",
                        floorPath,
                        "CanonicalFloorPlanAdjustment",
                        1m,
                        null,
                        null),
                    new ExportedPlanSheetDto(
                        electricalSheetId,
                        Guid.NewGuid(),
                        "ElectricalPlan",
                        "ProjectedAutomatically",
                        electricalPath,
                        "ElectricalWholeSheetSimilarity",
                        0.95m,
                        null,
                        null,
                        ExportAudit: new ProjectedPlanSheetExportAuditDto([], dxfSafety, outline))
                ],
                null,
                DateTime.UtcNow,
                CanonicalPlacement: placement,
                CanonicalRecipe: AdjustmentRecipeSummaryDto.FromPlacement(placement));
            var writer = new PlanSetExportManifestWriter(new AppWorkspace(Path.Combine(tempRoot, "workspace")));

            var report = writer.BuildVerificationReport(audit);

            Assert.True(report.IsGreen);
            Assert.Equal(PlanSetVerificationDecision.ReadyForExport, report.Decision);
            Assert.Equal(new PlanSetVerificationOutputDto(2, 2, []), report.Outputs);
            Assert.Equal(PlanSetVerificationCheckStatus.Passed, report.DxfSafety.Status);
            Assert.Equal(PlanSetVerificationCheckStatus.Passed, report.OutlineCongruence.Status);
            Assert.Equal(PlanSetVerificationCheckStatus.Passed, report.SegmentCongruence.Status);
            Assert.Equal(PlanSetVerificationCheckStatus.Passed, report.FinalOutputCongruence.Status);
            Assert.Equal(PlanSetVerificationCheckStatus.Passed, report.Capabilities.Status);
            Assert.Empty(report.Reasons);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void BuildVerificationReport_accepts_matching_outer_outlines_when_full_span_internal_walls_tie_support()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-verification-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            var floorPath = Path.Combine(tempRoot, "floor.dxf");
            var electricalPath = Path.Combine(tempRoot, "electrical.dxf");
            WriteRectangleWithFullHeightInteriorWallDxf(floorPath, "WALLS");
            WriteRectangleWithFullHeightInteriorWallDxf(electricalPath, "ELECTRICAL WALLS");
            var audit = CreateAutomaticVerificationAudit(floorPath, electricalPath, electricalEntityCount: 5);
            var writer = new PlanSetExportManifestWriter(new AppWorkspace(Path.Combine(tempRoot, "workspace")));

            var report = writer.BuildVerificationReport(audit);

            Assert.True(report.IsGreen);
            Assert.Equal(PlanSetVerificationDecision.ReadyForExport, report.Decision);
            Assert.Equal(PlanSetVerificationCheckStatus.Passed, report.FinalOutputCongruence.Status);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void BuildVerificationReport_accepts_matching_closed_lwpolyline_structural_rectangles()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-verification-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            var floorPath = Path.Combine(tempRoot, "floor.dxf");
            var electricalPath = Path.Combine(tempRoot, "electrical.dxf");
            WriteClosedLwPolylineRectangleDxf(floorPath, "WALLS");
            WriteClosedLwPolylineRectangleDxf(electricalPath, "ELECTRICAL WALLS");
            var audit = CreateAutomaticVerificationAudit(floorPath, electricalPath, electricalEntityCount: 1);
            var writer = new PlanSetExportManifestWriter(new AppWorkspace(Path.Combine(tempRoot, "workspace")));

            var report = writer.BuildVerificationReport(audit);

            Assert.True(report.IsGreen);
            Assert.Equal(PlanSetVerificationDecision.ReadyForExport, report.Decision);
            Assert.Equal(PlanSetVerificationCheckStatus.Passed, report.FinalOutputCongruence.Status);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void BuildVerificationReport_accepts_matching_closed_classic_polyline_structural_rectangles()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-verification-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            var floorPath = Path.Combine(tempRoot, "floor.dxf");
            var electricalPath = Path.Combine(tempRoot, "electrical.dxf");
            WriteClosedClassicPolylineRectangleDxf(floorPath, "WALLS");
            WriteClosedClassicPolylineRectangleDxf(electricalPath, "ELECTRICAL WALLS");
            var audit = CreateAutomaticVerificationAudit(floorPath, electricalPath, electricalEntityCount: 1);
            var writer = new PlanSetExportManifestWriter(new AppWorkspace(Path.Combine(tempRoot, "workspace")));

            var report = writer.BuildVerificationReport(audit);

            Assert.True(report.IsGreen);
            Assert.Equal(PlanSetVerificationDecision.ReadyForExport, report.Decision);
            Assert.Equal(PlanSetVerificationCheckStatus.Passed, report.FinalOutputCongruence.Status);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void BuildVerificationReport_accepts_matching_solid_structural_rectangles_in_dxf_point_order()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-verification-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            var floorPath = Path.Combine(tempRoot, "floor.dxf");
            var electricalPath = Path.Combine(tempRoot, "electrical.dxf");
            WriteSolidRectangleDxf(floorPath, "WALLS");
            WriteSolidRectangleDxf(electricalPath, "ELECTRICAL WALLS");
            var audit = CreateAutomaticVerificationAudit(floorPath, electricalPath, electricalEntityCount: 1);
            var writer = new PlanSetExportManifestWriter(new AppWorkspace(Path.Combine(tempRoot, "workspace")));

            var report = writer.BuildVerificationReport(audit);

            Assert.True(report.IsGreen);
            Assert.Equal(PlanSetVerificationDecision.ReadyForExport, report.Decision);
            Assert.Equal(PlanSetVerificationCheckStatus.Passed, report.FinalOutputCongruence.Status);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task WriteAsync_final_output_audit_reports_electrical_verification_path_actually_read()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-verification-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            var floorPath = Path.Combine(tempRoot, "floor.dxf");
            var electricalStoragePath = Path.Combine(tempRoot, "electrical-storage.dxf");
            var electricalVerificationPath = Path.Combine(tempRoot, "electrical-verification.dxf");
            WriteRectangleDxf(floorPath);
            WriteTranslatedElectricalRectangleDxf(electricalStoragePath);
            WriteRectangleDxf(electricalVerificationPath);
            var audit = CreateAutomaticVerificationAudit(
                floorPath,
                electricalStoragePath,
                electricalEntityCount: 4);
            audit = audit with
            {
                Status = "RequiresManualConfirmation",
                Sheets = audit.Sheets
                    .Select(sheet => sheet.SheetKind == "ElectricalPlan"
                        ? sheet with { VerificationPath = electricalVerificationPath }
                        : sheet)
                    .ToArray(),
                Verification = CreateBlockedVerification()
            };
            var writer = new PlanSetExportManifestWriter(new AppWorkspace(Path.Combine(tempRoot, "workspace")));

            var manifestPath = await writer.WriteAsync(audit, CancellationToken.None);

            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(
                Path.Combine(Path.GetDirectoryName(manifestPath)!, "audit", "final-output-congruence-audit.json")));
            var emittedPath = document.RootElement
                .GetProperty("sheets")[0]
                .GetProperty("finalOutputCongruence")
                .GetProperty("ElectricalOutputPath")
                .GetString();
            Assert.Equal(electricalVerificationPath, emittedPath);
            Assert.NotEqual(electricalStoragePath, emittedPath);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task WriteAsync_insufficient_final_output_audit_reports_verification_paths_actually_read()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-verification-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            var floorStoragePath = Path.Combine(tempRoot, "floor-storage.dxf");
            var floorVerificationPath = Path.Combine(tempRoot, "floor-verification.dxf");
            var electricalStoragePath = Path.Combine(tempRoot, "electrical-storage.dxf");
            var electricalVerificationPath = Path.Combine(tempRoot, "electrical-verification.dxf");
            WriteRectangleDxf(floorStoragePath);
            WriteRectangleDxf(electricalStoragePath);
            WriteDxfWithoutStructuralSegments(floorVerificationPath);
            WriteDxfWithoutStructuralSegments(electricalVerificationPath);
            var audit = CreateAutomaticVerificationAudit(
                floorStoragePath,
                electricalStoragePath,
                electricalEntityCount: 4);
            audit = audit with
            {
                Status = "RequiresManualConfirmation",
                Sheets = audit.Sheets
                    .Select(sheet => sheet.SheetKind switch
                    {
                        "CanonicalFloorPlan" => sheet with { VerificationPath = floorVerificationPath },
                        "ElectricalPlan" => sheet with { VerificationPath = electricalVerificationPath },
                        _ => sheet
                    })
                    .ToArray(),
                Verification = CreateBlockedVerification()
            };
            var writer = new PlanSetExportManifestWriter(new AppWorkspace(Path.Combine(tempRoot, "workspace")));

            var manifestPath = await writer.WriteAsync(audit, CancellationToken.None);

            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(
                Path.Combine(Path.GetDirectoryName(manifestPath)!, "audit", "final-output-congruence-audit.json")));
            var finalOutputCongruence = document.RootElement
                .GetProperty("sheets")[0]
                .GetProperty("finalOutputCongruence");
            Assert.Equal(floorVerificationPath, finalOutputCongruence.GetProperty("FloorOutputPath").GetString());
            Assert.Equal(electricalVerificationPath, finalOutputCongruence.GetProperty("ElectricalOutputPath").GetString());
            Assert.NotEqual(floorStoragePath, finalOutputCongruence.GetProperty("FloorOutputPath").GetString());
            Assert.NotEqual(electricalStoragePath, finalOutputCongruence.GetProperty("ElectricalOutputPath").GetString());
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void BuildVerificationReport_rejects_matching_fringe_bounds_when_dominant_walls_differ()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-verification-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            var floorPath = Path.Combine(tempRoot, "floor.dxf");
            var electricalPath = Path.Combine(tempRoot, "electrical.dxf");
            WriteRectangleDxf(floorPath);
            WriteElectricalRectangleWithOutboardFringeDxf(electricalPath);
            var audit = CreateAutomaticVerificationAudit(floorPath, electricalPath, electricalEntityCount: 6);
            var writer = new PlanSetExportManifestWriter(new AppWorkspace(Path.Combine(tempRoot, "workspace")));

            var report = writer.BuildVerificationReport(audit);

            Assert.False(report.IsGreen);
            Assert.Equal(PlanSetVerificationDecision.Blocked, report.Decision);
            Assert.Equal(PlanSetVerificationCheckStatus.Failed, report.FinalOutputCongruence.Status);
            Assert.Contains(
                report.Reasons,
                reason => reason.Code == PlanSetVerificationReasonCode.FinalOutputCongruenceMismatch);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void BuildVerificationReport_rejects_equal_size_outlines_with_native_translation()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-verification-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            var floorPath = Path.Combine(tempRoot, "floor.dxf");
            var electricalPath = Path.Combine(tempRoot, "electrical.dxf");
            WriteRectangleDxf(floorPath);
            WriteTranslatedElectricalRectangleDxf(electricalPath);
            var audit = CreateAutomaticVerificationAudit(floorPath, electricalPath, electricalEntityCount: 4);
            var writer = new PlanSetExportManifestWriter(new AppWorkspace(Path.Combine(tempRoot, "workspace")));

            var report = writer.BuildVerificationReport(audit);

            Assert.False(report.IsGreen);
            Assert.Equal(PlanSetVerificationDecision.Blocked, report.Decision);
            Assert.Equal(PlanSetVerificationCheckStatus.Failed, report.FinalOutputCongruence.Status);
            Assert.Contains(
                report.Reasons,
                reason => reason.Code == PlanSetVerificationReasonCode.FinalOutputCongruenceMismatch);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void BuildVerificationReport_missing_required_evidence_is_blocked()
    {
        var audit = new MultiSheetExportAuditDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PendingVerification",
            new ProjectionAuditSummaryDto(2, 1, 0, 0.95m, false),
            [
                new ExportedPlanSheetDto(
                    Guid.NewGuid(), null, "CanonicalFloorPlan", "Exported", null,
                    "CanonicalFloorPlanAdjustment", 1m, null, null),
                new ExportedPlanSheetDto(
                    Guid.NewGuid(), Guid.NewGuid(), "ElectricalPlan", "ProjectedAutomatically", null,
                    "ElectricalWholeSheetSimilarity", 0.95m, null, null)
            ],
            null,
            DateTime.UtcNow);
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-verification-{Guid.NewGuid():N}");
        var writer = new PlanSetExportManifestWriter(new AppWorkspace(Path.Combine(tempRoot, "workspace")));

        var report = writer.BuildVerificationReport(audit);

        Assert.False(report.IsGreen);
        Assert.Equal(PlanSetVerificationDecision.Blocked, report.Decision);
        Assert.Contains(report.Reasons, reason => reason.Code == PlanSetVerificationReasonCode.MissingExpectedOutput);
        Assert.Contains(report.Reasons, reason => reason.Code == PlanSetVerificationReasonCode.MissingRequiredEvidence);
    }

    private static MultiSheetExportAuditDto CreateWritableAudit()
    {
        var sheetId = Guid.NewGuid();
        var verification = new PlanSetVerificationReportDto(
            PlanSetVerificationReportDto.CurrentSchemaVersion,
            new PlanSetVerificationOutputDto(1, 0, [sheetId]),
            new PlanSetVerificationCheckDto(PlanSetVerificationCheckStatus.Passed, 0, 0, 0, 0),
            new PlanSetVerificationOperationDto(0, 0, 0, 0),
            new PlanSetVerificationOperationDto(0, 0, 0, 0),
            new PlanSetVerificationCheckDto(PlanSetVerificationCheckStatus.Passed, 0, 0, 0, 0),
            new PlanSetVerificationCheckDto(PlanSetVerificationCheckStatus.Passed, 0, 0, 0, 0),
            new PlanSetVerificationCheckDto(PlanSetVerificationCheckStatus.Passed, 0, 0, 0, 0),
            new PlanSetVerificationCheckDto(PlanSetVerificationCheckStatus.Passed, 0, 0, 0, 0),
            [new PlanSetVerificationReasonDto(
                PlanSetVerificationReasonCode.MissingExpectedOutput,
                "outputs",
                sheetId,
                "Focused atomic writer fixture remains review-blocked.")]);
        return new MultiSheetExportAuditDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "RequiresManualConfirmation",
            new ProjectionAuditSummaryDto(1, 0, 0, null, false),
            [new ExportedPlanSheetDto(
                sheetId,
                null,
                "CanonicalFloorPlan",
                "Exported",
                "exports/floor-plan.dxf",
                "CanonicalFloorPlanAdjustment",
                1m,
                null,
                null)],
            null,
            DateTime.UtcNow)
        {
            Verification = verification
        };
    }

    private static IReadOnlyList<string> FindStagingDirectories(string workspaceRoot, Guid exportId)
    {
        var parent = Path.Combine(workspaceRoot, "exports", "plan-sets");
        return Directory.Exists(parent)
            ? Directory.GetDirectories(
                parent,
                $".{exportId:N}.staging-*",
                SearchOption.TopDirectoryOnly)
            : [];
    }

    private static PlanSetVerificationReportDto CreateBlockedVerification()
        => new(
            PlanSetVerificationReportDto.CurrentSchemaVersion,
            new PlanSetVerificationOutputDto(3, 1, [Guid.NewGuid(), Guid.NewGuid()]),
            new PlanSetVerificationCheckDto(PlanSetVerificationCheckStatus.InsufficientData, 2, 0, 0, 2),
            new PlanSetVerificationOperationDto(0, 0, 0, 0),
            new PlanSetVerificationOperationDto(0, 0, 0, 0),
            new PlanSetVerificationCheckDto(PlanSetVerificationCheckStatus.InsufficientData, 1, 0, 0, 1),
            new PlanSetVerificationCheckDto(PlanSetVerificationCheckStatus.InsufficientData, 1, 0, 0, 1),
            new PlanSetVerificationCheckDto(PlanSetVerificationCheckStatus.InsufficientData, 1, 0, 0, 1),
            new PlanSetVerificationCheckDto(PlanSetVerificationCheckStatus.Unsupported, 2, 1, 1, 0),
            [new PlanSetVerificationReasonDto(
                PlanSetVerificationReasonCode.MissingExpectedOutput,
                "outputs",
                null,
                "Focused manifest serialization regression.")]);

    private static MultiSheetExportAuditDto CreateAutomaticVerificationAudit(
        string floorPath,
        string electricalPath,
        int electricalEntityCount)
    {
        var electricalSheetId = Guid.NewGuid();
        var dxfSafety = new ProjectedPlanSheetDxfSafetyAuditDto(
            OutputFileExists: true,
            OutputFileBytes: new FileInfo(electricalPath).Length,
            EntityCountBefore: electricalEntityCount,
            EntityCountAfter: electricalEntityCount,
            InsertCountAfter: 0,
            DimensionCountAfter: 0,
            EllipseCountAfter: 0,
            WireOrCurveCountAfter: 0,
            MissingHandleCountAfter: 0,
            MissingOwnerCountAfter: 0,
            UnsupportedCrossingEntityCount: 0);
        var outline = new ProjectedPlanSheetOutlineCongruenceAuditDto(
            "RegistrationProofAuthorized",
            "Source-bound whole-plan registration proof authorizes the canonical frame; outline bounds remain unmeasured here.",
            ToleranceInches: 0.05m,
            NormalizationApplied: false,
            CanonicalSourceOutline: null,
            ElectricalSourceOutline: null,
            ElectricalNormalizedSourceOutline: null,
            ElectricalExportOutline: null,
            SourceWidthMismatchInches: null,
            SourceHeightMismatchInches: null,
            ExportWidthMismatchInches: null,
            ExportHeightMismatchInches: null,
            AnchorX: null,
            AnchorY: null,
            ScaleX: null,
            ScaleY: null);
        var placement = new AdjustedSitePlanPlacementDto(1m, 0m, 0m, [], [])
        {
            InputAudit = new AdjustmentInputAuditDto(100m, 200m, 100m, 200m, 0m, 0m, "test"),
            FloorPlanImpactAudit = []
        };
        return new MultiSheetExportAuditDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PendingVerification",
            new ProjectionAuditSummaryDto(2, 1, 0, 0.95m, false),
            [
                new ExportedPlanSheetDto(
                    Guid.NewGuid(),
                    null,
                    "CanonicalFloorPlan",
                    "Exported",
                    floorPath,
                    "CanonicalFloorPlanAdjustment",
                    1m,
                    null,
                    null),
                new ExportedPlanSheetDto(
                    electricalSheetId,
                    Guid.NewGuid(),
                    "ElectricalPlan",
                    "ProjectedAutomatically",
                    electricalPath,
                    "ElectricalWholeSheetSimilarity",
                    0.95m,
                    null,
                    null,
                    ExportAudit: new ProjectedPlanSheetExportAuditDto([], dxfSafety, outline))
            ],
            null,
            DateTime.UtcNow,
            CanonicalPlacement: placement,
            CanonicalRecipe: AdjustmentRecipeSummaryDto.FromPlacement(placement));
    }

    private static void WriteDxfWithoutStructuralSegments(string path)
        => File.WriteAllText(
            path,
            """
            0
            SECTION
            2
            ENTITIES
            0
            ENDSEC
            0
            EOF
            """);

    private static void WriteClosedLwPolylineRectangleDxf(string path, string layer)
        => File.WriteAllText(
            path,
            $"""
            0
            SECTION
            2
            ENTITIES
            0
            LWPOLYLINE
            8
            {layer}
            90
            4
            70
            1
            10
            0
            20
            0
            10
            100
            20
            0
            10
            100
            20
            200
            10
            0
            20
            200
            0
            ENDSEC
            0
            EOF
            """);

    private static void WriteClosedClassicPolylineRectangleDxf(string path, string layer)
        => File.WriteAllText(
            path,
            $"""
            0
            SECTION
            2
            ENTITIES
            0
            POLYLINE
            8
            {layer}
            66
            1
            70
            1
            0
            VERTEX
            8
            {layer}
            10
            0
            20
            0
            0
            VERTEX
            8
            {layer}
            10
            100
            20
            0
            0
            VERTEX
            8
            {layer}
            10
            100
            20
            200
            0
            VERTEX
            8
            {layer}
            10
            0
            20
            200
            0
            SEQEND
            0
            ENDSEC
            0
            EOF
            """);

    private static void WriteSolidRectangleDxf(string path, string layer)
        => File.WriteAllText(
            path,
            $"""
            0
            SECTION
            2
            ENTITIES
            0
            SOLID
            8
            {layer}
            10
            0
            20
            0
            11
            100
            21
            0
            12
            0
            22
            200
            13
            100
            23
            200
            0
            ENDSEC
            0
            EOF
            """);

    private static void WriteRectangleWithFullHeightInteriorWallDxf(string path, string layer)
        => File.WriteAllText(
            path,
            $"""
            0
            SECTION
            2
            ENTITIES
            0
            LINE
            8
            {layer}
            10
            0
            20
            0
            11
            100
            21
            0
            0
            LINE
            8
            {layer}
            10
            100
            20
            0
            11
            100
            21
            200
            0
            LINE
            8
            {layer}
            10
            100
            20
            200
            11
            0
            21
            200
            0
            LINE
            8
            {layer}
            10
            0
            20
            200
            11
            0
            21
            0
            0
            LINE
            8
            {layer}
            10
            50
            20
            0
            11
            50
            21
            200
            0
            ENDSEC
            0
            EOF
            """);

    private static void WriteTranslatedElectricalRectangleDxf(string path)
        => File.WriteAllText(
            path,
            """
            0
            SECTION
            2
            ENTITIES
            0
            LINE
            8
            ELECTRICAL WALLS
            10
            1
            20
            0
            11
            101
            21
            0
            0
            LINE
            8
            ELECTRICAL WALLS
            10
            101
            20
            0
            11
            101
            21
            200
            0
            LINE
            8
            ELECTRICAL WALLS
            10
            101
            20
            200
            11
            1
            21
            200
            0
            LINE
            8
            ELECTRICAL WALLS
            10
            1
            20
            200
            11
            1
            21
            0
            0
            ENDSEC
            0
            EOF
            """);

    private static void WriteElectricalRectangleWithOutboardFringeDxf(string path)
        => File.WriteAllText(
            path,
            """
            0
            SECTION
            2
            ENTITIES
            0
            LINE
            8
            ELECTRICAL WALLS
            10
            0.5
            20
            0
            11
            99.5
            21
            0
            0
            LINE
            8
            ELECTRICAL WALLS
            10
            99.5
            20
            0
            11
            99.5
            21
            200
            0
            LINE
            8
            ELECTRICAL WALLS
            10
            99.5
            20
            200
            11
            0.5
            21
            200
            0
            LINE
            8
            ELECTRICAL WALLS
            10
            0.5
            20
            200
            11
            0.5
            21
            0
            0
            LINE
            8
            ELECTRICAL WALLS
            10
            0
            20
            0
            11
            0
            21
            30
            0
            LINE
            8
            ELECTRICAL WALLS
            10
            100
            20
            0
            11
            100
            21
            30
            0
            ENDSEC
            0
            EOF
            """);

    private static void WriteRectangleDxf(string path)
        => File.WriteAllText(
            path,
            """
            0
            SECTION
            2
            ENTITIES
            0
            LINE
            8
            WALLS
            10
            0
            20
            0
            11
            100
            21
            0
            0
            LINE
            8
            WALLS
            10
            100
            20
            0
            11
            100
            21
            200
            0
            LINE
            8
            WALLS
            10
            100
            20
            200
            11
            0
            21
            200
            0
            LINE
            8
            WALLS
            10
            0
            20
            200
            11
            0
            21
            0
            0
            ENDSEC
            0
            EOF
            """);
}
