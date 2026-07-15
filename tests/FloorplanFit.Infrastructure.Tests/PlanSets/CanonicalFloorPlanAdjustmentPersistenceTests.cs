using FloorplanFit.Domain.PlanSets;
using FloorplanFit.Infrastructure.Persistence;
using FloorplanFit.Infrastructure.Runtime;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Tests.PlanSets;

public sealed class CanonicalFloorPlanAdjustmentPersistenceTests
{
    [Fact]
    public async Task UpdateExportPathAsync_rolls_back_without_commit_and_persists_with_commit()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-canonical-update-{Guid.NewGuid():N}");

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);
            var adjustment = CreateAdjustment("exports/X.dxf");

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                await new SqliteCanonicalFloorPlanAdjustmentRepository(session).AddAsync(adjustment, CancellationToken.None);
                await new SqliteUnitOfWork(session).SaveChangesAsync(CancellationToken.None);
            }

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                await new SqliteCanonicalFloorPlanAdjustmentRepository(session).UpdateExportPathAsync(
                    adjustment.Id,
                    "exports/X-plan-set/X-floorplan.dxf",
                    CancellationToken.None);
                await session.RollbackAsync(CancellationToken.None);
                await new SqliteUnitOfWork(session).SaveChangesAsync(CancellationToken.None);
            }

            Assert.Equal("exports/X.dxf", await ReadExportPathAsync(workspace.DatabasePath, adjustment.Id));

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                await new SqliteCanonicalFloorPlanAdjustmentRepository(session).UpdateExportPathAsync(
                    adjustment.Id,
                    "exports/X-plan-set/X-floorplan.dxf",
                    CancellationToken.None);
                await new SqliteUnitOfWork(session).SaveChangesAsync(CancellationToken.None);
            }

            Assert.Equal(
                "exports/X-plan-set/X-floorplan.dxf",
                await ReadExportPathAsync(workspace.DatabasePath, adjustment.Id));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task UpdateExportPathAsync_throws_when_adjustment_does_not_exist()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-canonical-update-missing-{Guid.NewGuid():N}");

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);
            await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var repository = new SqliteCanonicalFloorPlanAdjustmentRepository(session);

            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpdateExportPathAsync(
                Guid.NewGuid(),
                "exports/X-plan-set/X-floorplan.dxf",
                CancellationToken.None));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task AddAsync_persists_canonical_floor_plan_adjustment()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-canonical-adjustment-{Guid.NewGuid():N}");

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var adjustment = new CanonicalFloorPlanAdjustment(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "library/site.dxf",
                "exports/floor-adjusted.dxf",
                "{\"FloorToSiteScale\":1.2}",
                "{\"Version\":\"v1\",\"FloorToSiteScale\":1.2,\"Operations\":[{\"Kind\":\"HorizontalCompression\"}]}",
                new DateTime(2026, 6, 30, 23, 59, 0, DateTimeKind.Utc));

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var repository = new SqliteCanonicalFloorPlanAdjustmentRepository(session);
                await repository.AddAsync(adjustment, CancellationToken.None);
                await new SqliteUnitOfWork(session).SaveChangesAsync(CancellationToken.None);
            }

            await using var connection = new SqliteConnection($"Data Source={workspace.DatabasePath}");
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT plan_set_version_id, canonical_floor_plan_version_id, site_plan_source_path, canonical_floor_plan_export_path, placement_json, adjustment_recipe_json
                FROM canonical_floor_plan_adjustments
                WHERE id = $id
                """;
            command.Parameters.AddWithValue("$id", adjustment.Id.ToString());

            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal(adjustment.PlanSetVersionId.ToString(), reader.GetString(0));
            Assert.Equal(adjustment.CanonicalFloorPlanVersionId.ToString(), reader.GetString(1));
            Assert.Equal("library/site.dxf", reader.GetString(2));
            Assert.Equal("exports/floor-adjusted.dxf", reader.GetString(3));
            Assert.Equal("{\"FloorToSiteScale\":1.2}", reader.GetString(4));
            Assert.Contains("FloorToSiteScale", reader.GetString(5), StringComparison.Ordinal);
            Assert.Contains("HorizontalCompression", reader.GetString(5), StringComparison.Ordinal);
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

    private static CanonicalFloorPlanAdjustment CreateAdjustment(string exportPath)
        => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "library/site.dxf",
            exportPath,
            "{\"FloorToSiteScale\":1.2}",
            "{\"Version\":\"v1\",\"FloorToSiteScale\":1.2,\"Operations\":[]}",
            new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc));

    private static async Task<string?> ReadExportPathAsync(string databasePath, Guid adjustmentId)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT canonical_floor_plan_export_path FROM canonical_floor_plan_adjustments WHERE id = $id";
        command.Parameters.AddWithValue("$id", adjustmentId.ToString());
        return (string?)await command.ExecuteScalarAsync();
    }
}
