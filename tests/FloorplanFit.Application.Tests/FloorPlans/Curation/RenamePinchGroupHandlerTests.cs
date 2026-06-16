using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class RenamePinchGroupHandlerTests
{
    [Fact]
    public async Task HandleAsync_renames_the_group_without_changing_identity_axis_or_sort_order()
    {
        var curationId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var curation = new FloorPlanCuration(
            curationId,
            Guid.NewGuid(),
            1,
            FloorPlanCurationStatus.Draft,
            null,
            null,
            DateTime.UtcNow,
            null);
        var group = new PinchGroup(groupId, curationId, "Ajuste 1", PinchAxisTag.Height, 3);
        var curationRepository = new InMemoryFloorPlanCurationRepository(curation);
        var groupRepository = new InMemoryPinchGroupRepository([group]);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RenamePinchGroupHandler(curationRepository, groupRepository, unitOfWork);

        await handler.HandleAsync(curationId, groupId, "  Patio trasero  ", CancellationToken.None);

        var renamed = Assert.Single(groupRepository.Items);
        Assert.Equal(groupId, renamed.Id);
        Assert.Equal(curationId, renamed.FloorPlanCurationId);
        Assert.Equal("Patio trasero", renamed.Name);
        Assert.Equal(PinchAxisTag.Height, renamed.AxisTag);
        Assert.Equal(3, renamed.SortOrder);
        Assert.True(groupRepository.UpdateCalled);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    [Fact]
    public async Task HandleAsync_rejects_groups_from_other_curations()
    {
        var activeCurationId = Guid.NewGuid();
        var otherCurationId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var curation = new FloorPlanCuration(
            activeCurationId,
            Guid.NewGuid(),
            1,
            FloorPlanCurationStatus.Draft,
            null,
            null,
            DateTime.UtcNow,
            null);
        var group = new PinchGroup(groupId, otherCurationId, "Patio", PinchAxisTag.Width, 1);
        var handler = new RenamePinchGroupHandler(
            new InMemoryFloorPlanCurationRepository(curation),
            new InMemoryPinchGroupRepository([group]),
            new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(activeCurationId, groupId, "Porche", CancellationToken.None));

        Assert.Equal("Pinch group does not belong to the active curation.", exception.Message);
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

        public bool UpdateCalled { get; private set; }

        public Task AddAsync(PinchGroup group, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<PinchGroup?> GetByIdAsync(Guid pinchGroupId, CancellationToken cancellationToken)
            => Task.FromResult(Items.SingleOrDefault(item => item.Id == pinchGroupId));

        public Task<IReadOnlyList<PinchGroup>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<PinchGroup>>(Items.Where(item => item.FloorPlanCurationId == curationId).ToArray());

        public Task RemoveAsync(Guid pinchGroupId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task UpdateAsync(PinchGroup group, CancellationToken cancellationToken)
        {
            var index = Items.FindIndex(item => item.Id == group.Id);
            if (index >= 0)
            {
                Items[index] = group;
            }

            UpdateCalled = true;
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
