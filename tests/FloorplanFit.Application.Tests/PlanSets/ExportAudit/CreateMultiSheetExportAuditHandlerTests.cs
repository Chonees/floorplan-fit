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

    private static SheetAdjustmentProjection CreateProjection(
        Guid planSetVersionId,
        Guid canonicalAdjustmentId,
        SheetAdjustmentProjectionStatus status,
        decimal confidence,
        string? warning)
    {
        return new SheetAdjustmentProjection(
            Guid.NewGuid(),
            planSetVersionId,
            Guid.NewGuid(),
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
