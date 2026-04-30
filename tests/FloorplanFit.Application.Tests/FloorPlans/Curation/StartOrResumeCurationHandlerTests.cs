using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class StartOrResumeCurationHandlerTests
{
    [Fact]
    public async Task HandleAsync_creates_a_new_draft_based_on_the_latest_published_curation()
    {
        var floorPlanVersionId = Guid.NewGuid();
        var published = new FloorPlanCuration(
            Guid.NewGuid(),
            floorPlanVersionId,
            curationVersion: 2,
            FloorPlanCurationStatus.Published,
            basedOnCurationId: null,
            notes: "published",
            createdAtUtc: new DateTime(2026, 4, 30, 18, 0, 0, DateTimeKind.Utc),
            publishedAtUtc: new DateTime(2026, 4, 30, 19, 0, 0, DateTimeKind.Utc));
        var repository = new InMemoryFloorPlanCurationRepository(draft: null, published, nextVersion: 3);
        var unitOfWork = new FakeUnitOfWork();
        var now = new DateTime(2026, 4, 30, 21, 0, 0, DateTimeKind.Utc);

        var handler = new StartOrResumeCurationHandler(repository, new FakeClock(now), unitOfWork);

        var draft = await handler.HandleAsync(floorPlanVersionId, CancellationToken.None);

        Assert.Equal(FloorPlanCurationStatus.Draft, draft.Status);
        Assert.Equal(3, draft.CurationVersion);
        Assert.Equal(published.Id, draft.BasedOnCurationId);
        Assert.Equal(now, draft.CreatedAtUtc);
        Assert.Same(draft, repository.AddedItem);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    [Fact]
    public async Task HandleAsync_returns_the_existing_draft_without_creating_a_new_one()
    {
        var floorPlanVersionId = Guid.NewGuid();
        var existingDraft = new FloorPlanCuration(
            Guid.NewGuid(),
            floorPlanVersionId,
            curationVersion: 4,
            FloorPlanCurationStatus.Draft,
            basedOnCurationId: Guid.NewGuid(),
            notes: "draft",
            createdAtUtc: new DateTime(2026, 4, 30, 20, 0, 0, DateTimeKind.Utc),
            publishedAtUtc: null);
        var repository = new InMemoryFloorPlanCurationRepository(existingDraft, published: null, nextVersion: 5);
        var unitOfWork = new FakeUnitOfWork();

        var handler = new StartOrResumeCurationHandler(repository, new FakeClock(DateTime.UtcNow), unitOfWork);

        var result = await handler.HandleAsync(floorPlanVersionId, CancellationToken.None);

        Assert.Same(existingDraft, result);
        Assert.Null(repository.AddedItem);
        Assert.False(unitOfWork.SaveChangesCalled);
    }

    private sealed class InMemoryFloorPlanCurationRepository : IFloorPlanCurationRepository
    {
        private readonly FloorPlanCuration? draft;
        private readonly FloorPlanCuration? published;
        private readonly int nextVersion;

        public InMemoryFloorPlanCurationRepository(FloorPlanCuration? draft, FloorPlanCuration? published, int nextVersion)
        {
            this.draft = draft;
            this.published = published;
            this.nextVersion = nextVersion;
        }

        public FloorPlanCuration? AddedItem { get; private set; }

        public Task<FloorPlanCuration?> GetDraftAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
            => Task.FromResult(draft?.FloorPlanVersionId == floorPlanVersionId ? draft : null);

        public Task<FloorPlanCuration?> GetPublishedAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
            => Task.FromResult(published?.FloorPlanVersionId == floorPlanVersionId ? published : null);

        public Task<int> GetNextCurationVersionAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
            => Task.FromResult(nextVersion);

        public Task AddAsync(FloorPlanCuration curation, CancellationToken cancellationToken)
        {
            AddedItem = curation;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(FloorPlanCuration curation, CancellationToken cancellationToken)
            => Task.CompletedTask;
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

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
