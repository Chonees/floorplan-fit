using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Classification;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets.Classification;

public sealed class CorrectPlanSheetTypeHandlerTests
{
    [Fact]
    public async Task HandleAsync_updates_unregistered_dependent_sheet_type_and_records_quality_event()
    {
        var planSetVersionId = Guid.NewGuid();
        var sheet = CreateSheet(planSetVersionId, PlanSheetType.RoofPlan);
        var sheetRepository = new CapturingPlanSheetRepository(sheet);
        var auditEvents = new CapturingPlanSetAuditEventRepository();
        var clock = new FakeClock(new DateTime(2026, 7, 1, 21, 0, 0, DateTimeKind.Utc));
        var handler = new CorrectPlanSheetTypeHandler(
            sheetRepository,
            new EmptySheetRegistrationRepository(),
            new CapturingUnitOfWork(),
            clock,
            auditEvents);

        var response = await handler.HandleAsync(
            new CorrectPlanSheetTypeRequest(
                planSetVersionId,
                sheet.Id,
                "ElectricalPlan",
                "Imported roof by mistake."),
            CancellationToken.None);

        Assert.Equal("ElectricalPlan", response.SheetType);
        Assert.Equal(PlanSheetType.ElectricalPlan, sheetRepository.Item.SheetType);

        var auditEvent = Assert.Single(auditEvents.Items);
        Assert.Equal("PlanSheet", auditEvent.AggregateType);
        Assert.Equal(sheet.Id, auditEvent.AggregateId);
        Assert.Equal("SheetClassificationQualityMeasured", auditEvent.EventType);
        Assert.Contains("\"source\":\"UserCorrection\"", auditEvent.PayloadJson, StringComparison.Ordinal);
        Assert.Contains("\"status\":\"Corrected\"", auditEvent.PayloadJson, StringComparison.Ordinal);
        Assert.Contains("\"previousSheetType\":\"RoofPlan\"", auditEvent.PayloadJson, StringComparison.Ordinal);
        Assert.Contains("\"sheetType\":\"ElectricalPlan\"", auditEvent.PayloadJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_rejects_registered_sheet_type_correction()
    {
        var planSetVersionId = Guid.NewGuid();
        var sheet = CreateSheet(planSetVersionId, PlanSheetType.RoofPlan);
        var handler = new CorrectPlanSheetTypeHandler(
            new CapturingPlanSheetRepository(sheet),
            new CapturingSheetRegistrationRepository(
                new SheetRegistration(
                    Guid.NewGuid(),
                    planSetVersionId,
                    sheet.Id,
                    planSetVersionId,
                    SheetRegistrationMethod.RoofFootprintWithOverhang,
                    new SheetRegistrationTransform(1m, 0m, 0m, 0m),
                    0.9m,
                    SheetRegistrationStatus.PendingConfirmation,
                    new DateTime(2026, 7, 1, 20, 0, 0, DateTimeKind.Utc),
                    null,
                    null)),
            new CapturingUnitOfWork(),
            new FakeClock(new DateTime(2026, 7, 1, 21, 0, 0, DateTimeKind.Utc)));

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(
            new CorrectPlanSheetTypeRequest(planSetVersionId, sheet.Id, "ElectricalPlan"),
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_allows_rejected_registration_sheet_type_correction()
    {
        var planSetVersionId = Guid.NewGuid();
        var sheet = CreateSheet(planSetVersionId, PlanSheetType.RoofPlan);
        var sheetRepository = new CapturingPlanSheetRepository(sheet);
        var handler = new CorrectPlanSheetTypeHandler(
            sheetRepository,
            new CapturingSheetRegistrationRepository(
                new SheetRegistration(
                    Guid.NewGuid(),
                    planSetVersionId,
                    sheet.Id,
                    planSetVersionId,
                    SheetRegistrationMethod.RoofFootprintWithOverhang,
                    new SheetRegistrationTransform(1m, 0m, 0m, 0m),
                    0.9m,
                    SheetRegistrationStatus.Rejected,
                    new DateTime(2026, 7, 1, 20, 0, 0, DateTimeKind.Utc),
                    null,
                    null)),
            new CapturingUnitOfWork(),
            new FakeClock(new DateTime(2026, 7, 1, 21, 0, 0, DateTimeKind.Utc)));

        var response = await handler.HandleAsync(
            new CorrectPlanSheetTypeRequest(planSetVersionId, sheet.Id, "ElectricalPlan"),
            CancellationToken.None);

        Assert.Equal("ElectricalPlan", response.SheetType);
        Assert.Equal(PlanSheetType.ElectricalPlan, sheetRepository.Item.SheetType);
    }

    [Fact]
    public async Task HandleAsync_rejects_floor_plan_correction_because_floor_plan_stays_canonical_flow()
    {
        var sheet = CreateSheet(Guid.NewGuid(), PlanSheetType.RoofPlan);
        var handler = new CorrectPlanSheetTypeHandler(
            new CapturingPlanSheetRepository(sheet),
            new EmptySheetRegistrationRepository(),
            new CapturingUnitOfWork(),
            new FakeClock(new DateTime(2026, 7, 1, 21, 0, 0, DateTimeKind.Utc)));

        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(
            new CorrectPlanSheetTypeRequest(sheet.PlanSetVersionId, sheet.Id, "FloorPlan"),
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
            "Dependent",
            PlanSheetStatus.Imported,
            new DateTime(2026, 7, 1, 19, 0, 0, DateTimeKind.Utc));
    }

    private sealed class CapturingPlanSheetRepository : IPlanSheetRepository
    {
        public CapturingPlanSheetRepository(PlanSheet item)
        {
            Item = item;
        }

        public PlanSheet Item { get; private set; }

        public Task AddAsync(PlanSheet sheet, CancellationToken cancellationToken)
        {
            Item = sheet;
            return Task.CompletedTask;
        }

        public Task<PlanSheet?> GetByIdAsync(Guid sheetId, CancellationToken cancellationToken)
            => Task.FromResult(Item.Id == sheetId ? Item : null);

        public Task UpdateAsync(PlanSheet sheet, CancellationToken cancellationToken)
        {
            Item = sheet;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Guid sheetId, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class EmptySheetRegistrationRepository : ISheetRegistrationRepository
    {
        public Task AddAsync(SheetRegistration registration, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<SheetRegistration?> GetByIdAsync(Guid registrationId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<SheetRegistration>> ListByPlanSetVersionAsync(
            Guid planSetVersionId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SheetRegistration>>([]);
    }

    private sealed class CapturingSheetRegistrationRepository : ISheetRegistrationRepository
    {
        private readonly IReadOnlyList<SheetRegistration> registrations;

        public CapturingSheetRegistrationRepository(params SheetRegistration[] registrations)
        {
            this.registrations = registrations;
        }

        public Task AddAsync(SheetRegistration registration, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<SheetRegistration?> GetByIdAsync(Guid registrationId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<SheetRegistration>> ListByPlanSetVersionAsync(
            Guid planSetVersionId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SheetRegistration>>(
                registrations.Where(item => item.PlanSetVersionId == planSetVersionId).ToArray());
    }

    private sealed class CapturingUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
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
}
