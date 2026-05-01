using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Review;

public sealed class OpenFloorPlanReviewSessionHandlerTests
{
    [Fact]
    public async Task HandleAsync_creates_or_resumes_a_draft_and_returns_the_review_session()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var existingTemplate = new FloorPlanTemplate(templateId, "santa-barbara", "SANTA-BARBARA", isActive: true);
        existingTemplate.SetCurrentVersion(versionId);

        var templateRepository = new InMemoryFloorPlanTemplateRepository(existingTemplate);
        var curationRepository = new InMemoryFloorPlanCurationRepository();
        var unitOfWork = new FakeUnitOfWork();
        var clock = new FakeClock(new DateTime(2026, 4, 30, 18, 0, 0, DateTimeKind.Utc));
        var startOrResumeHandler = new StartOrResumeCurationHandler(curationRepository, clock, unitOfWork);
        var expectedSession = new FloorPlanReviewSessionDto(
            templateId,
            "santa-barbara",
            "SANTA-BARBARA",
            "Curated Draft",
            ActiveVersionNumber: 1,
            ActivePublishedCurationId: null,
            GeometryPaths: [],
            WallCandidates: [],
            CuratedWalls: []);
        var reviewReader = new FakeFloorPlanReviewSessionReader(expectedSession);
        var handler = new OpenFloorPlanReviewSessionHandler(templateRepository, reviewReader, startOrResumeHandler);

        var response = await handler.HandleAsync(templateId, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(expectedSession, response.Session);
        Assert.NotEqual(Guid.Empty, response.DraftCurationId);
        Assert.Equal(templateId, reviewReader.LastTemplateId);
        Assert.True(unitOfWork.SaveChangesCalled);

        var storedDraft = await curationRepository.GetDraftAsync(versionId, CancellationToken.None);
        Assert.NotNull(storedDraft);
        Assert.Equal(response.DraftCurationId, storedDraft!.Id);
    }

    [Fact]
    public async Task HandleAsync_throws_when_the_template_has_no_current_version()
    {
        var template = new FloorPlanTemplate(Guid.NewGuid(), "santa-barbara", "SANTA-BARBARA", isActive: true);
        var templateRepository = new InMemoryFloorPlanTemplateRepository(template);
        var reviewReader = new FakeFloorPlanReviewSessionReader(null);
        var curationRepository = new InMemoryFloorPlanCurationRepository();
        var unitOfWork = new FakeUnitOfWork();
        var startOrResumeHandler = new StartOrResumeCurationHandler(
            curationRepository,
            new FakeClock(new DateTime(2026, 4, 30, 18, 0, 0, DateTimeKind.Utc)),
            unitOfWork);
        var handler = new OpenFloorPlanReviewSessionHandler(templateRepository, reviewReader, startOrResumeHandler);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(template.Id, CancellationToken.None));

        Assert.Equal("Floor plan template does not have an active version.", exception.Message);
    }

    private sealed class FakeFloorPlanReviewSessionReader : IFloorPlanReviewSessionReader
    {
        private readonly FloorPlanReviewSessionDto? session;

        public FakeFloorPlanReviewSessionReader(FloorPlanReviewSessionDto? session)
        {
            this.session = session;
        }

        public Guid? LastTemplateId { get; private set; }

        public Task<FloorPlanReviewSessionDto?> GetByTemplateAsync(Guid templateId, CancellationToken cancellationToken)
        {
            LastTemplateId = templateId;
            return Task.FromResult(session);
        }
    }

    private sealed class InMemoryFloorPlanTemplateRepository : IFloorPlanTemplateRepository
    {
        private readonly List<FloorPlanTemplate> items;

        public InMemoryFloorPlanTemplateRepository(params FloorPlanTemplate[] items)
        {
            this.items = items.ToList();
        }

        public Task<FloorPlanTemplate?> GetByCodeAsync(string code, CancellationToken cancellationToken)
        {
            return Task.FromResult(items.SingleOrDefault(item => item.Code == code));
        }

        public Task<FloorPlanTemplate?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken)
        {
            return Task.FromResult(items.SingleOrDefault(item => item.Id == templateId));
        }

        public Task AddAsync(FloorPlanTemplate template, CancellationToken cancellationToken)
        {
            items.Add(template);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(FloorPlanTemplate template, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryFloorPlanCurationRepository : IFloorPlanCurationRepository
    {
        private readonly List<FloorPlanCuration> items = [];

        public Task<FloorPlanCuration?> GetDraftAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
        {
            return Task.FromResult(items
                .Where(item => item.FloorPlanVersionId == floorPlanVersionId && item.Status == FloorPlanCurationStatus.Draft)
                .OrderByDescending(item => item.CurationVersion)
                .FirstOrDefault());
        }

        public Task<FloorPlanCuration?> GetPublishedAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
        {
            return Task.FromResult(items
                .Where(item => item.FloorPlanVersionId == floorPlanVersionId && item.Status == FloorPlanCurationStatus.Published)
                .OrderByDescending(item => item.CurationVersion)
                .FirstOrDefault());
        }

        public Task<int> GetNextCurationVersionAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
        {
            var nextVersion = items
                .Where(item => item.FloorPlanVersionId == floorPlanVersionId)
                .Select(item => item.CurationVersion)
                .DefaultIfEmpty(0)
                .Max() + 1;

            return Task.FromResult(nextVersion);
        }

        public Task AddAsync(FloorPlanCuration curation, CancellationToken cancellationToken)
        {
            items.Add(curation);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(FloorPlanCuration curation, CancellationToken cancellationToken)
        {
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
