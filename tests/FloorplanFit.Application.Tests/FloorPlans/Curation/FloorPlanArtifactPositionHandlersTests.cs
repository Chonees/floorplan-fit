using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class FloorPlanArtifactPositionHandlersTests
{
    [Fact]
    public async Task SaveFloorPlanArtifactPositionHandler_upserts_absolute_point_overlay_and_saves_changes()
    {
        var curation = new FloorPlanCuration(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            FloorPlanCurationStatus.Draft,
            null,
            null,
            new DateTime(2026, 5, 11, 20, 0, 0, DateTimeKind.Utc),
            null);
        var curationRepository = new InMemoryFloorPlanCurationRepository(curation);
        var positionRepository = new InMemoryFloorPlanArtifactPositionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var clock = new FakeClock(new DateTime(2026, 5, 11, 20, 5, 0, DateTimeKind.Utc));
        var handler = new SaveFloorPlanArtifactPositionHandler(curationRepository, positionRepository, unitOfWork, clock);

        await handler.HandleAsync(
            curation.Id,
            FloorPlanArtifactPositionSourceKinds.RoomLabel,
            Guid.NewGuid(),
            FloorPlanArtifactPositionMode.AbsolutePoint,
            512m,
            144m,
            null,
            null,
            CancellationToken.None);

        var saved = Assert.Single(positionRepository.Items);
        Assert.Equal(FloorPlanArtifactPositionMode.AbsolutePoint, saved.PositionMode);
        Assert.Equal(512m, saved.ResolvedX);
        Assert.Equal(144m, saved.ResolvedY);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    [Fact]
    public async Task RestoreFloorPlanArtifactPositionHandler_writes_neutral_translation_overlay_for_geometry_artifacts()
    {
        var curation = new FloorPlanCuration(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            FloorPlanCurationStatus.Draft,
            null,
            null,
            new DateTime(2026, 5, 11, 20, 0, 0, DateTimeKind.Utc),
            null);
        var curationRepository = new InMemoryFloorPlanCurationRepository(curation);
        var positionRepository = new InMemoryFloorPlanArtifactPositionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var clock = new FakeClock(new DateTime(2026, 5, 11, 20, 7, 0, DateTimeKind.Utc));
        var handler = new RestoreFloorPlanArtifactPositionHandler(curationRepository, positionRepository, unitOfWork, clock);

        await handler.HandleAsync(
            curation.Id,
            FloorPlanArtifactPositionSourceKinds.FixedPlanComponent,
            Guid.NewGuid(),
            FloorPlanArtifactPositionMode.Translation,
            null,
            null,
            0m,
            0m,
            CancellationToken.None);

        var saved = Assert.Single(positionRepository.Items);
        Assert.Equal(FloorPlanArtifactPositionMode.Translation, saved.PositionMode);
        Assert.Equal(0m, saved.TranslationDx);
        Assert.Equal(0m, saved.TranslationDy);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    [Fact]
    public async Task RestoreFloorPlanArtifactPositionHandler_writes_detected_label_coordinates_for_absolute_point_restore()
    {
        var curation = new FloorPlanCuration(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            FloorPlanCurationStatus.Draft,
            null,
            null,
            new DateTime(2026, 5, 11, 20, 0, 0, DateTimeKind.Utc),
            null);
        var curationRepository = new InMemoryFloorPlanCurationRepository(curation);
        var positionRepository = new InMemoryFloorPlanArtifactPositionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var clock = new FakeClock(new DateTime(2026, 5, 11, 20, 8, 0, DateTimeKind.Utc));
        var handler = new RestoreFloorPlanArtifactPositionHandler(curationRepository, positionRepository, unitOfWork, clock);

        await handler.HandleAsync(
            curation.Id,
            FloorPlanArtifactPositionSourceKinds.OpeningLabel,
            Guid.NewGuid(),
            FloorPlanArtifactPositionMode.AbsolutePoint,
            120m,
            84m,
            null,
            null,
            CancellationToken.None);

        var saved = Assert.Single(positionRepository.Items);
        Assert.Equal(120m, saved.ResolvedX);
        Assert.Equal(84m, saved.ResolvedY);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    private sealed class InMemoryFloorPlanArtifactPositionRepository : IFloorPlanArtifactPositionRepository
    {
        public List<FloorPlanArtifactPosition> Items { get; } = [];

        public Task<IReadOnlyList<FloorPlanArtifactPosition>> ListByCurationAsync(Guid floorPlanCurationId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<FloorPlanArtifactPosition>>(
                Items.Where(item => item.FloorPlanCurationId == floorPlanCurationId).ToArray());
        }

        public Task UpsertAsync(FloorPlanArtifactPosition position, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item =>
                item.FloorPlanCurationId == position.FloorPlanCurationId &&
                item.SourceArtifactKind == position.SourceArtifactKind &&
                item.SourceArtifactId == position.SourceArtifactId);
            Items.Add(position);
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
