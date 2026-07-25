using System.Text.Json;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Export;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;
using FloorplanFit.Infrastructure.Runtime;

namespace FloorplanFit.Infrastructure.Storage;

public sealed class PlanSetExportManifestWriter : IPlanSetExportManifestWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly AppWorkspace workspace;

    public PlanSetExportManifestWriter(AppWorkspace workspace)
    {
        this.workspace = workspace;
    }

    public PlanSetVerificationReportDto BuildVerificationReport(MultiSheetExportAuditDto audit)
    {
        ArgumentNullException.ThrowIfNull(audit);

        var reasons = new List<PlanSetVerificationReasonDto>();
        var dependentSheets = audit.Sheets.Where(sheet => !IsCanonicalSheet(sheet)).ToArray();
        var electricalSheets = audit.Sheets.Where(IsElectricalSheet).ToArray();

        var missingOutputs = audit.Sheets
            .Where(sheet => !HasOutputEvidence(sheet))
            .Select(sheet => sheet.SheetId)
            .ToArray();
        foreach (var sheetId in missingOutputs)
        {
            AddReason(
                reasons,
                PlanSetVerificationReasonCode.MissingExpectedOutput,
                "outputs",
                sheetId,
                "An expected FloorPlan or dependent-sheet output is absent or empty.");
        }

        var dxfStatuses = dependentSheets.Select(sheet =>
        {
            var safety = sheet.ExportAudit?.DxfSafety;
            if (safety is null)
            {
                AddReason(
                    reasons,
                    PlanSetVerificationReasonCode.MissingRequiredEvidence,
                    "dxf-safety",
                    sheet.SheetId,
                    "The dependent output has no typed DXF safety audit.");
                return PlanSetVerificationCheckStatus.InsufficientData;
            }

            if (!IsDxfSafe(safety))
            {
                AddReason(
                    reasons,
                    PlanSetVerificationReasonCode.UnsafeDxf,
                    "dxf-safety",
                    sheet.SheetId,
                    "The DXF is absent/empty or reports missing handles, missing owners, no entities, or unsupported crossings.");
                return PlanSetVerificationCheckStatus.Failed;
            }

            return PlanSetVerificationCheckStatus.Passed;
        }).ToArray();

        var floorPlanOperations = BuildFloorPlanOperationVerification(audit, reasons);
        var dependentOperations = BuildDependentOperationVerification(audit, electricalSheets, reasons);

        var outlineStatuses = electricalSheets.Select(sheet =>
        {
            var outline = sheet.ExportAudit?.OutlineCongruence;
            if (outline is null)
            {
                AddReason(
                    reasons,
                    PlanSetVerificationReasonCode.MissingRequiredEvidence,
                    "outline-congruence",
                    sheet.SheetId,
                    "The ElectricalPlan output has no typed outline-congruence audit.");
                return PlanSetVerificationCheckStatus.InsufficientData;
            }

            if (!IsOutlineCongruent(outline))
            {
                AddReason(
                    reasons,
                    PlanSetVerificationReasonCode.OutlineCongruenceMismatch,
                    "outline-congruence",
                    sheet.SheetId,
                    outline.Reason);
                return PlanSetVerificationCheckStatus.Failed;
            }

            return PlanSetVerificationCheckStatus.Passed;
        }).ToArray();

        var segmentEvidence = PlanSetOutlineSegmentCongruenceAuditBuilder.BuildVerification(audit);
        AddCongruenceReasons(
            segmentEvidence,
            PlanSetVerificationReasonCode.SegmentCongruenceMismatch,
            "segment-congruence",
            reasons);

        var finalOutputEvidence = PlanSetOutlineSegmentCongruenceAuditBuilder.BuildFinalOutputVerification(audit);
        AddCongruenceReasons(
            finalOutputEvidence,
            PlanSetVerificationReasonCode.FinalOutputCongruenceMismatch,
            "final-output-congruence",
            reasons);

        var capabilityStatuses = dependentSheets.Select(sheet =>
            BuildCapabilityStatus(audit, sheet, reasons)).ToArray();

        return new PlanSetVerificationReportDto(
            PlanSetVerificationReportDto.CurrentSchemaVersion,
            new PlanSetVerificationOutputDto(
                audit.Sheets.Count,
                audit.Sheets.Count - missingOutputs.Length,
                missingOutputs),
            BuildCheck(dependentSheets.Length, dxfStatuses),
            floorPlanOperations,
            dependentOperations,
            BuildCheck(electricalSheets.Length, outlineStatuses),
            BuildCheck(electricalSheets.Length, segmentEvidence.Select(item => item.Status)),
            BuildCheck(electricalSheets.Length, finalOutputEvidence.Select(item => item.Status)),
            BuildCheck(dependentSheets.Length, capabilityStatuses),
            reasons);
    }

    public async Task<string> WriteAsync(MultiSheetExportAuditDto audit, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(audit);
        cancellationToken.ThrowIfCancellationRequested();

        var verification = audit.Verification ?? throw new InvalidOperationException(
            "A typed verification report is required before writing a plan-set manifest.");
        if (audit.SchemaVersion != MultiSheetExportAuditDto.CurrentSchemaVersion ||
            verification.SchemaVersion != PlanSetVerificationReportDto.CurrentSchemaVersion)
        {
            throw new InvalidOperationException("Plan-set manifest or verification schemaVersion is not supported.");
        }

        var expectedStatus = verification.IsGreen ? "ReadyForExport" : "RequiresManualConfirmation";
        if (!string.Equals(audit.Status, expectedStatus, StringComparison.Ordinal) ||
            audit.Summary.CanExportPackageAutomatically != verification.IsGreen)
        {
            throw new InvalidOperationException(
                "Manifest status and automatic-export summary must match the typed verification decision.");
        }

        workspace.EnsureCreated();

        var packageDirectory = Path.Combine(
            workspace.RootPath,
            "exports",
            "plan-sets",
            audit.ExportId.ToString("N"));
        var manifestPath = Path.Combine(packageDirectory, "manifest.json");
        await AtomicDirectoryPublisher.PublishAsync(
            packageDirectory,
            async (stagingDirectory, stagingCancellationToken) =>
            {
                await WriteAuditArtifactsAsync(stagingDirectory, audit, stagingCancellationToken);
                await WriteJsonAsync(
                    Path.Combine(stagingDirectory, "manifest.json"),
                    audit,
                    stagingCancellationToken);
            },
            cancellationToken);

        return manifestPath;
    }

    public async Task WritePackageArtifactsAsync(
        MultiSheetExportAuditDto audit,
        string stagingDirectory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(audit);
        if (string.IsNullOrWhiteSpace(stagingDirectory))
        {
            throw new ArgumentException("Package staging directory is required.", nameof(stagingDirectory));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var comparison = GetRequiredPackageArtifact(audit, "Comparison", ".json");
        var manifest = GetRequiredPackageArtifact(audit, "Manifest", ".json");
        var humanAudit = GetRequiredPackageArtifact(audit, "Audit", ".txt");
        if (audit.HumanSummary.Count == 0)
        {
            throw new InvalidOperationException("A human-readable audit summary is required for the user package.");
        }

        var stagedComparisonPath = ResolveStagedArtifactPath(stagingDirectory, comparison);
        var stagedManifestPath = ResolveStagedArtifactPath(stagingDirectory, manifest);
        var stagedAuditPath = ResolveStagedArtifactPath(stagingDirectory, humanAudit);
        if (new[] { stagedComparisonPath, stagedManifestPath, stagedAuditPath }
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() != 3)
        {
            throw new InvalidOperationException("Package comparison, manifest, and audit names must be distinct.");
        }

        var packageAudit = audit with { PackageManifestPath = manifest.Path };
        await WriteJsonAsync(
            stagedComparisonPath,
            PlanSetOutlineSegmentCongruenceAuditBuilder.BuildFinalOutput(
                audit,
                reportStoragePaths: true),
            cancellationToken);
        await File.WriteAllTextAsync(
            stagedAuditPath,
            string.Join(Environment.NewLine, audit.HumanSummary) + Environment.NewLine,
            cancellationToken);
        await WriteJsonAsync(stagedManifestPath, packageAudit, cancellationToken);
    }

    private static PlanSetPackageArtifactDto GetRequiredPackageArtifact(
        MultiSheetExportAuditDto audit,
        string role,
        string requiredExtension)
    {
        var artifact = audit.Artifacts.SingleOrDefault(item =>
            string.Equals(item.Role, role, StringComparison.Ordinal));
        if (artifact is null)
        {
            throw new InvalidOperationException($"Package artifact role '{role}' is required.");
        }

        if (!string.Equals(Path.GetExtension(artifact.Path), requiredExtension, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Package artifact role '{role}' must use the '{requiredExtension}' extension.");
        }

        return artifact;
    }

    private static string ResolveStagedArtifactPath(
        string stagingDirectory,
        PlanSetPackageArtifactDto artifact)
    {
        var fileName = Path.GetFileName(artifact.Path);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidOperationException($"Package artifact role '{artifact.Role}' has no file name.");
        }

        return Path.Combine(stagingDirectory, fileName);
    }

    private static PlanSetVerificationOperationDto BuildFloorPlanOperationVerification(
        MultiSheetExportAuditDto audit,
        ICollection<PlanSetVerificationReasonDto> reasons)
    {
        if (audit.CanonicalRecipe is null || audit.CanonicalPlacement is null)
        {
            AddReason(
                reasons,
                PlanSetVerificationReasonCode.MissingRequiredEvidence,
                "floorplan-operations",
                null,
                "Canonical recipe and FloorPlan impact evidence are required, including affine-only recipes with zero operations.");
            return new PlanSetVerificationOperationDto(0, 0, 0, 1);
        }

        var expected = CanonicalOperationCount(audit.CanonicalRecipe);
        var actual = audit.CanonicalPlacement.FloorPlanImpactAudit;
        var applied = actual.Count(operation => string.Equals(operation.Status, "Applied", StringComparison.Ordinal));
        var failed = actual.Count - applied + Math.Max(0, actual.Count - expected);
        var missing = Math.Max(0, expected - actual.Count);
        if (failed > 0 || missing > 0)
        {
            AddReason(
                reasons,
                PlanSetVerificationReasonCode.CanonicalOperationMismatch,
                "floorplan-operations",
                null,
                $"FloorPlan canonical operations expected {expected}, applied {applied}, failed/extra {failed}, missing {missing}.");
        }

        return new PlanSetVerificationOperationDto(expected, applied, failed, missing);
    }

    private static PlanSetVerificationOperationDto BuildDependentOperationVerification(
        MultiSheetExportAuditDto audit,
        IReadOnlyList<ExportedPlanSheetDto> electricalSheets,
        ICollection<PlanSetVerificationReasonDto> reasons)
    {
        if (audit.CanonicalRecipe is null)
        {
            AddReason(
                reasons,
                PlanSetVerificationReasonCode.MissingRequiredEvidence,
                "dependent-operations",
                null,
                "The canonical recipe is required to verify dependent operation counts.");
            return new PlanSetVerificationOperationDto(0, 0, 0, 1);
        }

        var expectedPerSheet = CanonicalOperationCount(audit.CanonicalRecipe);
        var expected = expectedPerSheet * electricalSheets.Count;
        var applied = 0;
        var failed = 0;
        var missing = 0;
        foreach (var sheet in electricalSheets)
        {
            var operations = sheet.ExportAudit?.Operations ?? [];
            applied += operations.Count(operation => string.Equals(operation.Status, "Applied", StringComparison.Ordinal));
            failed += operations.Count(operation => !string.Equals(operation.Status, "Applied", StringComparison.Ordinal));
            failed += Math.Max(0, operations.Count - expectedPerSheet);
            missing += Math.Max(0, expectedPerSheet - operations.Count);
        }

        if (failed > 0 || missing > 0)
        {
            AddReason(
                reasons,
                PlanSetVerificationReasonCode.CanonicalOperationMismatch,
                "dependent-operations",
                null,
                $"Electrical canonical operations expected {expected}, applied {applied}, failed/extra {failed}, missing {missing}.");
        }

        return new PlanSetVerificationOperationDto(expected, applied, failed, missing);
    }

    private static PlanSetVerificationCheckStatus BuildCapabilityStatus(
        MultiSheetExportAuditDto audit,
        ExportedPlanSheetDto sheet,
        ICollection<PlanSetVerificationReasonDto> reasons)
    {
        if (!Enum.TryParse<SheetAdjustmentProjectionMethod>(sheet.ProjectionMethod, out var method))
        {
            AddReason(
                reasons,
                PlanSetVerificationReasonCode.MissingRequiredEvidence,
                "capabilities",
                sheet.SheetId,
                "The dependent sheet has no known typed projection method.");
            return PlanSetVerificationCheckStatus.InsufficientData;
        }

        if (audit.CanonicalRecipe is null)
        {
            AddReason(
                reasons,
                PlanSetVerificationReasonCode.MissingRequiredEvidence,
                "capabilities",
                sheet.SheetId,
                "The canonical recipe is required to evaluate projection capabilities.");
            return PlanSetVerificationCheckStatus.InsufficientData;
        }

        if (SheetAdjustmentProjectionCapabilities.TryGetUnsupportedReason(
                method,
                CanonicalOperationCount(audit.CanonicalRecipe),
                out var unsupportedReason))
        {
            AddReason(
                reasons,
                PlanSetVerificationReasonCode.UnsupportedCapability,
                "capabilities",
                sheet.SheetId,
                unsupportedReason ?? "The projection method does not support the canonical recipe.");
            return PlanSetVerificationCheckStatus.Unsupported;
        }

        return PlanSetVerificationCheckStatus.Passed;
    }

    private static PlanSetVerificationCheckDto BuildCheck(
        int expected,
        IEnumerable<PlanSetVerificationCheckStatus> statuses)
    {
        var evidence = statuses.ToArray();
        var passed = evidence.Count(status => status == PlanSetVerificationCheckStatus.Passed);
        var failed = evidence.Count(status => status is PlanSetVerificationCheckStatus.Failed or PlanSetVerificationCheckStatus.Unsupported);
        var missing = Math.Max(0, expected - evidence.Length) +
                      evidence.Count(status => status == PlanSetVerificationCheckStatus.InsufficientData);
        var status = evidence.Any(item => item == PlanSetVerificationCheckStatus.Unsupported)
            ? PlanSetVerificationCheckStatus.Unsupported
            : missing > 0
                ? PlanSetVerificationCheckStatus.InsufficientData
                : failed > 0
                    ? PlanSetVerificationCheckStatus.Failed
                    : PlanSetVerificationCheckStatus.Passed;
        return new PlanSetVerificationCheckDto(status, expected, passed, failed, missing);
    }

    private static void AddCongruenceReasons(
        IEnumerable<PlanSetCongruenceVerificationEvidence> evidence,
        PlanSetVerificationReasonCode failureCode,
        string check,
        ICollection<PlanSetVerificationReasonDto> reasons)
    {
        foreach (var item in evidence.Where(item => item.Status != PlanSetVerificationCheckStatus.Passed))
        {
            AddReason(
                reasons,
                item.Status == PlanSetVerificationCheckStatus.InsufficientData
                    ? PlanSetVerificationReasonCode.MissingRequiredEvidence
                    : failureCode,
                check,
                item.SheetId,
                item.Reason);
        }
    }

    private static void AddReason(
        ICollection<PlanSetVerificationReasonDto> reasons,
        PlanSetVerificationReasonCode code,
        string check,
        Guid? sheetId,
        string detail)
        => reasons.Add(new PlanSetVerificationReasonDto(code, check, sheetId, detail));

    private static bool IsDxfSafe(ProjectedPlanSheetDxfSafetyAuditDto safety)
        => safety.OutputFileExists &&
           safety.OutputFileBytes > 0 &&
           safety.EntityCountAfter > 0 &&
           safety.MissingHandleCountAfter == 0 &&
           safety.MissingOwnerCountAfter == 0 &&
           safety.UnsupportedCrossingEntityCount == 0;

    private static bool IsOutlineCongruent(ProjectedPlanSheetOutlineCongruenceAuditDto outline)
    {
        if (string.Equals(outline.Status, "Congruent", StringComparison.Ordinal))
        {
            return true;
        }

        if (string.Equals(outline.Status, "RegistrationProofAuthorized", StringComparison.Ordinal))
        {
            return !outline.NormalizationApplied &&
                   outline.CanonicalSourceOutline is null &&
                   outline.ElectricalSourceOutline is null &&
                   outline.ElectricalNormalizedSourceOutline is null &&
                   outline.ElectricalExportOutline is null &&
                   !outline.SourceWidthMismatchInches.HasValue &&
                   !outline.SourceHeightMismatchInches.HasValue &&
                   !outline.ExportWidthMismatchInches.HasValue &&
                   !outline.ExportHeightMismatchInches.HasValue &&
                   outline.AnchorX is null &&
                   outline.AnchorY is null &&
                   !outline.ScaleX.HasValue &&
                   !outline.ScaleY.HasValue;
        }

        return string.Equals(outline.Status, "MismatchRequiresNormalization", StringComparison.Ordinal) &&
               outline.NormalizationApplied &&
               outline.ExportWidthMismatchInches.HasValue &&
               outline.ExportHeightMismatchInches.HasValue &&
               Math.Abs(outline.ExportWidthMismatchInches.Value) <= outline.ToleranceInches &&
               Math.Abs(outline.ExportHeightMismatchInches.Value) <= outline.ToleranceInches;
    }

    private static bool HasOutputEvidence(ExportedPlanSheetDto sheet)
    {
        if (IsCanonicalSheet(sheet))
        {
            var verificationPath = ResolveVerificationPath(sheet);
            return !string.IsNullOrWhiteSpace(verificationPath) &&
                   File.Exists(verificationPath) &&
                   new FileInfo(verificationPath).Length > 0;
        }

        return sheet.ExportAudit?.DxfSafety is { OutputFileExists: true, OutputFileBytes: > 0 };
    }

    private static string? ResolveVerificationPath(ExportedPlanSheetDto sheet)
        => sheet.VerificationPath ?? sheet.StoragePath;

    private static bool IsCanonicalSheet(ExportedPlanSheetDto sheet)
        => string.Equals(sheet.SheetKind, "CanonicalFloorPlan", StringComparison.Ordinal);

    private static bool IsElectricalSheet(ExportedPlanSheetDto sheet)
        => Enum.TryParse<SheetAdjustmentProjectionMethod>(sheet.ProjectionMethod, out var method) &&
           method == SheetAdjustmentProjectionMethod.ElectricalWholeSheetSimilarity;

    private static async Task WriteAuditArtifactsAsync(
        string packageDirectory,
        MultiSheetExportAuditDto audit,
        CancellationToken cancellationToken)
    {
        var auditDirectory = Path.Combine(packageDirectory, "audit");
        Directory.CreateDirectory(auditDirectory);

        await WriteJsonAsync(Path.Combine(auditDirectory, "input-audit.json"), BuildInputAudit(audit), cancellationToken);
        await WriteJsonAsync(Path.Combine(auditDirectory, "canonical-recipe-audit.json"), BuildCanonicalRecipeAudit(audit), cancellationToken);
        await WriteJsonAsync(Path.Combine(auditDirectory, "floorplan-impact-audit.json"), BuildFloorPlanImpactAudit(audit), cancellationToken);
        await WriteJsonAsync(Path.Combine(auditDirectory, "electrical-registration-audit.json"), BuildElectricalRegistrationAudit(audit), cancellationToken);
        await WriteJsonAsync(Path.Combine(auditDirectory, "electrical-projection-audit.json"), BuildElectricalProjectionAudit(audit), cancellationToken);
        await WriteJsonAsync(Path.Combine(auditDirectory, "outline-congruence-audit.json"), BuildOutlineCongruenceAudit(audit), cancellationToken);
        await WriteJsonAsync(Path.Combine(auditDirectory, "outline-segment-congruence-audit.json"), PlanSetOutlineSegmentCongruenceAuditBuilder.Build(audit), cancellationToken);
        await WriteJsonAsync(Path.Combine(auditDirectory, "final-output-congruence-audit.json"), PlanSetOutlineSegmentCongruenceAuditBuilder.BuildFinalOutput(audit), cancellationToken);
        await WriteJsonAsync(Path.Combine(auditDirectory, "dxf-safety-audit.json"), BuildDxfSafetyAudit(audit), cancellationToken);
    }

    private static Task WriteJsonAsync(string path, object value, CancellationToken cancellationToken)
        => File.WriteAllTextAsync(path, JsonSerializer.Serialize(value, JsonOptions), cancellationToken);

    private static object BuildInputAudit(MultiSheetExportAuditDto audit)
    {
        var recipe = audit.CanonicalRecipe;
        var input = audit.CanonicalPlacement?.InputAudit;
        return new
        {
            stage = "input",
            audit.ExportId,
            audit.PlanSetVersionId,
            audit.CanonicalAdjustmentId,
            dimensionsInches = input is null
                ? null
                : new
                {
                    originalWidthInches = input.OriginalWidthInches,
                    originalHeightInches = input.OriginalHeightInches,
                    requestedWidthInches = input.RequestedWidthInches,
                    requestedHeightInches = input.RequestedHeightInches,
                    requiredWidthDeltaInches = input.RequiredWidthDeltaInches,
                    requiredHeightDeltaInches = input.RequiredHeightDeltaInches,
                    source = input.Source
                },
            inputAuditStatus = input is null
                ? "MissingInputAudit"
                : "Captured",
            requiredDeltaSourceUnits = new
            {
                width = SumDelta(recipe, "Width"),
                height = SumDelta(recipe, "Height")
            },
            placement = audit.CanonicalPlacement is null
                ? null
                : new
                {
                    audit.CanonicalPlacement.FloorToSiteScale,
                    audit.CanonicalPlacement.SiteOffsetX,
                    audit.CanonicalPlacement.SiteOffsetY,
                    CompressionStepCount = audit.CanonicalPlacement.CompressionSteps.Count,
                    StretchActionCount = audit.CanonicalPlacement.StretchActions.Count,
                    AdjustedDimensionCount = audit.CanonicalPlacement.AdjustedDimensions.Count
                }
        };
    }

    private static object BuildCanonicalRecipeAudit(MultiSheetExportAuditDto audit)
        => new
        {
            stage = "canonical-recipe",
            audit.ExportId,
            audit.CanonicalAdjustmentId,
            recipe = audit.CanonicalRecipe,
            operationCount = audit.CanonicalRecipe is null ? 0 : CanonicalOperationCount(audit.CanonicalRecipe),
            legacyOperationCount = audit.CanonicalRecipe?.Operations.Count ?? 0,
            stretchActionCount = audit.CanonicalRecipe?.StretchActions.Count ?? 0
        };

    private static object BuildFloorPlanImpactAudit(MultiSheetExportAuditDto audit)
    {
        var operations = audit.CanonicalPlacement?.FloorPlanImpactAudit;
        var operationRows = operations is { Count: > 0 }
            ? operations.Select(operation => (object)new
            {
                operationId = operation.OperationId,
                operationIndex = operation.OperationIndex,
                operation.Kind,
                operation.AxisTag,
                operation.Edge,
                operation.Coordinate,
                expectedDeltaSourceUnits = operation.ExpectedDeltaSourceUnits,
                affectedEntities = operation.AffectedEntities,
                affectedVertices = operation.AffectedVertices,
                measuredMinDeltaSourceUnits = operation.MeasuredMinDeltaSourceUnits,
                measuredMaxDeltaSourceUnits = operation.MeasuredMaxDeltaSourceUnits,
                operation.Status,
                operation.Warning
            }).ToArray()
            : Array.Empty<object>();

        return new
        {
            stage = "floorplan-impact",
            audit.ExportId,
            audit.CanonicalAdjustmentId,
            adjustedDimensionCount = audit.CanonicalPlacement?.AdjustedDimensions.Count,
            operations = operationRows
        };
    }

    private static object BuildElectricalRegistrationAudit(MultiSheetExportAuditDto audit)
        => new
        {
            stage = "electrical-registration",
            audit.ExportId,
            audit.CanonicalAdjustmentId,
            sheets = ElectricalSheets(audit)
                .Select(sheet => new
                {
                    sheet.SheetId,
                    sheet.ProjectionId,
                    sheet.Status,
                    sheet.ProjectionMethod,
                    sheet.Confidence,
                    sheet.RuleSummary,
                    registrationEvidence = sheet.ProjectionId.HasValue
                        ? "Projection references a confirmed registration."
                        : "No projection exists for this ElectricalPlan."
                })
                .ToArray()
        };

    private static object BuildElectricalProjectionAudit(MultiSheetExportAuditDto audit)
        => new
        {
            stage = "electrical-projection",
            audit.ExportId,
            audit.CanonicalAdjustmentId,
            sheets = ElectricalSheets(audit)
                .Select(sheet => new
                {
                    sheet.SheetId,
                    sheet.ProjectionId,
                    sheet.Status,
                    sheet.Warning,
                    operations = sheet.ExportAudit?.Operations ??
                        BuildOperationAudits(
                            audit.CanonicalRecipe,
                            statusWhenNoExportAudit: ResolveProjectionFallbackStatus(sheet),
                            reasonWhenNoExportAudit: sheet.Warning ?? "No projected Electrical DXF audit was attached.")
                })
                .ToArray()
        };

    private static object BuildDxfSafetyAudit(MultiSheetExportAuditDto audit)
        => new
        {
            stage = "dxf-safety",
            audit.ExportId,
            audit.CanonicalAdjustmentId,
            canonicalFloorPlan = new
            {
                path = audit.Sheets.FirstOrDefault(sheet => sheet.SheetKind == "CanonicalFloorPlan")?.StoragePath,
                status = "Exported"
            },
            dependentSheets = audit.Sheets
                .Where(sheet => sheet.SheetKind != "CanonicalFloorPlan")
                .Select(sheet => new
                {
                    sheet.SheetId,
                    sheet.ProjectionId,
                    sheet.SheetKind,
                    sheet.Status,
                    sheet.StoragePath,
                    dxfSafety = sheet.ExportAudit?.DxfSafety ?? BuildMissingDxfSafety(sheet)
                })
                .ToArray()
        };

    private static object BuildOutlineCongruenceAudit(MultiSheetExportAuditDto audit)
        => new
        {
            stage = "outline-congruence",
            audit.ExportId,
            audit.CanonicalAdjustmentId,
            sheets = ElectricalSheets(audit)
                .Select(sheet => new
                {
                    sheet.SheetId,
                    sheet.ProjectionId,
                    sheet.Status,
                    outlineCongruence = sheet.ExportAudit?.OutlineCongruence ?? new ProjectedPlanSheetOutlineCongruenceAuditDto(
                        "InsufficientData",
                        "No projected Electrical outline audit was attached.",
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
                        ScaleY: null)
                })
                .ToArray()
        };

    private static ProjectedPlanSheetDxfSafetyAuditDto BuildMissingDxfSafety(ExportedPlanSheetDto sheet)
    {
        var verificationPath = ResolveVerificationPath(sheet);
        return new ProjectedPlanSheetDxfSafetyAuditDto(
            OutputFileExists: !string.IsNullOrWhiteSpace(verificationPath) && File.Exists(verificationPath),
            OutputFileBytes: !string.IsNullOrWhiteSpace(verificationPath) && File.Exists(verificationPath)
                ? new FileInfo(verificationPath).Length
                : 0,
            EntityCountBefore: 0,
            EntityCountAfter: 0,
            InsertCountAfter: 0,
            DimensionCountAfter: 0,
            EllipseCountAfter: 0,
            WireOrCurveCountAfter: 0,
            MissingHandleCountAfter: 0,
            MissingOwnerCountAfter: 0,
            UnsupportedCrossingEntityCount: sheet.Status == "ProjectedAutomatically" ? 0 : 1);
    }

    private static IReadOnlyList<ProjectedPlanSheetOperationAuditDto> BuildOperationAudits(
        AdjustmentRecipeSummaryDto? recipe,
        string statusWhenNoExportAudit,
        string reasonWhenNoExportAudit)
    {
        if (recipe is null)
        {
            return [];
        }

        if (recipe.StretchActions.Count > 0)
        {
            return recipe.StretchActions
                .Select((action, index) => new ProjectedPlanSheetOperationAuditDto(
                    action.ActionId,
                    index,
                    "CadStretch",
                    action.AxisTag,
                    action.Edge,
                    action.CutCoordinate,
                    action.DeltaSourceUnits,
                    AffectedEntities: 0,
                    AffectedVertices: 0,
                    MeasuredMinDeltaSourceUnits: 0,
                    MeasuredMaxDeltaSourceUnits: 0,
                    statusWhenNoExportAudit,
                    reasonWhenNoExportAudit))
                .ToArray();
        }

        return recipe.Operations
            .Select((operation, index) => new ProjectedPlanSheetOperationAuditDto(
                $"operation-{index}",
                index,
                operation.Kind,
                operation.AxisTag,
                operation.Edge,
                operation.Coordinate,
                operation.DeltaSourceUnits,
                AffectedEntities: 0,
                AffectedVertices: 0,
                MeasuredMinDeltaSourceUnits: 0,
                MeasuredMaxDeltaSourceUnits: 0,
                statusWhenNoExportAudit,
                reasonWhenNoExportAudit))
            .ToArray();
    }

    private static IEnumerable<ExportedPlanSheetDto> ElectricalSheets(MultiSheetExportAuditDto audit)
        => audit.Sheets.Where(sheet => sheet.SheetKind.Contains("Electrical", StringComparison.OrdinalIgnoreCase));

    private static string ResolveProjectionFallbackStatus(ExportedPlanSheetDto sheet)
        => sheet.Status == "RequiresManualConfirmation"
            ? "RequiresManualReview"
            : "Failed";

    private static decimal SumDelta(AdjustmentRecipeSummaryDto? recipe, string axisTag)
        => recipe is null
            ? 0m
            : recipe.StretchActions.Count > 0
                ? recipe.StretchActions
                    .Where(action => string.Equals(action.AxisTag, axisTag, StringComparison.OrdinalIgnoreCase))
                    .Sum(action => action.DeltaSourceUnits)
                : recipe.Operations
                    .Where(operation => string.Equals(operation.AxisTag, axisTag, StringComparison.OrdinalIgnoreCase))
                    .Sum(operation => operation.DeltaSourceUnits);

    private static int CanonicalOperationCount(AdjustmentRecipeSummaryDto recipe)
        => recipe.StretchActions.Count > 0
            ? recipe.StretchActions.Count
            : recipe.Operations.Count;
}
