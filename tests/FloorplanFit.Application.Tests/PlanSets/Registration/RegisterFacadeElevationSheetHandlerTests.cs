using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Registration;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets.Registration;

public sealed class RegisterFacadeElevationSheetHandlerTests
{
    [Fact]
    public async Task HandleAsync_registers_facade_elevation_sheet_with_vertical_preservation_rule()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var facadeSheet = CreateSheet(planSetVersionId, PlanSheetType.FacadeElevation);
        var clock = new FakeClock(new DateTime(2026, 6, 30, 23, 0, 0, DateTimeKind.Utc));
        var registrationRepository = new CapturingSheetRegistrationRepository();
        var auditEventRepository = new CapturingPlanSetAuditEventRepository();
        var unitOfWork = new CapturingUnitOfWork();
        var handler = new RegisterFacadeElevationSheetHandler(
            new FakePlanSheetRepository(facadeSheet),
            new FakePlanSetVersionRepository(CreatePlanSetVersion(planSetVersionId, canonicalFloorPlanVersionId)),
            registrationRepository,
            unitOfWork,
            clock,
            auditEventRepository);

        var response = await handler.HandleAsync(
            new RegisterFacadeElevationSheetRequest(
                planSetVersionId,
                facadeSheet.Id,
                HorizontalScale: 1.2m,
                HorizontalOffset: 7m,
                Confidence: 0.88m,
                ConfirmRegistration: true,
                HorizontalReferenceName: "FrontWallBaseline",
                Warning: null),
            CancellationToken.None);

        Assert.True(unitOfWork.Saved);
        Assert.Equal(planSetVersionId, response.PlanSetVersionId);
        Assert.Equal(canonicalFloorPlanVersionId, response.CanonicalFloorPlanVersionId);
        Assert.Equal(facadeSheet.Id, response.DependentSheetId);
        Assert.Equal("FacadeHorizontalReference", response.Method);
        Assert.Equal("Confirmed", response.Status);
        Assert.Equal("PreserveVertical=true;HorizontalReference=FrontWallBaseline", response.RuleSummary);
        Assert.Equal(1.2m, response.Transform.Scale);
        Assert.Equal(0m, response.Transform.RotationDegrees);
        Assert.Equal(7m, response.Transform.TranslateX);
        Assert.Equal(0m, response.Transform.TranslateY);
        Assert.Equal(clock.UtcNow, response.ConfirmedAtUtc);

        var saved = Assert.Single(registrationRepository.Items);
        Assert.Equal(SheetRegistrationMethod.FacadeHorizontalReference, saved.Method);
        Assert.Equal("PreserveVertical=true;HorizontalReference=FrontWallBaseline", saved.RuleSummary);

        var auditEvent = Assert.Single(auditEventRepository.Items);
        Assert.Equal("SheetRegistration", auditEvent.AggregateType);
        Assert.Equal(response.RegistrationId, auditEvent.AggregateId);
        Assert.Equal("SheetRegistrationQualityMeasured", auditEvent.EventType);
        Assert.Contains("\"method\":\"FacadeHorizontalReference\"", auditEvent.PayloadJson, StringComparison.Ordinal);
        Assert.Contains("\"confidence\":0.88", auditEvent.PayloadJson, StringComparison.Ordinal);
        Assert.Contains("\"status\":\"Confirmed\"", auditEvent.PayloadJson, StringComparison.Ordinal);
        Assert.Contains("\"ruleSummary\":\"PreserveVertical=true;HorizontalReference=FrontWallBaseline\"", auditEvent.PayloadJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_rejects_non_facade_elevation_sheet()
    {
        var planSetVersionId = Guid.NewGuid();
        var roofSheet = CreateSheet(planSetVersionId, PlanSheetType.RoofPlan);
        var handler = new RegisterFacadeElevationSheetHandler(
            new FakePlanSheetRepository(roofSheet),
            new FakePlanSetVersionRepository(CreatePlanSetVersion(planSetVersionId, Guid.NewGuid())),
            new CapturingSheetRegistrationRepository(),
            new CapturingUnitOfWork(),
            new FakeClock(new DateTime(2026, 6, 30, 23, 0, 0, DateTimeKind.Utc)));

        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(
            new RegisterFacadeElevationSheetRequest(
                planSetVersionId,
                roofSheet.Id,
                HorizontalScale: 1m,
                HorizontalOffset: 0m,
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
            new DateTime(2026, 6, 30, 22, 0, 0, DateTimeKind.Utc));
    }

    private static PlanSetVersion CreatePlanSetVersion(Guid id, Guid canonicalFloorPlanVersionId)
        => new(
            id,
            Guid.NewGuid(),
            canonicalFloorPlanVersionId,
            versionNumber: 1,
            createdAtUtc: new DateTime(2026, 6, 30, 21, 0, 0, DateTimeKind.Utc));

    private sealed class FakePlanSetVersionRepository : IPlanSetVersionRepository
    {
        private readonly PlanSetVersion version;

        public FakePlanSetVersionRepository(PlanSetVersion version)
        {
            this.version = version;
        }

        public Task<PlanSetVersion?> GetByIdAsync(Guid planSetVersionId, CancellationToken cancellationToken)
            => Task.FromResult(version.Id == planSetVersionId ? version : null);

        public Task<PlanSetVersion?> GetByCanonicalFloorPlanVersionAsync(
            Guid canonicalFloorPlanVersionId,
            CancellationToken cancellationToken)
            => Task.FromResult(
                version.CanonicalFloorPlanVersionId == canonicalFloorPlanVersionId ? version : null);

        public Task AddAsync(PlanSetVersion version, CancellationToken cancellationToken)
            => throw new NotSupportedException();
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
