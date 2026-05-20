using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class DimensionIntervalBindingHandlersTests
{
    [Fact]
    public async Task RemoveMeasurementCorridorHandler_deletes_corridor_nodes_and_bindings_and_saves_changes()
    {
        var curation = new FloorPlanCuration(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            FloorPlanCurationStatus.Draft,
            null,
            null,
            new DateTime(2026, 5, 16, 15, 0, 0, DateTimeKind.Utc),
            null);
        var corridorId = Guid.NewGuid();
        var otherCorridorId = Guid.NewGuid();
        var floorPlanCurationRepository = new InMemoryFloorPlanCurationRepository(curation);
        var measurementCorridorRepository = new InMemoryMeasurementCorridorRepository(
                new MeasurementCorridor(
                    corridorId,
                    curation.Id,
                    "Patio-Width",
                    PinchAxisTag.Width,
                    Guid.NewGuid(),
                    95m,
                    145m,
                    "Verified",
                    1),
                new MeasurementCorridor(
                    otherCorridorId,
                    curation.Id,
                    "Bath-Width",
                    PinchAxisTag.Width,
                    Guid.NewGuid(),
                    200m,
                    260m,
                    "Verified",
                    2));
        var measurementNodeRepository = new InMemoryMeasurementNodeRepository(
                new MeasurementNode(
                    Guid.NewGuid(),
                    curation.Id,
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
                new MeasurementNode(
                    Guid.NewGuid(),
                    curation.Id,
                    otherCorridorId,
                    1,
                    "ProjectedGeometry",
                    FloorPlanArtifactSourceKinds.WallCandidate,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Projected",
                    224m,
                    120m,
                    224m,
                    0m,
                    0m,
                    1m));
        var dimensionIntervalBindingRepository = new InMemoryDimensionIntervalBindingRepository(
                new DimensionIntervalBinding(
                    curation.Id,
                    Guid.NewGuid(),
                    corridorId,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "ManualVerified",
                    100m,
                    224m,
                    DateTime.UtcNow),
                new DimensionIntervalBinding(
                    curation.Id,
                    Guid.NewGuid(),
                    otherCorridorId,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "ManualVerified",
                    200m,
                    260m,
                    DateTime.UtcNow));
        var unitOfWork = new FakeUnitOfWork();
        var handler = new FloorplanFit.Application.FloorPlans.Curation.RemoveMeasurementCorridorHandler(
            floorPlanCurationRepository,
            measurementCorridorRepository,
            measurementNodeRepository,
            dimensionIntervalBindingRepository,
            unitOfWork);

        await handler.HandleAsync(curation.Id, corridorId, CancellationToken.None);

        Assert.Single(measurementCorridorRepository.Items);
        Assert.DoesNotContain(measurementCorridorRepository.Items, item => item.Id == corridorId);
        Assert.Single(measurementNodeRepository.Items);
        Assert.DoesNotContain(measurementNodeRepository.Items, item => item.CorridorId == corridorId);
        Assert.Single(dimensionIntervalBindingRepository.Items);
        Assert.DoesNotContain(dimensionIntervalBindingRepository.Items, item => item.CorridorId == corridorId);
        Assert.True(measurementCorridorRepository.Deleted);
        Assert.True(measurementNodeRepository.DeletedByCorridor);
        Assert.True(dimensionIntervalBindingRepository.DeletedByCorridor);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    [Fact]
    public async Task RestoreDimensionIntervalBindingHandler_deletes_binding_and_saves_changes()
    {
        var curation = new FloorPlanCuration(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            FloorPlanCurationStatus.Draft,
            null,
            null,
            new DateTime(2026, 5, 15, 19, 0, 0, DateTimeKind.Utc),
            null);
        var dimensionId = Guid.NewGuid();
        var repository = new InMemoryDimensionIntervalBindingRepository(
            new DimensionIntervalBinding(
                curation.Id,
                dimensionId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "ManualVerified",
                100m,
                224m,
                new DateTime(2026, 5, 15, 19, 5, 0, DateTimeKind.Utc)));
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RestoreDimensionIntervalBindingHandler(
            new InMemoryFloorPlanCurationRepository(curation),
            repository,
            unitOfWork);

        await handler.HandleAsync(curation.Id, dimensionId, CancellationToken.None);

        Assert.Empty(repository.Items);
        Assert.True(repository.Deleted);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    private sealed class InMemoryFloorPlanCurationRepository : IFloorPlanCurationRepository
    {
        private readonly FloorPlanCuration curation;

        public InMemoryFloorPlanCurationRepository(FloorPlanCuration curation) => this.curation = curation;

        public Task<FloorPlanCuration?> GetByIdAsync(Guid curationId, CancellationToken cancellationToken)
            => Task.FromResult(curation.Id == curationId ? curation : null);

        public Task<FloorPlanCuration?> GetDraftAsync(Guid floorPlanVersionId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FloorPlanCuration?> GetPublishedAsync(Guid floorPlanVersionId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<int> GetNextCurationVersionAsync(Guid floorPlanVersionId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AddAsync(FloorPlanCuration curation, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task UpdateAsync(FloorPlanCuration curation, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class InMemoryDimensionIntervalBindingRepository : IDimensionIntervalBindingRepository
    {
        public InMemoryDimensionIntervalBindingRepository(params DimensionIntervalBinding[] items)
        {
            Items.AddRange(items);
        }

        public List<DimensionIntervalBinding> Items { get; } = [];
        public bool Deleted { get; private set; }
        public bool DeletedByCorridor { get; private set; }

        public Task<IReadOnlyList<DimensionIntervalBinding>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<DimensionIntervalBinding>>(Items.Where(item => item.FloorPlanCurationId == curationId).ToArray());

        public Task UpsertAsync(DimensionIntervalBinding binding, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.FloorPlanCurationId == binding.FloorPlanCurationId && item.DimensionId == binding.DimensionId);
            Items.Add(binding);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid floorPlanCurationId, Guid dimensionId, CancellationToken cancellationToken)
        {
            Deleted = true;
            Items.RemoveAll(item => item.FloorPlanCurationId == floorPlanCurationId && item.DimensionId == dimensionId);
            return Task.CompletedTask;
        }

        public Task DeleteByCorridorAsync(Guid floorPlanCurationId, Guid corridorId, CancellationToken cancellationToken)
        {
            DeletedByCorridor = true;
            Items.RemoveAll(item => item.FloorPlanCurationId == floorPlanCurationId && item.CorridorId == corridorId);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryMeasurementCorridorRepository : IMeasurementCorridorRepository
    {
        public InMemoryMeasurementCorridorRepository(params MeasurementCorridor[] items)
        {
            Items.AddRange(items);
        }

        public List<MeasurementCorridor> Items { get; } = [];
        public bool Deleted { get; private set; }

        public Task AddAsync(MeasurementCorridor corridor, CancellationToken cancellationToken)
        {
            Items.Add(corridor);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid corridorId, CancellationToken cancellationToken)
        {
            Deleted = true;
            Items.RemoveAll(item => item.Id == corridorId);
            return Task.CompletedTask;
        }

        public Task<MeasurementCorridor?> GetByIdAsync(Guid corridorId, CancellationToken cancellationToken)
            => Task.FromResult(Items.SingleOrDefault(item => item.Id == corridorId));

        public Task<IReadOnlyList<MeasurementCorridor>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<MeasurementCorridor>>(Items.Where(item => item.FloorPlanCurationId == curationId).ToArray());
    }

    private sealed class InMemoryMeasurementNodeRepository : IMeasurementNodeRepository
    {
        public InMemoryMeasurementNodeRepository(params MeasurementNode[] items)
        {
            Items.AddRange(items);
        }

        public List<MeasurementNode> Items { get; } = [];
        public bool DeletedByCorridor { get; private set; }

        public Task AddAsync(MeasurementNode node, CancellationToken cancellationToken)
        {
            Items.Add(node);
            return Task.CompletedTask;
        }

        public Task<MeasurementNode?> GetByIdAsync(Guid nodeId, CancellationToken cancellationToken)
            => Task.FromResult(Items.SingleOrDefault(item => item.Id == nodeId));

        public Task<IReadOnlyList<MeasurementNode>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<MeasurementNode>>(Items.Where(item => item.FloorPlanCurationId == curationId).ToArray());

        public Task<IReadOnlyList<MeasurementNode>> ListByCorridorAsync(Guid corridorId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<MeasurementNode>>(Items.Where(item => item.CorridorId == corridorId).ToArray());

        public Task DeleteByCorridorAsync(Guid corridorId, CancellationToken cancellationToken)
        {
            DeletedByCorridor = true;
            Items.RemoveAll(item => item.CorridorId == corridorId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public bool SaveChangesCalled { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }
}
