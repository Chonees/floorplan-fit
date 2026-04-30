using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class PublishFloorPlanCurationHandlerTests
{
    [Fact]
    public async Task HandleAsync_sets_the_active_published_curation_on_the_template()
    {
        var template = new FloorPlanTemplate(Guid.NewGuid(), "santa-barbara", "SANTA-BARBARA", isActive: true);
        var versionId = Guid.NewGuid();
        template.SetCurrentVersion(versionId);

        var curation = new FloorPlanCuration(
            Guid.NewGuid(),
            versionId,
            curationVersion: 1,
            FloorPlanCurationStatus.Draft,
            basedOnCurationId: null,
            notes: null,
            createdAtUtc: new DateTime(2026, 4, 30, 20, 0, 0, DateTimeKind.Utc),
            publishedAtUtc: null);

        var templateRepository = new InMemoryFloorPlanTemplateRepository(template);
        var curationRepository = new InMemoryFloorPlanCurationRepository(curation);
        var wallRepository = new InMemoryCuratedWallRepository(
        [
            new CuratedWall(
                Guid.NewGuid(),
                curation.Id,
                "W-001",
                sourceCandidateId: Guid.NewGuid(),
                sourceEntityRef: "LINE:12",
                geometryPathId: Guid.NewGuid(),
                WallRole.Partition,
                WallMobilityLevel.Flexible,
                WallProtectionLevel.None,
                thicknessMm: 101.6m,
                assemblyCode: "2x4",
                heightMm: null,
                isExterior: false,
                isStructuralHint: false,
                wallGroupId: null,
                sortOrder: 1,
                notes: null)
        ]);
        var unitOfWork = new FakeUnitOfWork();
        var publishedAtUtc = new DateTime(2026, 4, 30, 21, 0, 0, DateTimeKind.Utc);

        var handler = new PublishFloorPlanCurationHandler(
            curationRepository,
            wallRepository,
            templateRepository,
            unitOfWork,
            new FakeClock(publishedAtUtc));

        await handler.HandleAsync(template.Id, curation.Id, CancellationToken.None);

        Assert.Equal(curation.Id, template.ActivePublishedCurationId);
        Assert.Equal(FloorPlanCurationStatus.Published, curation.Status);
        Assert.Equal(publishedAtUtc, curation.PublishedAtUtc);
        Assert.True(unitOfWork.SaveChangesCalled);
        Assert.True(templateRepository.UpdateCalled);
        Assert.True(curationRepository.UpdateCalled);
    }

    private sealed class InMemoryFloorPlanTemplateRepository : IFloorPlanTemplateRepository
    {
        private readonly FloorPlanTemplate template;

        public InMemoryFloorPlanTemplateRepository(FloorPlanTemplate template)
        {
            this.template = template;
        }

        public bool UpdateCalled { get; private set; }

        public Task<FloorPlanTemplate?> GetByCodeAsync(string code, CancellationToken cancellationToken)
        {
            return Task.FromResult(template.Code == code ? template : null);
        }

        public Task<FloorPlanTemplate?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken)
        {
            return Task.FromResult(template.Id == templateId ? template : null);
        }

        public Task AddAsync(FloorPlanTemplate template, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task UpdateAsync(FloorPlanTemplate template, CancellationToken cancellationToken)
        {
            UpdateCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryFloorPlanCurationRepository : IFloorPlanCurationRepository
    {
        private readonly FloorPlanCuration curation;

        public InMemoryFloorPlanCurationRepository(FloorPlanCuration curation)
        {
            this.curation = curation;
        }

        public bool UpdateCalled { get; private set; }

        public Task<FloorPlanCuration?> GetDraftAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
        {
            return Task.FromResult(curation.FloorPlanVersionId == floorPlanVersionId && curation.Status == FloorPlanCurationStatus.Draft ? curation : null);
        }

        public Task<FloorPlanCuration?> GetPublishedAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
        {
            return Task.FromResult<FloorPlanCuration?>(null);
        }

        public Task<int> GetNextCurationVersionAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }

        public Task AddAsync(FloorPlanCuration curation, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task UpdateAsync(FloorPlanCuration curation, CancellationToken cancellationToken)
        {
            UpdateCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryCuratedWallRepository : ICuratedWallRepository
    {
        private readonly IReadOnlyList<CuratedWall> items;

        public InMemoryCuratedWallRepository(IReadOnlyList<CuratedWall> items)
        {
            this.items = items;
        }

        public Task AddAsync(CuratedWall wall, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<CuratedWall?> GetByIdAsync(Guid curatedWallId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<CuratedWall>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<CuratedWall>>(items.Where(item => item.FloorPlanCurationId == curationId).ToArray());
        }

        public Task RemoveBySourceCandidateAsync(Guid curationId, Guid sourceCandidateId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task UpdateAsync(CuratedWall wall, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
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


