using Xunit;

namespace FloorplanFit.Desktop.Tests.ViewModels;

public sealed class FloorPlanReviewViewModelArchitectureTests
{
    [Fact]
    public void FloorPlanReviewViewModel_sources_first_review_mutations_through_the_mutation_coordinator()
    {
        var viewModelPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "FloorplanFit.Desktop", "ViewModels", "FloorPlanReviewViewModel.cs");
        var source = File.ReadAllText(viewModelPath);

        Assert.Contains("FloorPlanReviewMutationCoordinator", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService<SaveFloorPlanLabelTextHeightHandler>()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService<RestoreFloorPlanLabelTextHeightHandler>()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService<SaveFloorPlanArtifactPositionHandler>()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService<RestoreFloorPlanArtifactPositionHandler>()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService<SaveFloorPlanDimensionOverrideHandler>()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService<ExportAdjustedDxfHandler>()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService<SaveCuratedArtifactClassificationHandler>()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService<RestoreCuratedArtifactClassificationHandler>()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService<ExcludeCuratedArtifactHandler>()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService<RemoveRoomLabelHandler>()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService<RemoveOpeningLabelHandler>()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService<RemovePinchMarkerHandler>()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService<RejectWallCandidateHandler>()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService<PublishFloorPlanCurationHandler>()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService<AddPinchGroupHandler>()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService<AddPinchMarkerHandler>()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService<RestoreFloorPlanDimensionOverrideHandler>()", source, StringComparison.Ordinal);
    }
}
