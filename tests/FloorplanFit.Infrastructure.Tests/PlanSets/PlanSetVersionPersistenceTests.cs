using FloorplanFit.Domain.PlanSets;
using FloorplanFit.Infrastructure.Persistence;
using FloorplanFit.Infrastructure.Runtime;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Tests.PlanSets;

public sealed class PlanSetVersionPersistenceTests
{
    [Fact]
    public async Task Committed_session_does_not_lock_database_before_it_is_disposed()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-plan-set-unlocked-{Guid.NewGuid():N}");

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var housePlanSet = new HousePlanSet(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "seminole2000",
                "Seminole",
                new DateTime(2026, 7, 1, 16, 30, 0, DateTimeKind.Utc));

            await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            await new SqliteHousePlanSetRepository(session).AddAsync(housePlanSet, CancellationToken.None);
            await new SqliteUnitOfWork(session).SaveChangesAsync(CancellationToken.None);

            await using var readSession = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var loaded = await new SqliteHousePlanSetRepository(readSession).GetBySourceFloorPlanTemplateAsync(
                housePlanSet.SourceFloorPlanTemplateId,
                CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equal(housePlanSet.Id, loaded!.Id);
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
    public async Task Session_allows_repository_commands_after_unit_of_work_commit()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-plan-set-session-{Guid.NewGuid():N}");

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var housePlanSet = new HousePlanSet(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "seminole2000",
                "Seminole",
                new DateTime(2026, 7, 1, 16, 0, 0, DateTimeKind.Utc));
            var version = new PlanSetVersion(
                Guid.NewGuid(),
                housePlanSet.Id,
                Guid.NewGuid(),
                versionNumber: 1,
                createdAtUtc: new DateTime(2026, 7, 1, 16, 5, 0, DateTimeKind.Utc));

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var unitOfWork = new SqliteUnitOfWork(session);
                await new SqliteHousePlanSetRepository(session).AddAsync(housePlanSet, CancellationToken.None);
                await unitOfWork.SaveChangesAsync(CancellationToken.None);

                var versionRepository = new SqlitePlanSetVersionRepository(session);
                Assert.Null(await versionRepository.GetByCanonicalFloorPlanVersionAsync(
                    version.CanonicalFloorPlanVersionId,
                    CancellationToken.None));
                await versionRepository.AddAsync(version, CancellationToken.None);
                await unitOfWork.SaveChangesAsync(CancellationToken.None);
            }

            await using var readSession = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var loaded = await new SqlitePlanSetVersionRepository(readSession).GetByCanonicalFloorPlanVersionAsync(
                version.CanonicalFloorPlanVersionId,
                CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equal(version.Id, loaded!.Id);
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
    public async Task AddAsync_persists_and_reads_plan_set_version_by_canonical_floor_plan()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-plan-set-version-{Guid.NewGuid():N}");

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var version = new PlanSetVersion(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                versionNumber: 1,
                createdAtUtc: new DateTime(2026, 6, 30, 22, 0, 0, DateTimeKind.Utc));

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var repository = new SqlitePlanSetVersionRepository(session);
                await repository.AddAsync(version, CancellationToken.None);
                await new SqliteUnitOfWork(session).SaveChangesAsync(CancellationToken.None);
            }

            await using var readSession = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var loaded = await new SqlitePlanSetVersionRepository(readSession).GetByCanonicalFloorPlanVersionAsync(
                version.CanonicalFloorPlanVersionId,
                CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equal(version.Id, loaded!.Id);
            Assert.Equal(version.HousePlanSetId, loaded.HousePlanSetId);
            Assert.Equal(version.CanonicalFloorPlanVersionId, loaded.CanonicalFloorPlanVersionId);
            Assert.Equal(1, loaded.VersionNumber);
            Assert.Equal(version.CreatedAtUtc, loaded.CreatedAtUtc);
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
}
