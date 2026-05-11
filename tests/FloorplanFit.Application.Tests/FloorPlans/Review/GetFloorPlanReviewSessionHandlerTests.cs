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
            RoomLabels:
            [
                new RoomLabelDto(Guid.NewGuid(), "TEXT:1", "ROOM LBLS", "KITCHEN", 125m, 784m, 0.95m, null, 1)
            ],
            OpeningCandidates: [],
            OpeningLabels: [],
            FixedPlanComponents: [],
            ProtectedDetailAssemblies: [],
            WallCandidates:
            [
                new WallCandidateDto(
                    Guid.NewGuid(),
                    "LINE:1",
                    "WALLS",
                    "Accepted",
                    0.95m,
                    null,
                    null,
                    Guid.NewGuid(),
                    1)
            ],
            PinchGroups: [],
            PinchMarkers: []);
        var reader = new FakeFloorPlanReviewSessionReader(expected);
        var handler = new GetFloorPlanReviewSessionHandler(reader);

        var session = await handler.HandleAsync(templateId, CancellationToken.None);

        Assert.Same(expected, session);
        Assert.Equal(templateId, reader.LastTemplateId);
        Assert.Equal("KITCHEN", session!.RoomLabels.Single().Text);
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

        public Task<FloorPlanReviewSessionDto?> GetByVersionAsync(
            Guid templateId,
            Guid floorPlanVersionId,
            CancellationToken cancellationToken)
        {
            LastTemplateId = templateId;
            return Task.FromResult<FloorPlanReviewSessionDto?>(expected);
        }
    }
}
