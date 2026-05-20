using FloorplanFit.Domain.FloorPlans;
using FloorplanFit.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Tests.Curation;

public sealed class FloorPlanLabelOverridePersistenceIntegrationTests
{
    [Fact]
    public async Task Schema_initializer_creates_floorplan_label_overrides_table()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"floorplan-fit-label-size-schema-{Guid.NewGuid():N}.db");

        try
        {
            await SqliteSchemaInitializer.InitializeAsync(dbPath, CancellationToken.None);
            await using var connection = new SqliteConnection($"Data Source={dbPath}");
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'floorplan_label_overrides'";
            var result = command.ExecuteScalar();

            Assert.Equal("floorplan_label_overrides", result);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Fact]
    public async Task Label_override_repository_upserts_and_reads_manual_and_detected_default_rows()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"floorplan-fit-label-size-repo-{Guid.NewGuid():N}.db");
        var curationId = Guid.NewGuid();

        try
        {
            await SqliteSchemaInitializer.InitializeAsync(dbPath, CancellationToken.None);
            await using var session = await SqliteSession.OpenAsync(dbPath, CancellationToken.None);
            var repository = new SqliteFloorPlanLabelOverrideRepository(session);

            await repository.UpsertAsync(
                FloorPlanLabelOverride.CreateResolvedTextHeight(
                    curationId,
                    FloorPlanLabelOverrideSourceKinds.RoomLabel,
                    Guid.NewGuid(),
                    6m,
                    new DateTime(2026, 5, 12, 0, 0, 0, DateTimeKind.Utc)),
                CancellationToken.None);
            await repository.UpsertAsync(
                FloorPlanLabelOverride.CreateDetectedDefault(
                    curationId,
                    FloorPlanLabelOverrideSourceKinds.OpeningLabel,
                    Guid.NewGuid(),
                    new DateTime(2026, 5, 12, 0, 1, 0, DateTimeKind.Utc)),
                CancellationToken.None);

            var items = await repository.ListByCurationAsync(curationId, CancellationToken.None);

            Assert.Equal(2, items.Count);
            Assert.Contains(items, item => item.SourceArtifactKind == FloorPlanLabelOverrideSourceKinds.RoomLabel && item.ResolvedTextHeight == 6m);
            Assert.Contains(items, item => item.SourceArtifactKind == FloorPlanLabelOverrideSourceKinds.OpeningLabel && item.ResolvedTextHeight is null);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }
}
