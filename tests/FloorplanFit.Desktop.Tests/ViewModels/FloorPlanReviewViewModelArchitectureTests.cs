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

    [Fact]
    public void FloorPlanReviewViewModel_sources_inspector_presentation_through_the_inspector_coordinator()
    {
        var viewModelPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "FloorplanFit.Desktop", "ViewModels", "FloorPlanReviewViewModel.cs");
        var source = File.ReadAllText(viewModelPath);

        Assert.Contains("FloorPlanReviewInspectorCoordinator", source, StringComparison.Ordinal);
        Assert.DoesNotContain("return \"Everything is included by default. Exclude only false positives before publishing.\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("return string.Empty;", source, StringComparison.Ordinal);
        Assert.DoesNotContain("return $\"Selected {SelectedCandidate.SourceEntityRef}. Group: {SelectedPinchGroup.Name}. Press '{AddPinchButtonLabel}' to place a pinch, drag the green {GetHandleHint()} handle to preview, or use '{ExcludeSelectedArtifactLabel}' if this line is a false positive.\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("return $\"Select a line to place a {SelectedPinchGroup.Name} pinch. To preview only this group, drag the green {GetHandleHint()} handle.\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("return $\"Click the preview to place a {SelectedPinchGroup.Name} pinch on {SelectedCandidate.SourceEntityRef}.\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("private void NormalizeSelectedInspectorTool()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("private string GetHandleHint()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("private static string FormatTextHeight(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("private string ResolveSelectedDimensionAssociationSummary()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("private string ResolveSelectedDimensionAssociationDebugSummary()", source, StringComparison.Ordinal);
    }

    [Fact]
    public void FloorPlanReviewViewModel_sources_session_query_and_projection_through_the_session_coordinator()
    {
        var viewModelPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "FloorplanFit.Desktop", "ViewModels", "FloorPlanReviewViewModel.cs");
        var source = File.ReadAllText(viewModelPath);

        Assert.Contains("FloorPlanReviewSessionCoordinator", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService<OpenFloorPlanReviewSessionHandler>()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService<GetFloorPlanReviewSessionHandler>()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("private IReadOnlyList<CuratedPlanArtifactDto> ResolveCuratedArtifacts(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("private void RefreshVisibleCuratedArtifacts()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DraftCurationId = response.DraftCurationId;", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DraftCurationId = string.Equals(session.Status, \"Published\"", source, StringComparison.Ordinal);
    }
}
