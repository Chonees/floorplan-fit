using FloorplanFit.Domain.FloorPlans;
using FloorplanFit.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Tests.Curation;

public sealed class FloorPlanArtifactPositionPersistenceIntegrationTests
{
    [Fact]
    public async Task Schema_initializer_creates_floorplan_artifact_positions_table()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"floorplan-fit-position-schema-{Guid.NewGuid():N}.db");

        try
        {
            await SqliteSchemaInitializer.InitializeAsync(dbPath, CancellationToken.None);
            await using var connection = new SqliteConnection($"Data Source={dbPath}");
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'floorplan_artifact_positions'";
            var result = command.ExecuteScalar();

            Assert.Equal("floorplan_artifact_positions", result);
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
    public async Task Artifact_position_repository_upserts_and_reads_absolute_and_translation_rows()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"floorplan-fit-position-repo-{Guid.NewGuid():N}.db");
        var curationId = Guid.NewGuid();

        try
        {
            await SqliteSchemaInitializer.InitializeAsync(dbPath, CancellationToken.None);
            await using var session = await SqliteSession.OpenAsync(dbPath, CancellationToken.None);
            var repository = new SqliteFloorPlanArtifactPositionRepository(session);

            await repository.UpsertAsync(
                FloorPlanArtifactPosition.CreateAbsolutePoint(
                    curationId,
                    FloorPlanArtifactPositionSourceKinds.RoomLabel,
                    Guid.NewGuid(),
                    320m,
                    640m,
                    new DateTime(2026, 5, 11, 21, 0, 0, DateTimeKind.Utc)),
                CancellationToken.None);
            await repository.UpsertAsync(
                FloorPlanArtifactPosition.CreateTranslation(
                    curationId,
                    FloorPlanArtifactPositionSourceKinds.OpeningCandidate,
                    Guid.NewGuid(),
                    18m,
                    -6m,
                    new DateTime(2026, 5, 11, 21, 1, 0, DateTimeKind.Utc)),
                CancellationToken.None);

            var items = await repository.ListByCurationAsync(curationId, CancellationToken.None);

            Assert.Equal(2, items.Count);
            Assert.Contains(items, item =>
                item.PositionMode == FloorPlanArtifactPositionMode.AbsolutePoint &&
                item.ResolvedX == 320m &&
                item.ResolvedY == 640m);
            Assert.Contains(items, item =>
                item.PositionMode == FloorPlanArtifactPositionMode.Translation &&
                item.TranslationDx == 18m &&
                item.TranslationDy == -6m);
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
