using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Library;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.Tests.PlanSets.Library;

public sealed class GetPlanSetLibraryHandlerTests
{
    [Fact]
    public async Task HandleAsync_projects_current_floor_plan_as_canonical_plan_set_sheet()
    {
        var templateId = Guid.NewGuid();
        var currentVersionId = Guid.NewGuid();
        var activePublishedCurationId = Guid.NewGuid();
        var importedAtUtc = new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
        var item = new FloorPlanLibraryItemDto(
            templateId,
            "seminole2000",
            "SEMINOLE2000",
            1,
            currentVersionId,
            1,
            [
                new FloorPlanLibraryVersionDto(
                    currentVersionId,
                    1,
                    "Published",
                    importedAtUtc,
                    "inch",
                    IsCurrent: true,
                    activePublishedCurationId)
            ]);

        var handler = new GetPlanSetLibraryHandler(new FakeFloorPlanLibraryReader([item]));

        var result = await handler.HandleAsync(CancellationToken.None);

        var planSet = Assert.Single(result);
        Assert.Equal(templateId, planSet.HousePlanSetId);
        Assert.Equal("seminole2000", planSet.Code);
        Assert.Equal("SEMINOLE2000", planSet.Name);
        Assert.Equal(currentVersionId, planSet.ActivePlanSetVersionId);
        Assert.Equal(currentVersionId, planSet.CanonicalFloorPlanVersionId);
        Assert.Equal(activePublishedCurationId, planSet.ActivePublishedCurationId);
        Assert.True(planSet.HasCanonicalFloorPlan);
        Assert.True(planSet.CanProduceCanonicalAdjustment);

        var sheet = Assert.Single(planSet.Sheets);
        Assert.Equal(currentVersionId, sheet.SheetId);
        Assert.Equal("FloorPlan", sheet.SheetType);
        Assert.Equal("SEMINOLE2000 Floor Plan", sheet.Name);
        Assert.Equal(currentVersionId, sheet.SourceFloorPlanVersionId);
        Assert.True(sheet.IsCanonical);
        Assert.Equal("Canonical", sheet.RegistrationStatus);
        Assert.Equal("CanonicalSource", sheet.ProjectionStatus);
    }

    [Fact]
    public async Task HandleAsync_keeps_plan_set_visible_when_floor_plan_has_no_current_version()
    {
        var templateId = Guid.NewGuid();
        var item = new FloorPlanLibraryItemDto(
            templateId,
            "empty-template",
            "Empty Template",
            0,
            null,
            null,
            []);

        var handler = new GetPlanSetLibraryHandler(new FakeFloorPlanLibraryReader([item]));

        var result = await handler.HandleAsync(CancellationToken.None);

        var planSet = Assert.Single(result);
        Assert.Equal(templateId, planSet.HousePlanSetId);
        Assert.Null(planSet.ActivePlanSetVersionId);
        Assert.Null(planSet.CanonicalFloorPlanVersionId);
        Assert.False(planSet.HasCanonicalFloorPlan);
        Assert.False(planSet.CanProduceCanonicalAdjustment);
        Assert.Empty(planSet.Sheets);
    }

    private sealed class FakeFloorPlanLibraryReader : IFloorPlanLibraryReader
    {
        private readonly IReadOnlyList<FloorPlanLibraryItemDto> items;

        public FakeFloorPlanLibraryReader(IReadOnlyList<FloorPlanLibraryItemDto> items)
        {
            this.items = items;
        }

        public Task<IReadOnlyList<FloorPlanLibraryItemDto>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(items);
        }
    }
}
