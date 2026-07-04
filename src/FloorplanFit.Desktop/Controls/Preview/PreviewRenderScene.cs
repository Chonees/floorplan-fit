using Avalonia;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Desktop.Controls.Preview;

internal sealed record PreviewRenderScene(
    Rect Bounds,
    PinchAxisTag? AxisTag,
    bool IsPinchPlacementArmed,
    FloorPlanPreviewGeometry.PreviewViewport? Viewport,
    IReadOnlyList<GeometryPathDto> PreviewGeometry,
    IReadOnlyList<RoomLabelDto> RoomLabels,
    IReadOnlyList<OpeningLabelDto> OpeningLabels,
    IReadOnlyList<DimensionDto> Dimensions,
    IReadOnlyList<Guid>? ChangedNumberDimensionIds,
    bool AreDimensionsVisible,
    PreviewArtifactGeometryIndex ArtifactIndex,
    IReadOnlyList<OpeningCandidateDto>? OpeningCandidates,
    IReadOnlyList<FixedPlanComponentDto>? FixedPlanComponents,
    IReadOnlyList<ProtectedDetailAssemblyDto>? ProtectedDetailAssemblies,
    IReadOnlyList<CuratedPlanArtifactDto>? CuratedPlanArtifacts,
    IReadOnlyList<PinchMarkerDto>? PinchMarkers,
    IReadOnlyList<MeasurementCorridorDto>? MeasurementCorridors,
    IReadOnlyList<MeasurementNodeDto>? MeasurementNodes,
    IReadOnlyList<DimensionIntervalBindingDto>? DimensionIntervalBindings,
    IReadOnlyList<ArticulationBandDto>? ArticulationBands,
    Guid? SelectedMeasurementCorridorId,
    Guid? SelectedMeasurementNodeId,
    Guid? SelectedMeasurementStartNodeId,
    Guid? SelectedMeasurementEndNodeId,
    Guid? HighlightGeometryPathId,
    Guid? HighlightRoomLabelId,
    Guid? HighlightOpeningLabelId,
    Guid? HighlightDimensionId,
    Guid? PreviewPinchGroupId,
    string? PreviewAxisTag,
    FloorPlanPreviewControl.DimensionHandleKind? ActiveDimensionHandleKind,
    IReadOnlyList<GeometryPathDto>? SitePlanGeometry = null,
    IReadOnlyList<SitePlanRenderPathDto>? SitePlanRenderPaths = null,
    IReadOnlyList<SitePlanTextDto>? SitePlanTexts = null,
    ManualWallLineDraft? ManualWallLineDraft = null,
    IReadOnlyList<GeometryPathDto>? ChangePreviewGhostGeometry = null,
    double ChangePreviewGhostOpacity = 0d)
{
    public bool HasGeometry => PreviewGeometry.Count > 0;

    public bool HasCuratedArtifacts => CuratedPlanArtifacts is { Count: > 0 };
}
