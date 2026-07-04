using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class EditPublishedFloorPlanCurationHandlerTests
{
    [Fact]
    public async Task HandleAsync_creates_an_editable_draft_from_the_active_published_curation_and_returns_its_session()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var publishedCurationId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        template.SetActivePublishedCuration(publishedCurationId);
        var published = new FloorPlanCuration(
            publishedCurationId,
            versionId,
            curationVersion: 1,
            FloorPlanCurationStatus.Published,
            basedOnCurationId: null,
            notes: "published",
            createdAtUtc: new DateTime(2026, 6, 3, 14, 0, 0, DateTimeKind.Utc),
            publishedAtUtc: new DateTime(2026, 6, 3, 15, 0, 0, DateTimeKind.Utc));
        var curationRepository = new InMemoryFloorPlanCurationRepository(published);
        var cloneService = new FakeFloorPlanCurationDataCloneService();
        var reviewReader = new FakeFloorPlanReviewSessionReader();
        var handler = new EditPublishedFloorPlanCurationHandler(
            new InMemoryFloorPlanTemplateRepository(template),
            curationRepository,
            cloneService,
            reviewReader,
            new FakeClock(new DateTime(2026, 6, 3, 16, 0, 0, DateTimeKind.Utc)),
            new FakeUnitOfWork());

        var response = await handler.HandleAsync(templateId, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.DraftCurationId);
        Assert.NotEqual(publishedCurationId, response.DraftCurationId);
        var draft = await curationRepository.GetDraftAsync(versionId, CancellationToken.None);
        Assert.NotNull(draft);
        Assert.Equal(response.DraftCurationId, draft!.Id);
        Assert.Equal(publishedCurationId, draft.BasedOnCurationId);
        Assert.Equal((publishedCurationId, response.DraftCurationId), cloneService.LastClone);
        Assert.Equal((templateId, versionId, response.DraftCurationId), reviewReader.LastCurationRead);
        Assert.Equal("Curated Draft", response.Session.Status);
    }

    private sealed class FakeFloorPlanCurationDataCloneService : IFloorPlanCurationDataCloneService
    {
        public (Guid SourceCurationId, Guid DestinationCurationId)? LastClone { get; private set; }

        public Task EnsureClonedAsync(Guid sourceCurationId, Guid destinationCurationId, CancellationToken cancellationToken)
        {
            LastClone = (sourceCurationId, destinationCurationId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeFloorPlanReviewSessionReader : IFloorPlanReviewSessionReader
    {
        public (Guid TemplateId, Guid VersionId, Guid CurationId)? LastCurationRead { get; private set; }

        public Task<FloorPlanReviewSessionDto?> GetByTemplateAsync(Guid templateId, CancellationToken cancellationToken)
            => Task.FromResult<FloorPlanReviewSessionDto?>(null);

        public Task<FloorPlanReviewSessionDto?> GetByVersionAsync(
            Guid templateId,
            Guid floorPlanVersionId,
            CancellationToken cancellationToken)
            => Task.FromResult<FloorPlanReviewSessionDto?>(null);

        public Task<FloorPlanReviewSessionDto?> GetByCurationAsync(
            Guid templateId,
            Guid floorPlanVersionId,
            Guid curationId,
            CancellationToken cancellationToken)
        {
            LastCurationRead = (templateId, floorPlanVersionId, curationId);
            return Task.FromResult<FloorPlanReviewSessionDto?>(new FloorPlanReviewSessionDto(
                templateId,
                "seminole2000",
                "SEMINOLE2000",
                "Curated Draft",
                1,
                Guid.NewGuid(),
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                []));
        }
    }

    private sealed class InMemoryFloorPlanTemplateRepository : IFloorPlanTemplateRepository
    {
        private readonly FloorPlanTemplate template;

        public InMemoryFloorPlanTemplateRepository(FloorPlanTemplate template)
        {
            this.template = template;
        }

        public Task<FloorPlanTemplate?> GetByCodeAsync(string code, CancellationToken cancellationToken)
            => Task.FromResult(template.Code == code ? template : null);

        public Task<FloorPlanTemplate?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken)
            => Task.FromResult(template.Id == templateId ? template : null);

        public Task AddAsync(FloorPlanTemplate template, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateAsync(FloorPlanTemplate template, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class InMemoryFloorPlanCurationRepository : IFloorPlanCurationRepository
    {
        private readonly List<FloorPlanCuration> items;

        public InMemoryFloorPlanCurationRepository(params FloorPlanCuration[] items)
        {
            this.items = items.ToList();
        }

        public Task<FloorPlanCuration?> GetByIdAsync(Guid curationId, CancellationToken cancellationToken)
            => Task.FromResult(items.SingleOrDefault(item => item.Id == curationId));

        public Task<FloorPlanCuration?> GetDraftAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
            => Task.FromResult(items
                .Where(item => item.FloorPlanVersionId == floorPlanVersionId && item.Status == FloorPlanCurationStatus.Draft)
                .OrderByDescending(item => item.CurationVersion)
                .FirstOrDefault());

        public Task<FloorPlanCuration?> GetPublishedAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
            => Task.FromResult(items
                .Where(item => item.FloorPlanVersionId == floorPlanVersionId && item.Status == FloorPlanCurationStatus.Published)
                .OrderByDescending(item => item.CurationVersion)
                .FirstOrDefault());

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

        public Task UpdateAsync(FloorPlanCuration curation, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
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
