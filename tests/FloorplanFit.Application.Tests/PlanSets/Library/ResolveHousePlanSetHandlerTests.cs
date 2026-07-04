using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Library;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets.Library;

public sealed class ResolveHousePlanSetHandlerTests
{
    [Fact]
    public async Task HandleAsync_creates_house_plan_set_for_floor_plan_template()
    {
        var sourceTemplateId = Guid.NewGuid();
        var repository = new CapturingHousePlanSetRepository();
        var unitOfWork = new CapturingUnitOfWork();
        var clock = new FakeClock(new DateTime(2026, 6, 30, 22, 30, 0, DateTimeKind.Utc));
        var handler = new ResolveHousePlanSetHandler(repository, unitOfWork, clock);

        var response = await handler.HandleAsync(
            new ResolveHousePlanSetRequest(sourceTemplateId, "seminole2000", "Seminole"),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.HousePlanSetId);
        Assert.NotEqual(sourceTemplateId, response.HousePlanSetId);
        Assert.Equal(sourceTemplateId, response.SourceFloorPlanTemplateId);
        Assert.Equal("seminole2000", response.Code);
        Assert.Equal("Seminole", response.Name);
        Assert.True(response.Created);
        Assert.True(unitOfWork.Saved);

        var saved = Assert.Single(repository.Items);
        Assert.Equal(response.HousePlanSetId, saved.Id);
        Assert.Equal(sourceTemplateId, saved.SourceFloorPlanTemplateId);
        Assert.Equal(clock.UtcNow, saved.CreatedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_reuses_existing_house_plan_set_for_floor_plan_template()
    {
        var sourceTemplateId = Guid.NewGuid();
        var existing = new HousePlanSet(
            Guid.NewGuid(),
            sourceTemplateId,
            "seminole2000",
            "Seminole",
            new DateTime(2026, 6, 30, 21, 0, 0, DateTimeKind.Utc));
        var repository = new CapturingHousePlanSetRepository(existing);
        var unitOfWork = new CapturingUnitOfWork();
        var handler = new ResolveHousePlanSetHandler(
            repository,
            unitOfWork,
            new FakeClock(new DateTime(2026, 6, 30, 22, 30, 0, DateTimeKind.Utc)));

        var response = await handler.HandleAsync(
            new ResolveHousePlanSetRequest(sourceTemplateId, "ignored", "Ignored"),
            CancellationToken.None);

        Assert.Equal(existing.Id, response.HousePlanSetId);
        Assert.Equal("seminole2000", response.Code);
        Assert.False(response.Created);
        Assert.False(unitOfWork.Saved);
    }

    private sealed class CapturingHousePlanSetRepository : IHousePlanSetRepository
    {
        public CapturingHousePlanSetRepository(params HousePlanSet[] sets)
        {
            Items.AddRange(sets);
        }

        public List<HousePlanSet> Items { get; } = [];

        public Task AddAsync(HousePlanSet housePlanSet, CancellationToken cancellationToken)
        {
            Items.Add(housePlanSet);
            return Task.CompletedTask;
        }

        public Task<HousePlanSet?> GetBySourceFloorPlanTemplateAsync(
            Guid sourceFloorPlanTemplateId,
            CancellationToken cancellationToken)
            => Task.FromResult(Items.FirstOrDefault(
                item => item.SourceFloorPlanTemplateId == sourceFloorPlanTemplateId));
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
