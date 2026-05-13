using Avalonia;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls;
using FloorplanFit.Desktop.Controls.Preview;
using FloorplanFit.Domain.FloorPlans;
using Xunit;

namespace FloorplanFit.Desktop.Tests.Controls;

public sealed class PreviewRenderComposerTests
{
    [Fact]
    public void ResolveOrderedBasePaths_excludes_detected_artifact_paths_and_keeps_highlighted_path_last()
    {
        var wallPathId = Guid.NewGuid();
        var highlightedWallPathId = Guid.NewGuid();
        var openingPathId = Guid.NewGuid();
        var componentPathId = Guid.NewGuid();
        GeometryPathDto[] geometry =
        [
            new(highlightedWallPathId, false, [new GeometrySegmentDto(highlightedWallPathId, 1, 0m, 10m, 50m, 10m)]),
            new(openingPathId, false, [new GeometrySegmentDto(openingPathId, 1, 0m, 20m, 50m, 20m)]),
            new(componentPathId, false, [new GeometrySegmentDto(componentPathId, 1, 0m, 30m, 50m, 30m)]),
            new(wallPathId, false, [new GeometrySegmentDto(wallPathId, 1, 0m, 0m, 50m, 0m)])
        ];
        var artifactIndex = PreviewArtifactGeometryIndex.Create(
            [new OpeningCandidateDto(Guid.NewGuid(), "LINE:1", "DOORS", "Door", "LINE", openingPathId, 0.95m, null, 1)],
            [new FixedPlanComponentDto(Guid.NewGuid(), "INSERT:1", "FIXTURES", "Toilet", "INSERT", "TOILET1", [componentPathId], 0.95m, null, 1)]);

        var ordered = PreviewRenderComposer.ResolveOrderedBasePaths(
            geometry,
            artifactIndex,
            highlightGeometryPathId: highlightedWallPathId);

        Assert.Equal([wallPathId, highlightedWallPathId], ordered.Select(path => path.Id).ToArray());
    }

    [Fact]
    public void ResolveArtifactLayerMode_prefers_curated_branch_when_curated_artifacts_exist()
    {
        var scene = CreateScene(
            curatedPlanArtifacts:
            [
                new CuratedPlanArtifactDto(
                    Guid.NewGuid(),
                    FloorPlanArtifactSourceKinds.OpeningCandidate,
                    "LINE:1",
                    "DOORS",
                    "LINE",
                    null,
                    [Guid.NewGuid()],
                    0.95m,
                    null,
                    1,
                    FloorPlanArtifactTaxonomy.OpeningFamily,
                    FloorPlanArtifactTaxonomy.OpeningCategory,
                    FloorPlanArtifactTaxonomy.DoorType,
                    FloorPlanArtifactTaxonomy.OpeningFamily,
                    FloorPlanArtifactTaxonomy.OpeningCategory,
                    FloorPlanArtifactTaxonomy.DoorType,
                    FloorPlanArtifactDecisionState.DetectedDefault.ToString(),
                    "#FF455668")
            ]);

        Assert.Equal(
            PreviewRenderComposer.PreviewArtifactLayerMode.Curated,
            PreviewRenderComposer.ResolveArtifactLayerMode(scene));
    }

    private static PreviewRenderScene CreateScene(IReadOnlyList<CuratedPlanArtifactDto>? curatedPlanArtifacts = null)
    {
        return new PreviewRenderScene(
            Bounds: new Rect(0, 0, 800, 600),
            AxisTag: null,
            IsPinchPlacementArmed: false,
            Viewport: null,
            PreviewGeometry: [],
            RoomLabels: [],
            OpeningLabels: [],
            Dimensions: [],
            ArtifactIndex: PreviewArtifactGeometryIndex.Create(openingCandidates: null, fixedPlanComponents: null),
            OpeningCandidates: [],
            FixedPlanComponents: [],
            ProtectedDetailAssemblies: [],
            CuratedPlanArtifacts: curatedPlanArtifacts,
            PinchMarkers: [],
            HighlightGeometryPathId: null,
            HighlightRoomLabelId: null,
            HighlightOpeningLabelId: null,
            HighlightDimensionId: null,
            PreviewPinchGroupId: null,
            PreviewAxisTag: null,
            ActiveDimensionHandleKind: null);
    }
}
