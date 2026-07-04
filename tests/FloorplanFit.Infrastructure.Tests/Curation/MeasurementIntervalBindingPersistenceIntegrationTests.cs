using FloorplanFit.Domain.FloorPlans;
using FloorplanFit.Infrastructure.Persistence;
using FloorplanFit.Infrastructure.Runtime;

namespace FloorplanFit.Infrastructure.Tests.Curation;

public sealed class MeasurementIntervalBindingPersistenceIntegrationTests
{
    [Fact]
    public async Task Repositories_can_update_corridor_axis_and_node_axis_coordinate()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-measurement-axis-update-{Guid.NewGuid():N}");

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var curationId = Guid.NewGuid();
            var corridorId = Guid.NewGuid();
            var nodeId = Guid.NewGuid();
            var geometryPathId = Guid.NewGuid();

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var corridorRepository = new SqliteMeasurementCorridorRepository(session);
                var nodeRepository = new SqliteMeasurementNodeRepository(session);

                await corridorRepository.AddAsync(
                    new MeasurementCorridor(corridorId, curationId, "Franja 1", PinchAxisTag.Width, geometryPathId, 80m, 120m, "Verified", 1),
                    CancellationToken.None);
                await nodeRepository.AddAsync(
                    new MeasurementNode(nodeId, curationId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), geometryPathId, "Projected", 100m, 240m, 100m, 0m, 0m, 0.5m),
                    CancellationToken.None);
                await session.CommitAsync(CancellationToken.None);
            }

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var corridorRepository = new SqliteMeasurementCorridorRepository(session);
                var nodeRepository = new SqliteMeasurementNodeRepository(session);

                await corridorRepository.UpdateAsync(
                    new MeasurementCorridor(corridorId, curationId, "Franja 1", PinchAxisTag.Height, geometryPathId, 95m, 145m, "Verified", 1),
                    CancellationToken.None);
                await nodeRepository.UpdateAsync(
                    new MeasurementNode(nodeId, curationId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), geometryPathId, "Projected", 100m, 240m, 240m, 0m, 0m, 0.5m),
                    CancellationToken.None);
                await session.CommitAsync(CancellationToken.None);
            }

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var corridor = Assert.Single(await new SqliteMeasurementCorridorRepository(session).ListByCurationAsync(curationId, CancellationToken.None));
                var node = Assert.Single(await new SqliteMeasurementNodeRepository(session).ListByCurationAsync(curationId, CancellationToken.None));

                Assert.Equal(PinchAxisTag.Height, corridor.AxisTag);
                Assert.Equal(95m, corridor.BandMinCoordinate);
                Assert.Equal(145m, corridor.BandMaxCoordinate);
                Assert.Equal(240m, node.AxisCoordinate);
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
    public async Task Repositories_can_delete_a_single_node_and_only_interval_bindings_that_reference_it()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-measurement-node-delete-{Guid.NewGuid():N}");

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var curationId = Guid.NewGuid();
            var corridorId = Guid.NewGuid();
            var nodeToDeleteId = Guid.NewGuid();
            var secondNodeId = Guid.NewGuid();
            var thirdNodeId = Guid.NewGuid();
            var fourthNodeId = Guid.NewGuid();
            var bindingWithDeletedStartDimensionId = Guid.NewGuid();
            var bindingWithDeletedEndDimensionId = Guid.NewGuid();
            var remainingBindingDimensionId = Guid.NewGuid();

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var corridorRepository = new SqliteMeasurementCorridorRepository(session);
                var nodeRepository = new SqliteMeasurementNodeRepository(session);
                var bindingRepository = new SqliteDimensionIntervalBindingRepository(session);

                await corridorRepository.AddAsync(
                    new MeasurementCorridor(corridorId, curationId, "Patio-Width", PinchAxisTag.Width, Guid.NewGuid(), 95m, 145m, "Verified", 1),
                    CancellationToken.None);

                await nodeRepository.AddAsync(
                    new MeasurementNode(nodeToDeleteId, curationId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), Guid.NewGuid(), "Projected", 100m, 120m, 100m, 0m, 0m, 0.5m),
                    CancellationToken.None);
                await nodeRepository.AddAsync(
                    new MeasurementNode(secondNodeId, curationId, corridorId, 2, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), Guid.NewGuid(), "Projected", 180m, 120m, 180m, 0m, 0m, 0.5m),
                    CancellationToken.None);
                await nodeRepository.AddAsync(
                    new MeasurementNode(thirdNodeId, curationId, corridorId, 3, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), Guid.NewGuid(), "Projected", 260m, 120m, 260m, 0m, 0m, 0.5m),
                    CancellationToken.None);
                await nodeRepository.AddAsync(
                    new MeasurementNode(fourthNodeId, curationId, corridorId, 4, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), Guid.NewGuid(), "Projected", 340m, 120m, 340m, 0m, 0m, 0.5m),
                    CancellationToken.None);

                await bindingRepository.UpsertAsync(
                    new DimensionIntervalBinding(curationId, bindingWithDeletedStartDimensionId, corridorId, nodeToDeleteId, secondNodeId, "ManualVerified", 100m, 180m, DateTime.UtcNow),
                    CancellationToken.None);
                await bindingRepository.UpsertAsync(
                    new DimensionIntervalBinding(curationId, bindingWithDeletedEndDimensionId, corridorId, thirdNodeId, nodeToDeleteId, "ManualVerified", 260m, 100m, DateTime.UtcNow),
                    CancellationToken.None);
                await bindingRepository.UpsertAsync(
                    new DimensionIntervalBinding(curationId, remainingBindingDimensionId, corridorId, thirdNodeId, fourthNodeId, "ManualVerified", 260m, 340m, DateTime.UtcNow),
                    CancellationToken.None);
                await session.CommitAsync(CancellationToken.None);
            }

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var nodeRepository = new SqliteMeasurementNodeRepository(session);
                var bindingRepository = new SqliteDimensionIntervalBindingRepository(session);

                await bindingRepository.DeleteByNodeAsync(curationId, nodeToDeleteId, CancellationToken.None);
                await nodeRepository.DeleteAsync(nodeToDeleteId, CancellationToken.None);
                await session.CommitAsync(CancellationToken.None);
            }

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var corridors = await new SqliteMeasurementCorridorRepository(session).ListByCurationAsync(curationId, CancellationToken.None);
                var nodes = await new SqliteMeasurementNodeRepository(session).ListByCurationAsync(curationId, CancellationToken.None);
                var bindings = await new SqliteDimensionIntervalBindingRepository(session).ListByCurationAsync(curationId, CancellationToken.None);

                Assert.Single(corridors);
                Assert.Equal(3, nodes.Count);
                Assert.DoesNotContain(nodes, item => item.Id == nodeToDeleteId);
                Assert.Contains(nodes, item => item.Id == secondNodeId);
                Assert.Contains(nodes, item => item.Id == thirdNodeId);
                Assert.Contains(nodes, item => item.Id == fourthNodeId);

                var binding = Assert.Single(bindings);
                Assert.Equal(remainingBindingDimensionId, binding.DimensionId);
                Assert.Equal(thirdNodeId, binding.StartNodeId);
                Assert.Equal(fourthNodeId, binding.EndNodeId);
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
