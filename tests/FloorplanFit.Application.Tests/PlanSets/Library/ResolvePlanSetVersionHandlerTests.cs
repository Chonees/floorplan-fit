using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Library;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets.Library;

public sealed class ResolvePlanSetVersionHandlerTests
{
    [Fact]
    public async Task HandleAsync_creates_explicit_plan_set_version_for_canonical_floor_plan()
    {
        var housePlanSetId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var repository = new CapturingPlanSetVersionRepository();
        var unitOfWork = new CapturingUnitOfWork();
        var clock = new FakeClock(new DateTime(2026, 6, 30, 22, 0, 0, DateTimeKind.Utc));
        var handler = new ResolvePlanSetVersionHandler(repository, unitOfWork, clock);

        var response = await handler.HandleAsync(
            new ResolvePlanSetVersionRequest(housePlanSetId, canonicalFloorPlanVersionId),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.PlanSetVersionId);
        Assert.NotEqual(canonicalFloorPlanVersionId, response.PlanSetVersionId);
        Assert.Equal(housePlanSetId, response.HousePlanSetId);
        Assert.Equal(canonicalFloorPlanVersionId, response.CanonicalFloorPlanVersionId);
        Assert.Equal(1, response.VersionNumber);
        Assert.True(response.Created);
        Assert.True(unitOfWork.Saved);

        var saved = Assert.Single(repository.Items);
        Assert.Equal(response.PlanSetVersionId, saved.Id);
        Assert.Equal(housePlanSetId, saved.HousePlanSetId);
        Assert.Equal(canonicalFloorPlanVersionId, saved.CanonicalFloorPlanVersionId);
        Assert.Equal(clock.UtcNow, saved.CreatedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_reuses_existing_plan_set_version_for_canonical_floor_plan()
    {
        var housePlanSetId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var existing = new PlanSetVersion(
            Guid.NewGuid(),
            housePlanSetId,
            canonicalFloorPlanVersionId,
            versionNumber: 3,
            createdAtUtc: new DateTime(2026, 6, 30, 21, 0, 0, DateTimeKind.Utc));
        var repository = new CapturingPlanSetVersionRepository(existing);
        var unitOfWork = new CapturingUnitOfWork();
        var handler = new ResolvePlanSetVersionHandler(
            repository,
            unitOfWork,
            new FakeClock(new DateTime(2026, 6, 30, 22, 0, 0, DateTimeKind.Utc)));

        var response = await handler.HandleAsync(
            new ResolvePlanSetVersionRequest(housePlanSetId, canonicalFloorPlanVersionId),
            CancellationToken.None);

        Assert.Equal(existing.Id, response.PlanSetVersionId);
        Assert.Equal(3, response.VersionNumber);
        Assert.False(response.Created);
        Assert.False(unitOfWork.Saved);
    }

    private sealed class CapturingPlanSetVersionRepository : IPlanSetVersionRepository
    {
        public CapturingPlanSetVersionRepository(params PlanSetVersion[] versions)
        {
            Items.AddRange(versions);
        }

        public List<PlanSetVersion> Items { get; } = [];

        public Task AddAsync(PlanSetVersion version, CancellationToken cancellationToken)
        {
            Items.Add(version);
            return Task.CompletedTask;
        }

        public Task<PlanSetVersion?> GetByCanonicalFloorPlanVersionAsync(
            Guid canonicalFloorPlanVersionId,
            CancellationToken cancellationToken)
            => Task.FromResult(Items.FirstOrDefault(
                item => item.CanonicalFloorPlanVersionId == canonicalFloorPlanVersionId));
    }

    private sealed class CapturingUnitOfWork : IUnitOfWork
    {
        public bool Saved { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            Saved = true;
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
