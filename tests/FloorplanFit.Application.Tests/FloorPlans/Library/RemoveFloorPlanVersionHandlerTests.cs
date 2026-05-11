using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Library;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Library;

public sealed class RemoveFloorPlanVersionHandlerTests
{
    [Fact]
    public async Task HandleAsync_removes_the_selected_version_and_saves_changes()
    {
        var version = new FloorPlanVersion(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "fingerprint",
            2,
            new DateTime(2026, 5, 9, 12, 0, 0, DateTimeKind.Utc));
        var repository = new InMemoryFloorPlanVersionRepository([version]);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RemoveFloorPlanVersionHandler(repository, unitOfWork);

        await handler.HandleAsync(version.Id, CancellationToken.None);

        Assert.Empty(repository.Items);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    [Fact]
    public async Task HandleAsync_throws_when_the_version_does_not_exist()
    {
        var handler = new RemoveFloorPlanVersionHandler(
            new InMemoryFloorPlanVersionRepository([]),
            new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal("Floor plan version was not found.", exception.Message);
    }

    private sealed class InMemoryFloorPlanVersionRepository : IFloorPlanVersionRepository
    {
        public InMemoryFloorPlanVersionRepository(IReadOnlyList<FloorPlanVersion> seed)
        {
            Items = [.. seed];
        }

        public List<FloorPlanVersion> Items { get; }

        public Task<FloorPlanVersion?> GetByIdAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.SingleOrDefault(item => item.Id == floorPlanVersionId));
        }

        public Task<int> GetNextVersionNumberAsync(Guid floorPlanTemplateId, CancellationToken cancellationToken)
        {
            var nextVersion = Items
                .Where(item => item.FloorPlanTemplateId == floorPlanTemplateId)
                .Select(item => item.VersionNumber)
                .DefaultIfEmpty(0)
                .Max() + 1;

            return Task.FromResult(nextVersion);
        }

        public Task AddAsync(FloorPlanVersion version, CancellationToken cancellationToken)
        {
            Items.Add(version);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.Id == floorPlanVersionId);
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
