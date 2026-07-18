using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class PublishFloorPlanCurationHandlerTests
{
    [Fact]
    public async Task HandleAsync_sets_the_active_published_curation_on_the_template_when_at_least_one_pinch_marker_exists()
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
        var markerRepository = new InMemoryPinchMarkerRepository(
        [
            new PinchMarker(
                Guid.NewGuid(),
                curation.Id,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                0.4m,
                120m,
                1)
        ]);
        var unitOfWork = new FakeUnitOfWork();
        var publishedAtUtc = new DateTime(2026, 4, 30, 21, 0, 0, DateTimeKind.Utc);

        var handler = new PublishFloorPlanCurationHandler(
            curationRepository,
            markerRepository,
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

    [Fact]
    public async Task HandleAsync_throws_when_no_pinch_markers_exist_for_the_curation()
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

        var handler = new PublishFloorPlanCurationHandler(
            new InMemoryFloorPlanCurationRepository(curation),
            new InMemoryPinchMarkerRepository([]),
            new InMemoryFloorPlanTemplateRepository(template),
            new FakeUnitOfWork(),
            new FakeClock(new DateTime(2026, 4, 30, 21, 0, 0, DateTimeKind.Utc)));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(template.Id, curation.Id, CancellationToken.None));

        Assert.Equal("A curation must contain at least one pinch marker before publish.", exception.Message);
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

        public Task<FloorPlanCuration?> GetByIdAsync(Guid curationId, CancellationToken cancellationToken)
        {
            return Task.FromResult(curation.Id == curationId ? curation : null);
        }

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

    private sealed class InMemoryPinchMarkerRepository : IPinchMarkerRepository
    {
        private readonly IReadOnlyList<PinchMarker> items;

        public InMemoryPinchMarkerRepository(IReadOnlyList<PinchMarker> items)
        {
            this.items = items;
        }

        public Task AddAsync(PinchMarker marker, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task UpdateAsync(PinchMarker marker, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<PinchMarker?> GetByIdAsync(Guid pinchMarkerId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<PinchMarker>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<PinchMarker>>(items.Where(item => item.FloorPlanCurationId == curationId).ToArray());
        }

        public Task RemoveAsync(Guid pinchMarkerId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task RemoveByGroupAsync(Guid curationId, Guid pinchGroupId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task RemoveBySourceCandidateAsync(Guid curationId, Guid sourceCandidateId, CancellationToken cancellationToken)
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
