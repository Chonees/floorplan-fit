using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Registration;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets.Registration;

public sealed class RegisterRoofSheetHandlerTests
{
    [Fact]
    public async Task HandleAsync_registers_roof_sheet_with_overhang_rule()
    {
        var planSetVersionId = Guid.NewGuid();
        var roofSheet = CreateSheet(planSetVersionId, PlanSheetType.RoofPlan);
        var clock = new FakeClock(new DateTime(2026, 6, 30, 21, 0, 0, DateTimeKind.Utc));
        var registrationRepository = new CapturingSheetRegistrationRepository();
        var auditEventRepository = new CapturingPlanSetAuditEventRepository();
        var unitOfWork = new CapturingUnitOfWork();
        var handler = new RegisterRoofSheetHandler(
            new FakePlanSheetRepository(roofSheet),
            registrationRepository,
            unitOfWork,
            clock,
            auditEventRepository);

        var response = await handler.HandleAsync(
            new RegisterRoofSheetRequest(
                planSetVersionId,
                roofSheet.Id,
                Scale: 1.1m,
                RotationDegrees: 0m,
                TranslateX: 4m,
                TranslateY: 8m,
                Confidence: 0.86m,
                OverhangInches: 18m,
                ConfirmRegistration: true,
                Warning: null),
            CancellationToken.None);

        Assert.True(unitOfWork.Saved);
        Assert.Equal(planSetVersionId, response.PlanSetVersionId);
        Assert.Equal(roofSheet.Id, response.DependentSheetId);
        Assert.Equal("RoofFootprintWithOverhang", response.Method);
        Assert.Equal("Confirmed", response.Status);
        Assert.Equal("PreserveOverhangInches=18", response.RuleSummary);
        Assert.Equal(clock.UtcNow, response.ConfirmedAtUtc);

        var saved = Assert.Single(registrationRepository.Items);
        Assert.Equal(SheetRegistrationMethod.RoofFootprintWithOverhang, saved.Method);
        Assert.Equal("PreserveOverhangInches=18", saved.RuleSummary);

        var auditEvent = Assert.Single(auditEventRepository.Items);
        Assert.Equal("SheetRegistration", auditEvent.AggregateType);
        Assert.Equal(response.RegistrationId, auditEvent.AggregateId);
        Assert.Equal("SheetRegistrationQualityMeasured", auditEvent.EventType);
        Assert.Contains("\"method\":\"RoofFootprintWithOverhang\"", auditEvent.PayloadJson, StringComparison.Ordinal);
        Assert.Contains("\"confidence\":0.86", auditEvent.PayloadJson, StringComparison.Ordinal);
        Assert.Contains("\"status\":\"Confirmed\"", auditEvent.PayloadJson, StringComparison.Ordinal);
        Assert.Contains("\"ruleSummary\":\"PreserveOverhangInches=18\"", auditEvent.PayloadJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_rejects_non_roof_sheet()
    {
        var planSetVersionId = Guid.NewGuid();
        var electricalSheet = CreateSheet(planSetVersionId, PlanSheetType.ElectricalPlan);
        var handler = new RegisterRoofSheetHandler(
            new FakePlanSheetRepository(electricalSheet),
            new CapturingSheetRegistrationRepository(),
            new CapturingUnitOfWork(),
            new FakeClock(new DateTime(2026, 6, 30, 21, 0, 0, DateTimeKind.Utc)));

        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(
            new RegisterRoofSheetRequest(
                planSetVersionId,
                electricalSheet.Id,
                Scale: 1m,
                RotationDegrees: 0m,
                TranslateX: 0m,
                TranslateY: 0m,
                Confidence: 0.9m,
                OverhangInches: 12m,
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
            new DateTime(2026, 6, 30, 20, 0, 0, DateTimeKind.Utc));
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
