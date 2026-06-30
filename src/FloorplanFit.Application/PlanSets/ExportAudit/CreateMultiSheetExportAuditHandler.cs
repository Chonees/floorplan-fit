using System.Text.Json;
using FloorplanFit.Application.Abstractions;
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

    public CreateMultiSheetExportAuditHandler(
        ISheetAdjustmentProjectionRepository sheetAdjustmentProjectionRepository,
        IPlanSheetReader planSheetReader,
        IPlanSetExportRepository planSetExportRepository,
        IPlanSetAuditEventRepository planSetAuditEventRepository,
        IPlanSetExportManifestWriter planSetExportManifestWriter,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        this.sheetAdjustmentProjectionRepository = sheetAdjustmentProjectionRepository;
        this.planSheetReader = planSheetReader;
        this.planSetExportRepository = planSetExportRepository;
        this.planSetAuditEventRepository = planSetAuditEventRepository;
        this.planSetExportManifestWriter = planSetExportManifestWriter;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
    }

    public async Task<MultiSheetExportAuditDto> HandleAsync(
        CreateMultiSheetExportAuditRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.DependentProjections);

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

        if (request.DependentProjections.Count == 0)
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

        var summary = BuildSummary(sheets);
        var status = summary.ManualConfirmationRequiredSheetCount == 0
            ? PlanSetExportStatus.ReadyForExport
            : PlanSetExportStatus.RequiresManualConfirmation;
        var draftExport = new PlanSetExport(
            exportId,
            request.PlanSetVersionId,
            request.CanonicalAdjustmentId,
            status,
            JsonSerializer.Serialize(summary),
            packageManifestPath: null,
            createdAtUtc,
            sheets);
        var draftAudit = ToDto(draftExport, summary);
        var packageManifestPath = await planSetExportManifestWriter.WriteAsync(draftAudit, cancellationToken);
        var export = new PlanSetExport(
            exportId,
            request.PlanSetVersionId,
            request.CanonicalAdjustmentId,
            status,
            JsonSerializer.Serialize(summary),
            packageManifestPath,
            createdAtUtc,
            sheets);

        await planSetExportRepository.AddAsync(export, cancellationToken);
        await TryRecordAuditEventAsync(export, summary, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(export, summary);
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
        var latestProjectionBySheet = projections
            .GroupBy(projection => projection.DependentSheetId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(projection => projection.CreatedAtUtc).Last());

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
                    storagePath: null,
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
            ruleSummary: projection.RuleSummary);
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
        ProjectionAuditSummaryDto summary,
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
                    JsonSerializer.Serialize(summary),
                    export.CreatedAtUtc),
                cancellationToken);
        }
        catch (Exception)
        {
            // ponytail: telemetry is best-effort; promote to durable retry queue if audit_events becomes business-critical.
        }
    }

    private static MultiSheetExportAuditDto ToDto(
        PlanSetExport export,
        ProjectionAuditSummaryDto summary)
    {
        return new MultiSheetExportAuditDto(
            export.Id,
            export.PlanSetVersionId,
            export.CanonicalAdjustmentId,
            export.Status.ToString(),
            summary,
            export.Sheets.Select(ToDto).ToArray(),
            export.PackageManifestPath,
            export.CreatedAtUtc);
    }

    private static ExportedPlanSheetDto ToDto(PlanSetExportedSheet sheet)
    {
        return new ExportedPlanSheetDto(
            sheet.PlanSheetId,
            sheet.SheetProjectionId,
            sheet.SheetKind,
            sheet.Status.ToString(),
            sheet.StoragePath,
            sheet.ProjectionMethod,
            sheet.Confidence,
            sheet.Warning,
            sheet.RuleSummary);
    }
}
