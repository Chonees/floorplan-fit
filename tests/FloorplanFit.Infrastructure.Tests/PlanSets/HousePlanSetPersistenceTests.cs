using FloorplanFit.Domain.PlanSets;
using FloorplanFit.Infrastructure.Persistence;
using FloorplanFit.Infrastructure.Runtime;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Tests.PlanSets;

public sealed class HousePlanSetPersistenceTests
{
    [Fact]
    public async Task AddAsync_persists_and_reads_house_plan_set_by_source_floor_plan_template()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-house-plan-set-{Guid.NewGuid():N}");

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
                new DateTime(2026, 6, 30, 22, 30, 0, DateTimeKind.Utc));

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var repository = new SqliteHousePlanSetRepository(session);
                await repository.AddAsync(housePlanSet, CancellationToken.None);
                await new SqliteUnitOfWork(session).SaveChangesAsync(CancellationToken.None);
            }

            await using var readSession = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var loaded = await new SqliteHousePlanSetRepository(readSession).GetBySourceFloorPlanTemplateAsync(
                housePlanSet.SourceFloorPlanTemplateId,
                CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equal(housePlanSet.Id, loaded!.Id);
            Assert.Equal(housePlanSet.SourceFloorPlanTemplateId, loaded.SourceFloorPlanTemplateId);
            Assert.Equal("seminole2000", loaded.Code);
            Assert.Equal("Seminole", loaded.Name);
            Assert.Equal(housePlanSet.CreatedAtUtc, loaded.CreatedAtUtc);
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
