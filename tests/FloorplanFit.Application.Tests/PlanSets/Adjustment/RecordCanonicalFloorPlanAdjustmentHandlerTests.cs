using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Adjustment;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets.Adjustment;

public sealed class RecordCanonicalFloorPlanAdjustmentHandlerTests
{
    [Fact]
    public async Task HandleAsync_persists_canonical_floor_plan_adjustment()
    {
        var planSetVersionId = Guid.NewGuid();
        var floorPlanVersionId = Guid.NewGuid();
        var repository = new CapturingCanonicalFloorPlanAdjustmentRepository();
        var unitOfWork = new CapturingUnitOfWork();
        var clock = new FakeClock(new DateTime(2026, 6, 30, 23, 59, 0, DateTimeKind.Utc));
        var handler = new RecordCanonicalFloorPlanAdjustmentHandler(repository, unitOfWork, clock);
        var placement = new AdjustedSitePlanPlacementDto(
            FloorToSiteScale: 1.2m,
            SiteOffsetX: 10m,
            SiteOffsetY: 20m,
            CompressionSteps:
            [
                new AdjustedCompressionStepDto(
                    "Width",
                    "Right",
                    [new AdjustedCompressionMarkerDto(50m, 2m)])
            ]);

        var response = await handler.HandleAsync(
            new RecordCanonicalFloorPlanAdjustmentRequest(
                planSetVersionId,
                floorPlanVersionId,
                "library/site.dxf",
                "exports/floor-adjusted.dxf",
                placement),
            CancellationToken.None);

        Assert.True(unitOfWork.Saved);
        Assert.Equal(planSetVersionId, response.PlanSetVersionId);
        Assert.Equal(floorPlanVersionId, response.CanonicalFloorPlanVersionId);
        Assert.Equal("exports/floor-adjusted.dxf", response.CanonicalFloorPlanExportPath);
        Assert.Equal(clock.UtcNow, response.CreatedAtUtc);
        Assert.Equal(1.2m, response.AdjustmentRecipe.FloorToSiteScale);
        Assert.Contains(response.AdjustmentRecipe.Operations, operation => operation.Kind == "HorizontalCompression");

        var saved = Assert.Single(repository.Items);
        Assert.Equal(response.AdjustmentId, saved.Id);
        Assert.Equal(planSetVersionId, saved.PlanSetVersionId);
        Assert.Equal(floorPlanVersionId, saved.CanonicalFloorPlanVersionId);
        Assert.Equal("library/site.dxf", saved.SitePlanSourcePath);
        Assert.Equal("exports/floor-adjusted.dxf", saved.CanonicalFloorPlanExportPath);
        Assert.Contains("\"FloorToSiteScale\":1.2", saved.PlacementJson, StringComparison.Ordinal);
        Assert.Contains("\"Version\":\"v1\"", saved.AdjustmentRecipeJson, StringComparison.Ordinal);
        Assert.Contains("\"FloorToSiteScale\":1.2", saved.AdjustmentRecipeJson, StringComparison.Ordinal);
        Assert.Contains("HorizontalCompression", saved.AdjustmentRecipeJson, StringComparison.Ordinal);
        Assert.Contains("\"Edge\":\"Right\"", saved.AdjustmentRecipeJson, StringComparison.Ordinal);
    }

    private sealed class CapturingCanonicalFloorPlanAdjustmentRepository : ICanonicalFloorPlanAdjustmentRepository
    {
        public List<CanonicalFloorPlanAdjustment> Items { get; } = [];

        public Task AddAsync(CanonicalFloorPlanAdjustment adjustment, CancellationToken cancellationToken)
        {
            Items.Add(adjustment);
            return Task.CompletedTask;
        }
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
