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
    PreviewArtifactGeometryIndex ArtifactIndex,
    IReadOnlyList<OpeningCandidateDto>? OpeningCandidates,
    IReadOnlyList<FixedPlanComponentDto>? FixedPlanComponents,
    IReadOnlyList<ProtectedDetailAssemblyDto>? ProtectedDetailAssemblies,
    IReadOnlyList<CuratedPlanArtifactDto>? CuratedPlanArtifacts,
    IReadOnlyList<PinchMarkerDto>? PinchMarkers,
    Guid? HighlightGeometryPathId,
    Guid? HighlightRoomLabelId,
    Guid? HighlightOpeningLabelId,
    Guid? HighlightDimensionId,
    Guid? PreviewPinchGroupId,
    string? PreviewAxisTag,
    FloorPlanPreviewControl.DimensionHandleKind? ActiveDimensionHandleKind)
{
    public bool HasGeometry => PreviewGeometry.Count > 0;

    public bool HasCuratedArtifacts => CuratedPlanArtifacts is { Count: > 0 };
}
