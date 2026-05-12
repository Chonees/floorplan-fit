using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class FloorPlanDimensionOverrideHandlersTests
{
    [Fact]
    public async Task SaveFloorPlanDimensionOverrideHandler_upserts_dimension_snapshot_and_saves_changes()
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
        var overrideRepository = new InMemoryFloorPlanDimensionOverrideRepository();
        var unitOfWork = new FakeUnitOfWork();
        var clock = new FakeClock(new DateTime(2026, 5, 11, 20, 5, 0, DateTimeKind.Utc));
        var handler = new SaveFloorPlanDimensionOverrideHandler(curationRepository, overrideRepository, unitOfWork, clock);
        var dimension = CreateDimensionDto();

        await handler.HandleAsync(
            curation.Id,
            dimension with
            {
                DisplayText = "11'-0\"",
                RenderTextX = 480m,
                RenderTextY = 160m,
                DefPointX = 100m,
                DefPoint2X = 232m,
                LinePrimitives =
                [
                    new DimensionLinePrimitiveDto("LINE-1", 1, 100m, 140m, 100m, 100m),
                    new DimensionLinePrimitiveDto("LINE-2", 2, 232m, 140m, 232m, 100m),
                    new DimensionLinePrimitiveDto("LINE-3", 3, 100m, 140m, 232m, 140m)
                ]
            },
            CancellationToken.None);

        var saved = Assert.Single(overrideRepository.Items);
        Assert.Equal("AB12", saved.SourceDimensionKey);
        Assert.Equal("11'-0\"", saved.DisplayText);
        Assert.Equal(480m, saved.RenderTextX);
        Assert.Equal(232m, saved.DefPoint2X);
        Assert.Equal(3, saved.LinePrimitives.Count);
        Assert.True(saved.IsDirty);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    [Fact]
    public async Task RestoreFloorPlanDimensionOverrideHandler_deletes_existing_override_and_saves_changes()
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
        var overrideRepository = new InMemoryFloorPlanDimensionOverrideRepository();
        var unitOfWork = new FakeUnitOfWork();
        overrideRepository.Items.Add(FloorPlanDimensionOverride.CreateManualSnapshot(
            curation.Id,
            "AB12",
            "DIMENSION:AB12",
            "AB12",
            "11'-0\"",
            100m,
            100m,
            0m,
            232m,
            100m,
            0m,
            100m,
            140m,
            0m,
            480m,
            160m,
            3.5m,
            0m,
            "ARCH",
            null,
            null,
            "MiddleCenter",
            [],
            [],
            [],
            [],
            [],
            [],
            new DateTime(2026, 5, 11, 20, 4, 0, DateTimeKind.Utc),
            null));
        var handler = new RestoreFloorPlanDimensionOverrideHandler(curationRepository, overrideRepository, unitOfWork);

        await handler.HandleAsync(curation.Id, "AB12", CancellationToken.None);

        Assert.Empty(overrideRepository.Items);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    private static DimensionDto CreateDimensionDto()
    {
        return new DimensionDto(
            Guid.NewGuid(),
            "DIMENSION:AB12",
            "DIMS",
            "DIMENSION",
            "*D169",
            "10'-4\"",
            "GeometryBlock",
            string.Empty,
            124m,
            3149.6m,
            "Inch",
            0,
            0m,
            0m,
            100m,
            100m,
            0m,
            224m,
            100m,
            0m,
            100m,
            140m,
            0m,
            0.99m,
            null,
            1)
        {
            SourceHandle = "AB12",
            RenderTextX = 162m,
            RenderTextY = 148m,
            RenderTextHeight = 3.5m,
            RenderTextStyleName = "ARCH",
            RenderTextAttachmentPoint = "MiddleCenter",
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto("LINE-1", 1, 100m, 140m, 100m, 100m),
                new DimensionLinePrimitiveDto("LINE-2", 2, 224m, 140m, 224m, 100m),
                new DimensionLinePrimitiveDto("LINE-3", 3, 100m, 140m, 224m, 140m)
            ],
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto("TEXT-1", 1, "10'-4\"", 162m, 148m, 3.5m, 0m)
                {
                    StyleName = "ARCH",
                    AttachmentPoint = "MiddleCenter"
                }
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("INSERT-1", 1, "_Dot", 100m, 140m, 0m),
                new DimensionInsertPrimitiveDto("INSERT-2", 2, "_Dot", 224m, 140m, 0m)
            ]
        };
    }

    private sealed class InMemoryFloorPlanDimensionOverrideRepository : IFloorPlanDimensionOverrideRepository
    {
        public List<FloorPlanDimensionOverride> Items { get; } = [];

        public Task<IReadOnlyList<FloorPlanDimensionOverride>> ListByCurationAsync(Guid floorPlanCurationId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<FloorPlanDimensionOverride>>(Items.Where(item => item.FloorPlanCurationId == floorPlanCurationId).ToArray());

        public Task UpsertAsync(FloorPlanDimensionOverride dimensionOverride, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item =>
                item.FloorPlanCurationId == dimensionOverride.FloorPlanCurationId &&
                string.Equals(item.SourceDimensionKey, dimensionOverride.SourceDimensionKey, StringComparison.Ordinal));
            Items.Add(dimensionOverride);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid floorPlanCurationId, string sourceDimensionKey, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item =>
                item.FloorPlanCurationId == floorPlanCurationId &&
                string.Equals(item.SourceDimensionKey, sourceDimensionKey, StringComparison.Ordinal));
            return Task.CompletedTask;
        }

        public Task MarkExportedAsync(Guid floorPlanCurationId, IReadOnlyList<string> sourceDimensionKeys, DateTime exportedAtUtc, CancellationToken cancellationToken)
            => Task.CompletedTask;
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
