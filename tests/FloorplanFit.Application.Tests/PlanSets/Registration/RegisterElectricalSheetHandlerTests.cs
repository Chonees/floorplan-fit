using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Registration;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets.Registration;

public sealed class RegisterElectricalSheetHandlerTests
{
    [Fact]
    public async Task HandleAsync_registers_electrical_sheet_with_pending_whole_sheet_similarity()
    {
        var planSetVersionId = Guid.NewGuid();
        var electricalSheet = CreateSheet(planSetVersionId, PlanSheetType.ElectricalPlan);
        var clock = new FakeClock(new DateTime(2026, 6, 30, 18, 0, 0, DateTimeKind.Utc));
        var sheetRepository = new FakePlanSheetRepository(electricalSheet);
        var registrationRepository = new CapturingSheetRegistrationRepository();
        var auditEventRepository = new CapturingPlanSetAuditEventRepository();
        var unitOfWork = new CapturingUnitOfWork();
        var handler = new RegisterElectricalSheetHandler(
            sheetRepository,
            registrationRepository,
            unitOfWork,
            clock,
            auditEventRepository);

        var response = await handler.HandleAsync(
            new RegisterElectricalSheetRequest(
                planSetVersionId,
                electricalSheet.Id,
                Scale: 1.25m,
                RotationDegrees: 0.5m,
                TranslateX: 12m,
                TranslateY: -3m,
                Confidence: 0.82m,
                ConfirmRegistration: false,
                Warning: "Needs visual review"),
            CancellationToken.None);

        Assert.True(unitOfWork.Saved);
        Assert.Equal(planSetVersionId, response.PlanSetVersionId);
        Assert.Equal(electricalSheet.Id, response.DependentSheetId);
        Assert.Equal(planSetVersionId, response.CanonicalFloorPlanVersionId);
        Assert.Equal("WholeSheetSimilarity", response.Method);
        Assert.Equal(1.25m, response.Transform.Scale);
        Assert.Equal(0.5m, response.Transform.RotationDegrees);
        Assert.Equal(12m, response.Transform.TranslateX);
        Assert.Equal(-3m, response.Transform.TranslateY);
        Assert.Equal(0.82m, response.Confidence);
        Assert.Equal("PendingConfirmation", response.Status);
        Assert.Equal("Needs visual review", response.Warning);
        Assert.Equal(clock.UtcNow, response.CreatedAtUtc);
        Assert.Null(response.ConfirmedAtUtc);

        var saved = Assert.Single(registrationRepository.Items);
        Assert.Equal(response.RegistrationId, saved.Id);
        Assert.Equal(SheetRegistrationMethod.WholeSheetSimilarity, saved.Method);
        Assert.Equal(SheetRegistrationStatus.PendingConfirmation, saved.Status);

        var auditEvent = Assert.Single(auditEventRepository.Items);
        Assert.Equal("SheetRegistration", auditEvent.AggregateType);
        Assert.Equal(response.RegistrationId, auditEvent.AggregateId);
        Assert.Equal("SheetRegistrationQualityMeasured", auditEvent.EventType);
        Assert.Equal(clock.UtcNow, auditEvent.OccurredAtUtc);
        Assert.Contains("\"method\":\"WholeSheetSimilarity\"", auditEvent.PayloadJson, StringComparison.Ordinal);
        Assert.Contains("\"confidence\":0.82", auditEvent.PayloadJson, StringComparison.Ordinal);
        Assert.Contains("\"status\":\"PendingConfirmation\"", auditEvent.PayloadJson, StringComparison.Ordinal);
        Assert.Contains("\"warning\":\"Needs visual review\"", auditEvent.PayloadJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_rejects_non_electrical_sheet()
    {
        var planSetVersionId = Guid.NewGuid();
        var roofSheet = CreateSheet(planSetVersionId, PlanSheetType.RoofPlan);
        var handler = new RegisterElectricalSheetHandler(
            new FakePlanSheetRepository(roofSheet),
            new CapturingSheetRegistrationRepository(),
            new CapturingUnitOfWork(),
            new FakeClock(new DateTime(2026, 6, 30, 18, 0, 0, DateTimeKind.Utc)));

        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(
            new RegisterElectricalSheetRequest(
                planSetVersionId,
                roofSheet.Id,
                Scale: 1m,
                RotationDegrees: 0m,
                TranslateX: 0m,
                TranslateY: 0m,
                Confidence: 0.9m,
                ConfirmRegistration: true),
            CancellationToken.None));
    }

    private static PlanSheet CreateSheet(Guid planSetVersionId, PlanSheetType sheetType)
    {
        return new PlanSheet(
            Guid.NewGuid(),
            planSetVersionId,
            sheetType,
            Guid.NewGuid(),
            Guid.NewGuid(),
            sheetType.ToString(),
            PlanSheetStatus.Imported,
            new DateTime(2026, 6, 30, 17, 0, 0, DateTimeKind.Utc));
    }

    private sealed class FakePlanSheetRepository : IPlanSheetRepository
    {
        private readonly PlanSheet sheet;

        public FakePlanSheetRepository(PlanSheet sheet)
        {
            this.sheet = sheet;
        }

        public Task AddAsync(PlanSheet sheet, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<PlanSheet?> GetByIdAsync(Guid sheetId, CancellationToken cancellationToken)
        {
            return Task.FromResult(sheet.Id == sheetId ? sheet : null);
        }

        public Task RemoveAsync(Guid sheetId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class CapturingSheetRegistrationRepository : ISheetRegistrationRepository
    {
        public List<SheetRegistration> Items { get; } = [];

        public Task AddAsync(SheetRegistration registration, CancellationToken cancellationToken)
        {
            Items.Add(registration);
            return Task.CompletedTask;
        }

        public Task<SheetRegistration?> GetByIdAsync(Guid registrationId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.FirstOrDefault(item => item.Id == registrationId));
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
