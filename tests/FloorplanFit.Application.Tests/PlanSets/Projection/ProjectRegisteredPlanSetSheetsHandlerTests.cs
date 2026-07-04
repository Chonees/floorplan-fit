using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Projection;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets.Projection;

public sealed class ProjectRegisteredPlanSetSheetsHandlerTests
{
    [Fact]
    public async Task HandleAsync_projects_latest_registration_for_each_dependent_sheet()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalAdjustmentId = Guid.NewGuid();
        var clock = new FakeClock(new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc));
        var oldElectrical = CreateRegistration(
            planSetVersionId,
            Guid.NewGuid(),
            SheetRegistrationMethod.WholeSheetSimilarity,
            SheetRegistrationStatus.Confirmed,
            confidence: 0.92m,
            createdAtUtc: clock.UtcNow.AddMinutes(-20));
        var latestElectrical = CreateRegistration(
            planSetVersionId,
            oldElectrical.DependentSheetId,
            SheetRegistrationMethod.WholeSheetSimilarity,
            SheetRegistrationStatus.Confirmed,
            confidence: 0.93m,
            createdAtUtc: clock.UtcNow.AddMinutes(-10));
        var roof = CreateRegistration(
            planSetVersionId,
            Guid.NewGuid(),
            SheetRegistrationMethod.RoofFootprintWithOverhang,
            SheetRegistrationStatus.Confirmed,
            confidence: 0.88m,
            createdAtUtc: clock.UtcNow.AddMinutes(-5),
            ruleSummary: "PreserveOverhangInches=18");
        var facade = CreateRegistration(
            planSetVersionId,
            Guid.NewGuid(),
            SheetRegistrationMethod.FacadeHorizontalReference,
            SheetRegistrationStatus.PendingConfirmation,
            confidence: 0.71m,
            createdAtUtc: clock.UtcNow.AddMinutes(-1),
            ruleSummary: "PreserveVertical=true;HorizontalReference=FrontWallBaseline");
        var registrationRepository = new FakeSheetRegistrationRepository(oldElectrical, latestElectrical, roof, facade);
        var projectionRepository = new CapturingSheetAdjustmentProjectionRepository();
        var unitOfWork = new CapturingUnitOfWork();
        var auditEvents = new CapturingPlanSetAuditEventRepository();
        var handler = new ProjectRegisteredPlanSetSheetsHandler(
            registrationRepository,
            new ProjectElectricalSheetAdjustmentHandler(registrationRepository, projectionRepository, unitOfWork, clock, auditEvents),
            new ProjectRoofSheetAdjustmentHandler(registrationRepository, projectionRepository, unitOfWork, clock, auditEvents),
            new ProjectFacadeElevationSheetAdjustmentHandler(registrationRepository, projectionRepository, unitOfWork, clock, auditEvents));

        var response = await handler.HandleAsync(
            new ProjectRegisteredPlanSetSheetsRequest(
                planSetVersionId,
                canonicalAdjustmentId,
                new AdjustedSitePlanPlacementDto(
                    FloorToSiteScale: 1.5m,
                    SiteOffsetX: 10m,
                    SiteOffsetY: -4m,
                    CompressionSteps: [])),
            CancellationToken.None);

        Assert.Equal(3, response.ProjectedSheetCount);
        Assert.Equal(3, response.Projections.Count);
        Assert.DoesNotContain(response.Projections, projection => projection.SheetRegistrationId == oldElectrical.Id);
        Assert.Contains(response.Projections, projection =>
            projection.SheetRegistrationId == latestElectrical.Id &&
            projection.Method == "ElectricalWholeSheetSimilarity" &&
            projection.Status == "ReadyForExport");
        Assert.Contains(response.Projections, projection =>
            projection.SheetRegistrationId == roof.Id &&
            projection.Method == "RoofOverhangPreserving" &&
            projection.Status == "ReadyForExport");
        Assert.Contains(response.Projections, projection =>
            projection.SheetRegistrationId == facade.Id &&
            projection.Method == "FacadeHorizontalPreservingVerticals" &&
            projection.Status == "RequiresManualConfirmation");
        Assert.Equal(3, projectionRepository.Items.Count);
        Assert.Equal(3, auditEvents.Items.Count);
    }

    private static SheetRegistration CreateRegistration(
        Guid planSetVersionId,
        Guid dependentSheetId,
        SheetRegistrationMethod method,
        SheetRegistrationStatus status,
        decimal confidence,
        DateTime createdAtUtc,
        string? ruleSummary = null)
        => new(
            Guid.NewGuid(),
            planSetVersionId,
            dependentSheetId,
            planSetVersionId,
            method,
            new SheetRegistrationTransform(1m, 0m, 2m, 3m),
            confidence,
            status,
            createdAtUtc,
            status is SheetRegistrationStatus.Confirmed ? createdAtUtc : null,
            null,
            ruleSummary);

    private sealed class FakeSheetRegistrationRepository : ISheetRegistrationRepository
    {
        private readonly IReadOnlyList<SheetRegistration> registrations;

        public FakeSheetRegistrationRepository(params SheetRegistration[] registrations)
        {
            this.registrations = registrations;
        }

        public Task AddAsync(SheetRegistration registration, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<SheetRegistration?> GetByIdAsync(Guid registrationId, CancellationToken cancellationToken)
            => Task.FromResult(registrations.FirstOrDefault(item => item.Id == registrationId));

        public Task<IReadOnlyList<SheetRegistration>> ListByPlanSetVersionAsync(
            Guid planSetVersionId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SheetRegistration>>(registrations
                .Where(item => item.PlanSetVersionId == planSetVersionId)
                .ToArray());
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
            => Task.FromResult(Items.FirstOrDefault(item => item.Id == projectionId));

        public Task<IReadOnlyList<SheetAdjustmentProjection>> ListByPlanSetVersionAndCanonicalAdjustmentAsync(
            Guid planSetVersionId,
            Guid canonicalAdjustmentId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SheetAdjustmentProjection>>(Items
                .Where(item => item.PlanSetVersionId == planSetVersionId && item.CanonicalAdjustmentId == canonicalAdjustmentId)
                .ToArray());
    }

    private sealed class CapturingUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
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
