using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class RemovePinchGroupHandlerTests
{
    [Fact]
    public async Task HandleAsync_removes_the_group_and_all_its_markers_from_the_draft()
    {
        var curationId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var otherGroupId = Guid.NewGuid();
        var curation = new FloorPlanCuration(
            curationId,
            Guid.NewGuid(),
            1,
            FloorPlanCurationStatus.Draft,
            null,
            null,
            DateTime.UtcNow,
            null);
        var group = new PinchGroup(groupId, curationId, "Patio", PinchAxisTag.Width, 1);
        var otherGroup = new PinchGroup(otherGroupId, curationId, "Hall", PinchAxisTag.Width, 2);
        var marker = CreateMarker(curationId, groupId, sortOrder: 1);
        var secondMarker = CreateMarker(curationId, groupId, sortOrder: 2);
        var otherMarker = CreateMarker(curationId, otherGroupId, sortOrder: 3);
        var curationRepository = new InMemoryFloorPlanCurationRepository(curation);
        var groupRepository = new InMemoryPinchGroupRepository([group, otherGroup]);
        var markerRepository = new InMemoryPinchMarkerRepository([marker, secondMarker, otherMarker]);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RemovePinchGroupHandler(
            curationRepository,
            groupRepository,
            markerRepository,
            unitOfWork);

        await handler.HandleAsync(curationId, groupId, CancellationToken.None);

        Assert.DoesNotContain(groupRepository.Items, item => item.Id == groupId);
        Assert.Contains(groupRepository.Items, item => item.Id == otherGroupId);
        Assert.DoesNotContain(markerRepository.Items, item => item.PinchGroupId == groupId);
        Assert.Contains(markerRepository.Items, item => item.Id == otherMarker.Id);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    private static PinchMarker CreateMarker(Guid curationId, Guid groupId, int sortOrder)
    {
        return new PinchMarker(
            Guid.NewGuid(),
            curationId,
            groupId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            0.5m,
            120m,
            sortOrder);
    }

    private sealed class InMemoryFloorPlanCurationRepository : IFloorPlanCurationRepository
    {
        private readonly IReadOnlyList<FloorPlanCuration> items;

        public InMemoryFloorPlanCurationRepository(params FloorPlanCuration[] items)
        {
            this.items = items;
        }

        public Task AddAsync(FloorPlanCuration curation, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<FloorPlanCuration?> GetByIdAsync(Guid curationId, CancellationToken cancellationToken)
            => Task.FromResult(items.SingleOrDefault(item => item.Id == curationId));

        public Task<FloorPlanCuration?> GetDraftAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<int> GetNextCurationVersionAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<FloorPlanCuration?> GetPublishedAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task UpdateAsync(FloorPlanCuration curation, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class InMemoryPinchGroupRepository : IPinchGroupRepository
    {
        public InMemoryPinchGroupRepository(IReadOnlyList<PinchGroup> seed)
        {
            Items = [.. seed];
        }

        public List<PinchGroup> Items { get; }

        public Task AddAsync(PinchGroup group, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<PinchGroup?> GetByIdAsync(Guid pinchGroupId, CancellationToken cancellationToken)
            => Task.FromResult(Items.SingleOrDefault(item => item.Id == pinchGroupId));

        public Task<IReadOnlyList<PinchGroup>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<PinchGroup>>(Items.Where(item => item.FloorPlanCurationId == curationId).ToArray());

        public Task RemoveAsync(Guid pinchGroupId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.Id == pinchGroupId);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(PinchGroup group, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class InMemoryPinchMarkerRepository : IPinchMarkerRepository
    {
        public InMemoryPinchMarkerRepository(IReadOnlyList<PinchMarker> seed)
        {
            Items = [.. seed];
        }

        public List<PinchMarker> Items { get; }

        public Task AddAsync(PinchMarker marker, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<PinchMarker?> GetByIdAsync(Guid pinchMarkerId, CancellationToken cancellationToken)
            => Task.FromResult(Items.SingleOrDefault(item => item.Id == pinchMarkerId));

        public Task<IReadOnlyList<PinchMarker>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<PinchMarker>>(Items.Where(item => item.FloorPlanCurationId == curationId).ToArray());

        public Task RemoveAsync(Guid pinchMarkerId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task RemoveByGroupAsync(Guid curationId, Guid pinchGroupId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.FloorPlanCurationId == curationId && item.PinchGroupId == pinchGroupId);
            return Task.CompletedTask;
        }

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
