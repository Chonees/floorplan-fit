using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Confirmation;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets.Confirmation;

public sealed class ConfirmSheetRegistrationHandlerTests
{
    [Fact]
    public async Task HandleAsync_confirms_pending_registration()
    {
        var clock = new FakeClock(new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc));
        var registration = CreateRegistration(SheetRegistrationStatus.PendingConfirmation, confirmedAtUtc: null);
        var repository = new CapturingSheetRegistrationRepository(registration);
        var unitOfWork = new CapturingUnitOfWork();
        var auditEventRepository = new CapturingPlanSetAuditEventRepository();
        var handler = new ConfirmSheetRegistrationHandler(repository, unitOfWork, clock, auditEventRepository);

        var response = await handler.HandleAsync(
            new ConfirmSheetRegistrationRequest(registration.Id),
            CancellationToken.None);

        Assert.True(unitOfWork.Saved);
        Assert.Equal(registration.Id, response.RegistrationId);
        Assert.Equal("Confirmed", response.Status);
        Assert.Equal(clock.UtcNow, response.ConfirmedAtUtc);

        Assert.NotNull(repository.Updated);
        Assert.Equal(SheetRegistrationStatus.Confirmed, repository.Updated!.Status);
        Assert.Equal(clock.UtcNow, repository.Updated.ConfirmedAtUtc);
        Assert.Equal(registration.Transform.Scale, repository.Updated.Transform.Scale);
        Assert.Equal(registration.Warning, repository.Updated.Warning);
        Assert.Same(registration.WholePlanRegistrationProof, repository.Updated.WholePlanRegistrationProof);

        var auditEvent = Assert.Single(auditEventRepository.Items);
        Assert.Equal("SheetRegistrationQualityMeasured", auditEvent.EventType);
        Assert.Equal(registration.Id, auditEvent.AggregateId);
        Assert.Contains("\"status\":\"Confirmed\"", auditEvent.PayloadJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_rejects_missing_registration()
    {
        var handler = new ConfirmSheetRegistrationHandler(
            new CapturingSheetRegistrationRepository(),
            new CapturingUnitOfWork(),
            new FakeClock(new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc)));

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(
            new ConfirmSheetRegistrationRequest(Guid.NewGuid()),
            CancellationToken.None));
    }

    private static SheetRegistration CreateRegistration(SheetRegistrationStatus status, DateTime? confirmedAtUtc)
    {
        var dependentSheetId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        return new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            dependentSheetId,
            canonicalFloorPlanVersionId,
            SheetRegistrationMethod.WholeSheetSimilarity,
            new SheetRegistrationTransform(1.2m, 0.5m, 10m, -2m),
            0.74m,
            status,
            new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc),
            confirmedAtUtc,
            "Needs visual review",
            "Whole sheet anchors",
            new WholePlanRegistrationProof(
                WholePlanRegistrationProof.CurrentVersion,
                Passed: true,
                canonicalFloorPlanVersionId,
                dependentSheetId,
                new string('a', 64),
                new string('b', 64),
                HorizontalCoverage: 0.94m,
                VerticalCoverage: 0.91m,
                RootMeanSquareResidual: 0.01m,
                MaximumResidual: 0.02m));
    }

    private sealed class CapturingSheetRegistrationRepository : ISheetRegistrationRepository
    {
        private readonly SheetRegistration? registration;

        public CapturingSheetRegistrationRepository(SheetRegistration? registration = null)
        {
            this.registration = registration;
        }

        public SheetRegistration? Updated { get; private set; }

        public Task AddAsync(SheetRegistration registration, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<SheetRegistration?> GetByIdAsync(Guid registrationId, CancellationToken cancellationToken)
            => Task.FromResult(registration?.Id == registrationId ? registration : null);

        public Task UpdateAsync(SheetRegistration registration, CancellationToken cancellationToken)
        {
            Updated = registration;
            return Task.CompletedTask;
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
