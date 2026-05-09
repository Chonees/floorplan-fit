using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class RejectWallCandidateHandlerTests
{
    [Fact]
    public async Task HandleAsync_rejects_a_pending_candidate_and_removes_its_pinch_markers_from_the_draft()
    {
        var curationId = Guid.NewGuid();
        var candidate = new ExtractedWallCandidate(Guid.NewGuid(), Guid.NewGuid(), "LINE:12", "A-WALL", Guid.NewGuid(), 101.6m, 0.95m, null, ExtractedWallCandidateStatus.Pending, 1);
        var marker = new PinchMarker(
            Guid.NewGuid(),
            curationId,
            Guid.NewGuid(),
            candidate.Id,
            candidate.GeometryPathId ?? Guid.NewGuid(),
            0.5m,
            120m,
            1);
        var candidateRepository = new InMemoryExtractedWallCandidateRepository(candidate);
        var markerRepository = new InMemoryPinchMarkerRepository([marker]);
        var unitOfWork = new FakeUnitOfWork();

        var handler = new RejectWallCandidateHandler(candidateRepository, markerRepository, unitOfWork);

        await handler.HandleAsync(curationId, candidate.Id, CancellationToken.None);

        Assert.Equal(ExtractedWallCandidateStatus.Rejected, candidate.Status);
        Assert.Empty(markerRepository.Items);
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

        public Task RemoveBySourceCandidateAsync(Guid curationId, Guid sourceCandidateId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.FloorPlanCurationId == curationId && item.SourceCandidateId == sourceCandidateId);
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
