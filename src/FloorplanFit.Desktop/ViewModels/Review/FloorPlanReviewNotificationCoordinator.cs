namespace FloorplanFit.Desktop.ViewModels;

internal sealed class FloorPlanReviewNotificationCoordinator
{
    private static readonly string[] QueuePropertyNames =
    [
        nameof(FloorPlanReviewViewModel.CuratedObjectCount),
        nameof(FloorPlanReviewViewModel.VisibleQueueItemCount),
        nameof(FloorPlanReviewViewModel.TotalQueueItemCount),
        nameof(FloorPlanReviewViewModel.AdjustedQueueItemCount),
        nameof(FloorPlanReviewViewModel.QueueSummary),
        nameof(FloorPlanReviewViewModel.QueueInsightsSummary),
        nameof(FloorPlanReviewViewModel.StructureSectionTitle),
        nameof(FloorPlanReviewViewModel.RoomNamesSectionTitle),
        nameof(FloorPlanReviewViewModel.OpeningCodesSectionTitle),
        nameof(FloorPlanReviewViewModel.DimensionsSectionTitle),
        nameof(FloorPlanReviewViewModel.CuratedObjectsSectionTitle),
        nameof(FloorPlanReviewViewModel.HasVisibleWallCandidates),
        nameof(FloorPlanReviewViewModel.HasVisibleRoomLabels),
        nameof(FloorPlanReviewViewModel.HasVisibleOpeningLabels),
        nameof(FloorPlanReviewViewModel.HasVisibleDimensions),
        nameof(FloorPlanReviewViewModel.HasVisibleCuratedArtifactGroups)
    ];

    private static readonly string[] UxPropertyNames =
    [
        nameof(FloorPlanReviewViewModel.AddPinchButtonLabel),
        nameof(FloorPlanReviewViewModel.AddManualWallLineButtonLabel),
        nameof(FloorPlanReviewViewModel.SelectedPinchGroupId),
        nameof(FloorPlanReviewViewModel.InteractionHint),
        nameof(FloorPlanReviewViewModel.HasSelectedArtifact),
        nameof(FloorPlanReviewViewModel.HasSelectedCuratedArtifact),
        nameof(FloorPlanReviewViewModel.CanUsePositionTool),
        nameof(FloorPlanReviewViewModel.CanUseTextTool),
        nameof(FloorPlanReviewViewModel.CanUseClassificationTool),
        nameof(FloorPlanReviewViewModel.CanUseActionsTool),
        nameof(FloorPlanReviewViewModel.IsOverviewToolSelected),
        nameof(FloorPlanReviewViewModel.IsPositionToolSelected),
        nameof(FloorPlanReviewViewModel.IsTextToolSelected),
        nameof(FloorPlanReviewViewModel.IsClassificationToolSelected),
        nameof(FloorPlanReviewViewModel.IsActionsToolSelected),
        nameof(FloorPlanReviewViewModel.IsFitToolSelected),
        nameof(FloorPlanReviewViewModel.CanSaveSelectedCuratedArtifactClassification),
        nameof(FloorPlanReviewViewModel.CanRestoreSelectedCuratedArtifactClassification),
        nameof(FloorPlanReviewViewModel.SelectedArtifactTypeLabel),
        nameof(FloorPlanReviewViewModel.SelectedArtifactTitle),
        nameof(FloorPlanReviewViewModel.SelectedArtifactSubtitle),
        nameof(FloorPlanReviewViewModel.SelectedArtifactDetails),
        nameof(FloorPlanReviewViewModel.SelectedCuratedArtifactDetectedSummary),
        nameof(FloorPlanReviewViewModel.SelectedCuratedArtifactResolvedSummary),
        nameof(FloorPlanReviewViewModel.SelectedCuratedArtifactDecisionSummary),
        nameof(FloorPlanReviewViewModel.SelectedCuratedArtifactColorArgb),
        nameof(FloorPlanReviewViewModel.CanRestoreSelectedArtifactPosition),
        nameof(FloorPlanReviewViewModel.SelectedArtifactPositionSummary),
        nameof(FloorPlanReviewViewModel.HasSelectedResizableLabel),
        nameof(FloorPlanReviewViewModel.CanSaveSelectedLabelTextHeight),
        nameof(FloorPlanReviewViewModel.CanRestoreSelectedLabelTextHeight),
        nameof(FloorPlanReviewViewModel.SelectedLabelTextHeightSummary),
        nameof(FloorPlanReviewViewModel.SelectedRoomLabelId),
        nameof(FloorPlanReviewViewModel.SelectedOpeningLabelId),
        nameof(FloorPlanReviewViewModel.SelectedDimensionId),
        nameof(FloorPlanReviewViewModel.ExcludeSelectedArtifactLabel)
    ];

    public string NormalizeSelectedInspectorTool(
        string selectedInspectorTool,
        bool canUsePositionTool,
        bool canUseTextTool,
        bool canUseClassificationTool,
        bool canUseActionsTool,
        string overviewTool,
        string positionTool,
        string textTool,
        string classificationTool,
        string actionsTool)
    {
        if (string.Equals(selectedInspectorTool, positionTool, StringComparison.Ordinal) && !canUsePositionTool)
        {
            return overviewTool;
        }

        if (string.Equals(selectedInspectorTool, textTool, StringComparison.Ordinal) && !canUseTextTool)
        {
            return overviewTool;
        }

        if (string.Equals(selectedInspectorTool, classificationTool, StringComparison.Ordinal) && !canUseClassificationTool)
        {
            return overviewTool;
        }

        if (string.Equals(selectedInspectorTool, actionsTool, StringComparison.Ordinal) && !canUseActionsTool)
        {
            return overviewTool;
        }

        return selectedInspectorTool;
    }

    public IReadOnlyList<string> GetQueuePropertyNames()
    {
        return QueuePropertyNames;
    }

    public IReadOnlyList<string> GetUxPropertyNames()
    {
        return UxPropertyNames;
    }
}
