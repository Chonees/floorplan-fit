using FloorplanFit.Domain.FloorPlans;
using FloorplanFit.Infrastructure.Persistence;
using FloorplanFit.Infrastructure.Runtime;

namespace FloorplanFit.Infrastructure.Tests.Curation;

public sealed class MeasurementIntervalBindingPersistenceIntegrationTests
{
    [Fact]
    public async Task Repositories_can_delete_a_corridor_with_its_nodes_and_interval_bindings()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-measurement-corridor-delete-{Guid.NewGuid():N}");

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var curationId = Guid.NewGuid();
            var corridorId = Guid.NewGuid();
            var otherCorridorId = Guid.NewGuid();

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var corridorRepository = new SqliteMeasurementCorridorRepository(session);
                var nodeRepository = new SqliteMeasurementNodeRepository(session);
                var bindingRepository = new SqliteDimensionIntervalBindingRepository(session);

                await corridorRepository.AddAsync(
                    new MeasurementCorridor(corridorId, curationId, "Patio-Width", PinchAxisTag.Width, Guid.NewGuid(), 95m, 145m, "Verified", 1),
                    CancellationToken.None);
                await corridorRepository.AddAsync(
                    new MeasurementCorridor(otherCorridorId, curationId, "Bath-Width", PinchAxisTag.Width, Guid.NewGuid(), 200m, 260m, "Verified", 2),
                    CancellationToken.None);

                await nodeRepository.AddAsync(
                    new MeasurementNode(Guid.NewGuid(), curationId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), Guid.NewGuid(), "Projected", 100m, 120m, 100m, 0m, 0m, 0.5m),
                    CancellationToken.None);
                await nodeRepository.AddAsync(
                    new MeasurementNode(Guid.NewGuid(), curationId, otherCorridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), Guid.NewGuid(), "Projected", 224m, 120m, 224m, 0m, 0m, 1m),
                    CancellationToken.None);

                await bindingRepository.UpsertAsync(
                    new DimensionIntervalBinding(curationId, Guid.NewGuid(), corridorId, Guid.NewGuid(), Guid.NewGuid(), "ManualVerified", 100m, 224m, DateTime.UtcNow),
                    CancellationToken.None);
                await bindingRepository.UpsertAsync(
                    new DimensionIntervalBinding(curationId, Guid.NewGuid(), otherCorridorId, Guid.NewGuid(), Guid.NewGuid(), "ManualVerified", 200m, 260m, DateTime.UtcNow),
                    CancellationToken.None);
                await session.CommitAsync(CancellationToken.None);
            }

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var corridorRepository = new SqliteMeasurementCorridorRepository(session);
                var nodeRepository = new SqliteMeasurementNodeRepository(session);
                var bindingRepository = new SqliteDimensionIntervalBindingRepository(session);

                await bindingRepository.DeleteByCorridorAsync(curationId, corridorId, CancellationToken.None);
                await nodeRepository.DeleteByCorridorAsync(corridorId, CancellationToken.None);
                await corridorRepository.DeleteAsync(corridorId, CancellationToken.None);
                await session.CommitAsync(CancellationToken.None);
            }

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var corridors = await new SqliteMeasurementCorridorRepository(session).ListByCurationAsync(curationId, CancellationToken.None);
                var nodes = await new SqliteMeasurementNodeRepository(session).ListByCurationAsync(curationId, CancellationToken.None);
                var bindings = await new SqliteDimensionIntervalBindingRepository(session).ListByCurationAsync(curationId, CancellationToken.None);

                Assert.Single(corridors);
                Assert.DoesNotContain(corridors, item => item.Id == corridorId);
                Assert.Single(nodes);
                Assert.DoesNotContain(nodes, item => item.CorridorId == corridorId);
                Assert.Single(bindings);
                Assert.DoesNotContain(bindings, item => item.CorridorId == corridorId);
            }
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Repositories_round_trip_corridors_nodes_and_manual_verified_interval_bindings()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-measurement-bindings-{Guid.NewGuid():N}");

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var curationId = Guid.NewGuid();
            var corridorId = Guid.NewGuid();
            var startNodeId = Guid.NewGuid();
            var endNodeId = Guid.NewGuid();

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var corridorRepository = new SqliteMeasurementCorridorRepository(session);
                var nodeRepository = new SqliteMeasurementNodeRepository(session);
                var bindingRepository = new SqliteDimensionIntervalBindingRepository(session);

                await corridorRepository.AddAsync(
                    new MeasurementCorridor(
                        corridorId,
                        curationId,
                        "Patio-Width",
                        PinchAxisTag.Width,
                        Guid.NewGuid(),
                        95m,
                        145m,
                        "Verified",
                        1),
                    CancellationToken.None);

                await nodeRepository.AddAsync(
                    new MeasurementNode(
                        startNodeId,
                        curationId,
                        corridorId,
                        1,
                        "ProjectedGeometry",
                        FloorPlanArtifactSourceKinds.WallCandidate,
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        "Projected",
                        100m,
                        120m,
                        100m,
                        0m,
                        0m,
                        0.5m),
                    CancellationToken.None);
                await nodeRepository.AddAsync(
                    new MeasurementNode(
                        endNodeId,
                        curationId,
                        corridorId,
                        2,
                        "ProjectedGeometry",
                        FloorPlanArtifactSourceKinds.OpeningCandidate,
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        "Projected",
                        224m,
                        120m,
                        224m,
                        0m,
                        0m,
                        0.5m),
                    CancellationToken.None);
                await bindingRepository.UpsertAsync(
                    new DimensionIntervalBinding(
                        curationId,
                        Guid.NewGuid(),
                        corridorId,
                        startNodeId,
                        endNodeId,
                        "ManualVerified",
                        100m,
                        224m,
                        new DateTime(2026, 5, 15, 18, 0, 0, DateTimeKind.Utc)),
                    CancellationToken.None);
                await session.CommitAsync(CancellationToken.None);
            }

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var corridors = await new SqliteMeasurementCorridorRepository(session).ListByCurationAsync(curationId, CancellationToken.None);
                var nodes = await new SqliteMeasurementNodeRepository(session).ListByCurationAsync(curationId, CancellationToken.None);
                var bindings = await new SqliteDimensionIntervalBindingRepository(session).ListByCurationAsync(curationId, CancellationToken.None);

                var corridor = Assert.Single(corridors);
                Assert.Equal("Patio-Width", corridor.Name);
                Assert.Equal(PinchAxisTag.Width, corridor.AxisTag);
                Assert.Equal(95m, corridor.BandMinCoordinate);
                Assert.Equal(145m, corridor.BandMaxCoordinate);

                Assert.Equal(2, nodes.Count);
                Assert.Contains(nodes, item => item.Id == startNodeId && item.PositionRatio == 0.5m);
                Assert.Contains(nodes, item => item.Id == endNodeId && item.SourceArtifactKind == FloorPlanArtifactSourceKinds.OpeningCandidate);

                var binding = Assert.Single(bindings);
                Assert.Equal("ManualVerified", binding.BindingStatus);
                Assert.Equal(100m, binding.IntervalStartCoordinate);
                Assert.Equal(224m, binding.IntervalEndCoordinate);
            }
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
