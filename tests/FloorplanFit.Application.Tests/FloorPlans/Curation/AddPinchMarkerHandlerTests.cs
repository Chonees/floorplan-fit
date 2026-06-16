using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class AddPinchMarkerHandlerTests
{
    [Fact]
    public async Task HandleAsync_creates_a_pinch_marker_bound_to_the_selected_candidate_and_group()
    {
        var curationId = Guid.NewGuid();
        var group = new PinchGroup(
            Guid.NewGuid(),
            curationId,
            "Patio",
            PinchAxisTag.Height,
            1);
        var candidate = new ExtractedWallCandidate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "LINE:1",
            "WALLS",
            Guid.NewGuid(),
            101.6m,
            0.95m,
            null,
            ExtractedWallCandidateStatus.Accepted,
            1);

        var candidateRepository = new InMemoryExtractedWallCandidateRepository(candidate);
        var groupRepository = new InMemoryPinchGroupRepository([group]);
        var markerRepository = new InMemoryPinchMarkerRepository([]);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new AddPinchMarkerHandler(candidateRepository, groupRepository, markerRepository, unitOfWork);

        await handler.HandleAsync(
            curationId,
            candidate.Id,
            group.Id,
            0.5m,
            120m,
            CancellationToken.None);

        var pinch = Assert.Single(markerRepository.Items);
        Assert.Equal(candidate.Id, pinch.SourceCandidateId);
        Assert.Equal(group.Id, pinch.PinchGroupId);
        Assert.Equal(candidate.GeometryPathId, pinch.GeometryPathId);
        Assert.Equal(0.5m, pinch.PositionRatio);
        Assert.Equal(120m, pinch.MaxTrimMm);
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

        public Task AddAsync(ExtractedWallCandidate domainCandidate, DetectedWallCandidate detectedCandidate, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<ExtractedWallCandidate?> GetByIdAsync(Guid candidateId, CancellationToken cancellationToken)
            => Task.FromResult(candidate.Id == candidateId ? candidate : null);

        public Task<int> GetNextSortOrderAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task UpdateAsync(ExtractedWallCandidate candidate, CancellationToken cancellationToken)
            => Task.CompletedTask;
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

    private sealed class InMemoryPinchMarkerRepository : IPinchMarkerRepository
    {
        public InMemoryPinchMarkerRepository(IReadOnlyList<PinchMarker> seed)
        {
            Items = [.. seed];
        }

        public List<PinchMarker> Items { get; }

        public Task AddAsync(PinchMarker marker, CancellationToken cancellationToken)
        {
            Items.Add(marker);
            return Task.CompletedTask;
        }

        public Task<PinchMarker?> GetByIdAsync(Guid pinchMarkerId, CancellationToken cancellationToken)
            => Task.FromResult(Items.SingleOrDefault(item => item.Id == pinchMarkerId));

        public Task<IReadOnlyList<PinchMarker>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<PinchMarker>>(Items.Where(item => item.FloorPlanCurationId == curationId).ToArray());

        public Task RemoveAsync(Guid pinchMarkerId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.Id == pinchMarkerId);
            return Task.CompletedTask;
        }

        public Task RemoveByGroupAsync(Guid curationId, Guid pinchGroupId, CancellationToken cancellationToken)
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
