using FloorplanFit.Application.FloorPlans.Import;
using FloorplanFit.Infrastructure.Dxf;
using FloorplanFit.Infrastructure.Persistence;
using FloorplanFit.Infrastructure.Runtime;
using FloorplanFit.Infrastructure.Security;
using FloorplanFit.Infrastructure.Storage;
using FloorplanFit.Infrastructure.Tests.TestSupport;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Tests.Library;

public sealed class SqliteFloorPlanVersionCleanupServiceIntegrationTests
{
    [Fact]
    public async Task CleanupAsync_hard_deletes_versions_that_were_soft_deleted_and_leaves_active_versions_intact()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-cleanup-{Guid.NewGuid():N}");
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            await ExecuteImportAsync(workspace, sourcePath, new DateTime(2026, 5, 9, 12, 0, 0, DateTimeKind.Utc));
            await ExecuteImportAsync(workspace, sourcePath, new DateTime(2026, 5, 9, 13, 0, 0, DateTimeKind.Utc));

            Guid softDeletedVersionId;
            Guid survivingVersionId;
            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var reader = new SqliteFloorPlanLibraryReader(session);
                var item = Assert.Single(await reader.ListAsync(CancellationToken.None));
                var ordered = item.Versions.OrderByDescending(v => v.VersionNumber).ToArray();
                softDeletedVersionId = ordered[0].VersionId;
                survivingVersionId = ordered[1].VersionId;

                var repository = new SqliteFloorPlanVersionRepository(session);
                await repository.RemoveAsync(softDeletedVersionId, CancellationToken.None);
                await new SqliteUnitOfWork(session).SaveChangesAsync(CancellationToken.None);
            }

            Assert.True(VersionRowExists(workspace.DatabasePath, softDeletedVersionId));
            Assert.True(VersionRowExists(workspace.DatabasePath, survivingVersionId));

            var cleanup = new SqliteFloorPlanVersionCleanupService(workspace);
            var result = await cleanup.CleanupAsync(CancellationToken.None);

            Assert.Equal(1, result.VersionsRemoved);
            Assert.False(VersionRowExists(workspace.DatabasePath, softDeletedVersionId));
            Assert.True(VersionRowExists(workspace.DatabasePath, survivingVersionId));

            await using var verifySession = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var remainingItem = Assert.Single(await new SqliteFloorPlanLibraryReader(verifySession).ListAsync(CancellationToken.None));
            var remainingVersion = Assert.Single(remainingItem.Versions);
            Assert.Equal(survivingVersionId, remainingVersion.VersionId);
            Assert.True(remainingVersion.IsCurrent);
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
    public async Task CleanupAsync_is_a_noop_when_no_versions_are_soft_deleted()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-cleanup-noop-{Guid.NewGuid():N}");
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            await ExecuteImportAsync(workspace, sourcePath, new DateTime(2026, 5, 9, 12, 0, 0, DateTimeKind.Utc));

            var cleanup = new SqliteFloorPlanVersionCleanupService(workspace);
            var result = await cleanup.CleanupAsync(CancellationToken.None);

            Assert.Equal(0, result.VersionsRemoved);

            await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var item = Assert.Single(await new SqliteFloorPlanLibraryReader(session).ListAsync(CancellationToken.None));
            Assert.Single(item.Versions);
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

    private static bool VersionRowExists(string databasePath, Guid versionId)
    {
        using var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM floorplan_versions WHERE id = $id";
        command.Parameters.AddWithValue("$id", versionId.ToString());
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static async Task ExecuteImportAsync(
        AppWorkspace workspace,
        string sourcePath,
        DateTime now)
    {
        await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);

        var handler = new ImportFloorPlanHandler(
            new IxMiliaDxfGateway(),
            new ManagedFileStorage(workspace),
            new SqliteFloorPlanTemplateRepository(session),
            new SqliteFloorPlanVersionRepository(session),
            new SqliteImportedDocumentRepository(session),
            new SqliteMeasurementContextRepository(session),
            new SqliteUnitOfWork(session),
            new Sha256FileHashService(),
            new FixedClock(now),
            new ImportFloorPlanResultFactory());

        await handler.HandleAsync(new Contracts.FloorPlans.ImportFloorPlanRequest(sourcePath), CancellationToken.None);
    }

    private sealed class FixedClock : FloorplanFit.Application.Abstractions.IClock
    {
        public FixedClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
