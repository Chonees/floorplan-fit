using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class FloorPlanCurationTests
{
    [Fact]
    public void Publish_marks_the_curation_as_published_and_blocks_second_publish()
    {
        var curation = new FloorPlanCuration(
            Guid.NewGuid(),
            Guid.NewGuid(),
            curationVersion: 1,
            FloorPlanCurationStatus.Draft,
            basedOnCurationId: null,
            notes: null,
            createdAtUtc: new DateTime(2026, 4, 30, 18, 0, 0, DateTimeKind.Utc),
            publishedAtUtc: null);

        var publishedAtUtc = new DateTime(2026, 4, 30, 19, 0, 0, DateTimeKind.Utc);

        curation.Publish(publishedAtUtc);

        Assert.Equal(FloorPlanCurationStatus.Published, curation.Status);
        Assert.Equal(publishedAtUtc, curation.PublishedAtUtc);

        Action republish = () => curation.Publish(new DateTime(2026, 4, 30, 20, 0, 0, DateTimeKind.Utc));
        Assert.Throws<InvalidOperationException>(republish);
    }

    [Fact]
    public void SetActivePublishedCuration_tracks_the_active_curation_on_the_template()
    {
        var template = new FloorPlanTemplate(
            Guid.NewGuid(),
            "santa-barbara",
            "SANTA-BARBARA",
            isActive: true);

        var curationId = Guid.NewGuid();

        template.SetActivePublishedCuration(curationId);

        Assert.Equal(curationId, template.ActivePublishedCurationId);
    }
}
