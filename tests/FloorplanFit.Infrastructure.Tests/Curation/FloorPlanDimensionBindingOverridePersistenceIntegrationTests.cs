using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.FloorPlans;
using FloorplanFit.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Tests.Curation;

public sealed class FloorPlanDimensionBindingOverridePersistenceIntegrationTests
{
    [Fact]
    public async Task Schema_initializer_creates_floorplan_dimension_binding_override_tables()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"floorplan-fit-dimension-binding-override-schema-{Guid.NewGuid():N}.db");

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
                  AND name IN ('floorplan_dimension_binding_overrides', 'floorplan_dimension_binding_override_anchors')
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
    public async Task Dimension_binding_override_repository_upserts_reads_and_deletes()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"floorplan-fit-dimension-binding-override-repo-{Guid.NewGuid():N}.db");
        var curationId = Guid.NewGuid();
        var updatedAt = new DateTime(2026, 5, 15, 12, 10, 0, DateTimeKind.Utc);

        try
        {
            await SqliteSchemaInitializer.InitializeAsync(dbPath, CancellationToken.None);
            await using var session = await SqliteSession.OpenAsync(dbPath, CancellationToken.None);
            var repository = new SqliteFloorPlanDimensionBindingOverrideRepository(session);

            await repository.UpsertAsync(
                FloorPlanDimensionBindingOverride.CreateManualOverride(
                    curationId,
                    "AB12",
                    "LinearSpan",
                    true,
                    0.96m,
                    "Manual endpoint rebind.",
                    new FloorPlanDimensionMeasuredSpanOverride("Width", 100m, 260m, 0m),
                    [
                        new FloorPlanDimensionBindingAnchorOverride(1, "edge-a", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), "Start", 100m, 100m, 0m, null),
                        new FloorPlanDimensionBindingAnchorOverride(2, "edge-b", "OpeningCandidate", Guid.NewGuid(), Guid.NewGuid(), "Projected", 260m, 120m, 0m, 0.5m)
                    ],
                    updatedAt),
                CancellationToken.None);

            var items = await repository.ListByCurationAsync(curationId, CancellationToken.None);

            var saved = Assert.Single(items);
            Assert.Equal("AB12", saved.SourceDimensionKey);
            Assert.Equal("LinearSpan", saved.BindingKind);
            Assert.True(saved.IsResolved);
            Assert.Equal("Width", saved.MeasuredSpan?.AxisTag);
            Assert.Equal(2, saved.Anchors.Count);
            Assert.Equal(0.5m, saved.Anchors[1].SegmentRatio);

            await repository.DeleteAsync(curationId, "AB12", CancellationToken.None);

            Assert.Empty(await repository.ListByCurationAsync(curationId, CancellationToken.None));
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
