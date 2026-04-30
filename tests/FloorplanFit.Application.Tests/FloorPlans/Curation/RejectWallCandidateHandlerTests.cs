using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class RejectWallCandidateHandlerTests
{
    [Fact]
    public async Task HandleAsync_rejects_a_pending_candidate_and_removes_its_curated_wall_from_the_draft()
    {
        var candidate = new ExtractedWallCandidate(Guid.NewGuid(), Guid.NewGuid(), "LINE:12", "A-WALL", Guid.NewGuid(), 101.6m, 0.95m, null, ExtractedWallCandidateStatus.Pending, 1);
        var wall = new CuratedWall(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "W-001",
            candidate.Id,
            candidate.SourceEntityRef,
            candidate.GeometryPathId,
            WallRole.Partition,
            WallMobilityLevel.Flexible,
            WallProtectionLevel.None,
            101.6m,
            "2x4",
            null,
            isExterior: false,
            isStructuralHint: false,
            wallGroupId: null,
            sortOrder: 1,
            notes: null);
        var candidateRepository = new InMemoryExtractedWallCandidateRepository(candidate);
        var wallRepository = new InMemoryCuratedWallRepository([wall]);
        var unitOfWork = new FakeUnitOfWork();

        var handler = new RejectWallCandidateHandler(candidateRepository, wallRepository, unitOfWork);

        await handler.HandleAsync(wall.FloorPlanCurationId, candidate.Id, CancellationToken.None);

        Assert.Equal(ExtractedWallCandidateStatus.Rejected, candidate.Status);
        Assert.Empty(wallRepository.Items);
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
            => throw new NotSupportedException();

        public Task<CuratedWall?> GetByIdAsync(Guid curatedWallId, CancellationToken cancellationToken)
            => Task.FromResult(Items.SingleOrDefault(item => item.Id == curatedWallId));

        public Task<IReadOnlyList<CuratedWall>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<CuratedWall>>(Items.Where(item => item.FloorPlanCurationId == curationId).ToArray());

        public Task RemoveBySourceCandidateAsync(Guid curationId, Guid sourceCandidateId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.FloorPlanCurationId == curationId && item.SourceCandidateId == sourceCandidateId);
            return Task.CompletedTask;
        }

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
