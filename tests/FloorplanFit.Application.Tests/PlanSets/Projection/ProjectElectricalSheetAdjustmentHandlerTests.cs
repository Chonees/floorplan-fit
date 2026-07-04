using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Projection;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets.Projection;

public sealed class ProjectElectricalSheetAdjustmentHandlerTests
{
    [Fact]
    public async Task HandleAsync_projects_confirmed_electrical_registration_to_site_transform_ready_for_export()
    {
        var clock = new FakeClock(new DateTime(2026, 6, 30, 20, 0, 0, DateTimeKind.Utc));
        var registration = CreateRegistration(SheetRegistrationStatus.Confirmed, 0.92m, clock.UtcNow);
        var projectionRepository = new CapturingSheetAdjustmentProjectionRepository();
        var auditEventRepository = new CapturingPlanSetAuditEventRepository();
        var unitOfWork = new CapturingUnitOfWork();
        var handler = new ProjectElectricalSheetAdjustmentHandler(
            new FakeSheetRegistrationRepository(registration),
            projectionRepository,
            unitOfWork,
            clock,
            auditEventRepository);
        var canonicalAdjustmentId = Guid.NewGuid();

        var response = await handler.HandleAsync(
            new ProjectElectricalSheetAdjustmentRequest(
                registration.Id,
                canonicalAdjustmentId,
                new AdjustedSitePlanPlacementDto(
                    FloorToSiteScale: 2m,
                    SiteOffsetX: 100m,
                    SiteOffsetY: 200m,
                    CompressionSteps: [])),
            CancellationToken.None);

        Assert.True(unitOfWork.Saved);
        Assert.Equal(registration.PlanSetVersionId, response.PlanSetVersionId);
        Assert.Equal(registration.DependentSheetId, response.DependentSheetId);
        Assert.Equal(registration.Id, response.SheetRegistrationId);
        Assert.Equal(canonicalAdjustmentId, response.CanonicalAdjustmentId);
        Assert.Equal("ElectricalWholeSheetSimilarity", response.Method);
        Assert.Equal(3m, response.Transform.Scale);
        Assert.Equal(10m, response.Transform.RotationDegrees);
        Assert.Equal(110m, response.Transform.TranslateX);
        Assert.Equal(194m, response.Transform.TranslateY);
        Assert.Equal(0.92m, response.Confidence);
        Assert.Equal("ReadyForExport", response.Status);
        Assert.Equal(0, response.CanonicalCompressionStepCount);
        Assert.Null(response.Warning);
        Assert.Equal(clock.UtcNow, response.CreatedAtUtc);

        var saved = Assert.Single(projectionRepository.Items);
        Assert.Equal(response.ProjectionId, saved.Id);
        Assert.Equal(SheetAdjustmentProjectionStatus.ReadyForExport, saved.Status);

        var auditEvent = Assert.Single(auditEventRepository.Items);
        Assert.Equal("SheetAdjustmentProjection", auditEvent.AggregateType);
        Assert.Equal(response.ProjectionId, auditEvent.AggregateId);
        Assert.Equal("SheetAdjustmentProjectionQualityMeasured", auditEvent.EventType);
        Assert.Equal(clock.UtcNow, auditEvent.OccurredAtUtc);
        Assert.Contains("\"confidence\":0.92", auditEvent.PayloadJson, StringComparison.Ordinal);
        Assert.Contains("\"status\":\"ReadyForExport\"", auditEvent.PayloadJson, StringComparison.Ordinal);
        Assert.Contains("\"method\":\"ElectricalWholeSheetSimilarity\"", auditEvent.PayloadJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_keeps_pending_registration_out_of_export_ready_status()
    {
        var clock = new FakeClock(new DateTime(2026, 6, 30, 20, 0, 0, DateTimeKind.Utc));
        var registration = CreateRegistration(SheetRegistrationStatus.PendingConfirmation, 0.92m, null);
        var handler = new ProjectElectricalSheetAdjustmentHandler(
            new FakeSheetRegistrationRepository(registration),
            new CapturingSheetAdjustmentProjectionRepository(),
            new CapturingUnitOfWork(),
            clock);

        var response = await handler.HandleAsync(
            new ProjectElectricalSheetAdjustmentRequest(
                registration.Id,
                Guid.NewGuid(),
                new AdjustedSitePlanPlacementDto(
                    FloorToSiteScale: 1m,
                    SiteOffsetX: 0m,
                    SiteOffsetY: 0m,
                    CompressionSteps: [])),
            CancellationToken.None);

        Assert.Equal("RequiresManualConfirmation", response.Status);
        Assert.Contains("confirmed", response.Warning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleAsync_reports_canonical_recipe_compression_as_electrical_review_required()
    {
        var clock = new FakeClock(new DateTime(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc));
        var registration = CreateRegistration(SheetRegistrationStatus.Confirmed, 0.92m, clock.UtcNow);
        var projectionRepository = new CapturingSheetAdjustmentProjectionRepository();
        var handler = new ProjectElectricalSheetAdjustmentHandler(
            new FakeSheetRegistrationRepository(registration),
            projectionRepository,
            new CapturingUnitOfWork(),
            clock);

        var response = await handler.HandleAsync(
            new ProjectElectricalSheetAdjustmentRequest(
                registration.Id,
                Guid.NewGuid(),
                new AdjustedSitePlanPlacementDto(
                    1m,
                    0m,
                    0m,
                    [new AdjustedCompressionStepDto("Width", "Right", [new AdjustedCompressionMarkerDto(50m, 2m)])])),
            CancellationToken.None);

        Assert.Equal("RequiresManualConfirmation", response.Status);
        Assert.Equal(1, response.CanonicalCompressionStepCount);
        var recipeHandling = Assert.IsType<string>(response.RecipeHandlingSummary);
        Assert.Contains("HorizontalCompression", recipeHandling);
        Assert.Contains("ElectricalPlan", recipeHandling);
        Assert.Contains("review", recipeHandling, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleAsync_consumes_explicit_recipe_for_transform_and_review_summary()
    {
        var clock = new FakeClock(new DateTime(2026, 7, 3, 12, 0, 0, DateTimeKind.Utc));
        var registration = CreateRegistration(SheetRegistrationStatus.Confirmed, 0.92m, clock.UtcNow);
        var handler = new ProjectElectricalSheetAdjustmentHandler(
            new FakeSheetRegistrationRepository(registration),
            new CapturingSheetAdjustmentProjectionRepository(),
            new CapturingUnitOfWork(),
            clock);
        var placementWithoutCompression = new AdjustedSitePlanPlacementDto(
            1m,
            0m,
            0m,
            []);
        var explicitRecipe = new AdjustmentRecipeSummaryDto(
            "v1",
            2m,
            100m,
            200m,
            [new AdjustmentRecipeOperationDto("HorizontalCompression", "Width", "Right", 50m, 2m)]);

        var response = await handler.HandleAsync(
            new ProjectElectricalSheetAdjustmentRequest(
                registration.Id,
                Guid.NewGuid(),
                placementWithoutCompression,
                explicitRecipe),
            CancellationToken.None);

        Assert.Equal("RequiresManualConfirmation", response.Status);
        Assert.Equal(1, response.CanonicalCompressionStepCount);
        Assert.Equal(3m, response.Transform.Scale);
        Assert.Equal(110m, response.Transform.TranslateX);
        Assert.Equal(194m, response.Transform.TranslateY);
        Assert.Contains("HorizontalCompression", response.RecipeHandlingSummary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_rejects_non_electrical_registration_method()
    {
        var registration = CreateRegistration(
            SheetRegistrationMethod.FacadeHorizontalReference,
            SheetRegistrationStatus.Confirmed,
            0.92m,
            new DateTime(2026, 6, 30, 20, 0, 0, DateTimeKind.Utc));
        var handler = new ProjectElectricalSheetAdjustmentHandler(
            new FakeSheetRegistrationRepository(registration),
            new CapturingSheetAdjustmentProjectionRepository(),
            new CapturingUnitOfWork(),
            new FakeClock(new DateTime(2026, 6, 30, 20, 0, 0, DateTimeKind.Utc)));

        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(
            new ProjectElectricalSheetAdjustmentRequest(
                registration.Id,
                Guid.NewGuid(),
                new AdjustedSitePlanPlacementDto(
                    FloorToSiteScale: 1m,
                    SiteOffsetX: 0m,
                    SiteOffsetY: 0m,
                    CompressionSteps: [])),
            CancellationToken.None));
    }

    private static SheetRegistration CreateRegistration(
        SheetRegistrationStatus status,
        decimal confidence,
        DateTime? confirmedAtUtc)
    {
        return CreateRegistration(
            SheetRegistrationMethod.WholeSheetSimilarity,
            status,
            confidence,
            confirmedAtUtc);
    }

    private static SheetRegistration CreateRegistration(
        SheetRegistrationMethod method,
        SheetRegistrationStatus status,
        decimal confidence,
        DateTime? confirmedAtUtc)
    {
        return new SheetRegistration(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            method,
            new SheetRegistrationTransform(
                scale: 1.5m,
                rotationDegrees: 10m,
                translateX: 5m,
                translateY: -3m),
            confidence,
            status,
            new DateTime(2026, 6, 30, 19, 0, 0, DateTimeKind.Utc),
            confirmedAtUtc,
            warning: null);
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
