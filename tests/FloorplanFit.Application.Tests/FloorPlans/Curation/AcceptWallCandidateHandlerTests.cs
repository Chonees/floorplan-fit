using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class AcceptWallCandidateHandlerTests
{
    [Fact]
    public async Task HandleAsync_accepts_a_pending_candidate_and_creates_a_curated_wall()
    {
        var curation = new FloorPlanCuration(Guid.NewGuid(), Guid.NewGuid(), 1, FloorPlanCurationStatus.Draft, null, null, DateTime.UtcNow, null);
        var candidate = new ExtractedWallCandidate(Guid.NewGuid(), Guid.NewGuid(), "LINE:12", "A-WALL", Guid.NewGuid(), 101.6m, 0.95m, null, ExtractedWallCandidateStatus.Pending, 1);
        var candidateRepository = new InMemoryExtractedWallCandidateRepository(candidate);
        var wallRepository = new InMemoryCuratedWallRepository([]);
        var unitOfWork = new FakeUnitOfWork();

        var handler = new AcceptWallCandidateHandler(candidateRepository, wallRepository, unitOfWork);

        await handler.HandleAsync(curation.Id, candidate.Id, "W-001", CancellationToken.None);

        Assert.Equal(ExtractedWallCandidateStatus.Accepted, candidate.Status);
        var wall = Assert.Single(wallRepository.Items);
        Assert.Equal("W-001", wall.StableWallId);
        Assert.Equal(candidate.Id, wall.SourceCandidateId);
        Assert.Equal(candidate.SortOrder, wall.SortOrder);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    private sealed class InMemoryExtractedWallCandidateRepository : IExtractedWallCandidateRepository
    {
        private readonly ExtractedWallCandidate candidate;

        public InMemoryExtractedWallCandidateRepository(ExtractedWallCandidate candidate)
        {
            this.candidate = candidate;
        }

        public Task AddRangeAsync(IReadOnlyList<ExtractedWallCandidate> domainCandidates, IReadOnlyList<DetectedWallCandidate> detectedCandidates, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<ExtractedWallCandidate?> GetByIdAsync(Guid candidateId, CancellationToken cancellationToken)
            => Task.FromResult(candidate.Id == candidateId ? candidate : null);

        public Task UpdateAsync(ExtractedWallCandidate candidate, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class InMemoryCuratedWallRepository : ICuratedWallRepository
    {
        public InMemoryCuratedWallRepository(IReadOnlyList<CuratedWall> seed)
        {
            Items = [.. seed];
        }

        public List<CuratedWall> Items { get; }

        public Task AddAsync(CuratedWall wall, CancellationToken cancellationToken)
        {
            Items.Add(wall);
            return Task.CompletedTask;
        }

        public Task<CuratedWall?> GetByIdAsync(Guid curatedWallId, CancellationToken cancellationToken)
            => Task.FromResult(Items.SingleOrDefault(item => item.Id == curatedWallId));

        public Task<IReadOnlyList<CuratedWall>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<CuratedWall>>(Items.Where(item => item.FloorPlanCurationId == curationId).ToArray());

        public Task RemoveBySourceCandidateAsync(Guid curationId, Guid sourceCandidateId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task UpdateAsync(CuratedWall wall, CancellationToken cancellationToken)
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
}
