using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Confirmation;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets.Confirmation;

public sealed class ConfirmSheetAdjustmentProjectionHandlerTests
{
    [Fact]
    public async Task HandleAsync_marks_manual_projection_ready_for_export()
    {
        var clock = new FakeClock(new DateTime(2026, 7, 1, 9, 30, 0, DateTimeKind.Utc));
        var projection = CreateProjection(SheetAdjustmentProjectionStatus.RequiresManualConfirmation);
        var registration = CreateRegistration(projection.SheetRegistrationId, SheetRegistrationStatus.Confirmed);
        var repository = new CapturingSheetAdjustmentProjectionRepository(projection);
        var unitOfWork = new CapturingUnitOfWork();
        var auditEventRepository = new CapturingPlanSetAuditEventRepository();
        var handler = new ConfirmSheetAdjustmentProjectionHandler(
            repository,
            new FakeSheetRegistrationRepository(registration),
            unitOfWork,
            clock,
            auditEventRepository);

        var response = await handler.HandleAsync(
            new ConfirmSheetAdjustmentProjectionRequest(projection.Id),
            CancellationToken.None);

        Assert.True(unitOfWork.Saved);
        Assert.Equal(projection.Id, response.ProjectionId);
        Assert.Equal("ReadyForExport", response.Status);
        Assert.Equal(projection.CreatedAtUtc, response.CreatedAtUtc);

        Assert.NotNull(repository.Updated);
        Assert.Equal(SheetAdjustmentProjectionStatus.ReadyForExport, repository.Updated!.Status);
        Assert.Equal(projection.Transform.Scale, repository.Updated.Transform.Scale);
        Assert.Equal(projection.Warning, repository.Updated.Warning);

        var auditEvent = Assert.Single(auditEventRepository.Items);
        Assert.Equal("SheetAdjustmentProjectionQualityMeasured", auditEvent.EventType);
        Assert.Equal(projection.Id, auditEvent.AggregateId);
        Assert.Equal(clock.UtcNow, auditEvent.OccurredAtUtc);
        Assert.Contains("\"status\":\"ReadyForExport\"", auditEvent.PayloadJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_marks_compression_recipe_as_confirmed_for_recipe_aware_export()
    {
        var projection = CreateProjection(
            SheetAdjustmentProjectionStatus.RequiresManualConfirmation,
            recipeHandlingSummary: "ElectricalPlan: affine placement applied; local recipe requires review before DXF deformation: HorizontalCompression Right @50 delta 2.");
        var registration = CreateRegistration(projection.SheetRegistrationId, SheetRegistrationStatus.Confirmed);
        var repository = new CapturingSheetAdjustmentProjectionRepository(projection);
        var handler = new ConfirmSheetAdjustmentProjectionHandler(
            repository,
            new FakeSheetRegistrationRepository(registration),
            new CapturingUnitOfWork(),
            new FakeClock(new DateTime(2026, 7, 1, 9, 30, 0, DateTimeKind.Utc)));

        var response = await handler.HandleAsync(
            new ConfirmSheetAdjustmentProjectionRequest(projection.Id),
            CancellationToken.None);

        Assert.Contains("recipe-aware DXF export", response.RecipeHandlingSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("recipe-aware DXF export", repository.Updated!.RecipeHandlingSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleAsync_marks_export_time_manual_review_as_completed_for_recipe_aware_export()
    {
        var projection = CreateProjection(
            SheetAdjustmentProjectionStatus.RequiresManualConfirmation,
            recipeHandlingSummary: "ElectricalPlan: local recipe manually confirmed; recipe-aware DXF export will apply canonical operations; manual review required: ELLIPSE crosses a canonical recipe pinch line.");
        var registration = CreateRegistration(projection.SheetRegistrationId, SheetRegistrationStatus.Confirmed);
        var repository = new CapturingSheetAdjustmentProjectionRepository(projection);
        var handler = new ConfirmSheetAdjustmentProjectionHandler(
            repository,
            new FakeSheetRegistrationRepository(registration),
            new CapturingUnitOfWork(),
            new FakeClock(new DateTime(2026, 7, 1, 9, 30, 0, DateTimeKind.Utc)));

        var response = await handler.HandleAsync(
            new ConfirmSheetAdjustmentProjectionRequest(projection.Id),
            CancellationToken.None);

        Assert.Equal("ReadyForExport", response.Status);
        Assert.Contains("manual review completed", response.RecipeHandlingSummary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("manual review required", response.RecipeHandlingSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleAsync_rejects_missing_projection()
    {
        var handler = new ConfirmSheetAdjustmentProjectionHandler(
            new CapturingSheetAdjustmentProjectionRepository(),
            new FakeSheetRegistrationRepository(),
            new CapturingUnitOfWork(),
            new FakeClock(new DateTime(2026, 7, 1, 9, 30, 0, DateTimeKind.Utc)));

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(
            new ConfirmSheetAdjustmentProjectionRequest(Guid.NewGuid()),
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_rejects_projection_when_registration_is_not_confirmed()
    {
        var projection = CreateProjection(SheetAdjustmentProjectionStatus.RequiresManualConfirmation);
        var handler = new ConfirmSheetAdjustmentProjectionHandler(
            new CapturingSheetAdjustmentProjectionRepository(projection),
            new FakeSheetRegistrationRepository(CreateRegistration(
                projection.SheetRegistrationId,
                SheetRegistrationStatus.PendingConfirmation)),
            new CapturingUnitOfWork(),
            new FakeClock(new DateTime(2026, 7, 1, 9, 30, 0, DateTimeKind.Utc)));

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(
            new ConfirmSheetAdjustmentProjectionRequest(projection.Id),
            CancellationToken.None));
    }

    [Theory]
    [InlineData(
        SheetAdjustmentProjectionMethod.RoofOverhangPreserving,
        SheetAdjustmentProjectionStatus.RequiresManualConfirmation)]
    [InlineData(
        SheetAdjustmentProjectionMethod.RoofOverhangPreserving,
        SheetAdjustmentProjectionStatus.ReadyForExport)]
    [InlineData(
        SheetAdjustmentProjectionMethod.FacadeHorizontalPreservingVerticals,
        SheetAdjustmentProjectionStatus.RequiresManualConfirmation)]
    [InlineData(
        SheetAdjustmentProjectionMethod.FacadeHorizontalPreservingVerticals,
        SheetAdjustmentProjectionStatus.ReadyForExport)]
    public async Task HandleAsync_rejects_compressed_affine_only_projection(
        SheetAdjustmentProjectionMethod method,
        SheetAdjustmentProjectionStatus status)
    {
        var projection = CreateProjection(status, method: method);
        var repository = new CapturingSheetAdjustmentProjectionRepository(projection);
        var unitOfWork = new CapturingUnitOfWork();
        var handler = new ConfirmSheetAdjustmentProjectionHandler(
            repository,
            new FakeSheetRegistrationRepository(CreateRegistration(
                projection.SheetRegistrationId,
                SheetRegistrationStatus.Confirmed)),
            unitOfWork,
            new FakeClock(new DateTime(2026, 7, 1, 9, 30, 0, DateTimeKind.Utc)));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(
            new ConfirmSheetAdjustmentProjectionRequest(projection.Id),
            CancellationToken.None));

        Assert.Contains("canonical compression", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(repository.Updated);
        Assert.False(unitOfWork.Saved);
    }

    private static SheetAdjustmentProjection CreateProjection(
        SheetAdjustmentProjectionStatus status,
        string? recipeHandlingSummary = null,
        SheetAdjustmentProjectionMethod method = SheetAdjustmentProjectionMethod.ElectricalWholeSheetSimilarity)
        => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            method,
            new SheetAdjustmentProjectionTransform(1.1m, 0m, 12m, -3m),
            0.76m,
            status,
            "Needs visual review",
            1,
            new DateTime(2026, 7, 1, 8, 30, 0, DateTimeKind.Utc),
            "electrical follows floor plan",
            recipeHandlingSummary);

    private static SheetRegistration CreateRegistration(Guid id, SheetRegistrationStatus status)
        => new(
            id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            SheetRegistrationMethod.WholeSheetSimilarity,
            new SheetRegistrationTransform(1m, 0m, 0m, 0m),
            0.9m,
            status,
            new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc),
            status is SheetRegistrationStatus.Confirmed
                ? new DateTime(2026, 7, 1, 8, 5, 0, DateTimeKind.Utc)
                : null,
            null);

    private sealed class CapturingSheetAdjustmentProjectionRepository : ISheetAdjustmentProjectionRepository
    {
        private readonly SheetAdjustmentProjection? projection;

        public CapturingSheetAdjustmentProjectionRepository(SheetAdjustmentProjection? projection = null)
        {
            this.projection = projection;
        }

        public SheetAdjustmentProjection? Updated { get; private set; }

        public Task AddAsync(SheetAdjustmentProjection projection, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<SheetAdjustmentProjection?> GetByIdAsync(Guid projectionId, CancellationToken cancellationToken)
            => Task.FromResult(projection?.Id == projectionId ? projection : null);

        public Task<IReadOnlyList<SheetAdjustmentProjection>> ListByPlanSetVersionAndCanonicalAdjustmentAsync(
            Guid planSetVersionId,
            Guid canonicalAdjustmentId,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task UpdateAsync(SheetAdjustmentProjection projection, CancellationToken cancellationToken)
        {
            Updated = projection;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSheetRegistrationRepository : ISheetRegistrationRepository
    {
        private readonly SheetRegistration? registration;

        public FakeSheetRegistrationRepository(SheetRegistration? registration = null)
        {
            this.registration = registration;
        }

        public Task AddAsync(SheetRegistration registration, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<SheetRegistration?> GetByIdAsync(Guid registrationId, CancellationToken cancellationToken)
            => Task.FromResult(registration?.Id == registrationId ? registration : null);
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
