using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Extraction;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Extraction;

public sealed class ExtractWallCandidatesHandlerTests
{
    [Fact]
    public async Task HandleAsync_persists_pending_candidates_and_returns_extracted_status()
    {
        var versionId = Guid.NewGuid();
        var extractor = new FakeWallExtractor(
        [
            new DetectedWallCandidate(
                "LINE:12",
                "A-WALL",
                [new GeometryPoint(0, 0), new GeometryPoint(1200, 0)],
                ThicknessMm: 101.6m,
                Confidence: 0.95m,
                DetectionNotes: null)
        ]);
        var runs = new InMemoryWallExtractionRunRepository();
        var candidates = new InMemoryExtractedWallCandidateRepository();
        var unitOfWork = new FakeUnitOfWork();

        var handler = new ExtractWallCandidatesHandler(
            extractor,
            runs,
            candidates,
            unitOfWork,
            new FakeClock(new DateTime(2026, 4, 30, 20, 0, 0, DateTimeKind.Utc)));

        var result = await handler.HandleAsync(versionId, @"C:\managed\SANTA-BARBARA.dxf", CancellationToken.None);

        Assert.Equal("Extracted", result.Status);
        var run = Assert.Single(runs.Items);
        var candidate = Assert.Single(candidates.Items);
        Assert.Equal(versionId, run.FloorPlanVersionId);
        Assert.Equal(run.Id, candidate.WallExtractionRunId);
        Assert.Equal(ExtractedWallCandidateStatus.Pending, candidate.Status);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    private sealed class FakeWallExtractor : IWallExtractor
    {
        private readonly IReadOnlyList<DetectedWallCandidate> candidates;

        public FakeWallExtractor(IReadOnlyList<DetectedWallCandidate> candidates)
        {
            this.candidates = candidates;
        }

        public Task<IReadOnlyList<DetectedWallCandidate>> ExtractAsync(string managedFilePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(candidates);
        }
    }

    private sealed class InMemoryWallExtractionRunRepository : IWallExtractionRunRepository
    {
        public List<WallExtractionRun> Items { get; } = [];

        public Task AddAsync(WallExtractionRun run, CancellationToken cancellationToken)
        {
            Items.Add(run);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryExtractedWallCandidateRepository : IExtractedWallCandidateRepository
    {
        public List<ExtractedWallCandidate> Items { get; } = [];

        public Task AddRangeAsync(
            IReadOnlyList<ExtractedWallCandidate> domainCandidates,
            IReadOnlyList<DetectedWallCandidate> detectedCandidates,
            CancellationToken cancellationToken)
        {
            Items.AddRange(domainCandidates);
            return Task.CompletedTask;
        }

        public Task<ExtractedWallCandidate?> GetByIdAsync(Guid candidateId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.SingleOrDefault(item => item.Id == candidateId));
        }

        public Task UpdateAsync(ExtractedWallCandidate candidate, CancellationToken cancellationToken)
        {
            var index = Items.FindIndex(item => item.Id == candidate.Id);
            if (index >= 0)
            {
                Items[index] = candidate;
            }

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

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}

