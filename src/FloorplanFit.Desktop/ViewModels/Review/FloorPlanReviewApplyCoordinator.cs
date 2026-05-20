using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.ViewModels;

internal sealed class FloorPlanReviewApplyCoordinator
{
    public ReviewSessionApplyPlan BuildSessionApplyPlan(
        ReviewSessionProjection session,
        ReviewSelectionSnapshot selection,
        FloorPlanReviewSelectionCoordinator selectionCoordinator)
    {
        var resolvedSelection = selectionCoordinator.ResolveSelectionSnapshot(
            selection,
            session.WallCandidates,
            session.PinchMarkers,
            session.PinchGroups,
            session.RoomLabels,
            session.OpeningLabels,
            session.Dimensions,
            session.CuratedPlanArtifacts);

        return new ReviewSessionApplyPlan(session, resolvedSelection);
    }

    public SelectionPresentationApplyPlan BuildSelectionPresentationApplyPlan(
        SelectionPresentationOutcome outcome,
        IReadOnlyList<PinchGroupDto> pinchGroups,
        IReadOnlyList<WallCandidateDto> wallCandidates)
    {
        var selectedPinchGroup = outcome.SelectedPinchGroupId is Guid pinchGroupId
            ? pinchGroups.FirstOrDefault(item => item.PinchGroupId == pinchGroupId)
            : null;
        var linkedCandidate = outcome.LinkedCandidateId is Guid candidateId
            ? wallCandidates.FirstOrDefault(item => item.CandidateId == candidateId)
            : null;

        return new SelectionPresentationApplyPlan(
            outcome.ClearSelections,
            selectedPinchGroup,
            linkedCandidate,
            outcome.HasHighlightGeometryPathId,
            outcome.HighlightGeometryPathId,
            outcome.HasPreviewSelectionLabel,
            outcome.PreviewSelectionLabel,
            outcome.SelectedPinchAxis,
            outcome.CuratedArtifactEditorAction,
            outcome.CuratedArtifactEditorArtifact,
            outcome.LabelTextHeightEditorAction,
            outcome.LabelTextHeight);
    }
}

internal readonly record struct ReviewSessionApplyPlan(
    ReviewSessionProjection Session,
    ReviewSelectionReplayResult SelectionReplay);

internal readonly record struct SelectionPresentationApplyPlan(
    ReviewSelectionSlots ClearSelections,
    PinchGroupDto? SelectedPinchGroup,
    WallCandidateDto? LinkedCandidate,
    bool HasHighlightGeometryPathId,
    Guid? HighlightGeometryPathId,
    bool HasPreviewSelectionLabel,
    string? PreviewSelectionLabel,
    string? SelectedPinchAxis,
    CuratedArtifactEditorAction CuratedArtifactEditorAction,
    CuratedPlanArtifactDto? CuratedArtifactEditorArtifact,
    LabelTextHeightEditorAction LabelTextHeightEditorAction,
    decimal? LabelTextHeight);
