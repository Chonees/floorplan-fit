using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Projection;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets.Projection;

public sealed class ProjectRoofSheetAdjustmentHandlerTests
{
    [Fact]
    public async Task HandleAsync_projects_roof_sheet_and_preserves_overhang_rule()
    {
        var clock = new FakeClock(new DateTime(2026, 6, 30, 22, 0, 0, DateTimeKind.Utc));
        var registration = CreateRoofRegistration("PreserveOverhangInches=18");
        var projectionRepository = new CapturingSheetAdjustmentProjectionRepository();
        var auditEventRepository = new CapturingPlanSetAuditEventRepository();
        var unitOfWork = new CapturingUnitOfWork();
        var handler = new ProjectRoofSheetAdjustmentHandler(
            new FakeSheetRegistrationRepository(registration),
            projectionRepository,
            unitOfWork,
            clock,
            auditEventRepository);
        var canonicalAdjustmentId = Guid.NewGuid();

        var response = await handler.HandleAsync(
            new ProjectRoofSheetAdjustmentRequest(
                registration.Id,
                canonicalAdjustmentId,
                new AdjustedSitePlanPlacementDto(
                    FloorToSiteScale: 2m,
                    SiteOffsetX: 10m,
                    SiteOffsetY: 20m,
                    CompressionSteps: [])),
            CancellationToken.None);

        Assert.True(unitOfWork.Saved);
        Assert.Equal(registration.PlanSetVersionId, response.PlanSetVersionId);
        Assert.Equal(registration.DependentSheetId, response.DependentSheetId);
        Assert.Equal("RoofOverhangPreserving", response.Method);
        Assert.Equal("ReadyForExport", response.Status);
        Assert.Equal("PreserveOverhangInches=18", response.RuleSummary);
        Assert.Equal(2.2m, response.Transform.Scale);
        Assert.Equal(18m, response.Transform.TranslateX);
        Assert.Equal(36m, response.Transform.TranslateY);

        var saved = Assert.Single(projectionRepository.Items);
        Assert.Equal(SheetAdjustmentProjectionMethod.RoofOverhangPreserving, saved.Method);
        Assert.Equal("PreserveOverhangInches=18", saved.RuleSummary);

        var auditEvent = Assert.Single(auditEventRepository.Items);
        Assert.Equal("SheetAdjustmentProjection", auditEvent.AggregateType);
        Assert.Equal(response.ProjectionId, auditEvent.AggregateId);
        Assert.Equal("SheetAdjustmentProjectionQualityMeasured", auditEvent.EventType);
        Assert.Contains("\"method\":\"RoofOverhangPreserving\"", auditEvent.PayloadJson, StringComparison.Ordinal);
        Assert.Contains("\"confidence\":0.91", auditEvent.PayloadJson, StringComparison.Ordinal);
        Assert.Contains("\"status\":\"ReadyForExport\"", auditEvent.PayloadJson, StringComparison.Ordinal);
        Assert.Contains("\"ruleSummary\":\"PreserveOverhangInches=18\"", auditEvent.PayloadJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_requires_manual_confirmation_when_overhang_rule_is_missing()
    {
        var registration = CreateRoofRegistration(ruleSummary: null);
        var handler = new ProjectRoofSheetAdjustmentHandler(
            new FakeSheetRegistrationRepository(registration),
            new CapturingSheetAdjustmentProjectionRepository(),
            new CapturingUnitOfWork(),
            new FakeClock(new DateTime(2026, 6, 30, 22, 0, 0, DateTimeKind.Utc)));

        var response = await handler.HandleAsync(
            new ProjectRoofSheetAdjustmentRequest(
                registration.Id,
                Guid.NewGuid(),
                new AdjustedSitePlanPlacementDto(
                    FloorToSiteScale: 1m,
                    SiteOffsetX: 0m,
                    SiteOffsetY: 0m,
                    CompressionSteps: [])),
            CancellationToken.None);

        Assert.Equal("RequiresManualConfirmation", response.Status);
        Assert.Contains("overhang", response.Warning, StringComparison.OrdinalIgnoreCase);
    }

    private static SheetRegistration CreateRoofRegistration(string? ruleSummary)
    {
        return new SheetRegistration(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            SheetRegistrationMethod.RoofFootprintWithOverhang,
            new SheetRegistrationTransform(
                scale: 1.1m,
                rotationDegrees: 0m,
                translateX: 4m,
                translateY: 8m),
            0.91m,
            SheetRegistrationStatus.Confirmed,
            new DateTime(2026, 6, 30, 21, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 30, 21, 30, 0, DateTimeKind.Utc),
            warning: null,
            ruleSummary: ruleSummary);
    }

    private sealed class FakeSheetRegistrationRepository : ISheetRegistrationRepository
    {
        private readonly SheetRegistration registration;

        public FakeSheetRegistrationRepository(SheetRegistration registration)
        {
            this.registration = registration;
        }

        public Task AddAsync(SheetRegistration registration, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<SheetRegistration?> GetByIdAsync(Guid registrationId, CancellationToken cancellationToken)
        {
            return Task.FromResult(registration.Id == registrationId ? registration : null);
        }
    }

    private sealed class CapturingSheetAdjustmentProjectionRepository : ISheetAdjustmentProjectionRepository
    {
        public List<SheetAdjustmentProjection> Items { get; } = [];

        public Task AddAsync(SheetAdjustmentProjection projection, CancellationToken cancellationToken)
        {
            Items.Add(projection);
            return Task.CompletedTask;
        }

        public Task<SheetAdjustmentProjection?> GetByIdAsync(Guid projectionId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.FirstOrDefault(item => item.Id == projectionId));
        }

        public Task<IReadOnlyList<SheetAdjustmentProjection>> ListByPlanSetVersionAndCanonicalAdjustmentAsync(
            Guid planSetVersionId,
            Guid canonicalAdjustmentId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
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

    private sealed class CapturingPlanSetAuditEventRepository : IPlanSetAuditEventRepository
    {
        public List<PlanSetAuditEvent> Items { get; } = [];

        public Task AddAsync(PlanSetAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            Items.Add(auditEvent);
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

