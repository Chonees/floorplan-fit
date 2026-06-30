using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.ExportAudit;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets.ExportAudit;

public sealed class CreateMultiSheetExportAuditHandlerTests
{
    [Fact]
    public async Task HandleAsync_persists_export_audit_with_automatic_and_manual_sheet_statuses()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var canonicalAdjustmentId = Guid.NewGuid();
        var readyProjection = CreateProjection(
            planSetVersionId,
            canonicalAdjustmentId,
            SheetAdjustmentProjectionStatus.ReadyForExport,
            confidence: 0.91m,
            warning: null);
        var manualProjection = CreateProjection(
            planSetVersionId,
            canonicalAdjustmentId,
            SheetAdjustmentProjectionStatus.RequiresManualConfirmation,
            confidence: 0.72m,
            warning: "Needs review");
        var exportRepository = new CapturingPlanSetExportRepository();
        var auditEventRepository = new CapturingPlanSetAuditEventRepository();
        var unitOfWork = new CapturingUnitOfWork();
        var clock = new FakeClock(new DateTime(2026, 6, 30, 23, 55, 0, DateTimeKind.Utc));
        var handler = new CreateMultiSheetExportAuditHandler(
            new FakeSheetAdjustmentProjectionRepository(readyProjection, manualProjection),
            new FakePlanSheetReader(),
            exportRepository,
            auditEventRepository,
            unitOfWork,
            clock);

        var response = await handler.HandleAsync(
            new CreateMultiSheetExportAuditRequest(
                planSetVersionId,
                canonicalFloorPlanVersionId,
                canonicalAdjustmentId,
                "exports/floor-plan.dxf",
                [
                    new MultiSheetExportProjectionRequestDto(readyProjection.Id, "exports/electrical.dxf"),
                    new MultiSheetExportProjectionRequestDto(manualProjection.Id)
                ]),
            CancellationToken.None);

        Assert.True(unitOfWork.Saved);
        Assert.Equal("RequiresManualConfirmation", response.Status);
        Assert.Equal(3, response.Summary.TotalSheetCount);
        Assert.Equal(1, response.Summary.AutomaticallyProjectedSheetCount);
        Assert.Equal(1, response.Summary.ManualConfirmationRequiredSheetCount);
        Assert.Equal(0.72m, response.Summary.LowestConfidence);
        Assert.False(response.Summary.CanExportPackageAutomatically);

        var canonicalSheet = Assert.Single(response.Sheets, sheet => sheet.SheetKind == "CanonicalFloorPlan");
        Assert.Equal(canonicalFloorPlanVersionId, canonicalSheet.SheetId);
        Assert.Equal("Exported", canonicalSheet.Status);
        Assert.Equal("exports/floor-plan.dxf", canonicalSheet.StoragePath);
        Assert.Equal(1m, canonicalSheet.Confidence);

        var automaticSheet = Assert.Single(response.Sheets, sheet => sheet.ProjectionId == readyProjection.Id);
        Assert.Equal("ProjectedAutomatically", automaticSheet.Status);
        Assert.Equal("ElectricalWholeSheetSimilarity", automaticSheet.ProjectionMethod);
        Assert.Equal(0.91m, automaticSheet.Confidence);

        var manualSheet = Assert.Single(response.Sheets, sheet => sheet.ProjectionId == manualProjection.Id);
        Assert.Equal("RequiresManualConfirmation", manualSheet.Status);
        Assert.Equal("Needs review", manualSheet.Warning);

        var savedExport = Assert.Single(exportRepository.Items);
        Assert.Equal(response.ExportId, savedExport.Id);
        Assert.Equal(3, savedExport.Sheets.Count);

        var auditEvent = Assert.Single(auditEventRepository.Items);
        Assert.Equal("PlanSetExport", auditEvent.AggregateType);
        Assert.Equal(response.ExportId, auditEvent.AggregateId);
        Assert.Equal("PlanSetExportAuditCreated", auditEvent.EventType);
    }

    [Fact]
    public async Task HandleAsync_saves_export_even_when_quality_event_fails()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var canonicalAdjustmentId = Guid.NewGuid();
        var projection = CreateProjection(
            planSetVersionId,
            canonicalAdjustmentId,
            SheetAdjustmentProjectionStatus.ReadyForExport,
            confidence: 0.95m,
            warning: null);
        var exportRepository = new CapturingPlanSetExportRepository();
        var unitOfWork = new CapturingUnitOfWork();
        var handler = new CreateMultiSheetExportAuditHandler(
            new FakeSheetAdjustmentProjectionRepository(projection),
            new FakePlanSheetReader(),
            exportRepository,
            new ThrowingPlanSetAuditEventRepository(),
            unitOfWork,
            new FakeClock(new DateTime(2026, 6, 30, 23, 55, 0, DateTimeKind.Utc)));

        var response = await handler.HandleAsync(
            new CreateMultiSheetExportAuditRequest(
                planSetVersionId,
                canonicalFloorPlanVersionId,
                canonicalAdjustmentId,
                "exports/floor-plan.dxf",
                [new MultiSheetExportProjectionRequestDto(projection.Id, "exports/electrical.dxf")]),
            CancellationToken.None);

        Assert.True(unitOfWork.Saved);
        Assert.Equal("ReadyForExport", response.Status);
        Assert.Single(exportRepository.Items);
    }

    [Fact]
    public async Task HandleAsync_with_empty_projection_request_audits_all_dependent_sheets_and_marks_missing_projection()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var canonicalAdjustmentId = Guid.NewGuid();
        var electricalSheetId = Guid.NewGuid();
        var roofSheetId = Guid.NewGuid();
        var electricalProjection = CreateProjection(
            planSetVersionId,
            canonicalAdjustmentId,
            SheetAdjustmentProjectionStatus.ReadyForExport,
            confidence: 0.93m,
            warning: null,
            dependentSheetId: electricalSheetId);
        var exportRepository = new CapturingPlanSetExportRepository();
        var handler = new CreateMultiSheetExportAuditHandler(
            new FakeSheetAdjustmentProjectionRepository(electricalProjection),
            new FakePlanSheetReader(
                planSetVersionId,
                [
                    CreateSheet(electricalSheetId, "ElectricalPlan"),
                    CreateSheet(roofSheetId, "RoofPlan")
                ]),
            exportRepository,
            new CapturingPlanSetAuditEventRepository(),
            new CapturingUnitOfWork(),
            new FakeClock(new DateTime(2026, 6, 30, 23, 55, 0, DateTimeKind.Utc)));

        var response = await handler.HandleAsync(
            new CreateMultiSheetExportAuditRequest(
                planSetVersionId,
                canonicalFloorPlanVersionId,
                canonicalAdjustmentId,
                "exports/floor-plan.dxf",
                []),
            CancellationToken.None);

        Assert.Equal("RequiresManualConfirmation", response.Status);
        Assert.Equal(3, response.Summary.TotalSheetCount);
        Assert.Equal(1, response.Summary.AutomaticallyProjectedSheetCount);
        Assert.Equal(1, response.Summary.ManualConfirmationRequiredSheetCount);

        var projectedSheet = Assert.Single(response.Sheets, sheet => sheet.SheetId == electricalSheetId);
        Assert.Equal("ProjectedAutomatically", projectedSheet.Status);
        Assert.Equal(electricalProjection.Id, projectedSheet.ProjectionId);

        var missingSheet = Assert.Single(response.Sheets, sheet => sheet.SheetId == roofSheetId);
        Assert.Equal("MissingProjection", missingSheet.Status);
        Assert.Null(missingSheet.ProjectionId);
        Assert.Contains("No projection", missingSheet.Warning, StringComparison.OrdinalIgnoreCase);

        var saved = Assert.Single(exportRepository.Items);
        Assert.Contains(saved.Sheets, sheet => sheet.PlanSheetId == roofSheetId && sheet.Status == PlanSetExportedSheetStatus.MissingProjection);
    }

    private static SheetAdjustmentProjection CreateProjection(
        Guid planSetVersionId,
        Guid canonicalAdjustmentId,
        SheetAdjustmentProjectionStatus status,
        decimal confidence,
        string? warning,
        Guid? dependentSheetId = null)
    {
        return new SheetAdjustmentProjection(
            Guid.NewGuid(),
            planSetVersionId,
            dependentSheetId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            canonicalAdjustmentId,
            SheetAdjustmentProjectionMethod.ElectricalWholeSheetSimilarity,
            new SheetAdjustmentProjectionTransform(
                scale: 1.1m,
                rotationDegrees: 0m,
                translateX: 10m,
                translateY: 20m),
            confidence,
            status,
            warning,
            canonicalCompressionStepCount: 0,
            new DateTime(2026, 6, 30, 23, 50, 0, DateTimeKind.Utc),
            ruleSummary: "WholeSheetSimilarity");
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
        private readonly IReadOnlyDictionary<Guid, SheetAdjustmentProjection> projections;

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
            IReadOnlyDictionary<Guid, IReadOnlyList<PlanSetSheetDto>> result =
                planSetVersionIds.Contains(planSetVersionId)
                    ? new Dictionary<Guid, IReadOnlyList<PlanSetSheetDto>>
                    {
                        [planSetVersionId] = sheets
                    }
                    : new Dictionary<Guid, IReadOnlyList<PlanSetSheetDto>>();

            return Task.FromResult(result);
        }
    }

    private sealed class CapturingPlanSetExportRepository : IPlanSetExportRepository
    {
        public List<PlanSetExport> Items { get; } = [];

        public Task AddAsync(PlanSetExport export, CancellationToken cancellationToken)
        {
            Items.Add(export);
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingPlanSetAuditEventRepository : IPlanSetAuditEventRepository
    {
        public List<PlanSetAuditEvent> Items { get; } = [];

        public Task AddAsync(PlanSetAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            Items.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingPlanSetAuditEventRepository : IPlanSetAuditEventRepository
    {
        public Task AddAsync(PlanSetAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Telemetry sink unavailable.");
        }
    }

    private sealed class CapturingUnitOfWork : IUnitOfWork
    {
        public bool Saved { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            Saved = true;
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
