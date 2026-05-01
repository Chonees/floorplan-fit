using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Review;

public sealed class GetFloorPlanReviewSessionHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_the_review_session_loaded_by_template_id()
    {
        var templateId = Guid.NewGuid();
        var expected = new FloorPlanReviewSessionDto(
            templateId,
            "santa-barbara",
            "SANTA-BARBARA",
            "Extracted",
            ActiveVersionNumber: 2,
            ActivePublishedCurationId: null,
            GeometryPaths: [],
            WallCandidates:
            [
                new WallCandidateDto(
                    Guid.NewGuid(),
                    "LINE:1",
                    "WALLS",
                    "Pending",
                    0.95m,
                    null,
                    null,
                    Guid.NewGuid(),
                    1)
            ],
            CuratedWalls: []);
        var reader = new FakeFloorPlanReviewSessionReader(expected);
        var handler = new GetFloorPlanReviewSessionHandler(reader);

        var session = await handler.HandleAsync(templateId, CancellationToken.None);

        Assert.Same(expected, session);
        Assert.Equal(templateId, reader.LastTemplateId);
    }

    private sealed class FakeFloorPlanReviewSessionReader : IFloorPlanReviewSessionReader
    {
        private readonly FloorPlanReviewSessionDto expected;

        public FakeFloorPlanReviewSessionReader(FloorPlanReviewSessionDto expected)
        {
            this.expected = expected;
        }

        public Guid? LastTemplateId { get; private set; }

        public Task<FloorPlanReviewSessionDto?> GetByTemplateAsync(Guid templateId, CancellationToken cancellationToken)
        {
            LastTemplateId = templateId;
            return Task.FromResult<FloorPlanReviewSessionDto?>(expected);
        }
    }
}
