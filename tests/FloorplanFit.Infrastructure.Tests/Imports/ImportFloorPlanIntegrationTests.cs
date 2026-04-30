using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Import;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Infrastructure.Dxf;
using FloorplanFit.Infrastructure.Persistence;
using FloorplanFit.Infrastructure.Runtime;
using FloorplanFit.Infrastructure.Security;
using FloorplanFit.Infrastructure.Storage;
using FloorplanFit.Infrastructure.Tests.TestSupport;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Tests.Imports;

public sealed class ImportFloorPlanIntegrationTests
{
    [Fact]
    public async Task HandleAsync_persists_real_import_into_sqlite_and_returns_library_item()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-import-{Guid.NewGuid():N}");
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");
        var now = new DateTime(2026, 4, 29, 18, 0, 0, DateTimeKind.Utc);

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
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

                var response = await handler.HandleAsync(new ImportFloorPlanRequest(sourcePath), CancellationToken.None);

                Assert.Equal("santa-barbara", response.Item.Code);
                Assert.Equal("SANTA-BARBARA", response.Item.Name);
                Assert.Equal("Imported", response.Item.Status);
                Assert.Equal(1, response.Item.ActiveVersionNumber);
                Assert.Equal("inch", response.Item.SourceUnit);
            }

            await using (var connection = new SqliteConnection($"Data Source={workspace.DatabasePath}"))
            {
                await connection.OpenAsync();

                Assert.Equal(1L, await CountRowsAsync(connection, "measurement_contexts"));
                Assert.Equal(1L, await CountRowsAsync(connection, "imported_documents"));
                Assert.Equal(1L, await CountRowsAsync(connection, "floorplan_templates"));
                Assert.Equal(1L, await CountRowsAsync(connection, "floorplan_versions"));

                var managedPath = await GetImportedDocumentStoragePathAsync(connection);

                Assert.StartsWith(workspace.LibraryRawDxfDirectory, managedPath, StringComparison.OrdinalIgnoreCase);
                Assert.True(File.Exists(managedPath));
                Assert.NotEqual(sourcePath, managedPath);
            }
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
    public async Task HandleAsync_reimporting_the_same_dxf_creates_version_2_of_the_same_template()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-reimport-{Guid.NewGuid():N}");
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");
        var now = new DateTime(2026, 4, 29, 19, 0, 0, DateTimeKind.Utc);

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var firstResponse = await ExecuteImportAsync(workspace, sourcePath, now);
            var secondResponse = await ExecuteImportAsync(workspace, sourcePath, now);

            Assert.Equal("santa-barbara", firstResponse.Item.Code);
            Assert.Equal("santa-barbara", secondResponse.Item.Code);
            Assert.Equal("SANTA-BARBARA", secondResponse.Item.Name);
            Assert.Equal(2, secondResponse.Item.ActiveVersionNumber);

            await using (var connection = new SqliteConnection($"Data Source={workspace.DatabasePath}"))
            {
                await connection.OpenAsync();

                Assert.Equal(2L, await CountRowsAsync(connection, "measurement_contexts"));
                Assert.Equal(2L, await CountRowsAsync(connection, "imported_documents"));
                Assert.Equal(1L, await CountRowsAsync(connection, "floorplan_templates"));
                Assert.Equal(2L, await CountRowsAsync(connection, "floorplan_versions"));

                var templateCodes = await GetTemplateCodesAsync(connection);
                Assert.Equal(["santa-barbara"], templateCodes);

                var versionNumbers = await GetVersionNumbersAsync(connection);
                Assert.Equal([1, 2], versionNumbers);

                var originalFileNames = await GetImportedDocumentOriginalFileNamesAsync(connection);
                Assert.Equal(["SANTA-BARBARA.dxf", "SANTA-BARBARA.dxf"], originalFileNames);

                var storagePaths = await GetImportedDocumentStoragePathsAsync(connection);
                Assert.Contains(storagePaths, path => path.EndsWith("SANTA-BARBARA.dxf", StringComparison.OrdinalIgnoreCase));
                Assert.Contains(storagePaths, path => path.EndsWith("SANTA-BARBARA-2.dxf", StringComparison.OrdinalIgnoreCase));
            }
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

    private static async Task<ImportFloorPlanResponse> ExecuteImportAsync(
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

        return await handler.HandleAsync(new ImportFloorPlanRequest(sourcePath), CancellationToken.None);
    }

    private static async Task<long> CountRowsAsync(SqliteConnection connection, string tableName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {tableName}";

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    private static async Task<string> GetImportedDocumentStoragePathAsync(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT storage_path FROM imported_documents LIMIT 1";

        var result = await command.ExecuteScalarAsync();
        return Convert.ToString(result)!;
    }

    private static async Task<string[]> GetTemplateCodesAsync(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT code FROM floorplan_templates ORDER BY code";

        var results = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        return [.. results];
    }

    private static async Task<int[]> GetVersionNumbersAsync(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT version_number FROM floorplan_versions ORDER BY version_number";

        var results = new List<int>();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(reader.GetInt32(0));
        }

        return [.. results];
    }

    private static async Task<string[]> GetImportedDocumentOriginalFileNamesAsync(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT original_file_name FROM imported_documents ORDER BY storage_path";

        var results = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        return [.. results];
    }

    private static async Task<string[]> GetImportedDocumentStoragePathsAsync(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT storage_path FROM imported_documents ORDER BY storage_path";

        var results = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        return [.. results];
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
