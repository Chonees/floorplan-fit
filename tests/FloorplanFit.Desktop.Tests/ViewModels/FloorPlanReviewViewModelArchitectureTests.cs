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

        Assert.Contains("FloorPlanReviewSelectionCoordinator", source, StringComparison.Ordinal);
        Assert.DoesNotContain("private ReviewSelectionSnapshot CaptureSelection()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("private ReviewSelectionSnapshot BuildSelectionSnapshotForMovedArtifact(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("VisibleCuratedPlanArtifacts.FirstOrDefault(item => item.GeometryPathIds.Contains(geometryPathId))", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SelectedCandidate = selection.CandidateId is null", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SelectedPinchMarker = selection.PinchMarkerId is null", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PreviewSelectionLabel = $\"Previewing candidate:", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PreviewSelectionLabel = $\"Previewing curated object:", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PreviewSelectionLabel = $\"Previewing room label:", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PreviewSelectionLabel = $\"Previewing opening:", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PreviewSelectionLabel = $\"Previewing opening label:", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PreviewSelectionLabel = $\"Previewing fixed component:", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PreviewSelectionLabel = $\"Previewing protected detail:", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PreviewSelectionLabel = $\"Previewing dimension:", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PreviewSelectionLabel = $\"Previewing {value.AxisTag} pinch\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HighlightGeometryPathId = value.GeometryPathId;", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HighlightGeometryPathId = value.GeometryPathIds.FirstOrDefault();", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SyncCuratedArtifactEditors(value);", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ClearCuratedArtifactEditors();", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SyncSelectedLabelTextHeightEditor(value.TextHeight);", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ClearSelectedLabelTextHeightEditor();", source, StringComparison.Ordinal);
    }

    [Fact]
    public void FloorPlanReviewViewModel_sources_review_queue_orchestration_through_the_queue_coordinator()
    {
        var viewModelPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "FloorplanFit.Desktop", "ViewModels", "FloorPlanReviewViewModel.cs");
        var source = File.ReadAllText(viewModelPath);

        Assert.Contains("FloorPlanReviewQueueCoordinator", source, StringComparison.Ordinal);
        Assert.DoesNotContain("private void NormalizeQueueExpansion()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("private void CollapseQueueSectionsExcept(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("private bool MatchesReviewQueueFilter(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("private bool MatchesReviewQueueSearch(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CuratedArtifactGroups.Add(new CuratedArtifactGroupViewModel(", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".GroupBy(item => (item.ResolvedFamily, item.ResolvedCategory))", source, StringComparison.Ordinal);
    }
}
