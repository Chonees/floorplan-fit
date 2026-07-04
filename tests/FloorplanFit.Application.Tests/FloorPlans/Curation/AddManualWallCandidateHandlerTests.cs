using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Domain.FloorPlans;
using Xunit;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class AddManualWallCandidateHandlerTests
{
    [Fact]
    public async Task HandleAsync_adds_an_accepted_manual_wall_candidate_to_the_latest_extraction_run()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var extractionRunId = Guid.NewGuid();
        var templateRepository = new InMemoryFloorPlanTemplateRepository(new FloorPlanTemplate(templateId, "plan", "Plan", isActive: true));
        templateRepository.Template.SetCurrentVersion(versionId);
        var runRepository = new InMemoryWallExtractionRunRepository(extractionRunId);
        var candidateRepository = new InMemoryExtractedWallCandidateRepository(nextSortOrder: 7);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new AddManualWallCandidateHandler(
            templateRepository,
            runRepository,
            candidateRepository,
            unitOfWork);

        var candidateId = await handler.HandleAsync(
            templateId,
            floorPlanVersionId: null,
            startX: 10m,
            startY: 20m,
            endX: 110m,
            endY: 20m,
            CancellationToken.None);

        Assert.Equal(candidateId, candidateRepository.AddedCandidate?.Id);
        Assert.Equal(extractionRunId, candidateRepository.AddedCandidate?.WallExtractionRunId);
        Assert.Equal("MANUAL-WALLS", candidateRepository.AddedCandidate?.SourceLayer);
        Assert.StartsWith("MANUAL-WALL:", candidateRepository.AddedCandidate?.SourceEntityRef, StringComparison.Ordinal);
        Assert.Equal(ExtractedWallCandidateStatus.Accepted, candidateRepository.AddedCandidate?.Status);
        Assert.Equal(1m, candidateRepository.AddedCandidate?.Confidence);
        Assert.Equal(7, candidateRepository.AddedCandidate?.SortOrder);
        Assert.Equal(candidateRepository.AddedCandidate?.GeometryPathId, candidateRepository.AddedGeometryPathId);
        Assert.Equal(
            [new GeometryPoint(10m, 20m), new GeometryPoint(110m, 20m)],
            candidateRepository.AddedDetectedCandidate?.Points);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    [Fact]
    public async Task HandleAsync_rejects_zero_length_manual_wall_lines()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var templateRepository = new InMemoryFloorPlanTemplateRepository(new FloorPlanTemplate(templateId, "plan", "Plan", isActive: true));
        templateRepository.Template.SetCurrentVersion(versionId);
        var handler = new AddManualWallCandidateHandler(
            templateRepository,
            new InMemoryWallExtractionRunRepository(Guid.NewGuid()),
            new InMemoryExtractedWallCandidateRepository(nextSortOrder: 1),
            new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(
            templateId,
            floorPlanVersionId: null,
            startX: 10m,
            startY: 20m,
            endX: 10m,
            endY: 20m,
            CancellationToken.None));

        Assert.Equal("Manual wall line must have a measurable length.", exception.Message);
    }

    private sealed class InMemoryFloorPlanTemplateRepository : IFloorPlanTemplateRepository
    {
        public InMemoryFloorPlanTemplateRepository(FloorPlanTemplate template)
        {
            Template = template;
        }

        public FloorPlanTemplate Template { get; }

        public Task AddAsync(FloorPlanTemplate template, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<FloorPlanTemplate?> GetByCodeAsync(string code, CancellationToken cancellationToken)
            => Task.FromResult<FloorPlanTemplate?>(Template.Code == code ? Template : null);

        public Task<FloorPlanTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult<FloorPlanTemplate?>(Template.Id == id ? Template : null);

        public Task UpdateAsync(FloorPlanTemplate template, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class InMemoryWallExtractionRunRepository : IWallExtractionRunRepository
    {
        private readonly Guid extractionRunId;

        public InMemoryWallExtractionRunRepository(Guid extractionRunId)
        {
            this.extractionRunId = extractionRunId;
        }

        public Task AddAsync(WallExtractionRun run, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<WallExtractionRun?> GetLatestByVersionAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
            => Task.FromResult<WallExtractionRun?>(new WallExtractionRun(
                extractionRunId,
                floorPlanVersionId,
                "Completed",
                DateTime.UtcNow,
                DateTime.UtcNow,
                "test",
                null));
    }

    private sealed class InMemoryExtractedWallCandidateRepository : IExtractedWallCandidateRepository
    {
        private readonly int nextSortOrder;

        public InMemoryExtractedWallCandidateRepository(int nextSortOrder)
        {
            this.nextSortOrder = nextSortOrder;
        }

        public ExtractedWallCandidate? AddedCandidate { get; private set; }

        public DetectedWallCandidate? AddedDetectedCandidate { get; private set; }

        public Guid? AddedGeometryPathId => AddedCandidate?.GeometryPathId;

        public Task AddAsync(
            ExtractedWallCandidate domainCandidate,
            DetectedWallCandidate detectedCandidate,
            CancellationToken cancellationToken)
        {
            AddedCandidate = domainCandidate;
            AddedDetectedCandidate = detectedCandidate;
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(
            IReadOnlyList<ExtractedWallCandidate> domainCandidates,
            IReadOnlyList<DetectedWallCandidate> detectedCandidates,
            CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<ExtractedWallCandidate?> GetByIdAsync(Guid candidateId, CancellationToken cancellationToken)
            => Task.FromResult<ExtractedWallCandidate?>(null);

        public Task<int> GetNextSortOrderAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
            => Task.FromResult(nextSortOrder);

        public Task UpdateAsync(ExtractedWallCandidate candidate, CancellationToken cancellationToken)
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
