using FloorplanFit.Application.FloorPlans.Import;
using FloorplanFit.Infrastructure.Dxf;
using FloorplanFit.Infrastructure.Persistence;
using FloorplanFit.Infrastructure.Runtime;
using FloorplanFit.Infrastructure.Security;
using FloorplanFit.Infrastructure.Storage;
using FloorplanFit.Infrastructure.Tests.TestSupport;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Tests.Library;

public sealed class SqliteFloorPlanLibraryReaderIntegrationTests
{
    [Fact]
    public async Task ListAsync_groups_reimported_floorplans_under_one_template_with_all_versions()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-library-reader-{Guid.NewGuid():N}");
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            await ExecuteImportAsync(workspace, sourcePath, new DateTime(2026, 5, 9, 12, 0, 0, DateTimeKind.Utc));
            await ExecuteImportAsync(workspace, sourcePath, new DateTime(2026, 5, 9, 13, 0, 0, DateTimeKind.Utc));

            await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var reader = new SqliteFloorPlanLibraryReader(session);

            var items = await reader.ListAsync(CancellationToken.None);

            var item = Assert.Single(items);
            Assert.Equal("santa-barbara", item.Code);
            Assert.Equal(2, item.VersionCount);
            Assert.Equal(2, item.ActiveVersionNumber);
            Assert.Equal([2, 1], item.Versions.Select(version => version.VersionNumber).ToArray());
            Assert.True(item.Versions.Single(version => version.VersionNumber == 2).IsCurrent);
            Assert.False(item.Versions.Single(version => version.VersionNumber == 1).IsCurrent);
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
    public async Task RemoveAsync_deletes_one_version_and_promotes_the_latest_remaining_version()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-version-delete-{Guid.NewGuid():N}");
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            await ExecuteImportAsync(workspace, sourcePath, new DateTime(2026, 5, 9, 12, 0, 0, DateTimeKind.Utc));
            await ExecuteImportAsync(workspace, sourcePath, new DateTime(2026, 5, 9, 13, 0, 0, DateTimeKind.Utc));

            await using (var deleteSession = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var reader = new SqliteFloorPlanLibraryReader(deleteSession);
                var item = Assert.Single(await reader.ListAsync(CancellationToken.None));
                var currentVersion = item.Versions.Single(version => version.IsCurrent);

                var repository = new SqliteFloorPlanVersionRepository(deleteSession);
                await repository.RemoveAsync(currentVersion.VersionId, CancellationToken.None);
                await new SqliteUnitOfWork(deleteSession).SaveChangesAsync(CancellationToken.None);
            }

            await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var remainingItem = Assert.Single(await new SqliteFloorPlanLibraryReader(session).ListAsync(CancellationToken.None));

            var remainingVersion = Assert.Single(remainingItem.Versions);
            Assert.Equal(1, remainingVersion.VersionNumber);
            Assert.True(remainingVersion.IsCurrent);
            Assert.Equal(1, remainingItem.ActiveVersionNumber);
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
