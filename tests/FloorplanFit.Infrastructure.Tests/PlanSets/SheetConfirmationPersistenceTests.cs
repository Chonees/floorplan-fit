using FloorplanFit.Domain.PlanSets;
using FloorplanFit.Infrastructure.Persistence;
using FloorplanFit.Infrastructure.Runtime;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Tests.PlanSets;

public sealed class SheetConfirmationPersistenceTests
{
    [Fact]
    public async Task SheetRegistration_list_by_plan_set_version_reads_only_that_plan_set()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-sheet-registration-list-{Guid.NewGuid():N}");

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var planSetVersionId = Guid.NewGuid();
            var first = CreateRegistration(
                SheetRegistrationStatus.Confirmed,
                new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc),
                planSetVersionId: planSetVersionId);
            var second = CreateRegistration(
                SheetRegistrationStatus.PendingConfirmation,
                confirmedAtUtc: null,
                planSetVersionId: planSetVersionId);
            var other = CreateRegistration(
                SheetRegistrationStatus.Confirmed,
                new DateTime(2026, 7, 1, 8, 30, 0, DateTimeKind.Utc),
                planSetVersionId: Guid.NewGuid());

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var repository = new SqliteSheetRegistrationRepository(session);
                await repository.AddAsync(first, CancellationToken.None);
                await repository.AddAsync(second, CancellationToken.None);
                await repository.AddAsync(other, CancellationToken.None);
                await new SqliteUnitOfWork(session).SaveChangesAsync(CancellationToken.None);
            }

            await using var readSession = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var registrations = await new SqliteSheetRegistrationRepository(readSession).ListByPlanSetVersionAsync(
                planSetVersionId,
                CancellationToken.None);

            Assert.Equal(2, registrations.Count);
            Assert.Contains(registrations, registration => registration.Id == first.Id);
            Assert.Contains(registrations, registration => registration.Id == second.Id);
            Assert.DoesNotContain(registrations, registration => registration.Id == other.Id);
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SheetRegistration_update_persists_confirmed_status()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-sheet-registration-confirm-{Guid.NewGuid():N}");

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var registration = CreateRegistration(SheetRegistrationStatus.PendingConfirmation, confirmedAtUtc: null);
            var confirmed = CreateRegistration(SheetRegistrationStatus.Confirmed, new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc), registration.Id);

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var repository = new SqliteSheetRegistrationRepository(session);
                await repository.AddAsync(registration, CancellationToken.None);
                await repository.UpdateAsync(confirmed, CancellationToken.None);
                await new SqliteUnitOfWork(session).SaveChangesAsync(CancellationToken.None);
            }

            await using var readSession = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var loaded = await new SqliteSheetRegistrationRepository(readSession).GetByIdAsync(registration.Id, CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equal(SheetRegistrationStatus.Confirmed, loaded!.Status);
            Assert.Equal(confirmed.ConfirmedAtUtc, loaded.ConfirmedAtUtc);
            Assert.Equal(registration.Transform.Scale, loaded.Transform.Scale);
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SheetAdjustmentProjection_update_persists_ready_for_export_status()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-sheet-projection-confirm-{Guid.NewGuid():N}");

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var projection = CreateProjection(SheetAdjustmentProjectionStatus.RequiresManualConfirmation);
            var confirmed = CreateProjection(SheetAdjustmentProjectionStatus.ReadyForExport, projection.Id);

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var repository = new SqliteSheetAdjustmentProjectionRepository(session);
                await repository.AddAsync(projection, CancellationToken.None);
                await repository.UpdateAsync(confirmed, CancellationToken.None);
                await new SqliteUnitOfWork(session).SaveChangesAsync(CancellationToken.None);
            }

            await using var readSession = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var loaded = await new SqliteSheetAdjustmentProjectionRepository(readSession).GetByIdAsync(projection.Id, CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equal(SheetAdjustmentProjectionStatus.ReadyForExport, loaded!.Status);
            Assert.Equal(projection.Transform.Scale, loaded.Transform.Scale);
            Assert.Equal(projection.Warning, loaded.Warning);
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static SheetRegistration CreateRegistration(
        SheetRegistrationStatus status,
        DateTime? confirmedAtUtc,
        Guid? id = null,
        Guid? planSetVersionId = null)
    {
        var resolvedPlanSetVersionId = planSetVersionId ?? Guid.NewGuid();
        return new SheetRegistration(
            id ?? Guid.NewGuid(),
            resolvedPlanSetVersionId,
            Guid.NewGuid(),
            resolvedPlanSetVersionId,
            SheetRegistrationMethod.WholeSheetSimilarity,
            new SheetRegistrationTransform(1.2m, 0.5m, 10m, -2m),
            0.74m,
            status,
            new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc),
            confirmedAtUtc,
            "Needs visual review",
            "Whole sheet anchors");
    }

    private static SheetAdjustmentProjection CreateProjection(
        SheetAdjustmentProjectionStatus status,
        Guid? id = null)
        => new(
            id ?? Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            SheetAdjustmentProjectionMethod.ElectricalWholeSheetSimilarity,
            new SheetAdjustmentProjectionTransform(1.1m, 0m, 12m, -3m),
            0.76m,
            status,
            "Needs visual review",
            1,
            new DateTime(2026, 7, 1, 8, 30, 0, DateTimeKind.Utc),
            "electrical follows floor plan");
}
