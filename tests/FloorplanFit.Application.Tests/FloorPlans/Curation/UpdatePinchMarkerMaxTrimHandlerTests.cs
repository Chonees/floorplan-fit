using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class UpdatePinchMarkerMaxTrimHandlerTests
{
    [Fact]
    public async Task HandleAsync_updates_capacity_without_replacing_marker_identity()
    {
        var curation = CreateCuration(FloorPlanCurationStatus.Draft);
        var marker = CreateMarker(curation.Id);
        var markerRepository = new InMemoryPinchMarkerRepository(marker);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdatePinchMarkerMaxTrimHandler(
            new InMemoryFloorPlanCurationRepository(curation),
            markerRepository,
            unitOfWork);

        await handler.HandleAsync(curation.Id, marker.Id, 0.099218750m, CancellationToken.None);

        var updated = Assert.Single(markerRepository.Items);
        Assert.Same(marker, updated);
        Assert.Equal(marker.Id, updated.Id);
        Assert.Equal(0.099218750m, updated.MaxTrimMm);
        Assert.True(markerRepository.UpdateCalled);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    [Fact]
    public async Task HandleAsync_rejects_non_draft_curation()
    {
        var curation = CreateCuration(FloorPlanCurationStatus.Published);
        var marker = CreateMarker(curation.Id);
        var handler = CreateHandler(curation, marker);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(curation.Id, marker.Id, 25.4m, CancellationToken.None));

        Assert.Equal("Only draft curations can update pinch markers.", exception.Message);
    }

    [Fact]
    public async Task HandleAsync_rejects_marker_from_another_curation()
    {
        var curation = CreateCuration(FloorPlanCurationStatus.Draft);
        var marker = CreateMarker(Guid.NewGuid());
        var handler = CreateHandler(curation, marker);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(curation.Id, marker.Id, 25.4m, CancellationToken.None));

        Assert.Equal("Pinch marker does not belong to the active curation.", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task HandleAsync_rejects_nonpositive_capacity(double maxTrimMm)
    {
        var curation = CreateCuration(FloorPlanCurationStatus.Draft);
        var marker = CreateMarker(curation.Id);
        var handler = CreateHandler(curation, marker);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            handler.HandleAsync(curation.Id, marker.Id, (decimal)maxTrimMm, CancellationToken.None));
    }

    private static UpdatePinchMarkerMaxTrimHandler CreateHandler(FloorPlanCuration curation, PinchMarker marker)
        => new(
            new InMemoryFloorPlanCurationRepository(curation),
            new InMemoryPinchMarkerRepository(marker),
            new FakeUnitOfWork());

    private static FloorPlanCuration CreateCuration(FloorPlanCurationStatus status)
        => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            status,
            null,
            null,
            DateTime.UtcNow,
            status == FloorPlanCurationStatus.Published ? DateTime.UtcNow : null);

    private static PinchMarker CreateMarker(Guid curationId)
        => new(
            Guid.NewGuid(),
            curationId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            0.5m,
            25.4m,
            1);

    private sealed class InMemoryFloorPlanCurationRepository : IFloorPlanCurationRepository
    {
        private readonly FloorPlanCuration curation;

        public InMemoryFloorPlanCurationRepository(FloorPlanCuration curation)
        {
            this.curation = curation;
        }

        public Task<FloorPlanCuration?> GetByIdAsync(Guid curationId, CancellationToken cancellationToken)
            => Task.FromResult(curation.Id == curationId ? curation : null);

        public Task<FloorPlanCuration?> GetDraftAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<FloorPlanCuration?> GetPublishedAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<int> GetNextCurationVersionAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task AddAsync(FloorPlanCuration curation, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task UpdateAsync(FloorPlanCuration curation, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class InMemoryPinchMarkerRepository : IPinchMarkerRepository
    {
        public InMemoryPinchMarkerRepository(PinchMarker marker)
        {
            Items = [marker];
        }

        public List<PinchMarker> Items { get; }

        public bool UpdateCalled { get; private set; }

        public Task AddAsync(PinchMarker marker, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task UpdateAsync(PinchMarker marker, CancellationToken cancellationToken)
        {
            UpdateCalled = true;
            return Task.CompletedTask;
        }

        public Task<PinchMarker?> GetByIdAsync(Guid pinchMarkerId, CancellationToken cancellationToken)
            => Task.FromResult(Items.SingleOrDefault(item => item.Id == pinchMarkerId));

        public Task<IReadOnlyList<PinchMarker>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task RemoveAsync(Guid pinchMarkerId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task RemoveByGroupAsync(Guid curationId, Guid pinchGroupId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task RemoveBySourceCandidateAsync(Guid curationId, Guid sourceCandidateId, CancellationToken cancellationToken)
            => throw new NotSupportedException();
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
