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
