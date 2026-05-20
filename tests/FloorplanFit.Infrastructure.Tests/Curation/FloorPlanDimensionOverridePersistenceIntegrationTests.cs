using FloorplanFit.Domain.FloorPlans;
using FloorplanFit.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Tests.Curation;

public sealed class FloorPlanDimensionOverridePersistenceIntegrationTests
{
    [Fact]
    public async Task Schema_initializer_creates_floorplan_dimension_override_tables()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"floorplan-fit-dimension-override-schema-{Guid.NewGuid():N}.db");

        try
        {
            await SqliteSchemaInitializer.InitializeAsync(dbPath, CancellationToken.None);
            await using var connection = new SqliteConnection($"Data Source={dbPath}");
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type = 'table'
                  AND name IN ('floorplan_dimension_overrides', 'floorplan_dimension_override_primitives')
                """;
            var result = Convert.ToInt32(command.ExecuteScalar());

            Assert.Equal(2, result);
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
    public async Task Dimension_override_repository_upserts_reads_and_marks_exported()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"floorplan-fit-dimension-override-repo-{Guid.NewGuid():N}.db");
        var curationId = Guid.NewGuid();
        var updatedAt = new DateTime(2026, 5, 12, 0, 10, 0, DateTimeKind.Utc);
        var exportedAt = updatedAt.AddMinutes(5);

        try
        {
            await SqliteSchemaInitializer.InitializeAsync(dbPath, CancellationToken.None);
            await using var session = await SqliteSession.OpenAsync(dbPath, CancellationToken.None);
            var repository = new SqliteFloorPlanDimensionOverrideRepository(session);

            await repository.UpsertAsync(
                FloorPlanDimensionOverride.CreateManualSnapshot(
                    curationId,
                    "AB12",
                    "DIMENSION:AB12",
                    "AB12",
                    "11'-0\"",
                    100m,
                    100m,
                    0m,
                    248m,
                    100m,
                    0m,
                    100m,
                    140m,
                    0m,
                    180m,
                    148m,
                    3.5m,
                    0m,
                    "ARCH",
                    null,
                    null,
                    "MiddleCenter",
                    [
                        new ExtractedDimensionLinePrimitive("LINE-1", 1, 100m, 140m, 100m, 100m),
                        new ExtractedDimensionLinePrimitive("LINE-2", 2, 248m, 140m, 248m, 100m),
                        new ExtractedDimensionLinePrimitive("LINE-3", 3, 100m, 140m, 248m, 140m)
                    ],
                    [
                        new ExtractedDimensionTextPrimitive("TEXT-1", 1, "11'-0\"", 180m, 148m, 3.5m, 0m)
                        {
                            StyleName = "ARCH",
                            AttachmentPoint = "MiddleCenter"
                        }
                    ],
                    [
                        new ExtractedDimensionInsertPrimitive("INSERT-1", 1, "_Dot", 100m, 140m, 0m),
                        new ExtractedDimensionInsertPrimitive("INSERT-2", 2, "_Dot", 248m, 140m, 0m)
                    ],
                    [],
                    [],
                    [],
                    updatedAt,
                    null),
                CancellationToken.None);

            await repository.MarkExportedAsync(curationId, ["AB12"], exportedAt, CancellationToken.None);

            var items = await repository.ListByCurationAsync(curationId, CancellationToken.None);

            var saved = Assert.Single(items);
            Assert.Equal("AB12", saved.SourceDimensionKey);
            Assert.Equal("11'-0\"", saved.DisplayText);
            Assert.Equal(248m, saved.DefPoint2X);
            Assert.Equal(3, saved.LinePrimitives.Count);
            Assert.False(saved.IsDirty);
            Assert.Equal(exportedAt, saved.LastExportedAtUtc);
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
