using System.Globalization;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls.Preview;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Desktop.ViewModels;

internal sealed class FloorPlanReviewInspectorCoordinator
{
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

    public ReviewInspectorPresentation BuildPresentation(
        CuratedPlanArtifactDto? selectedCuratedArtifact,
        WallCandidateDto? selectedCandidate,
        RoomLabelDto? selectedRoomLabel,
        OpeningLabelDto? selectedOpeningLabel,
        DimensionDto? selectedDimension,
        PinchGroupDto? selectedPinchGroup,
        string selectedPinchAxis,
        bool isPinchPlacementArmed,
        string addPinchButtonLabel,
        string excludeSelectedArtifactLabel,
        DimensionAssociationDto? selectedDimensionAssociation)
    {
        var artifactTypeLabel =
            selectedCuratedArtifact is not null ? "Curated Object" :
            selectedCandidate is not null ? "Wall Candidate" :
            selectedRoomLabel is not null ? "Room Name" :
            selectedOpeningLabel is not null ? "Door / Window Code" :
            selectedDimension is not null ? "Dimension" :
            "Nothing selected";

        var artifactTitle =
            selectedCuratedArtifact?.SourceEntityRef ??
            selectedCandidate?.SourceEntityRef ??
            selectedRoomLabel?.Text ??
            selectedOpeningLabel?.Text ??
            selectedDimension?.DisplayText ??
            "Select something from the review queue or preview.";

        var artifactSubtitle = ResolveArtifactSubtitle(
            selectedCuratedArtifact,
            selectedCandidate,
            selectedRoomLabel,
            selectedOpeningLabel,
            selectedDimension);

        var artifactDetails = ResolveArtifactDetails(
            selectedCuratedArtifact,
            selectedCandidate,
            selectedRoomLabel,
            selectedOpeningLabel,
            selectedDimension,
            selectedDimensionAssociation);

        var curatedDetectedSummary = selectedCuratedArtifact is null
            ? string.Empty
            : $"Detected: {selectedCuratedArtifact.DetectedFamily} > {selectedCuratedArtifact.DetectedCategory} > {selectedCuratedArtifact.DetectedType}";

        var curatedResolvedSummary = selectedCuratedArtifact is null
            ? string.Empty
            : $"Resolved: {selectedCuratedArtifact.ResolvedFamily} > {selectedCuratedArtifact.ResolvedCategory} > {selectedCuratedArtifact.ResolvedType}";

        var curatedDecisionSummary = selectedCuratedArtifact is null
            ? string.Empty
            : $"Decision: {selectedCuratedArtifact.DecisionState}";

        var curatedColorArgb = selectedCuratedArtifact?.ResolvedColorArgb ?? PreviewSemanticPalette.TransparentArgb;

        var artifactPositionSummary = ResolveArtifactPositionSummary(
            selectedCuratedArtifact,
            selectedRoomLabel,
            selectedOpeningLabel,
            selectedDimension,
            selectedDimensionAssociation);

        var labelTextHeightSummary = ResolveLabelTextHeightSummary(selectedRoomLabel, selectedOpeningLabel);

        var interactionHint = ResolveInteractionHint(
            selectedCuratedArtifact,
            selectedCandidate,
            selectedRoomLabel,
            selectedOpeningLabel,
            selectedPinchGroup,
            selectedPinchAxis,
            isPinchPlacementArmed,
            addPinchButtonLabel,
            excludeSelectedArtifactLabel);

        return new ReviewInspectorPresentation(
            artifactTypeLabel,
            artifactTitle,
            artifactSubtitle,
            artifactDetails,
            curatedDetectedSummary,
            curatedResolvedSummary,
            curatedDecisionSummary,
            curatedColorArgb,
            artifactPositionSummary,
            labelTextHeightSummary,
            interactionHint);
    }

    private static string ResolveArtifactSubtitle(
        CuratedPlanArtifactDto? selectedCuratedArtifact,
        WallCandidateDto? selectedCandidate,
        RoomLabelDto? selectedRoomLabel,
        OpeningLabelDto? selectedOpeningLabel,
        DimensionDto? selectedDimension)
    {
        if (selectedCuratedArtifact is not null)
        {
            return $"Layer: {selectedCuratedArtifact.SourceLayer} • {selectedCuratedArtifact.ResolvedFamily} > {selectedCuratedArtifact.ResolvedCategory} > {selectedCuratedArtifact.ResolvedType}";
        }

        if (selectedCandidate is not null)
        {
            return $"Layer: {selectedCandidate.SourceLayer} • {selectedCandidate.AssemblyHint}";
        }

        if (selectedRoomLabel is not null)
        {
            return $"Layer: {selectedRoomLabel.SourceLayer} • Ref: {selectedRoomLabel.SourceEntityRef}";
        }

        if (selectedOpeningLabel is not null)
        {
            return $"Layer: {selectedOpeningLabel.SourceLayer} • {selectedOpeningLabel.Kind}";
        }

        if (selectedDimension is not null)
        {
            return $"Layer: {selectedDimension.SourceLayer} • Ref: {selectedDimension.SourceEntityRef}";
        }

        return "Everything is included by default. Exclude only false positives before publishing.";
    }

    private static string ResolveArtifactDetails(
        CuratedPlanArtifactDto? selectedCuratedArtifact,
        WallCandidateDto? selectedCandidate,
        RoomLabelDto? selectedRoomLabel,
        OpeningLabelDto? selectedOpeningLabel,
        DimensionDto? selectedDimension,
        DimensionAssociationDto? selectedDimensionAssociation)
    {
        if (selectedCuratedArtifact is not null)
        {
            return $"Detected: {selectedCuratedArtifact.DetectedFamily} > {selectedCuratedArtifact.DetectedCategory} > {selectedCuratedArtifact.DetectedType} • Decision: {selectedCuratedArtifact.DecisionState} • Confidence: {selectedCuratedArtifact.Confidence:P0}";
        }

        if (selectedCandidate is not null)
        {
            return $"Confidence: {selectedCandidate.Confidence:P0}";
        }

        if (selectedRoomLabel is not null)
        {
            return $"Confidence: {selectedRoomLabel.Confidence:P0}";
        }

        if (selectedOpeningLabel is not null)
        {
            return $"Confidence: {selectedOpeningLabel.Confidence:P0}";
        }

        if (selectedDimension is not null)
        {
            return $"Confidence: {selectedDimension.Confidence:P0} • {selectedDimension.SourceUnit} • {ResolveDimensionAssociationSummary(selectedDimensionAssociation)}";
        }

        return string.Empty;
    }

    private static string ResolveArtifactPositionSummary(
        CuratedPlanArtifactDto? selectedCuratedArtifact,
        RoomLabelDto? selectedRoomLabel,
        OpeningLabelDto? selectedOpeningLabel,
        DimensionDto? selectedDimension,
        DimensionAssociationDto? selectedDimensionAssociation)
    {
        if (selectedRoomLabel is not null)
        {
            return $"Room label at ({selectedRoomLabel.X}, {selectedRoomLabel.Y}) mm. Detected: ({selectedRoomLabel.DetectedX ?? selectedRoomLabel.X}, {selectedRoomLabel.DetectedY ?? selectedRoomLabel.Y}) mm.";
        }

        if (selectedOpeningLabel is not null)
        {
            return $"Opening label at ({selectedOpeningLabel.X}, {selectedOpeningLabel.Y}) mm. Detected: ({selectedOpeningLabel.DetectedX ?? selectedOpeningLabel.X}, {selectedOpeningLabel.DetectedY ?? selectedOpeningLabel.Y}) mm.";
        }

        if (selectedDimension is not null)
        {
            return $"Dimension defpoints: ({selectedDimension.DefPointX}, {selectedDimension.DefPointY}) -> ({selectedDimension.DefPoint2X}, {selectedDimension.DefPoint2Y}) • text anchor: ({selectedDimension.RenderTextX ?? 0m}, {selectedDimension.RenderTextY ?? 0m}) • {ResolveDimensionAssociationDebugSummary(selectedDimensionAssociation)} • dirty: {selectedDimension.IsDirty}.";
        }

        if (selectedCuratedArtifact is not null)
        {
            return $"Curated translation dx/dy: ({selectedCuratedArtifact.TranslationDx}, {selectedCuratedArtifact.TranslationDy}) mm.";
        }

        return string.Empty;
    }

    private static string ResolveLabelTextHeightSummary(RoomLabelDto? selectedRoomLabel, OpeningLabelDto? selectedOpeningLabel)
    {
        if (selectedRoomLabel is not null)
        {
            return $"Room label text height: {FormatTextHeight(selectedRoomLabel.TextHeight)} mm. Detected: {FormatTextHeight(selectedRoomLabel.DetectedTextHeight)} mm.";
        }

        if (selectedOpeningLabel is not null)
        {
            return $"Opening label text height: {FormatTextHeight(selectedOpeningLabel.TextHeight)} mm. Detected: {FormatTextHeight(selectedOpeningLabel.DetectedTextHeight)} mm.";
        }

        return string.Empty;
    }

    private static string ResolveInteractionHint(
        CuratedPlanArtifactDto? selectedCuratedArtifact,
        WallCandidateDto? selectedCandidate,
        RoomLabelDto? selectedRoomLabel,
        OpeningLabelDto? selectedOpeningLabel,
        PinchGroupDto? selectedPinchGroup,
        string selectedPinchAxis,
        bool isPinchPlacementArmed,
        string addPinchButtonLabel,
        string excludeSelectedArtifactLabel)
    {
        if (selectedCuratedArtifact is not null)
        {
            return string.Equals(
                selectedCuratedArtifact.DecisionState,
                FloorPlanArtifactDecisionState.Excluded.ToString(),
                StringComparison.Ordinal)
                ? "This curated object is excluded from the active preview. Restore detected classification or save a new classification to bring it back into the curation set."
                : $"Selected curated object {selectedCuratedArtifact.SourceEntityRef}. Adjust Family, Category, and Type, then use 'Save Classification' to persist the correction or '{excludeSelectedArtifactLabel}' if this is a false positive.";
        }

        if (selectedRoomLabel is not null)
        {
            return $"Selected room label {selectedRoomLabel.Text}. Press '{excludeSelectedArtifactLabel}' if this label should not persist.";
        }

        if (selectedOpeningLabel is not null)
        {
            return $"Selected opening label {selectedOpeningLabel.Text}. Press '{excludeSelectedArtifactLabel}' if this label should not persist.";
        }

        if (selectedPinchGroup is null)
        {
            return "Select an artifact to inspect it, or create/select a pinch group before placing pinches. Groups tell the fit engine which area may shrink together.";
        }

        var handleHint = ResolveHandleHint(selectedPinchAxis);

        if (selectedCandidate is null)
        {
            return $"Select a line to place a {selectedPinchGroup.Name} pinch. To preview only this group, drag the green {handleHint} handle.";
        }

        if (isPinchPlacementArmed)
        {
            return $"Click the preview to place a {selectedPinchGroup.Name} pinch on {selectedCandidate.SourceEntityRef}.";
        }

        return $"Selected {selectedCandidate.SourceEntityRef}. Group: {selectedPinchGroup.Name}. Press '{addPinchButtonLabel}' to place a pinch, drag the green {handleHint} handle to preview, or use '{excludeSelectedArtifactLabel}' if this line is a false positive.";
    }

    private static string ResolveDimensionAssociationSummary(DimensionAssociationDto? selectedDimensionAssociation)
    {
        if (selectedDimensionAssociation is null)
        {
            return "Association: unresolved";
        }

        var start = selectedDimensionAssociation.StartAnchor?.SourceArtifactKind ?? "?";
        var end = selectedDimensionAssociation.EndAnchor?.SourceArtifactKind ?? "?";
        return selectedDimensionAssociation.IsFullyResolved
            ? $"Association: {start} -> {end} ({selectedDimensionAssociation.Confidence:P0})"
            : $"Association: partial ({selectedDimensionAssociation.Confidence:P0})";
    }

    private static string ResolveDimensionAssociationDebugSummary(DimensionAssociationDto? selectedDimensionAssociation)
    {
        if (selectedDimensionAssociation is null)
        {
            return "assoc: unresolved";
        }

        var start = selectedDimensionAssociation.StartAnchor is null
            ? "start=?"
            : $"start={selectedDimensionAssociation.StartAnchor.EdgeKey}/{selectedDimensionAssociation.StartAnchor.EdgeAnchorKind}";
        var end = selectedDimensionAssociation.EndAnchor is null
            ? "end=?"
            : $"end={selectedDimensionAssociation.EndAnchor.EdgeKey}/{selectedDimensionAssociation.EndAnchor.EdgeAnchorKind}";
        return $"assoc: {start} • {end}";
    }

    private static string FormatTextHeight(decimal? textHeight)
    {
        return textHeight?.ToString(CultureInfo.InvariantCulture) ?? "Auto";
    }

    private static string ResolveHandleHint(string selectedPinchAxis)
    {
        return string.Equals(selectedPinchAxis, nameof(PinchAxisTag.Height), StringComparison.OrdinalIgnoreCase)
            ? "top or bottom"
            : "left or right";
    }
}

internal readonly record struct ReviewInspectorPresentation(
    string ArtifactTypeLabel,
    string ArtifactTitle,
    string ArtifactSubtitle,
    string ArtifactDetails,
    string CuratedArtifactDetectedSummary,
    string CuratedArtifactResolvedSummary,
    string CuratedArtifactDecisionSummary,
    string CuratedArtifactColorArgb,
    string ArtifactPositionSummary,
    string LabelTextHeightSummary,
    string InteractionHint);
