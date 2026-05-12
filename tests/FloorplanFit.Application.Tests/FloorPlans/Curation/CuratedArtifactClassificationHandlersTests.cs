using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class CuratedArtifactClassificationHandlersTests
{
    [Fact]
    public async Task SaveCuratedArtifactClassificationHandler_upserts_reclassified_overlay_and_saves_changes()
    {
        var curation = new FloorPlanCuration(
            Guid.NewGuid(),
            Guid.NewGuid(),
            curationVersion: 1,
            FloorPlanCurationStatus.Draft,
            basedOnCurationId: null,
            notes: null,
            createdAtUtc: new DateTime(2026, 5, 11, 12, 0, 0, DateTimeKind.Utc),
            publishedAtUtc: null);
        var curationRepository = new InMemoryFloorPlanCurationRepository(curation);
        var classificationRepository = new InMemoryFloorPlanArtifactClassificationRepository();
        var unitOfWork = new FakeUnitOfWork();
        var clock = new FakeClock(new DateTime(2026, 5, 11, 12, 5, 0, DateTimeKind.Utc));
        var handler = new SaveCuratedArtifactClassificationHandler(
            curationRepository,
            classificationRepository,
            unitOfWork,
            clock);

        await handler.HandleAsync(
            curation.Id,
            FloorPlanArtifactSourceKinds.OpeningCandidate,
            Guid.NewGuid(),
            FloorPlanArtifactTaxonomy.FixedFamily,
            FloorPlanArtifactTaxonomy.WetFixtureCategory,
            FloorPlanArtifactTaxonomy.TubType,
            CancellationToken.None);

        var saved = Assert.Single(classificationRepository.Items);
        Assert.Equal(curation.Id, saved.FloorPlanCurationId);
        Assert.Equal(FloorPlanArtifactSourceKinds.OpeningCandidate, saved.SourceArtifactKind);
        Assert.Equal(FloorPlanArtifactTaxonomy.FixedFamily, saved.ResolvedFamily);
        Assert.Equal(FloorPlanArtifactTaxonomy.WetFixtureCategory, saved.ResolvedCategory);
        Assert.Equal(FloorPlanArtifactTaxonomy.TubType, saved.ResolvedType);
        Assert.Equal(FloorPlanArtifactDecisionState.Reclassified, saved.DecisionState);
        Assert.Equal(clock.UtcNow, saved.UpdatedAtUtc);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    [Fact]
    public async Task RestoreCuratedArtifactClassificationHandler_writes_detected_default_overlay_and_saves_changes()
    {
        var curation = new FloorPlanCuration(
            Guid.NewGuid(),
            Guid.NewGuid(),
            curationVersion: 1,
            FloorPlanCurationStatus.Draft,
            basedOnCurationId: null,
            notes: null,
            createdAtUtc: new DateTime(2026, 5, 11, 12, 0, 0, DateTimeKind.Utc),
            publishedAtUtc: null);
        var curationRepository = new InMemoryFloorPlanCurationRepository(curation);
        var classificationRepository = new InMemoryFloorPlanArtifactClassificationRepository();
        var unitOfWork = new FakeUnitOfWork();
        var clock = new FakeClock(new DateTime(2026, 5, 11, 12, 6, 0, DateTimeKind.Utc));
        var handler = new RestoreCuratedArtifactClassificationHandler(
            curationRepository,
            classificationRepository,
            unitOfWork,
            clock);

        await handler.HandleAsync(
            curation.Id,
            FloorPlanArtifactSourceKinds.ProtectedDetailAssembly,
            Guid.NewGuid(),
            FloorPlanArtifactTaxonomy.ProtectedFamily,
            FloorPlanArtifactTaxonomy.WetAssemblyCategory,
            FloorPlanArtifactTaxonomy.UnknownWetAssemblyType,
            CancellationToken.None);

        var saved = Assert.Single(classificationRepository.Items);
        Assert.Equal(FloorPlanArtifactDecisionState.DetectedDefault, saved.DecisionState);
        Assert.Equal(FloorPlanArtifactTaxonomy.ProtectedFamily, saved.ResolvedFamily);
        Assert.Equal(FloorPlanArtifactTaxonomy.WetAssemblyCategory, saved.ResolvedCategory);
        Assert.Equal(FloorPlanArtifactTaxonomy.UnknownWetAssemblyType, saved.ResolvedType);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    [Fact]
    public async Task ExcludeCuratedArtifactHandler_writes_excluded_overlay_and_saves_changes()
    {
        var curation = new FloorPlanCuration(
            Guid.NewGuid(),
            Guid.NewGuid(),
            curationVersion: 1,
            FloorPlanCurationStatus.Draft,
            basedOnCurationId: null,
            notes: null,
            createdAtUtc: new DateTime(2026, 5, 11, 12, 0, 0, DateTimeKind.Utc),
            publishedAtUtc: null);
        var curationRepository = new InMemoryFloorPlanCurationRepository(curation);
        var classificationRepository = new InMemoryFloorPlanArtifactClassificationRepository();
        var unitOfWork = new FakeUnitOfWork();
        var clock = new FakeClock(new DateTime(2026, 5, 11, 12, 7, 0, DateTimeKind.Utc));
        var handler = new ExcludeCuratedArtifactHandler(
            curationRepository,
            classificationRepository,
            unitOfWork,
            clock);

        await handler.HandleAsync(
            curation.Id,
            FloorPlanArtifactSourceKinds.FixedPlanComponent,
            Guid.NewGuid(),
            FloorPlanArtifactTaxonomy.FixedFamily,
            FloorPlanArtifactTaxonomy.GenericFixedCategory,
            FloorPlanArtifactTaxonomy.GenericFixtureType,
            CancellationToken.None);

        var saved = Assert.Single(classificationRepository.Items);
        Assert.Equal(FloorPlanArtifactDecisionState.Excluded, saved.DecisionState);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    private sealed class InMemoryFloorPlanArtifactClassificationRepository : IFloorPlanArtifactClassificationRepository
    {
        public List<FloorPlanArtifactClassification> Items { get; } = [];

        public Task<IReadOnlyList<FloorPlanArtifactClassification>> ListByCurationAsync(Guid floorPlanCurationId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<FloorPlanArtifactClassification>>(
                Items.Where(item => item.FloorPlanCurationId == floorPlanCurationId).ToArray());
        }

        public Task UpsertAsync(FloorPlanArtifactClassification classification, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item =>
                item.FloorPlanCurationId == classification.FloorPlanCurationId &&
                item.SourceArtifactKind == classification.SourceArtifactKind &&
                item.SourceArtifactId == classification.SourceArtifactId);
            Items.Add(classification);
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

        public Task<FloorPlanCuration?> GetByIdAsync(Guid curationId, CancellationToken cancellationToken)
            => Task.FromResult(curation.Id == curationId ? curation : null);

        public Task<FloorPlanCuration?> GetDraftAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<FloorPlanCuration?> GetPublishedAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<int> GetNextCurationVersionAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task AddAsync(FloorPlanCuration curation, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task UpdateAsync(FloorPlanCuration curation, CancellationToken cancellationToken)
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

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
