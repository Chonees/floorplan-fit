using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class FloorPlanLabelTextHeightHandlersTests
{
    [Fact]
    public async Task SaveFloorPlanLabelTextHeightHandler_upserts_manual_override_and_saves_changes()
    {
        var curation = new FloorPlanCuration(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            FloorPlanCurationStatus.Draft,
            null,
            null,
            new DateTime(2026, 5, 11, 23, 40, 0, DateTimeKind.Utc),
            null);
        var curationRepository = new InMemoryFloorPlanCurationRepository(curation);
        var overrideRepository = new InMemoryFloorPlanLabelOverrideRepository();
        var unitOfWork = new FakeUnitOfWork();
        var clock = new FakeClock(new DateTime(2026, 5, 11, 23, 41, 0, DateTimeKind.Utc));
        var handler = new SaveFloorPlanLabelTextHeightHandler(curationRepository, overrideRepository, unitOfWork, clock);

        await handler.HandleAsync(
            curation.Id,
            FloorPlanLabelOverrideSourceKinds.RoomLabel,
            Guid.NewGuid(),
            6m,
            CancellationToken.None);

        var saved = Assert.Single(overrideRepository.Items);
        Assert.Equal(6m, saved.ResolvedTextHeight);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    [Fact]
    public async Task RestoreFloorPlanLabelTextHeightHandler_writes_detected_default_row()
    {
        var curation = new FloorPlanCuration(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            FloorPlanCurationStatus.Draft,
            null,
            null,
            new DateTime(2026, 5, 11, 23, 40, 0, DateTimeKind.Utc),
            null);
        var curationRepository = new InMemoryFloorPlanCurationRepository(curation);
        var overrideRepository = new InMemoryFloorPlanLabelOverrideRepository();
        var unitOfWork = new FakeUnitOfWork();
        var clock = new FakeClock(new DateTime(2026, 5, 11, 23, 42, 0, DateTimeKind.Utc));
        var handler = new RestoreFloorPlanLabelTextHeightHandler(curationRepository, overrideRepository, unitOfWork, clock);

        await handler.HandleAsync(
            curation.Id,
            FloorPlanLabelOverrideSourceKinds.OpeningLabel,
            Guid.NewGuid(),
            CancellationToken.None);

        var saved = Assert.Single(overrideRepository.Items);
        Assert.Null(saved.ResolvedTextHeight);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    private sealed class InMemoryFloorPlanLabelOverrideRepository : IFloorPlanLabelOverrideRepository
    {
        public List<FloorPlanLabelOverride> Items { get; } = [];

        public Task<IReadOnlyList<FloorPlanLabelOverride>> ListByCurationAsync(Guid floorPlanCurationId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<FloorPlanLabelOverride>>(
                Items.Where(item => item.FloorPlanCurationId == floorPlanCurationId).ToArray());
        }

        public Task UpsertAsync(FloorPlanLabelOverride labelOverride, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item =>
                item.FloorPlanCurationId == labelOverride.FloorPlanCurationId &&
                item.SourceArtifactKind == labelOverride.SourceArtifactKind &&
                item.SourceArtifactId == labelOverride.SourceArtifactId);
            Items.Add(labelOverride);
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
