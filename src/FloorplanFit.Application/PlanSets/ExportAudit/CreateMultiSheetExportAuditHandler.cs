using System.Globalization;
using System.Text.Json;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.DataCollection;
using FloorplanFit.Application.PlanSets.Export;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.PlanSets.ExportAudit;

public sealed class CreateMultiSheetExportAuditHandler
{
    private const string CanonicalSheetKind = "CanonicalFloorPlan";
    private const string DependentSheetKind = "DependentPlanSheet";
    private const string CanonicalProjectionMethod = "CanonicalFloorPlanAdjustment";
    private const string MissingProjectionWarning = "No projection exists for this sheet and canonical adjustment.";

    private readonly ISheetAdjustmentProjectionRepository sheetAdjustmentProjectionRepository;
    private readonly IPlanSheetReader planSheetReader;
    private readonly IPlanSetExportRepository planSetExportRepository;
    private readonly IPlanSetAuditEventRepository planSetAuditEventRepository;
    private readonly IPlanSetExportManifestWriter planSetExportManifestWriter;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;
    private readonly GetPlanSetQualityReportHandler? qualityReportHandler;
    private readonly ICanonicalFloorPlanAdjustmentRepository? canonicalFloorPlanAdjustmentRepository;

    public CreateMultiSheetExportAuditHandler(
        ISheetAdjustmentProjectionRepository sheetAdjustmentProjectionRepository,
        IPlanSheetReader planSheetReader,
        IPlanSetExportRepository planSetExportRepository,
        IPlanSetAuditEventRepository planSetAuditEventRepository,
        IPlanSetExportManifestWriter planSetExportManifestWriter,
        IUnitOfWork unitOfWork,
        IClock clock,
        GetPlanSetQualityReportHandler? qualityReportHandler = null,
        ICanonicalFloorPlanAdjustmentRepository? canonicalFloorPlanAdjustmentRepository = null)
    {
        this.sheetAdjustmentProjectionRepository = sheetAdjustmentProjectionRepository;
        this.planSheetReader = planSheetReader;
        this.planSetExportRepository = planSetExportRepository;
        this.planSetAuditEventRepository = planSetAuditEventRepository;
        this.planSetExportManifestWriter = planSetExportManifestWriter;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
        this.qualityReportHandler = qualityReportHandler;
        this.canonicalFloorPlanAdjustmentRepository = canonicalFloorPlanAdjustmentRepository;
    }

    public Task<MultiSheetExportAuditDto> HandleAsync(
        CreateMultiSheetExportAuditRequest request,
        CancellationToken cancellationToken)
        => HandleAsync(request, _ => Task.CompletedTask, cancellationToken);

    public async Task<MultiSheetExportAuditDto> HandleAsync(
        CreateMultiSheetExportAuditRequest request,
        Func<CancellationToken, Task> publishPackageAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.DependentProjections);
        ArgumentNullException.ThrowIfNull(publishPackageAsync);

        if (request.PlanSetVersionId == Guid.Empty)
        {
            throw new ArgumentException("Plan-set version is required.", nameof(request));
        }

        if (request.CanonicalFloorPlanVersionId == Guid.Empty)
        {
            throw new ArgumentException("Canonical floor-plan version is required.", nameof(request));
        }

        if (request.CanonicalAdjustmentId == Guid.Empty)
        {
            throw new ArgumentException("Canonical adjustment is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.CanonicalFloorPlanExportPath))
        {
            throw new ArgumentException("Canonical floor-plan export path is required.", nameof(request));
        }

        var exportId = Guid.NewGuid();
        var createdAtUtc = clock.UtcNow;
        var sheets = new List<PlanSetExportedSheet>
        {
            new(
                id: Guid.NewGuid(),
                planSetExportId: exportId,
                planSheetId: request.CanonicalFloorPlanVersionId,
                sheetProjectionId: null,
                sheetKind: CanonicalSheetKind,
                storagePath: request.CanonicalFloorPlanExportPath,
                status: PlanSetExportedSheetStatus.Exported,
                projectionMethod: CanonicalProjectionMethod,
                confidence: 1m,
                warning: null,
                ruleSummary: null)
        };

        if (request.DiscoverAllDependentSheets || request.DependentProjections.Count == 0)
        {
            await AddDiscoveredDependentSheetsAsync(exportId, request, sheets, cancellationToken);
        }
        else
        {
            foreach (var projectionRequest in request.DependentProjections)
            {
                sheets.Add(await BuildDependentSheetAsync(
                    exportId,
                    request,
                    projectionRequest,
                    cancellationToken));
            }
        }

        var preVerificationSummary = BuildSummary(sheets);
        var qualityReport = await TryBuildQualityReportAsync(request.PlanSetVersionId, cancellationToken);
        var storedCanonicalDetails = request.CanonicalPlacement is null || request.CanonicalRecipe is null
            ? await TryLoadCanonicalAdjustmentDetailsAsync(request.CanonicalAdjustmentId, cancellationToken)
            : (Placement: request.CanonicalPlacement, Recipe: request.CanonicalRecipe);
        var canonicalPlacement = request.CanonicalPlacement ?? storedCanonicalDetails.Placement;
        var canonicalRecipe = request.CanonicalRecipe ?? storedCanonicalDetails.Recipe;
        PlanSetVerificationReportDto verificationReport;
        PlanSetExportStatus status;
        ProjectionAuditSummaryDto summary;
        string? packageManifestPath = null;
        var failureStage = PlanSetExportFailureStage.VerificationAndWorkspacePublication;
        try
        {
            var verificationInput = ToDto(
                exportId,
                request.PlanSetVersionId,
                request.CanonicalAdjustmentId,
                status: "PendingVerification",
                packageManifestPath: null,
                createdAtUtc,
                sheets,
                preVerificationSummary,
                qualityReport,
                request.DependentProjections,
                canonicalPlacement,
                canonicalRecipe,
                verificationReport: null,
                packageArtifacts: request.PackageArtifacts,
                canonicalFloorPlanVerificationPath: request.CanonicalFloorPlanVerificationPath);
            verificationReport = planSetExportManifestWriter.BuildVerificationReport(verificationInput);
            status = verificationReport.IsGreen
                ? PlanSetExportStatus.ReadyForExport
                : PlanSetExportStatus.RequiresManualConfirmation;
            summary = preVerificationSummary with
            {
                CanExportPackageAutomatically = verificationReport.IsGreen
            };
            var draftAudit = ToDto(
                exportId,
                request.PlanSetVersionId,
                request.CanonicalAdjustmentId,
                status.ToString(),
                packageManifestPath: null,
                createdAtUtc,
                sheets,
                summary,
                qualityReport,
                request.DependentProjections,
                canonicalPlacement,
                canonicalRecipe,
                verificationReport,
                request.PackageArtifacts,
                request.CanonicalFloorPlanVerificationPath);
            packageManifestPath = await planSetExportManifestWriter.WriteAsync(draftAudit, cancellationToken);
            if (string.IsNullOrWhiteSpace(packageManifestPath))
            {
                throw new InvalidOperationException("Workspace manifest writer returned no manifest path.");
            }

            failureStage = PlanSetExportFailureStage.UserPackagePublication;
            if (!string.IsNullOrWhiteSpace(request.PackageStagingDirectory))
            {
                await planSetExportManifestWriter.WritePackageArtifactsAsync(
                    draftAudit with { PackageManifestPath = packageManifestPath },
                    request.PackageStagingDirectory,
                    cancellationToken);
            }

            await publishPackageAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            TryRollbackWorkspacePublication(packageManifestPath, exception);
            await TryPersistFailureAsync(
                exportId,
                request.PlanSetVersionId,
                request.CanonicalAdjustmentId,
                createdAtUtc,
                sheets,
                failureStage,
                exception);
            throw;
        }

        var publishedManifestPath = packageManifestPath
            ?? throw new InvalidOperationException("Workspace manifest publication did not complete.");
        try
        {
            if (canonicalFloorPlanAdjustmentRepository is not null)
            {
                await canonicalFloorPlanAdjustmentRepository.UpdateExportPathAsync(
                    request.CanonicalAdjustmentId,
                    request.CanonicalFloorPlanExportPath,
                    CancellationToken.None);
            }

            var export = new PlanSetExport(
                exportId,
                request.PlanSetVersionId,
                request.CanonicalAdjustmentId,
                status,
                JsonSerializer.Serialize(verificationReport),
                publishedManifestPath,
                createdAtUtc,
                sheets);
            var response = ToDto(
                export.Id,
                export.PlanSetVersionId,
                export.CanonicalAdjustmentId,
                export.Status.ToString(),
                export.PackageManifestPath,
                export.CreatedAtUtc,
                export.Sheets,
                summary,
                qualityReport,
                request.DependentProjections,
                canonicalPlacement,
                canonicalRecipe,
                verificationReport,
                request.PackageArtifacts,
                request.CanonicalFloorPlanVerificationPath);

            // Atomic files are already visible. Finish the DB commit even if cancellation arrives after that point.
            await planSetExportRepository.AddAsync(export, CancellationToken.None);
            await TryRecordAuditEventAsync(export, verificationReport, CancellationToken.None);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            return response;
        }
        catch (Exception exception)
        {
            TryRollbackWorkspacePublication(publishedManifestPath, exception);
            try
            {
                await unitOfWork.RollbackAsync(CancellationToken.None);
            }
            catch (Exception rollbackException)
            {
                exception.Data["UnitOfWorkRollbackFailure"] = rollbackException.Message;
            }

            throw;
        }
    }

    private static void TryRollbackWorkspacePublication(string? manifestPath, Exception exception)
    {
        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            return;
        }

        try
        {
            var workspacePackageDirectory = Path.GetDirectoryName(manifestPath);
            if (!string.IsNullOrWhiteSpace(workspacePackageDirectory))
            {
                AtomicDirectoryPublisher.RollbackPublishedDirectory(workspacePackageDirectory);
            }
        }
        catch (Exception rollbackException)
        {
            exception.Data["WorkspacePackageRollbackFailure"] = rollbackException.Message;
        }
    }

    public async Task<bool> TryRecordFailureAsync(
        Guid planSetVersionId,
        Guid canonicalFloorPlanVersionId,
        Guid canonicalAdjustmentId,
        string canonicalFloorPlanExportPath,
        PlanSetExportFailureStage stage,
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var exportId = Guid.NewGuid();
        var createdAtUtc = clock.UtcNow;
        var sheets = new[]
        {
            new PlanSetExportedSheet(
                id: Guid.NewGuid(),
                planSetExportId: exportId,
                planSheetId: canonicalFloorPlanVersionId,
                sheetProjectionId: null,
                sheetKind: CanonicalSheetKind,
                storagePath: canonicalFloorPlanExportPath,
                status: PlanSetExportedSheetStatus.Exported,
                projectionMethod: CanonicalProjectionMethod,
                confidence: 1m,
                warning: null,
                ruleSummary: null)
        };

        return await TryPersistFailureAsync(
            exportId,
            planSetVersionId,
            canonicalAdjustmentId,
            createdAtUtc,
            sheets,
            stage,
            exception);
    }

    private async Task<bool> TryPersistFailureAsync(
        Guid exportId,
        Guid planSetVersionId,
        Guid canonicalAdjustmentId,
        DateTime createdAtUtc,
        IReadOnlyList<PlanSetExportedSheet> sheets,
        PlanSetExportFailureStage stage,
        Exception exception)
    {
        try
        {
            var failure = new PlanSetExportFailureDto(
                PlanSetExportFailureDto.CurrentSchemaVersion,
                stage,
                exception is OperationCanceledException,
                exception.GetType().FullName ?? exception.GetType().Name,
                string.IsNullOrWhiteSpace(exception.Message)
                    ? "Plan-set package export failed."
                    : exception.Message);
            var failedExport = new PlanSetExport(
                exportId,
                planSetVersionId,
                canonicalAdjustmentId,
                PlanSetExportStatus.Failed,
                JsonSerializer.Serialize(failure),
                packageManifestPath: null,
                createdAtUtc,
                sheets);

            await planSetExportRepository.AddAsync(failedExport, CancellationToken.None);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            return true;
        }
        catch (Exception)
        {
            // Failure telemetry is compensating evidence. Never replace the original export error.
            return false;
        }
    }

    private async Task<PlanSetExportedSheet> BuildDependentSheetAsync(
        Guid exportId,
        CreateMultiSheetExportAuditRequest request,
        MultiSheetExportProjectionRequestDto projectionRequest,
        CancellationToken cancellationToken)
    {
        if (projectionRequest.ProjectionId == Guid.Empty)
        {
            throw new ArgumentException("Projection id is required.", nameof(request));
        }

        var projection = await sheetAdjustmentProjectionRepository.GetByIdAsync(
            projectionRequest.ProjectionId,
            cancellationToken);
        if (projection is null)
        {
            throw new InvalidOperationException("Sheet adjustment projection was not found.");
        }

        if (projection.PlanSetVersionId != request.PlanSetVersionId)
        {
            throw new ArgumentException("Projection does not belong to the requested plan-set version.", nameof(request));
        }

        if (projection.CanonicalAdjustmentId != request.CanonicalAdjustmentId)
        {
            throw new ArgumentException("Projection does not belong to the requested canonical adjustment.", nameof(request));
        }

        var status = projection.Status is SheetAdjustmentProjectionStatus.ReadyForExport
            ? PlanSetExportedSheetStatus.ProjectedAutomatically
            : PlanSetExportedSheetStatus.RequiresManualConfirmation;

        return BuildProjectedSheet(
            exportId,
            projection,
            DependentSheetKind,
            projectionRequest.ExportPath,
            status);
    }

    private async Task AddDiscoveredDependentSheetsAsync(
        Guid exportId,
        CreateMultiSheetExportAuditRequest request,
        List<PlanSetExportedSheet> sheets,
        CancellationToken cancellationToken)
    {
        var dependentSheetsByVersion = await planSheetReader.ListByPlanSetVersionIdsAsync(
            [request.PlanSetVersionId],
            cancellationToken);

        if (!dependentSheetsByVersion.TryGetValue(request.PlanSetVersionId, out var dependentSheets))
        {
            return;
        }

        var projections = await sheetAdjustmentProjectionRepository.ListByPlanSetVersionAndCanonicalAdjustmentAsync(
            request.PlanSetVersionId,
            request.CanonicalAdjustmentId,
            cancellationToken);
        var exportPathByProjectionId = request.DependentProjections.ToDictionary(
            projection => projection.ProjectionId,
            projection => projection.ExportPath);
        var latestProjectionBySheet = projections
            .GroupBy(projection => projection.DependentSheetId)
            .ToDictionary(
                group => group.Key,
                // Tie-break by id exactly as ExportMultiSheetPlanSetPackageHandler does when
                // it assigns export paths. Without it, equal timestamps make the two places
                // choose different projections, so the projection audited here has no export
                // path and PlanSetExportedSheet rejects ProjectedAutomatically without one.
                group => group
                    .OrderBy(projection => projection.CreatedAtUtc)
                    .ThenBy(projection => projection.Id)
                    .Last());

        foreach (var sheet in dependentSheets.Where(sheet => !sheet.IsCanonical))
        {
            if (latestProjectionBySheet.TryGetValue(sheet.SheetId, out var projection))
            {
                var status = projection.Status is SheetAdjustmentProjectionStatus.ReadyForExport
                    ? PlanSetExportedSheetStatus.ProjectedAutomatically
                    : PlanSetExportedSheetStatus.RequiresManualConfirmation;

                sheets.Add(BuildProjectedSheet(
                    exportId,
                    projection,
                    sheet.SheetType,
                    storagePath: exportPathByProjectionId.GetValueOrDefault(projection.Id),
                    status: status));
                continue;
            }

            sheets.Add(new PlanSetExportedSheet(
                id: Guid.NewGuid(),
                planSetExportId: exportId,
                planSheetId: sheet.SheetId,
                sheetProjectionId: null,
                sheetKind: sheet.SheetType,
                storagePath: null,
                status: PlanSetExportedSheetStatus.MissingProjection,
                projectionMethod: null,
                confidence: null,
                warning: MissingProjectionWarning,
                ruleSummary: null));
        }
    }

    private static PlanSetExportedSheet BuildProjectedSheet(
        Guid exportId,
        SheetAdjustmentProjection projection,
        string sheetKind,
        string? storagePath,
        PlanSetExportedSheetStatus status)
    {
        return new PlanSetExportedSheet(
            id: Guid.NewGuid(),
            planSetExportId: exportId,
            planSheetId: projection.DependentSheetId,
            sheetProjectionId: projection.Id,
            sheetKind: sheetKind,
            storagePath: storagePath,
            status: status,
            projectionMethod: projection.Method.ToString(),
            confidence: projection.Confidence,
            warning: projection.Warning,
            ruleSummary: projection.RuleSummary,
            recipeHandlingSummary: projection.RecipeHandlingSummary);
    }

    private static ProjectionAuditSummaryDto BuildSummary(IReadOnlyList<PlanSetExportedSheet> sheets)
    {
        var dependentSheets = sheets
            .Where(sheet => sheet.SheetKind != CanonicalSheetKind)
            .ToArray();
        var automaticallyProjectedCount = dependentSheets.Count(
            sheet => sheet.Status is PlanSetExportedSheetStatus.ProjectedAutomatically);
        var manualCount = dependentSheets.Count(
            sheet => sheet.Status is PlanSetExportedSheetStatus.RequiresManualConfirmation or
                PlanSetExportedSheetStatus.MissingProjection);
        var confidences = dependentSheets
            .Where(sheet => sheet.Confidence.HasValue)
            .Select(sheet => sheet.Confidence!.Value)
            .ToArray();

        return new ProjectionAuditSummaryDto(
            sheets.Count,
            automaticallyProjectedCount,
            manualCount,
            confidences.Length == 0 ? null : confidences.Min(),
            manualCount == 0);
    }

    private async Task TryRecordAuditEventAsync(
        PlanSetExport export,
        PlanSetVerificationReportDto verificationReport,
        CancellationToken cancellationToken)
    {
        try
        {
            await planSetAuditEventRepository.AddAsync(
                new PlanSetAuditEvent(
                    Guid.NewGuid(),
                    "PlanSetExport",
                    export.Id,
                    "PlanSetExportAuditCreated",
                    JsonSerializer.Serialize(verificationReport),
                    export.CreatedAtUtc),
                cancellationToken);
        }
        catch (Exception)
        {
            // ponytail: telemetry is best-effort; promote to durable retry queue if audit_events becomes business-critical.
        }
    }

    private async Task<PlanSetQualityReportDto?> TryBuildQualityReportAsync(
        Guid planSetVersionId,
        CancellationToken cancellationToken)
    {
        if (qualityReportHandler is null)
        {
            return null;
        }

        try
        {
            return await qualityReportHandler.HandleAsync(planSetVersionId, cancellationToken);
        }
        catch (Exception)
        {
            // ponytail: quality reporting is export metadata; keep package export alive unless it becomes a hard business gate.
            return null;
        }
    }

    private async Task<(AdjustedSitePlanPlacementDto? Placement, AdjustmentRecipeSummaryDto? Recipe)> TryLoadCanonicalAdjustmentDetailsAsync(
        Guid canonicalAdjustmentId,
        CancellationToken cancellationToken)
    {
        if (canonicalFloorPlanAdjustmentRepository is null)
        {
            return (null, null);
        }

        try
        {
            var adjustment = await canonicalFloorPlanAdjustmentRepository.GetByIdAsync(
                canonicalAdjustmentId,
                cancellationToken);
            return adjustment is null
                ? (null, null)
                : (
                    JsonSerializer.Deserialize<AdjustedSitePlanPlacementDto>(adjustment.PlacementJson),
                    JsonSerializer.Deserialize<AdjustmentRecipeSummaryDto>(adjustment.AdjustmentRecipeJson));
        }
        catch (Exception)
        {
            // ponytail: observability metadata is best-effort here; projection/export safety still decides success.
            return (null, null);
        }
    }

    private static MultiSheetExportAuditDto ToDto(
        Guid exportId,
        Guid planSetVersionId,
        Guid canonicalAdjustmentId,
        string status,
        string? packageManifestPath,
        DateTime createdAtUtc,
        IReadOnlyList<PlanSetExportedSheet> sheets,
        ProjectionAuditSummaryDto summary,
        PlanSetQualityReportDto? qualityReport = null,
        IReadOnlyList<MultiSheetExportProjectionRequestDto>? dependentProjections = null,
        AdjustedSitePlanPlacementDto? canonicalPlacement = null,
        AdjustmentRecipeSummaryDto? canonicalRecipe = null,
        PlanSetVerificationReportDto? verificationReport = null,
        IReadOnlyList<PlanSetPackageArtifactDto>? packageArtifacts = null,
        string? canonicalFloorPlanVerificationPath = null)
    {
        var exportAudits = dependentProjections?
            .Where(projection => projection.ExportAudit is not null)
            .ToDictionary(
                projection => projection.ProjectionId,
                projection => projection.ExportAudit!);
        var verificationPaths = dependentProjections?
            .Where(projection => !string.IsNullOrWhiteSpace(projection.VerificationPath))
            .ToDictionary(
                projection => projection.ProjectionId,
                projection => projection.VerificationPath!);
        var sheetDtos = sheets
            .Select(sheet => ToDto(
                sheet,
                exportAudits,
                verificationPaths,
                canonicalFloorPlanVerificationPath))
            .ToArray();

        return new MultiSheetExportAuditDto(
            exportId,
            planSetVersionId,
            canonicalAdjustmentId,
            status,
            summary,
            sheetDtos,
            packageManifestPath,
            createdAtUtc,
            qualityReport,
            canonicalPlacement,
            canonicalRecipe)
        {
            Verification = verificationReport,
            HumanSummary = BuildHumanSummary(summary, sheetDtos, canonicalPlacement, canonicalRecipe, verificationReport),
            Artifacts = packageArtifacts ?? []
        };
    }

    private static IReadOnlyList<string> BuildHumanSummary(
        ProjectionAuditSummaryDto summary,
        IReadOnlyList<ExportedPlanSheetDto> sheets,
        AdjustedSitePlanPlacementDto? canonicalPlacement,
        AdjustmentRecipeSummaryDto? canonicalRecipe,
        PlanSetVerificationReportDto? verificationReport)
    {
        var floorPlanImpacts = canonicalPlacement?.FloorPlanImpactAudit ?? [];
        var recipeOperations = canonicalRecipe?.Operations ?? [];
        var stretchActions = canonicalRecipe?.StretchActions ?? [];
        var floorPlanTotal = stretchActions.Count > 0
            ? stretchActions.Count
            : recipeOperations.Count == 0
                ? floorPlanImpacts.Count
                : recipeOperations.Count;
        var floorPlanApplied = floorPlanImpacts.Count(operation => operation.Status == "Applied");
        var floorPlanWarnings = floorPlanImpacts.Count(operation =>
            operation.Status != "Applied" || !string.IsNullOrWhiteSpace(operation.Warning));
        var widthDelta = SumRecipeDelta(canonicalRecipe, "Width");
        var heightDelta = SumRecipeDelta(canonicalRecipe, "Height");
        var widthOperationCount = CountRecipeActions(canonicalRecipe, "Width");
        var heightOperationCount = CountRecipeActions(canonicalRecipe, "Height");

        var electricalOperations = sheets
            .Where(sheet => sheet.SheetKind.Contains("Electrical", StringComparison.OrdinalIgnoreCase))
            .SelectMany(sheet => sheet.ExportAudit?.Operations ?? [])
            .ToArray();
        var electricalApplied = electricalOperations.Count(operation => operation.Status == "Applied");
        var electricalWarnings = electricalOperations.Count(operation => operation.Status != "Applied") +
            summary.ManualConfirmationRequiredSheetCount;
        var missingData = BuildMissingDataList(
            canonicalPlacement,
            canonicalRecipe,
            floorPlanImpacts.Count,
            electricalOperations.Length,
            HasOutlineCongruenceAudit(sheets));

        var lines = new List<string>
        {
            BuildInputSummary(canonicalPlacement?.InputAudit, widthDelta, heightDelta),
            $"Que achico el FloorPlan: ancho {FormatInches(widthDelta)} con {widthOperationCount} operacion(es); alto {FormatInches(heightDelta)} con {heightOperationCount} operacion(es).",
            $"FloorPlan: aplico {floorPlanApplied}/{floorPlanTotal}; afecto {floorPlanImpacts.Sum(operation => operation.AffectedEntities)} entidades y {floorPlanImpacts.Sum(operation => operation.AffectedVertices)} vertices; warnings {floorPlanWarnings}.",
            $"Electrical: recibio {floorPlanTotal} operacion(es) canonicas; aplico {electricalApplied}/{electricalOperations.Length}; afecto {electricalOperations.Sum(operation => operation.AffectedEntities)} entidades y {electricalOperations.Sum(operation => operation.AffectedVertices)} vertices.",
            BuildElectricalNotAppliedSummary(electricalOperations),
            BuildOutlineCongruenceSummary(sheets),
            "Segment congruence: revisar outline-segment-congruence-audit.json; el verifier bloquea export automatico si las paredes/bordes no matchean.",
            BuildDxfSafetySummary(sheets),
            floorPlanWarnings + electricalWarnings == 0
                ? "Warnings/failures: none."
                : $"Warnings/failures: floorplan {floorPlanWarnings}, electrical {electricalWarnings}, manual/missing {summary.ManualConfirmationRequiredSheetCount}.",
            missingData.Count == 0
                ? "Datos suficientes: input, receta canonica, impacto FloorPlan, proyeccion Electrical, segmentos estructurales y DXF safety estan auditados."
                : $"Datos insuficientes: falta {string.Join(", ", missingData)}. No declares success sin completar esa evidencia."
        };
        if (verificationReport is not null)
        {
            lines.Add(verificationReport.IsGreen
                ? $"Verification v{verificationReport.SchemaVersion}: ReadyForExport; todos los gates tipados pasaron."
                : $"Verification v{verificationReport.SchemaVersion}: bloqueado por {string.Join(", ", verificationReport.Reasons.Select(reason => reason.Code))}.");
        }

        return lines;
    }

    private static string BuildOutlineCongruenceSummary(IReadOnlyList<ExportedPlanSheetDto> sheets)
    {
        var outline = sheets
            .Where(sheet => sheet.SheetKind.Contains("Electrical", StringComparison.OrdinalIgnoreCase))
            .Select(sheet => sheet.ExportAudit?.OutlineCongruence)
            .FirstOrDefault(audit => audit is not null);
        if (outline is null)
        {
            return "Outline congruence: faltan datos; no puedo declarar overlay confiable.";
        }

        if (outline.Status == "Congruent")
        {
            return "Outline congruence: OK; Electrical y FloorPlan arrancan con outline compatible dentro de tolerancia.";
        }

        if (outline.Status == "RegistrationProofAuthorized")
        {
            return "Outline authorization: el registro global confirmado y ligado a las fuentes autoriza el marco canonico; este stage no inventa bounds ni residuales medidos. Congruencia final, operaciones y DXF safety siguen siendo obligatorios.";
        }

        if (outline.Status == "MismatchRequiresNormalization" && outline.NormalizationApplied)
        {
            return $"Outline congruence: Electrical normalizado antes de la receta; mismatch origen ancho {FormatNullableInches(outline.SourceWidthMismatchInches)}, alto {FormatNullableInches(outline.SourceHeightMismatchInches)}; mismatch export ancho {FormatNullableInches(outline.ExportWidthMismatchInches)}, alto {FormatNullableInches(outline.ExportHeightMismatchInches)}.";
        }

        return $"Outline congruence: {outline.Status}; {outline.Reason}";
    }

    private static string BuildInputSummary(
        AdjustmentInputAuditDto? input,
        decimal widthDelta,
        decimal heightDelta)
        => input is null
            ? $"Resumen AI: faltan dimensiones de entrada; solo puedo inferir recorte desde la receta: ancho {FormatInches(widthDelta)}, alto {FormatInches(heightDelta)}."
            : $"Resumen AI: el usuario pidio achicar ancho {FormatInches(input.RequiredWidthDeltaInches)} y alto {FormatInches(input.RequiredHeightDeltaInches)} ({FormatInches(input.OriginalWidthInches)} x {FormatInches(input.OriginalHeightInches)} -> {FormatInches(input.RequestedWidthInches)} x {FormatInches(input.RequestedHeightInches)}).";

    private static string BuildElectricalNotAppliedSummary(
        IReadOnlyList<ProjectedPlanSheetOperationAuditDto> electricalOperations)
    {
        var notApplied = electricalOperations
            .Where(operation => operation.Status != "Applied")
            .ToArray();
        if (notApplied.Length == 0)
        {
            var anchored = electricalOperations
                .Where(operation =>
                    operation.Status == "Applied" &&
                    operation.Reason?.Contains("dependent-sheet edge anchor", StringComparison.OrdinalIgnoreCase) == true)
                .ToArray();
            return anchored.Length == 0
                ? "Que no se achico en Electrical: nada; todas las operaciones con geometria electrica aplicaron."
                : $"Que no se achico en Electrical: nada; todas las operaciones aplicaron. Nota: {anchored.Length} operacion(es) usaron borde registrado porque el pinch canonico quedaba fuera del bbox electrico.";
        }

        var totalDelta = notApplied.Sum(operation => operation.ExpectedDeltaSourceUnits);
        var reasons = notApplied
            .GroupBy(operation => TranslateElectricalReason(operation.Reason))
            .Select(group => $"{string.Join(", ", group.Select(operation => $"op {operation.OperationIndex}"))}: {group.Key}")
            .Distinct()
            .ToArray();
        var groups = notApplied
            .GroupBy(operation => $"{operation.Kind} {operation.Edge}")
            .Select(group => $"{group.Key} x{group.Count()}")
            .ToArray();

        return $"Que no se achico en Electrical: {notApplied.Length} operacion(es), delta total {FormatInches(totalDelta)} ({string.Join(", ", groups)}). Motivo: {string.Join("; ", reasons)}";
    }

    private static string BuildDxfSafetySummary(IReadOnlyList<ExportedPlanSheetDto> sheets)
    {
        var dxfAudits = sheets
            .Where(sheet => sheet.SheetKind.Contains("Electrical", StringComparison.OrdinalIgnoreCase))
            .Select(sheet => sheet.ExportAudit?.DxfSafety)
            .Where(audit => audit is not null)
            .Select(audit => audit!)
            .ToArray();
        if (dxfAudits.Length == 0)
        {
            return "DXF safety: faltan datos del DXF electrico exportado; revisar dxf-safety-audit.json.";
        }

        var unsafeCount = dxfAudits.Count(audit =>
            !audit.OutputFileExists ||
            audit.OutputFileBytes <= 0 ||
            audit.MissingHandleCountAfter != 0 ||
            audit.MissingOwnerCountAfter != 0 ||
            audit.UnsupportedCrossingEntityCount != 0);

        return unsafeCount == 0
            ? $"DXF safety: OK; {dxfAudits.Sum(audit => audit.EntityCountAfter)} entidades; sin handles faltantes, sin owners faltantes, sin cruces no soportados."
            : $"DXF safety: requiere revision; {unsafeCount} hoja(s) con archivo faltante, handles/owners faltantes o cruces no soportados.";
    }

    private static string TranslateElectricalReason(string? reason)
        => string.IsNullOrWhiteSpace(reason)
            ? "sin motivo registrado; revisar electrical-projection-audit.json"
            : reason.Trim() switch
            {
                "No electrical geometry matched this canonical operation." =>
                    "no habia geometria electrica en esa zona para mover",
                "No registered electrical geometry fell on the affected side after Electrical-to-Floor registration; treated as a truly empty dependent-sheet zone." =>
                    "zona electrica registrada realmente vacia para esa operacion",
                var edgeAnchor when edgeAnchor.Contains("dependent-sheet edge anchor", StringComparison.OrdinalIgnoreCase) =>
                    "el pinch canonico quedaba fuera del bbox electrico registrado; se intento usar el borde equivalente",
                var known => known
            };

    private static IReadOnlyList<string> BuildMissingDataList(
        AdjustedSitePlanPlacementDto? canonicalPlacement,
        AdjustmentRecipeSummaryDto? canonicalRecipe,
        int floorPlanImpactCount,
        int electricalOperationCount,
        bool hasOutlineCongruenceAudit)
    {
        var missing = new List<string>();
        if (canonicalPlacement?.InputAudit is null)
        {
            missing.Add("input-audit dimensions");
        }

        if (canonicalRecipe is null ||
            canonicalRecipe.Operations.Count == 0 && canonicalRecipe.StretchActions.Count == 0)
        {
            missing.Add("canonical recipe actions");
        }

        if (floorPlanImpactCount == 0)
        {
            missing.Add("floorplan impact operations");
        }

        if (electricalOperationCount == 0)
        {
            missing.Add("electrical projection operations");
        }

        if (!hasOutlineCongruenceAudit)
        {
            missing.Add("outline congruence");
        }

        return missing;
    }

    private static bool HasOutlineCongruenceAudit(IReadOnlyList<ExportedPlanSheetDto> sheets)
        => sheets
            .Where(sheet => sheet.SheetKind.Contains("Electrical", StringComparison.OrdinalIgnoreCase))
            .Any(sheet => sheet.ExportAudit?.OutlineCongruence is not null);

    private static decimal SumRecipeDelta(
        AdjustmentRecipeSummaryDto? recipe,
        string axisTag)
        => recipe is null
            ? 0m
            : recipe.StretchActions.Count > 0
                ? recipe.StretchActions
                    .Where(action => string.Equals(action.AxisTag, axisTag, StringComparison.OrdinalIgnoreCase))
                    .Sum(action => action.DeltaSourceUnits)
                : recipe.Operations
                    .Where(operation => string.Equals(operation.AxisTag, axisTag, StringComparison.OrdinalIgnoreCase))
                    .Sum(operation => operation.DeltaSourceUnits);

    private static int CountRecipeActions(
        AdjustmentRecipeSummaryDto? recipe,
        string axisTag)
        => recipe is null
            ? 0
            : recipe.StretchActions.Count > 0
                ? recipe.StretchActions.Count(action =>
                    string.Equals(action.AxisTag, axisTag, StringComparison.OrdinalIgnoreCase))
                : recipe.Operations.Count(operation =>
                    string.Equals(operation.AxisTag, axisTag, StringComparison.OrdinalIgnoreCase));

    private static string FormatInches(decimal value)
        => $"{decimal.Round(value, 3, MidpointRounding.AwayFromZero).ToString("0.###", CultureInfo.InvariantCulture)}\"";

    private static string FormatNullableInches(decimal? value)
        => value.HasValue ? FormatInches(value.Value) : "n/a";

    private static ExportedPlanSheetDto ToDto(
        PlanSetExportedSheet sheet,
        IReadOnlyDictionary<Guid, ProjectedPlanSheetExportAuditDto>? exportAudits,
        IReadOnlyDictionary<Guid, string>? verificationPaths,
        string? canonicalFloorPlanVerificationPath)
    {
        var exportAudit = sheet.SheetProjectionId.HasValue && exportAudits is not null
            ? exportAudits.GetValueOrDefault(sheet.SheetProjectionId.Value)
            : null;

        return new ExportedPlanSheetDto(
            sheet.PlanSheetId,
            sheet.SheetProjectionId,
            sheet.SheetKind,
            sheet.Status.ToString(),
            sheet.StoragePath,
            sheet.ProjectionMethod,
            sheet.Confidence,
            sheet.Warning,
            sheet.RuleSummary,
            sheet.RecipeHandlingSummary,
            exportAudit)
        {
            VerificationPath = sheet.SheetProjectionId.HasValue
                ? verificationPaths?.GetValueOrDefault(sheet.SheetProjectionId.Value)
                : canonicalFloorPlanVerificationPath
        };
    }
}
