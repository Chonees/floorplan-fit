using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FloorplanFit.Desktop.Tests.ViewModels;

public sealed class FloorPlanReviewSessionCoordinatorTests
{
    [Fact]
    public async Task RefreshAsync_clears_current_draft_when_the_requested_curation_is_now_published()
    {
        var templateId = Guid.NewGuid();
        var draftCurationId = Guid.NewGuid();
        var publishedCurationId = draftCurationId;
        var reader = new FakeFloorPlanReviewSessionReader(
            CreateSession(templateId, "Published", publishedCurationId));
        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanReviewSessionReader>(reader);
        services.AddTransient<GetFloorPlanReviewSessionHandler>();

        using var provider = services.BuildServiceProvider();
        var coordinator = new FloorPlanReviewSessionCoordinator(provider.GetRequiredService<IServiceScopeFactory>());

        var result = await coordinator.RefreshAsync(templateId, floorPlanVersionId: null, draftCurationId, CancellationToken.None);

        Assert.Equal(draftCurationId, reader.LastRequestedCurationId);
        Assert.Equal(Guid.Empty, result.DraftCurationId);
    }

    private static FloorPlanReviewSessionDto CreateSession(
        Guid templateId,
        string status,
        Guid? activePublishedCurationId)
    {
        return new FloorPlanReviewSessionDto(
            templateId,
            "seminole2000",
            "SEMINOLE2000",
            status,
            1,
            activePublishedCurationId,
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            []);
    }

    private sealed class FakeFloorPlanReviewSessionReader : IFloorPlanReviewSessionReader
    {
        private readonly FloorPlanReviewSessionDto session;

        public FakeFloorPlanReviewSessionReader(FloorPlanReviewSessionDto session)
        {
            this.session = session;
        }

        public Guid? LastRequestedCurationId { get; private set; }

        public Task<FloorPlanReviewSessionDto?> GetByTemplateAsync(Guid templateId, CancellationToken cancellationToken)
            => Task.FromResult<FloorPlanReviewSessionDto?>(session);

        public Task<FloorPlanReviewSessionDto?> GetByVersionAsync(
            Guid templateId,
            Guid floorPlanVersionId,
            CancellationToken cancellationToken)
            => Task.FromResult<FloorPlanReviewSessionDto?>(session);

        public Task<FloorPlanReviewSessionDto?> GetByCurationAsync(
            Guid templateId,
            Guid curationId,
            CancellationToken cancellationToken)
        {
            LastRequestedCurationId = curationId;
            return Task.FromResult<FloorPlanReviewSessionDto?>(session);
        }
    }
}
