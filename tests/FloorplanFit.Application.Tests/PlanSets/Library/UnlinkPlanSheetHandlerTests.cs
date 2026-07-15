using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Library;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets.Library;

public sealed class UnlinkPlanSheetHandlerTests
{
    [Fact]
    public void UnlinkPlanSheetRequest_exposes_only_the_dependent_sheet_identity()
    {
        var request = new UnlinkPlanSheetRequest(Guid.NewGuid());

        Assert.Equal(
            ["SheetId"],
            typeof(UnlinkPlanSheetRequest)
                .GetProperties()
                .Select(property => property.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray());
        Assert.NotEqual(Guid.Empty, request.SheetId);
    }

    [Fact]
    public async Task HandleAsync_rejects_canonical_floor_plan_sheet_without_mutating_workflow_records()
    {
        var canonicalSheet = CreateSheet(PlanSheetType.FloorPlan);
        var operations = new List<string>();
        var planSheetRepository = new CapturingPlanSheetRepository([canonicalSheet], operations);
        var registrationRepository = new CapturingSheetRegistrationRepository([], operations);
        var projectionRepository = new CapturingSheetAdjustmentProjectionRepository([], operations);
        var unitOfWork = new CapturingUnitOfWork(operations);
        var handler = new UnlinkPlanSheetHandler(
            planSheetRepository,
            registrationRepository,
            projectionRepository,
            unitOfWork);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(
            new UnlinkPlanSheetRequest(canonicalSheet.Id),
            CancellationToken.None));

        Assert.Contains("canonical", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal([canonicalSheet], planSheetRepository.Items);
        Assert.Empty(registrationRepository.RemovedDependentSheetIds);
        Assert.Empty(projectionRepository.RemovedDependentSheetIds);
        Assert.Empty(operations);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task HandleAsync_removes_all_active_workflow_records_before_confirmed_ready_for_export_electrical_sheet_and_commits_once()
    {
        var planSetVersionId = Guid.NewGuid();
        var electricalSheet = CreateSheet(planSetVersionId, PlanSheetType.ElectricalPlan);
        var unaffectedSheet = CreateSheet(planSetVersionId, PlanSheetType.RoofPlan);
        var confirmedRegistration = CreateRegistration(
            planSetVersionId,
            electricalSheet.Id,
            SheetRegistrationStatus.Confirmed,
            confirmedAtUtc: new DateTime(2026, 7, 14, 16, 0, 0, DateTimeKind.Utc));
        var priorRegistration = CreateRegistration(
            planSetVersionId,
            electricalSheet.Id,
            SheetRegistrationStatus.Rejected,
            confirmedAtUtc: null);
        var unaffectedRegistration = CreateRegistration(
            planSetVersionId,
            unaffectedSheet.Id,
            SheetRegistrationStatus.Confirmed,
            confirmedAtUtc: new DateTime(2026, 7, 14, 16, 5, 0, DateTimeKind.Utc));
        var readyForExportProjection = CreateProjection(
            planSetVersionId,
            electricalSheet.Id,
            confirmedRegistration.Id,
            SheetAdjustmentProjectionStatus.ReadyForExport);
        var priorProjection = CreateProjection(
            planSetVersionId,
            electricalSheet.Id,
            priorRegistration.Id,
            SheetAdjustmentProjectionStatus.RequiresManualConfirmation);
        var unaffectedProjection = CreateProjection(
            planSetVersionId,
            unaffectedSheet.Id,
            unaffectedRegistration.Id,
            SheetAdjustmentProjectionStatus.ReadyForExport);
        var operations = new List<string>();
        var planSheetRepository = new CapturingPlanSheetRepository(
            [electricalSheet, unaffectedSheet],
            operations);
        var registrationRepository = new CapturingSheetRegistrationRepository(
            [confirmedRegistration, priorRegistration, unaffectedRegistration],
            operations);
        var projectionRepository = new CapturingSheetAdjustmentProjectionRepository(
            [readyForExportProjection, priorProjection, unaffectedProjection],
            operations);
        var unitOfWork = new CapturingUnitOfWork(operations);
        var handler = new UnlinkPlanSheetHandler(
            planSheetRepository,
            registrationRepository,
            projectionRepository,
            unitOfWork);

        await handler.HandleAsync(
            new UnlinkPlanSheetRequest(electricalSheet.Id),
            CancellationToken.None);

        Assert.Equal(
            [
                "projection", "projection",
                "registration", "registration",
                "sheet", "commit"
            ],
            operations);
        Assert.Equal([electricalSheet.Id], projectionRepository.RemovedDependentSheetIds);
        Assert.Equal([electricalSheet.Id], registrationRepository.RemovedDependentSheetIds);
        Assert.Equal(1, unitOfWork.SaveCallCount);

        Assert.Equal([unaffectedSheet], planSheetRepository.Items);
        Assert.Equal([unaffectedRegistration], registrationRepository.Items);
        Assert.Equal([unaffectedProjection], projectionRepository.Items);
    }

    private static PlanSheet CreateSheet(PlanSheetType sheetType)
        => CreateSheet(Guid.NewGuid(), sheetType);

    private static PlanSheet CreateSheet(Guid planSetVersionId, PlanSheetType sheetType)
        => new(
            Guid.NewGuid(),
            planSetVersionId,
            sheetType,
            Guid.NewGuid(),
            Guid.NewGuid(),
            sheetType.ToString(),
            PlanSheetStatus.Imported,
            new DateTime(2026, 7, 14, 15, 0, 0, DateTimeKind.Utc));

    private static SheetRegistration CreateRegistration(
        Guid planSetVersionId,
        Guid dependentSheetId,
        SheetRegistrationStatus status,
        DateTime? confirmedAtUtc)
        => new(
            Guid.NewGuid(),
            planSetVersionId,
            dependentSheetId,
            Guid.NewGuid(),
            SheetRegistrationMethod.WholeSheetSimilarity,
            new SheetRegistrationTransform(1m, 0m, 0m, 0m),
            0.92m,
            status,
            new DateTime(2026, 7, 14, 15, 30, 0, DateTimeKind.Utc),
            confirmedAtUtc,
            warning: null);

    private static SheetAdjustmentProjection CreateProjection(
        Guid planSetVersionId,
        Guid dependentSheetId,
        Guid sheetRegistrationId,
        SheetAdjustmentProjectionStatus status)
        => new(
            Guid.NewGuid(),
            planSetVersionId,
            dependentSheetId,
            sheetRegistrationId,
            Guid.NewGuid(),
            SheetAdjustmentProjectionMethod.ElectricalWholeSheetSimilarity,
            new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
            0.92m,
            status,
            warning: null,
            canonicalCompressionStepCount: 0,
            new DateTime(2026, 7, 14, 15, 45, 0, DateTimeKind.Utc));

    private sealed class CapturingPlanSheetRepository : IPlanSheetRepository
    {
        private readonly List<string> operations;

        public CapturingPlanSheetRepository(IEnumerable<PlanSheet> sheets, List<string> operations)
        {
            Items = [.. sheets];
            this.operations = operations;
        }

        public List<PlanSheet> Items { get; }

        public Task AddAsync(PlanSheet sheet, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<PlanSheet?> GetByIdAsync(Guid sheetId, CancellationToken cancellationToken)
            => Task.FromResult(Items.SingleOrDefault(sheet => sheet.Id == sheetId));

        public Task RemoveAsync(Guid sheetId, CancellationToken cancellationToken)
        {
            operations.Add("sheet");
            Items.RemoveAll(sheet => sheet.Id == sheetId);
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingSheetRegistrationRepository : ISheetRegistrationRepository
    {
        private readonly List<string> operations;

        public CapturingSheetRegistrationRepository(
            IEnumerable<SheetRegistration> registrations,
            List<string> operations)
        {
            Items = [.. registrations];
            this.operations = operations;
        }

        public List<SheetRegistration> Items { get; }

        public List<Guid> RemovedDependentSheetIds { get; } = [];

        public Task AddAsync(SheetRegistration registration, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<SheetRegistration?> GetByIdAsync(Guid registrationId, CancellationToken cancellationToken)
            => Task.FromResult(Items.SingleOrDefault(registration => registration.Id == registrationId));

        public Task RemoveByDependentSheetIdAsync(Guid dependentSheetId, CancellationToken cancellationToken)
        {
            foreach (var registration in Items.Where(item => item.DependentSheetId == dependentSheetId).ToArray())
            {
                operations.Add("registration");
            }

            RemovedDependentSheetIds.Add(dependentSheetId);
            Items.RemoveAll(item => item.DependentSheetId == dependentSheetId);
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingSheetAdjustmentProjectionRepository : ISheetAdjustmentProjectionRepository
    {
        private readonly List<string> operations;

        public CapturingSheetAdjustmentProjectionRepository(
            IEnumerable<SheetAdjustmentProjection> projections,
            List<string> operations)
        {
            Items = [.. projections];
            this.operations = operations;
        }

        public List<SheetAdjustmentProjection> Items { get; }

        public List<Guid> RemovedDependentSheetIds { get; } = [];

        public Task AddAsync(SheetAdjustmentProjection projection, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<SheetAdjustmentProjection?> GetByIdAsync(Guid projectionId, CancellationToken cancellationToken)
            => Task.FromResult(Items.SingleOrDefault(projection => projection.Id == projectionId));

        public Task<IReadOnlyList<SheetAdjustmentProjection>> ListByPlanSetVersionAndCanonicalAdjustmentAsync(
            Guid planSetVersionId,
            Guid canonicalAdjustmentId,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task RemoveByDependentSheetIdAsync(Guid dependentSheetId, CancellationToken cancellationToken)
        {
            foreach (var projection in Items.Where(item => item.DependentSheetId == dependentSheetId).ToArray())
            {
                operations.Add("projection");
            }

            RemovedDependentSheetIds.Add(dependentSheetId);
            Items.RemoveAll(item => item.DependentSheetId == dependentSheetId);
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingUnitOfWork : IUnitOfWork
    {
        private readonly List<string> operations;

        public CapturingUnitOfWork(List<string> operations)
        {
            this.operations = operations;
        }

        public int SaveCallCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            operations.Add("commit");
            SaveCallCount++;
            return Task.CompletedTask;
        }
    }
}
