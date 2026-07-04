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

    [Fact]
    public void ShouldRenderDimensions_returns_false_when_preview_switch_hides_them()
    {
        var scene = CreateScene(
            areDimensionsVisible: false,
            dimensions:
            [
                new DimensionDto(
                    Guid.NewGuid(),
                    "DIMENSION:1",
                    "DIMS",
                    "DIMENSION",
                    "*D169",
                    "10'-4\"",
                    "GeometryBlock",
                    string.Empty,
                    123.81m,
                    3144.78m,
                    "Inch",
                    0,
                    0m,
                    0m,
                    0m,
                    0m,
                    0m,
                    100m,
                    0m,
                    0m,
                    0m,
                    20m,
                    0m,
                    0.99m,
                    null,
                    1)
            ]);

        Assert.False(PreviewRenderComposer.ShouldRenderDimensions(scene));
    }

    [Fact]
    public void Render_clips_cad_content_to_preview_bounds_to_prevent_off_canvas_rays()
    {
        var solutionRoot = FindSolutionRoot();
        var sourcePath = Path.Combine(
            solutionRoot,
            "src",
            "FloorplanFit.Desktop",
            "Controls",
            "Preview",
            "PreviewRenderComposer.cs");
        var source = File.ReadAllText(sourcePath);

        Assert.Contains("context.PushClip(scene.Bounds)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ClipLineToBounds_trims_zoomed_segment_to_preview_edges()
    {
        var bounds = new Rect(10, 20, 100, 80);
        var start = new Point(-500, -300);
        var end = new Point(250, 180);

        var clipped = PreviewLineClipper.TryClipToBounds(bounds, start, end, out var clippedStart, out var clippedEnd);

        Assert.True(clipped);
        Assert.True(bounds.Contains(clippedStart));
        Assert.True(bounds.Contains(clippedEnd));
        Assert.Equal(10, clippedStart.X, precision: 6);
        Assert.Equal(26.4, clippedStart.Y, precision: 6);
        Assert.Equal(110, clippedEnd.X, precision: 6);
        Assert.Equal(90.4, clippedEnd.Y, precision: 6);
    }

    [Fact]
    public void ClipLineToBounds_rejects_zoomed_segment_that_never_enters_preview()
    {
        var bounds = new Rect(10, 20, 100, 80);
        var start = new Point(-500, -300);
        var end = new Point(-50, 120);

        var clipped = PreviewLineClipper.TryClipToBounds(bounds, start, end, out _, out _);

        Assert.False(clipped);
    }

    [Fact]
    public void ResolveNodeBoundDimensionIds_returns_dimensions_that_have_interval_bindings()
    {
        var boundDimensionId = Guid.NewGuid();
        var unboundDimensionId = Guid.NewGuid();

        var boundIds = PreviewRenderComposer.ResolveNodeBoundDimensionIds(
            [
                new DimensionIntervalBindingDto(
                    boundDimensionId,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "ManualVerified",
                    0m,
                    100m)
            ]);

        Assert.Contains(boundDimensionId, boundIds);
        Assert.DoesNotContain(unboundDimensionId, boundIds);
    }

    [Fact]
    public void ResolveDimensionStrokeColor_uses_node_bound_color_when_dimension_has_interval_binding()
    {
        var color = DimensionPreviewLayerRenderer.ResolveDimensionStrokeColor(
            isHighlighted: false,
            isNodeBound: true);

        Assert.Equal(PreviewSemanticPalette.DimensionNodeBound, color);
    }

    [Fact]
    public void ResolveDimensionStrokeColor_prefers_selection_highlight_over_node_bound_color()
    {
        var color = DimensionPreviewLayerRenderer.ResolveDimensionStrokeColor(
            isHighlighted: true,
            isNodeBound: true);

        Assert.Equal(PreviewSemanticPalette.SelectionHighlight, color);
    }

    [Fact]
    public void CreateDimensionRenderPlan_uses_node_bound_color_for_bound_dimension_text()
    {
        var dimension = new DimensionDto(
            Guid.NewGuid(),
            "DIMENSION:1",
            "DIMS",
            "DIMENSION",
            "*D169",
            "10'-4\"",
            "GeometryBlock",
            string.Empty,
            123.81m,
            3144.78m,
            "Inch",
            0,
            0m,
            0m,
            0m,
            0m,
            0m,
            100m,
            0m,
            0m,
            0m,
            20m,
            0m,
            0.99m,
            null,
            1);
        var viewport = new FloorPlanPreviewGeometry.PreviewViewport(
            new Rect(0, 0, 800, 600),
            MinX: 0d,
            MinY: 0d,
            Scale: 2d,
            OffsetX: 0d,
            OffsetY: 0d);

        var plan = CadTextPreviewLayerRenderer.CreateDimensionRenderPlan(
            dimension,
            viewport,
            isSelected: false,
            isNodeBound: true);

        Assert.Equal(PreviewSemanticPalette.DimensionNodeBoundArgb, plan.ColorArgb);
    }

    [Fact]
    public void ResolvePreviewRadius_clamps_extreme_zoom_dimension_primitives_to_safe_render_size()
    {
        var radius = DimensionPreviewLayerRenderer.ResolvePreviewRadius(3.5m, viewportScale: 1_000_000d);

        Assert.Equal(DimensionPreviewLayerRenderer.MaxPreviewPrimitiveRadius, radius);
    }

    private static PreviewRenderScene CreateScene(
        IReadOnlyList<CuratedPlanArtifactDto>? curatedPlanArtifacts = null,
        IReadOnlyList<DimensionDto>? dimensions = null,
        bool areDimensionsVisible = true)
    {
        return new PreviewRenderScene(
            Bounds: new Rect(0, 0, 800, 600),
            AxisTag: null,
            IsPinchPlacementArmed: false,
            Viewport: null,
            PreviewGeometry: [],
            RoomLabels: [],
            OpeningLabels: [],
            Dimensions: dimensions ?? [],
            ChangedNumberDimensionIds: [],
            AreDimensionsVisible: areDimensionsVisible,
            ArtifactIndex: PreviewArtifactGeometryIndex.Create(openingCandidates: null, fixedPlanComponents: null),
            OpeningCandidates: [],
            FixedPlanComponents: [],
            ProtectedDetailAssemblies: [],
            CuratedPlanArtifacts: curatedPlanArtifacts,
            PinchMarkers: [],
            MeasurementCorridors: [],
            MeasurementNodes: [],
            DimensionIntervalBindings: [],
            ArticulationBands: [],
            SelectedMeasurementCorridorId: null,
            SelectedMeasurementNodeId: null,
            SelectedMeasurementStartNodeId: null,
            SelectedMeasurementEndNodeId: null,
            HighlightGeometryPathId: null,
            HighlightRoomLabelId: null,
            HighlightOpeningLabelId: null,
            HighlightDimensionId: null,
            PreviewPinchGroupId: null,
            PreviewAxisTag: null,
            ActiveDimensionHandleKind: null);
    }

    private static string FindSolutionRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "FloorplanFit.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate FloorplanFit.sln from test base directory.");
    }
}
