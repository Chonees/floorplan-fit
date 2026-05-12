using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class FloorPlanArtifactTaxonomyTests
{
    [Fact]
    public void ResolveColorArgb_uses_the_same_color_for_windows_and_doors()
    {
        var doorColor = FloorPlanArtifactTaxonomy.ResolveColorArgb(
            FloorPlanArtifactTaxonomy.OpeningFamily,
            FloorPlanArtifactTaxonomy.OpeningCategory,
            FloorPlanArtifactTaxonomy.DoorType);
        var windowColor = FloorPlanArtifactTaxonomy.ResolveColorArgb(
            FloorPlanArtifactTaxonomy.OpeningFamily,
            FloorPlanArtifactTaxonomy.OpeningCategory,
            FloorPlanArtifactTaxonomy.WindowType);

        Assert.Equal(FloorPlanArtifactTaxonomy.DoorColorArgb, doorColor);
        Assert.Equal(FloorPlanArtifactTaxonomy.DoorColorArgb, windowColor);
        Assert.Equal(doorColor, windowColor);
    }
}
