using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Desktop.ViewModels;

internal sealed class FloorPlanReviewSelectionCoordinator
{
    public PreviewHitSelectionResult ResolvePreviewHit(
        Guid geometryPathId,
        IReadOnlyList<CuratedPlanArtifactDto> visibleCuratedPlanArtifacts,
        IReadOnlyList<ProtectedDetailAssemblyDto> protectedDetailAssemblies,
        IReadOnlyList<FixedPlanComponentDto> fixedPlanComponents,
        IReadOnlyList<OpeningCandidateDto> openingCandidates,
        IReadOnlyList<WallCandidateDto> wallCandidates)
    {
        var curatedArtifact = visibleCuratedPlanArtifacts.FirstOrDefault(item => item.GeometryPathIds.Contains(geometryPathId));
        if (curatedArtifact is not null)
        {
            return new PreviewHitSelectionResult(
                Candidate: null,
                CuratedArtifact: curatedArtifact,
                OpeningCandidate: null,
                FixedPlanComponent: null,
                ProtectedDetailAssembly: null,
                ClearSelectedPinchMarker: false);
        }

        var protectedDetail = protectedDetailAssemblies.FirstOrDefault(item => item.GeometryPathIds.Contains(geometryPathId));
        if (protectedDetail is not null)
        {
            return new PreviewHitSelectionResult(
                Candidate: null,
                CuratedArtifact: null,
                OpeningCandidate: null,
                FixedPlanComponent: null,
                ProtectedDetailAssembly: protectedDetail,
                ClearSelectedPinchMarker: false);
        }

        var fixedComponent = fixedPlanComponents.FirstOrDefault(item => item.GeometryPathIds.Contains(geometryPathId));
        if (fixedComponent is not null)
        {
            return new PreviewHitSelectionResult(
                Candidate: null,
                CuratedArtifact: null,
                OpeningCandidate: null,
                FixedPlanComponent: fixedComponent,
                ProtectedDetailAssembly: null,
                ClearSelectedPinchMarker: false);
        }

        var opening = openingCandidates.FirstOrDefault(item => item.GeometryPathId == geometryPathId);
        if (opening is not null)
        {
            return new PreviewHitSelectionResult(
                Candidate: null,
                CuratedArtifact: null,
                OpeningCandidate: opening,
                FixedPlanComponent: null,
                ProtectedDetailAssembly: null,
                ClearSelectedPinchMarker: false);
        }

        var candidate = wallCandidates.FirstOrDefault(item => item.GeometryPathId == geometryPathId);
        return new PreviewHitSelectionResult(
            Candidate: candidate,
            CuratedArtifact: null,
            OpeningCandidate: null,
            FixedPlanComponent: null,
            ProtectedDetailAssembly: null,
            ClearSelectedPinchMarker: candidate is not null);
    }

    public ReviewSelectionSnapshot CaptureSelection(
        WallCandidateDto? selectedCandidate,
        PinchMarkerDto? selectedPinchMarker,
        PinchGroupDto? selectedPinchGroup,
        RoomLabelDto? selectedRoomLabel,
        OpeningLabelDto? selectedOpeningLabel,
        DimensionDto? selectedDimension,
        CuratedPlanArtifactDto? selectedCuratedArtifact)
    {
        return new ReviewSelectionSnapshot(
            selectedCandidate?.CandidateId,
            selectedPinchMarker?.PinchMarkerId,
            selectedPinchGroup?.PinchGroupId,
            selectedRoomLabel?.RoomLabelId,
            selectedOpeningLabel?.OpeningLabelId,
            selectedDimension?.DimensionId,
            selectedCuratedArtifact?.SourceArtifactKind,
            selectedCuratedArtifact?.SourceArtifactId);
    }

    public ReviewSelectionSnapshot BuildSelectionSnapshotForMovedArtifact(
        Guid? selectedPinchGroupId,
        FloorPlanPreviewControl.MovableArtifactMovedEventArgs movement)
    {
        return movement.PositionMode == FloorPlanArtifactPositionMode.AbsolutePoint
            ? string.Equals(movement.SourceArtifactKind, FloorPlanArtifactPositionSourceKinds.RoomLabel, StringComparison.Ordinal)
                ? new ReviewSelectionSnapshot(
                    null,
                    null,
                    selectedPinchGroupId,
                    movement.SourceArtifactId,
                    null,
                    null,
                    null,
                    null)
                : new ReviewSelectionSnapshot(
                    null,
                    null,
                    selectedPinchGroupId,
                    null,
                    movement.SourceArtifactId,
                    null,
                    null,
                    null)
            : new ReviewSelectionSnapshot(
                null,
                null,
                selectedPinchGroupId,
                null,
                null,
                null,
                movement.SourceArtifactKind,
                movement.SourceArtifactId);
    }

    public ReviewSelectionReplayResult ResolveSelectionSnapshot(
        ReviewSelectionSnapshot selection,
        IReadOnlyList<WallCandidateDto> wallCandidates,
        IReadOnlyList<PinchMarkerDto> pinchMarkers,
        IReadOnlyList<PinchGroupDto> pinchGroups,
        IReadOnlyList<RoomLabelDto> roomLabels,
        IReadOnlyList<OpeningLabelDto> openingLabels,
        IReadOnlyList<DimensionDto> dimensions,
        IReadOnlyList<CuratedPlanArtifactDto> curatedPlanArtifacts)
    {
        var candidate = selection.CandidateId is null
            ? wallCandidates.FirstOrDefault()
            : wallCandidates.FirstOrDefault(item => item.CandidateId == selection.CandidateId) ?? wallCandidates.FirstOrDefault();

        var pinchMarker = selection.PinchMarkerId is null
            ? null
            : pinchMarkers.FirstOrDefault(item => item.PinchMarkerId == selection.PinchMarkerId);

        var pinchGroup = selection.PinchGroupId is null
            ? pinchMarker is null
                ? pinchGroups.FirstOrDefault()
                : pinchGroups.FirstOrDefault(item => item.PinchGroupId == pinchMarker.PinchGroupId) ?? pinchGroups.FirstOrDefault()
            : pinchGroups.FirstOrDefault(item => item.PinchGroupId == selection.PinchGroupId) ?? pinchGroups.FirstOrDefault();

        CuratedPlanArtifactDto? curatedArtifact = null;
        RoomLabelDto? roomLabel = null;
        OpeningLabelDto? openingLabel = null;
        DimensionDto? dimension = null;

        if (selection.ArtifactSourceId is Guid artifactSourceId &&
            !string.IsNullOrWhiteSpace(selection.ArtifactSourceKind))
        {
            curatedArtifact = curatedPlanArtifacts.FirstOrDefault(item =>
                item.SourceArtifactId == artifactSourceId &&
                string.Equals(item.SourceArtifactKind, selection.ArtifactSourceKind, StringComparison.Ordinal));
        }
        else if (selection.OpeningLabelId is Guid openingLabelId)
        {
            openingLabel = openingLabels.FirstOrDefault(item => item.OpeningLabelId == openingLabelId);
        }
        else if (selection.DimensionId is Guid dimensionId)
        {
            dimension = dimensions.FirstOrDefault(item => item.DimensionId == dimensionId);
        }
        else if (selection.RoomLabelId is Guid roomLabelId)
        {
            roomLabel = roomLabels.FirstOrDefault(item => item.RoomLabelId == roomLabelId);
        }

        return new ReviewSelectionReplayResult(
            candidate,
            pinchMarker,
            pinchGroup,
            roomLabel,
            openingLabel,
            dimension,
            curatedArtifact);
    }

    public SelectionPresentationOutcome ResolveCandidatePresentation(
        WallCandidateDto? candidate,
        PinchMarkerDto? selectedPinchMarker,
        string selectedPinchAxis)
    {
        return candidate is null
            ? new SelectionPresentationOutcome(
                ReviewSelectionSlots.None,
                HasHighlightGeometryPathId: true,
                HighlightGeometryPathId: selectedPinchMarker?.GeometryPathId,
                HasPreviewSelectionLabel: true,
                PreviewSelectionLabel: selectedPinchMarker is null
                    ? "Previewing floor plan"
                    : $"Previewing pinch marker on {selectedPinchAxis}",
                SelectedPinchAxis: null,
                SelectedPinchGroupId: null,
                LinkedCandidateId: null,
                CuratedArtifactEditorAction: CuratedArtifactEditorAction.None,
                CuratedArtifactEditorArtifact: null,
                LabelTextHeightEditorAction: LabelTextHeightEditorAction.None,
                LabelTextHeight: null)
            : new SelectionPresentationOutcome(
                ReviewSelectionSlots.CuratedArtifact |
                ReviewSelectionSlots.RoomLabel |
                ReviewSelectionSlots.OpeningCandidate |
                ReviewSelectionSlots.OpeningLabel |
                ReviewSelectionSlots.Dimension |
                ReviewSelectionSlots.FixedPlanComponent |
                ReviewSelectionSlots.ProtectedDetailAssembly,
                HasHighlightGeometryPathId: true,
                HighlightGeometryPathId: candidate.GeometryPathId,
                HasPreviewSelectionLabel: true,
                PreviewSelectionLabel: $"Previewing candidate: {candidate.SourceEntityRef}",
                SelectedPinchAxis: null,
                SelectedPinchGroupId: null,
                LinkedCandidateId: null,
                CuratedArtifactEditorAction: CuratedArtifactEditorAction.None,
                CuratedArtifactEditorArtifact: null,
                LabelTextHeightEditorAction: LabelTextHeightEditorAction.None,
                LabelTextHeight: null);
    }

    public SelectionPresentationOutcome ResolveCuratedArtifactPresentation(CuratedPlanArtifactDto? curatedArtifact)
    {
        return curatedArtifact is null
            ? new SelectionPresentationOutcome(
                ReviewSelectionSlots.None,
                HasHighlightGeometryPathId: false,
                HighlightGeometryPathId: null,
                HasPreviewSelectionLabel: false,
                PreviewSelectionLabel: null,
                SelectedPinchAxis: null,
                SelectedPinchGroupId: null,
                LinkedCandidateId: null,
                CuratedArtifactEditorAction: CuratedArtifactEditorAction.Clear,
                CuratedArtifactEditorArtifact: null,
                LabelTextHeightEditorAction: LabelTextHeightEditorAction.None,
                LabelTextHeight: null)
            : new SelectionPresentationOutcome(
                ReviewSelectionSlots.Candidate |
                ReviewSelectionSlots.RoomLabel |
                ReviewSelectionSlots.PinchMarker |
                ReviewSelectionSlots.OpeningCandidate |
                ReviewSelectionSlots.OpeningLabel |
                ReviewSelectionSlots.Dimension |
                ReviewSelectionSlots.FixedPlanComponent |
                ReviewSelectionSlots.ProtectedDetailAssembly,
                HasHighlightGeometryPathId: true,
                HighlightGeometryPathId: curatedArtifact.GeometryPathIds.FirstOrDefault(),
                HasPreviewSelectionLabel: true,
                PreviewSelectionLabel: $"Previewing curated object: {curatedArtifact.SourceEntityRef}",
                SelectedPinchAxis: null,
                SelectedPinchGroupId: null,
                LinkedCandidateId: null,
                CuratedArtifactEditorAction: CuratedArtifactEditorAction.Sync,
                CuratedArtifactEditorArtifact: curatedArtifact,
                LabelTextHeightEditorAction: LabelTextHeightEditorAction.None,
                LabelTextHeight: null);
    }

    public SelectionPresentationOutcome ResolveRoomLabelPresentation(RoomLabelDto? roomLabel)
    {
        return roomLabel is null
            ? new SelectionPresentationOutcome(
                ReviewSelectionSlots.None,
                HasHighlightGeometryPathId: false,
                HighlightGeometryPathId: null,
                HasPreviewSelectionLabel: false,
                PreviewSelectionLabel: null,
                SelectedPinchAxis: null,
                SelectedPinchGroupId: null,
                LinkedCandidateId: null,
                CuratedArtifactEditorAction: CuratedArtifactEditorAction.None,
                CuratedArtifactEditorArtifact: null,
                LabelTextHeightEditorAction: LabelTextHeightEditorAction.Clear,
                LabelTextHeight: null)
            : new SelectionPresentationOutcome(
                ReviewSelectionSlots.CuratedArtifact |
                ReviewSelectionSlots.Candidate |
                ReviewSelectionSlots.PinchMarker |
                ReviewSelectionSlots.OpeningCandidate |
                ReviewSelectionSlots.OpeningLabel |
                ReviewSelectionSlots.Dimension |
                ReviewSelectionSlots.FixedPlanComponent |
                ReviewSelectionSlots.ProtectedDetailAssembly,
                HasHighlightGeometryPathId: true,
                HighlightGeometryPathId: null,
                HasPreviewSelectionLabel: true,
                PreviewSelectionLabel: $"Previewing room label: {roomLabel.Text}",
                SelectedPinchAxis: null,
                SelectedPinchGroupId: null,
                LinkedCandidateId: null,
                CuratedArtifactEditorAction: CuratedArtifactEditorAction.None,
                CuratedArtifactEditorArtifact: null,
                LabelTextHeightEditorAction: LabelTextHeightEditorAction.Sync,
                LabelTextHeight: roomLabel.TextHeight);
    }

    public SelectionPresentationOutcome ResolvePinchMarkerPresentation(PinchMarkerDto? pinchMarker)
    {
        return pinchMarker is null
            ? SelectionPresentationOutcome.Empty
            : new SelectionPresentationOutcome(
                ReviewSelectionSlots.None,
                HasHighlightGeometryPathId: true,
                HighlightGeometryPathId: pinchMarker.GeometryPathId,
                HasPreviewSelectionLabel: true,
                PreviewSelectionLabel: $"Previewing {pinchMarker.AxisTag} pinch",
                SelectedPinchAxis: pinchMarker.AxisTag,
                SelectedPinchGroupId: pinchMarker.PinchGroupId,
                LinkedCandidateId: pinchMarker.SourceCandidateId,
                CuratedArtifactEditorAction: CuratedArtifactEditorAction.None,
                CuratedArtifactEditorArtifact: null,
                LabelTextHeightEditorAction: LabelTextHeightEditorAction.None,
                LabelTextHeight: null);
    }

    public SelectionPresentationOutcome ResolveOpeningCandidatePresentation(OpeningCandidateDto? openingCandidate)
    {
        return openingCandidate is null
            ? SelectionPresentationOutcome.Empty
            : new SelectionPresentationOutcome(
                ReviewSelectionSlots.CuratedArtifact |
                ReviewSelectionSlots.Candidate |
                ReviewSelectionSlots.RoomLabel |
                ReviewSelectionSlots.PinchMarker |
                ReviewSelectionSlots.OpeningLabel |
                ReviewSelectionSlots.Dimension |
                ReviewSelectionSlots.FixedPlanComponent |
                ReviewSelectionSlots.ProtectedDetailAssembly,
                HasHighlightGeometryPathId: true,
                HighlightGeometryPathId: openingCandidate.GeometryPathId,
                HasPreviewSelectionLabel: true,
                PreviewSelectionLabel: $"Previewing opening: {openingCandidate.SourceEntityRef}",
                SelectedPinchAxis: null,
                SelectedPinchGroupId: null,
                LinkedCandidateId: null,
                CuratedArtifactEditorAction: CuratedArtifactEditorAction.None,
                CuratedArtifactEditorArtifact: null,
                LabelTextHeightEditorAction: LabelTextHeightEditorAction.None,
                LabelTextHeight: null);
    }

    public SelectionPresentationOutcome ResolveOpeningLabelPresentation(OpeningLabelDto? openingLabel)
    {
        return openingLabel is null
            ? new SelectionPresentationOutcome(
                ReviewSelectionSlots.None,
                HasHighlightGeometryPathId: false,
                HighlightGeometryPathId: null,
                HasPreviewSelectionLabel: false,
                PreviewSelectionLabel: null,
                SelectedPinchAxis: null,
                SelectedPinchGroupId: null,
                LinkedCandidateId: null,
                CuratedArtifactEditorAction: CuratedArtifactEditorAction.None,
                CuratedArtifactEditorArtifact: null,
                LabelTextHeightEditorAction: LabelTextHeightEditorAction.Clear,
                LabelTextHeight: null)
            : new SelectionPresentationOutcome(
                ReviewSelectionSlots.CuratedArtifact |
                ReviewSelectionSlots.Candidate |
                ReviewSelectionSlots.RoomLabel |
                ReviewSelectionSlots.PinchMarker |
                ReviewSelectionSlots.OpeningCandidate |
                ReviewSelectionSlots.Dimension |
                ReviewSelectionSlots.FixedPlanComponent |
                ReviewSelectionSlots.ProtectedDetailAssembly,
                HasHighlightGeometryPathId: true,
                HighlightGeometryPathId: null,
                HasPreviewSelectionLabel: true,
                PreviewSelectionLabel: $"Previewing opening label: {openingLabel.Text}",
                SelectedPinchAxis: null,
                SelectedPinchGroupId: null,
                LinkedCandidateId: null,
                CuratedArtifactEditorAction: CuratedArtifactEditorAction.None,
                CuratedArtifactEditorArtifact: null,
                LabelTextHeightEditorAction: LabelTextHeightEditorAction.Sync,
                LabelTextHeight: openingLabel.TextHeight);
    }

    public SelectionPresentationOutcome ResolveFixedPlanComponentPresentation(FixedPlanComponentDto? fixedPlanComponent)
    {
        return fixedPlanComponent is null
            ? SelectionPresentationOutcome.Empty
            : new SelectionPresentationOutcome(
                ReviewSelectionSlots.CuratedArtifact |
                ReviewSelectionSlots.Candidate |
                ReviewSelectionSlots.RoomLabel |
                ReviewSelectionSlots.PinchMarker |
                ReviewSelectionSlots.OpeningCandidate |
                ReviewSelectionSlots.OpeningLabel |
                ReviewSelectionSlots.Dimension |
                ReviewSelectionSlots.ProtectedDetailAssembly,
                HasHighlightGeometryPathId: true,
                HighlightGeometryPathId: fixedPlanComponent.GeometryPathIds.FirstOrDefault(),
                HasPreviewSelectionLabel: true,
                PreviewSelectionLabel: $"Previewing fixed component: {fixedPlanComponent.SourceEntityRef}",
                SelectedPinchAxis: null,
                SelectedPinchGroupId: null,
                LinkedCandidateId: null,
                CuratedArtifactEditorAction: CuratedArtifactEditorAction.None,
                CuratedArtifactEditorArtifact: null,
                LabelTextHeightEditorAction: LabelTextHeightEditorAction.None,
                LabelTextHeight: null);
    }

    public SelectionPresentationOutcome ResolveProtectedDetailAssemblyPresentation(ProtectedDetailAssemblyDto? protectedDetailAssembly)
    {
        return protectedDetailAssembly is null
            ? SelectionPresentationOutcome.Empty
            : new SelectionPresentationOutcome(
                ReviewSelectionSlots.CuratedArtifact |
                ReviewSelectionSlots.Candidate |
                ReviewSelectionSlots.RoomLabel |
                ReviewSelectionSlots.PinchMarker |
                ReviewSelectionSlots.OpeningCandidate |
                ReviewSelectionSlots.OpeningLabel |
                ReviewSelectionSlots.FixedPlanComponent |
                ReviewSelectionSlots.Dimension,
                HasHighlightGeometryPathId: true,
                HighlightGeometryPathId: protectedDetailAssembly.GeometryPathIds.FirstOrDefault(),
                HasPreviewSelectionLabel: true,
                PreviewSelectionLabel: $"Previewing protected detail: {protectedDetailAssembly.SourceEntityRef}",
                SelectedPinchAxis: null,
                SelectedPinchGroupId: null,
                LinkedCandidateId: null,
                CuratedArtifactEditorAction: CuratedArtifactEditorAction.None,
                CuratedArtifactEditorArtifact: null,
                LabelTextHeightEditorAction: LabelTextHeightEditorAction.None,
                LabelTextHeight: null);
    }

    public SelectionPresentationOutcome ResolveDimensionPresentation(DimensionDto? dimension)
    {
        return dimension is null
            ? SelectionPresentationOutcome.Empty
            : new SelectionPresentationOutcome(
                ReviewSelectionSlots.CuratedArtifact |
                ReviewSelectionSlots.Candidate |
                ReviewSelectionSlots.RoomLabel |
                ReviewSelectionSlots.PinchMarker |
                ReviewSelectionSlots.OpeningCandidate |
                ReviewSelectionSlots.OpeningLabel |
                ReviewSelectionSlots.FixedPlanComponent |
                ReviewSelectionSlots.ProtectedDetailAssembly,
                HasHighlightGeometryPathId: true,
                HighlightGeometryPathId: null,
                HasPreviewSelectionLabel: true,
                PreviewSelectionLabel: $"Previewing dimension: {dimension.DisplayText}",
                SelectedPinchAxis: null,
                SelectedPinchGroupId: null,
                LinkedCandidateId: null,
                CuratedArtifactEditorAction: CuratedArtifactEditorAction.None,
                CuratedArtifactEditorArtifact: null,
                LabelTextHeightEditorAction: LabelTextHeightEditorAction.None,
                LabelTextHeight: null);
    }
}

internal readonly record struct PreviewHitSelectionResult(
    WallCandidateDto? Candidate,
    CuratedPlanArtifactDto? CuratedArtifact,
    OpeningCandidateDto? OpeningCandidate,
    FixedPlanComponentDto? FixedPlanComponent,
    ProtectedDetailAssemblyDto? ProtectedDetailAssembly,
    bool ClearSelectedPinchMarker)
{
    public bool IsMatched =>
        Candidate is not null ||
        CuratedArtifact is not null ||
        OpeningCandidate is not null ||
        FixedPlanComponent is not null ||
        ProtectedDetailAssembly is not null;
}

internal readonly record struct ReviewSelectionReplayResult(
    WallCandidateDto? Candidate,
    PinchMarkerDto? PinchMarker,
    PinchGroupDto? PinchGroup,
    RoomLabelDto? RoomLabel,
    OpeningLabelDto? OpeningLabel,
    DimensionDto? Dimension,
    CuratedPlanArtifactDto? CuratedArtifact);

internal readonly record struct ReviewSelectionSnapshot(
    Guid? CandidateId,
    Guid? PinchMarkerId,
    Guid? PinchGroupId,
    Guid? RoomLabelId,
    Guid? OpeningLabelId,
    Guid? DimensionId,
    string? ArtifactSourceKind,
    Guid? ArtifactSourceId)
{
    public static ReviewSelectionSnapshot Empty { get; } = new(
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null);
}

[Flags]
internal enum ReviewSelectionSlots
{
    None = 0,
    Candidate = 1 << 0,
    RoomLabel = 1 << 1,
    PinchMarker = 1 << 2,
    OpeningCandidate = 1 << 3,
    OpeningLabel = 1 << 4,
    Dimension = 1 << 5,
    FixedPlanComponent = 1 << 6,
    ProtectedDetailAssembly = 1 << 7,
    CuratedArtifact = 1 << 8
}

internal enum CuratedArtifactEditorAction
{
    None,
    Sync,
    Clear
}

internal enum LabelTextHeightEditorAction
{
    None,
    Sync,
    Clear
}

internal readonly record struct SelectionPresentationOutcome(
    ReviewSelectionSlots ClearSelections,
    bool HasHighlightGeometryPathId,
    Guid? HighlightGeometryPathId,
    bool HasPreviewSelectionLabel,
    string? PreviewSelectionLabel,
    string? SelectedPinchAxis,
    Guid? SelectedPinchGroupId,
    Guid? LinkedCandidateId,
    CuratedArtifactEditorAction CuratedArtifactEditorAction,
    CuratedPlanArtifactDto? CuratedArtifactEditorArtifact,
    LabelTextHeightEditorAction LabelTextHeightEditorAction,
    decimal? LabelTextHeight)
{
    public static SelectionPresentationOutcome Empty { get; } = new(
        ReviewSelectionSlots.None,
        HasHighlightGeometryPathId: false,
        HighlightGeometryPathId: null,
        HasPreviewSelectionLabel: false,
        PreviewSelectionLabel: null,
        SelectedPinchAxis: null,
        SelectedPinchGroupId: null,
        LinkedCandidateId: null,
        CuratedArtifactEditorAction: CuratedArtifactEditorAction.None,
        CuratedArtifactEditorArtifact: null,
        LabelTextHeightEditorAction: LabelTextHeightEditorAction.None,
        LabelTextHeight: null);
}
