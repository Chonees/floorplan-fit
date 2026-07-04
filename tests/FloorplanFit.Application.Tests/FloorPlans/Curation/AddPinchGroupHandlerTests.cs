using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class AddPinchGroupHandlerTests
{
    [Fact]
    public async Task HandleAsync_creates_a_named_group_for_the_selected_axis()
    {
        var curationId = Guid.NewGuid();
        var repository = new InMemoryPinchGroupRepository([]);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new AddPinchGroupHandler(repository, unitOfWork);

        var groupId = await handler.HandleAsync(curationId, "Patio", PinchAxisTag.Width, CancellationToken.None);

        var group = Assert.Single(repository.Items);
        Assert.Equal(groupId, group.Id);
        Assert.Equal(curationId, group.FloorPlanCurationId);
        Assert.Equal("Patio", group.Name);
        Assert.Equal(PinchAxisTag.Width, group.AxisTag);
        Assert.Equal(1, group.SortOrder);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    private sealed class InMemoryPinchGroupRepository : IPinchGroupRepository
    {
        public InMemoryPinchGroupRepository(IReadOnlyList<PinchGroup> seed)
        {
            Items = [.. seed];
        }

        public List<PinchGroup> Items { get; }

        public Task AddAsync(PinchGroup group, CancellationToken cancellationToken)
        {
            Items.Add(group);
            return Task.CompletedTask;
        }

        public Task<PinchGroup?> GetByIdAsync(Guid pinchGroupId, CancellationToken cancellationToken)
            => Task.FromResult(Items.SingleOrDefault(item => item.Id == pinchGroupId));

        public Task<IReadOnlyList<PinchGroup>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<PinchGroup>>(Items.Where(item => item.FloorPlanCurationId == curationId).ToArray());

        public Task RemoveAsync(Guid pinchGroupId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task UpdateAsync(PinchGroup group, CancellationToken cancellationToken)
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
