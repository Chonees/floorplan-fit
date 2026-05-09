using Avalonia;
using Avalonia.Media;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls;
using FloorplanFit.Desktop.Controls.Preview;
using Xunit;

namespace FloorplanFit.Desktop.Tests.Controls;

public sealed class FloorPlanPreviewControlTests
{
    [Fact]
    public void Preview_control_exposes_room_labels_for_canvas_overlay()
    {
        RoomLabelDto[] roomLabels =
        [
            new(
                Guid.NewGuid(),
                "TEXT:1",
                "ROOM LBLS",
                "KITCHEN",
                40m,
                75m,
                0.95m,
                "Detected from ROOM LBLS text entity.",
                1)
        ];

        var control = new FloorPlanPreviewControl
        {
            RoomLabels = roomLabels
        };

        Assert.Same(roomLabels, control.RoomLabels);
    }

    [Fact]
    public void Preview_control_exposes_openings_and_opening_labels_for_canvas_overlay()
    {
        var pathId = Guid.NewGuid();
        OpeningCandidateDto[] openings =
        [
            new(Guid.NewGuid(), "LINE:1", "DOORS", "Door", "LINE", pathId, 0.95m, null, 1)
        ];
        OpeningLabelDto[] labels =
        [
            new(Guid.NewGuid(), "TEXT:1", "DOORTEXT", "Door", "2668", 40m, 75m, 0.95m, null, 1)
        ];

        var control = new FloorPlanPreviewControl
        {
            OpeningCandidates = openings,
            OpeningLabels = labels
        };

        Assert.Same(openings, control.OpeningCandidates);
        Assert.Same(labels, control.OpeningLabels);
    }

    [Fact]
    public void Preview_control_exposes_fixed_plan_components_for_canvas_overlay()
    {
        var pathId = Guid.NewGuid();
        FixedPlanComponentDto[] components =
        [
            new(Guid.NewGuid(), "INSERT:1", "FIXTURES", "Toilet", "INSERT", "TOILET1", [pathId], 0.95m, null, 1)
        ];

        var control = new FloorPlanPreviewControl
        {
            FixedPlanComponents = components
        };

        Assert.Same(components, control.FixedPlanComponents);
    }

    [Fact]
    public void Preview_control_exposes_protected_detail_assemblies_for_canvas_overlay()
    {
        var pathId = Guid.NewGuid();
        ProtectedDetailAssemblyDto[] assemblies =
        [
            new(Guid.NewGuid(), "DETAIL:MISC:1", "MISC", "WetAreaDetail", "DETAIL-GROUP", [pathId], 0.90m, null, 1)
        ];

        var control = new FloorPlanPreviewControl
        {
            ProtectedDetailAssemblies = assemblies
        };

        Assert.Same(assemblies, control.ProtectedDetailAssemblies);
    }

    [Fact]
    public void BuildHitTestGeometry_keeps_opening_paths_selectable_and_topmost()
    {
        var wallPathId = Guid.NewGuid();
        var openingPathId = Guid.NewGuid();
        GeometryPathDto[] geometryPaths =
        [
            new(wallPathId, false, [new GeometrySegmentDto(wallPathId, 1, 0m, 0m, 120m, 0m)]),
            new(openingPathId, false, [new GeometrySegmentDto(openingPathId, 1, 40m, 0m, 76m, 0m)])
        ];
        OpeningCandidateDto[] openings =
        [
            new(Guid.NewGuid(), "LINE:1", "DOORS", "Door", "LINE", openingPathId, 0.95m, null, 1)
        ];

        var hitTestGeometry = FloorPlanPreviewControl.BuildHitTestGeometry(geometryPaths, openings);

        Assert.Equal([openingPathId, wallPathId], hitTestGeometry.Select(item => item.Id).ToArray());
    }

    [Fact]
    public void BuildHitTestGeometry_keeps_fixed_component_paths_selectable_and_topmost()
    {
        var wallPathId = Guid.NewGuid();
        var componentPathId = Guid.NewGuid();
        GeometryPathDto[] geometryPaths =
        [
            new(wallPathId, false, [new GeometrySegmentDto(wallPathId, 1, 0m, 0m, 120m, 0m)]),
            new(componentPathId, false, [new GeometrySegmentDto(componentPathId, 1, 40m, 0m, 76m, 0m)])
        ];
        FixedPlanComponentDto[] components =
        [
            new(Guid.NewGuid(), "INSERT:1", "FIXTURES", "Toilet", "INSERT", "TOILET1", [componentPathId], 0.95m, null, 1)
        ];

        var hitTestGeometry = FloorPlanPreviewControl.BuildHitTestGeometry(geometryPaths, openings: [], fixedPlanComponents: components);

        Assert.Equal([componentPathId, wallPathId], hitTestGeometry.Select(item => item.Id).ToArray());
    }

    [Fact]
    public void PreviewArtifactGeometryIndex_orders_fixed_components_before_openings_and_walls()
    {
        var wallPathId = Guid.NewGuid();
        var openingPathId = Guid.NewGuid();
        var componentPathId = Guid.NewGuid();
        GeometryPathDto[] geometryPaths =
        [
            new(wallPathId, false, [new GeometrySegmentDto(wallPathId, 1, 0m, 0m, 120m, 0m)]),
            new(openingPathId, false, [new GeometrySegmentDto(openingPathId, 1, 40m, 0m, 76m, 0m)]),
            new(componentPathId, false, [new GeometrySegmentDto(componentPathId, 1, 40m, 4m, 76m, 4m)])
        ];
        OpeningCandidateDto[] openings =
        [
            new(Guid.NewGuid(), "LINE:1", "DOORS", "Door", "LINE", openingPathId, 0.95m, null, 1)
        ];
        FixedPlanComponentDto[] components =
        [
            new(Guid.NewGuid(), "INSERT:1", "FIXTURES", "Toilet", "INSERT", "TOILET1", [componentPathId], 0.95m, null, 1)
        ];

        var index = PreviewArtifactGeometryIndex.Create(openings, components);
        var ordered = index.OrderForHitTesting(geometryPaths);

        Assert.Equal([componentPathId, openingPathId, wallPathId], ordered.Select(item => item.Id).ToArray());
    }

    [Fact]
    public void PreviewArtifactGeometryIndex_orders_protected_details_before_fixed_components_openings_and_walls()
    {
        var wallPathId = Guid.NewGuid();
        var openingPathId = Guid.NewGuid();
        var componentPathId = Guid.NewGuid();
        var protectedPathId = Guid.NewGuid();
        GeometryPathDto[] geometryPaths =
        [
            new(wallPathId, false, [new GeometrySegmentDto(wallPathId, 1, 0m, 0m, 120m, 0m)]),
            new(openingPathId, false, [new GeometrySegmentDto(openingPathId, 1, 40m, 0m, 76m, 0m)]),
            new(componentPathId, false, [new GeometrySegmentDto(componentPathId, 1, 40m, 4m, 76m, 4m)]),
            new(protectedPathId, false, [new GeometrySegmentDto(protectedPathId, 1, 48m, 8m, 70m, 8m)])
        ];
        OpeningCandidateDto[] openings =
        [
            new(Guid.NewGuid(), "LINE:1", "DOORS", "Door", "LINE", openingPathId, 0.95m, null, 1)
        ];
        FixedPlanComponentDto[] components =
        [
            new(Guid.NewGuid(), "INSERT:1", "FIXTURES", "Toilet", "INSERT", "TOILET1", [componentPathId], 0.95m, null, 1)
        ];
        ProtectedDetailAssemblyDto[] protectedDetails =
        [
            new(Guid.NewGuid(), "DETAIL:MISC:1", "MISC", "WetAreaDetail", "DETAIL-GROUP", [protectedPathId], 0.90m, null, 1)
        ];

        var index = PreviewArtifactGeometryIndex.Create(openings, components, protectedDetails);
        var ordered = index.OrderForHitTesting(geometryPaths);

        Assert.Equal([protectedPathId, componentPathId, openingPathId, wallPathId], ordered.Select(item => item.Id).ToArray());
    }

    [Fact]
    public void OpeningPreviewLayerRenderer_uses_original_semantic_opening_colors_and_highlight()
    {
        var windowPen = OpeningPreviewLayerRenderer.CreatePen("Window", isHighlighted: false);
        var doorPen = OpeningPreviewLayerRenderer.CreatePen("Door", isHighlighted: false);
        var highlightedPen = OpeningPreviewLayerRenderer.CreatePen("Door", isHighlighted: true);

        Assert.Equal(Color.FromRgb(0, 147, 197), Assert.IsType<SolidColorBrush>(windowPen.Brush).Color);
        Assert.Equal(Color.FromRgb(150, 83, 13), Assert.IsType<SolidColorBrush>(doorPen.Brush).Color);
        Assert.Equal(Color.FromRgb(255, 72, 24), Assert.IsType<SolidColorBrush>(highlightedPen.Brush).Color);
        Assert.True(highlightedPen.Thickness > doorPen.Thickness);
    }

    [Fact]
    public void CreateFixedPlanComponentPen_uses_original_dxf_color_when_available()
    {
        var component = new FixedPlanComponentDto(
            Guid.NewGuid(),
            "INSERT:1",
            "FIXTURES",
            "Toilet",
            "INSERT",
            "TOILET1",
            [Guid.NewGuid()],
            0.95m,
            null,
            1,
            ColorArgb: "#FF00AEEF");

        var pen = FixedPlanComponentPreviewLayerRenderer.CreatePen(component, isHighlighted: false);

        var brush = Assert.IsType<SolidColorBrush>(pen.Brush);
        Assert.Equal(Color.Parse("#FF00AEEF"), brush.Color);
    }

    [Fact]
    public void FixedPlanComponentPreviewLayerRenderer_falls_back_to_semantic_color_when_dxf_color_is_missing()
    {
        var component = new FixedPlanComponentDto(
            Guid.NewGuid(),
            "INSERT:1",
            "FIXTURES",
            "Toilet",
            "INSERT",
            "TOILET1",
            [Guid.NewGuid()],
            0.95m,
            null,
            1);

        var pen = FixedPlanComponentPreviewLayerRenderer.CreatePen(component, isHighlighted: false);

        var brush = Assert.IsType<SolidColorBrush>(pen.Brush);
        Assert.Equal(Color.FromRgb(111, 66, 193), brush.Color);
    }

    [Fact]
    public void ProtectedDetailPreviewLayerRenderer_uses_original_dxf_color_when_available()
    {
        var assembly = new ProtectedDetailAssemblyDto(
            Guid.NewGuid(),
            "DETAIL:MISC:1",
            "MISC",
            "WetAreaDetail",
            "DETAIL-GROUP",
            [Guid.NewGuid()],
            0.90m,
            null,
            1,
            ColorArgb: "#FF00FF00");

        var pen = ProtectedDetailPreviewLayerRenderer.CreatePen(assembly, isHighlighted: false);

        var brush = Assert.IsType<SolidColorBrush>(pen.Brush);
        Assert.Equal(Color.Parse("#FF00FF00"), brush.Color);
    }

    [Fact]
    public void ProjectRoomLabel_uses_room_label_dxf_coordinates()
    {
        var pathId = Guid.NewGuid();
        GeometryPathDto[] geometryPaths =
        [
            new GeometryPathDto(
                pathId,
                IsClosed: false,
                [
                    new GeometrySegmentDto(pathId, 1, 0m, 0m, 100m, 0m),
                    new GeometrySegmentDto(pathId, 2, 100m, 0m, 100m, 100m)
                ])
        ];
        var viewport = FloorPlanPreviewGeometry.CalculateViewport(geometryPaths, new Rect(0, 0, 500, 500), 48d);
        Assert.NotNull(viewport);

        var roomLabel = new RoomLabelDto(
            Guid.NewGuid(),
            "TEXT:2",
            "ROOM LBLS",
            "LIVING ROOM",
            25m,
            75m,
            0.95m,
            "Detected from ROOM LBLS text entity.",
            1);

        var projected = CadTextPreviewLayerRenderer.ProjectRoomLabel(roomLabel, viewport.Value);

        Assert.Equal(viewport.Value.Project(roomLabel.X, roomLabel.Y), projected);
    }

    [Fact]
    public void CreateRoomLabelRenderPlan_uses_dxf_text_height_and_rotation()
    {
        var pathId = Guid.NewGuid();
        GeometryPathDto[] geometryPaths =
        [
            new GeometryPathDto(
                pathId,
                IsClosed: false,
                [new GeometrySegmentDto(pathId, 1, 0m, 0m, 100m, 100m)])
        ];
        var viewport = FloorPlanPreviewGeometry.CalculateViewport(geometryPaths, new Rect(0, 0, 500, 500), 48d);
        Assert.NotNull(viewport);
        var roomLabel = new RoomLabelDto(
            Guid.NewGuid(),
            "TEXT:2",
            "ROOM LBLS",
            "LIVING ROOM",
            25m,
            75m,
            0.95m,
            "Detected from ROOM LBLS text entity.",
            1,
            SourceEntityKind: "TEXT",
            TextHeight: 12m,
            RotationDegrees: 30m,
            TextStyleName: "STANDARD",
            HorizontalAlignment: "Center",
            VerticalAlignment: "Middle",
            AttachmentPoint: null,
            ColorArgb: null);

        var plan = CadTextPreviewLayerRenderer.CreateRoomLabelRenderPlan(roomLabel, viewport.Value);

        Assert.Equal("LIVING ROOM", plan.Text);
        Assert.Equal(viewport.Value.Project(roomLabel.X, roomLabel.Y), plan.Anchor);
        Assert.Equal((double)roomLabel.TextHeight!.Value * viewport.Value.Scale, plan.FontSize);
        Assert.Equal(30d, plan.RotationDegrees);
    }

    [Fact]
    public void CreateRoomLabelRenderPlan_forces_black_text_for_readable_preview()
    {
        var pathId = Guid.NewGuid();
        GeometryPathDto[] geometryPaths =
        [
            new GeometryPathDto(
                pathId,
                IsClosed: false,
                [new GeometrySegmentDto(pathId, 1, 0m, 0m, 100m, 100m)])
        ];
        var viewport = FloorPlanPreviewGeometry.CalculateViewport(geometryPaths, new Rect(0, 0, 500, 500), 48d);
        Assert.NotNull(viewport);
        var roomLabel = new RoomLabelDto(
            Guid.NewGuid(),
            "TEXT:2",
            "ROOM LBLS",
            "MASTER BATH",
            25m,
            75m,
            0.95m,
            null,
            1,
            ColorArgb: "#FFFFFFFF");

        var plan = CadTextPreviewLayerRenderer.CreateRoomLabelRenderPlan(roomLabel, viewport.Value);

        Assert.Equal("#FF000000", plan.ColorArgb);
    }

    [Fact]
    public void CreateOpeningLabelRenderPlan_uses_dxf_text_height_rotation_and_kind()
    {
        var pathId = Guid.NewGuid();
        GeometryPathDto[] geometryPaths =
        [
            new GeometryPathDto(
                pathId,
                IsClosed: false,
                [new GeometrySegmentDto(pathId, 1, 0m, 0m, 100m, 100m)])
        ];
        var viewport = FloorPlanPreviewGeometry.CalculateViewport(geometryPaths, new Rect(0, 0, 500, 500), 48d);
        Assert.NotNull(viewport);
        var label = new OpeningLabelDto(
            Guid.NewGuid(),
            "TEXT:1",
            "WINDWS LBLS",
            "Window",
            "3050 S.H.",
            25m,
            75m,
            0.95m,
            null,
            1,
            SourceEntityKind: "TEXT",
            TextHeight: 3.5m,
            RotationDegrees: 90m,
            TextStyleName: "TEXT1",
            HorizontalAlignment: "Left",
            VerticalAlignment: "Baseline",
            AttachmentPoint: null,
            ColorArgb: "#FF000000");

        var plan = CadTextPreviewLayerRenderer.CreateOpeningLabelRenderPlan(label, viewport.Value);

        Assert.Equal("3050 S.H.", plan.Text);
        Assert.Equal(viewport.Value.Project(label.X, label.Y), plan.Anchor);
        Assert.Equal((double)label.TextHeight!.Value * viewport.Value.Scale, plan.FontSize);
        Assert.Equal(90d, plan.RotationDegrees);
    }

    [Fact]
    public void CreateOpeningLabelRenderPlan_forces_black_text_for_readable_preview()
    {
        var pathId = Guid.NewGuid();
        GeometryPathDto[] geometryPaths =
        [
            new GeometryPathDto(
                pathId,
                IsClosed: false,
                [new GeometrySegmentDto(pathId, 1, 0m, 0m, 100m, 100m)])
        ];
        var viewport = FloorPlanPreviewGeometry.CalculateViewport(geometryPaths, new Rect(0, 0, 500, 500), 48d);
        Assert.NotNull(viewport);
        var label = new OpeningLabelDto(
            Guid.NewGuid(),
            "TEXT:1",
            "DOORTEXT",
            "Door",
            "2668",
            25m,
            75m,
            0.95m,
            null,
            1,
            ColorArgb: "#FFFFFFFF");

        var plan = CadTextPreviewLayerRenderer.CreateOpeningLabelRenderPlan(label, viewport.Value);

        Assert.Equal("#FF000000", plan.ColorArgb);
    }

    [Fact]
    public void ResolveRoomLabelTextOrigin_uses_baseline_for_default_dxf_text()
    {
        var plan = new CadTextPreviewLayerRenderer.TextRenderPlan(
            "KITCHEN",
            new Point(200d, 300d),
            FontSize: 20d,
            RotationDegrees: 0d,
            HorizontalAlignment: "Left",
            VerticalAlignment: "Baseline",
            AttachmentPoint: null,
            ColorArgb: null);

        var origin = CadTextPreviewLayerRenderer.ResolveTextOriginForMetrics(
            plan,
            textWidth: 80d,
            textHeight: 24d,
            textBaseline: 18d);

        Assert.Equal(200d, origin.X);
        Assert.Equal(282d, origin.Y);
    }

    [Fact]
    public void ResolveRoomLabelTextOrigin_centers_middle_aligned_dxf_text()
    {
        var plan = new CadTextPreviewLayerRenderer.TextRenderPlan(
            "LIVING ROOM",
            new Point(200d, 300d),
            FontSize: 20d,
            RotationDegrees: 0d,
            HorizontalAlignment: "Center",
            VerticalAlignment: "Middle",
            AttachmentPoint: null,
            ColorArgb: null);

        var origin = CadTextPreviewLayerRenderer.ResolveTextOriginForMetrics(
            plan,
            textWidth: 100d,
            textHeight: 24d,
            textBaseline: 18d);

        Assert.Equal(150d, origin.X);
        Assert.Equal(288d, origin.Y);
    }

    [Fact]
    public void CalculateWheelZoomFactor_zooms_in_and_out_with_bounds()
    {
        var zoomedIn = FloorPlanPreviewControl.CalculateWheelZoomFactor(1d, wheelDeltaY: 1d);
        var zoomedOut = FloorPlanPreviewControl.CalculateWheelZoomFactor(1d, wheelDeltaY: -1d);
        var clampedMinimum = FloorPlanPreviewControl.CalculateWheelZoomFactor(0.2d, wheelDeltaY: -10d);
        var clampedMaximum = FloorPlanPreviewControl.CalculateWheelZoomFactor(20d, wheelDeltaY: 10d);

        Assert.True(zoomedIn > 1d);
        Assert.True(zoomedOut < 1d);
        Assert.Equal(FloorPlanPreviewControl.MinimumUserZoomFactor, clampedMinimum);
        Assert.Equal(FloorPlanPreviewControl.MaximumUserZoomFactor, clampedMaximum);
    }

    [Fact]
    public void ResolveZoomStateForWheel_keeps_pointer_anchored_to_same_world_point()
    {
        var baseViewport = new FloorPlanPreviewGeometry.PreviewViewport(
            new Rect(0, 0, 800, 600),
            MinX: 0d,
            MinY: 0d,
            Scale: 2d,
            OffsetX: 100d,
            OffsetY: 80d);
        var pointer = new Point(420d, 270d);
        var currentState = FloorPlanPreviewControl.PreviewZoomState.Default;
        var currentViewport = baseViewport.WithUserTransform(currentState.ZoomFactor, currentState.PanOffset);
        var worldBeforeZoom = currentViewport.Unproject(pointer);

        var nextState = FloorPlanPreviewControl.ResolveZoomStateForWheel(
            baseViewport,
            currentState,
            pointer,
            wheelDeltaY: 1d);
        var nextViewport = baseViewport.WithUserTransform(nextState.ZoomFactor, nextState.PanOffset);
        var projectedAfterZoom = nextViewport.Project(worldBeforeZoom.X, worldBeforeZoom.Y);

        Assert.True(nextState.ZoomFactor > currentState.ZoomFactor);
        Assert.InRange(Math.Abs(pointer.X - projectedAfterZoom.X), 0d, 0.001d);
        Assert.InRange(Math.Abs(pointer.Y - projectedAfterZoom.Y), 0d, 0.001d);
    }

    [Fact]
    public void ResolvePanStateForDrag_keeps_zoom_and_adds_pointer_delta_to_pan_offset()
    {
        var startState = new FloorPlanPreviewControl.PreviewZoomState(
            ZoomFactor: 2.5d,
            PanOffset: new Vector(30d, -12d));
        var startPointer = new Point(100d, 120d);
        var currentPointer = new Point(145d, 90d);

        var nextState = FloorPlanPreviewControl.ResolvePanStateForDrag(
            startState,
            startPointer,
            currentPointer);

        Assert.Equal(startState.ZoomFactor, nextState.ZoomFactor);
        Assert.Equal(new Vector(75d, -42d), nextState.PanOffset);
    }

    [Fact]
    public void ResolvePanStateForDrag_moves_projected_world_points_by_drag_delta()
    {
        var viewport = new FloorPlanPreviewGeometry.PreviewViewport(
            new Rect(0, 0, 800, 600),
            MinX: 0d,
            MinY: 0d,
            Scale: 2d,
            OffsetX: 100d,
            OffsetY: 80d);
        var startState = new FloorPlanPreviewControl.PreviewZoomState(
            ZoomFactor: 1.25d,
            PanOffset: new Vector(10d, 15d));
        var startPointer = new Point(220d, 280d);
        var currentPointer = new Point(250d, 325d);
        var worldPoint = new Point(40d, 50d);
        var projectedBeforePan = viewport
            .WithUserTransform(startState.ZoomFactor, startState.PanOffset)
            .Project(worldPoint.X, worldPoint.Y);

        var nextState = FloorPlanPreviewControl.ResolvePanStateForDrag(
            startState,
            startPointer,
            currentPointer);
        var projectedAfterPan = viewport
            .WithUserTransform(nextState.ZoomFactor, nextState.PanOffset)
            .Project(worldPoint.X, worldPoint.Y);

        Assert.Equal(currentPointer.X - startPointer.X, projectedAfterPan.X - projectedBeforePan.X);
        Assert.Equal(currentPointer.Y - startPointer.Y, projectedAfterPan.Y - projectedBeforePan.Y);
    }

    [Fact]
    public void GetWorkspaceDotCenters_returns_regular_dots_inside_bounds()
    {
        var dots = PreviewWorkspaceRenderer.GetDotCenters(new Rect(0, 0, 50, 50), spacing: 20d);

        Assert.Equal(
            [new Point(10d, 10d), new Point(30d, 10d), new Point(10d, 30d), new Point(30d, 30d)],
            dots);
    }

    [Fact]
    public void CompressionHandlePreviewLayerRenderer_hides_handles_when_pinch_placement_is_armed()
    {
        var handles = CompressionHandlePreviewLayerRenderer.GetVisibleHandles(
            new Rect(0, 0, 1000, 700),
            Domain.FloorPlans.PinchAxisTag.Height,
            isPinchPlacementArmed: true);

        Assert.Empty(handles);
    }

    [Fact]
    public void CompressionHandlePreviewLayerRenderer_returns_height_handles_when_drag_preview_is_available()
    {
        var handles = CompressionHandlePreviewLayerRenderer.GetVisibleHandles(
            new Rect(0, 0, 1000, 700),
            Domain.FloorPlans.PinchAxisTag.Height,
            isPinchPlacementArmed: false);

        Assert.Equal(2, handles.Count);
        Assert.Contains(handles, item => item.Edge == FloorPlanPreviewGeometry.PreviewCompressionEdge.Top);
        Assert.Contains(handles, item => item.Edge == FloorPlanPreviewGeometry.PreviewCompressionEdge.Bottom);
    }

    [Fact]
    public void PinchMarkerPreviewLayerRenderer_filters_markers_to_selected_preview_group()
    {
        var selectedGroupId = Guid.NewGuid();
        var otherGroupId = Guid.NewGuid();
        var selectedMarker = CreatePinchMarker(selectedGroupId, "Height");
        var otherMarker = CreatePinchMarker(otherGroupId, "Height");

        var filtered = PinchMarkerPreviewLayerRenderer.FilterForPreviewGroup(
            [selectedMarker, otherMarker],
            selectedGroupId);

        Assert.Equal([selectedMarker.PinchMarkerId], filtered!.Select(item => item.PinchMarkerId).ToArray());
    }

    [Fact]
    public void PinchMarkerPreviewLayerRenderer_styles_active_group_above_axis_and_inactive_markers()
    {
        var selectedGroupId = Guid.NewGuid();
        var activeGroupMarker = CreatePinchMarker(selectedGroupId, "Height");
        var activeAxisMarker = CreatePinchMarker(Guid.NewGuid(), "Height");
        var inactiveMarker = CreatePinchMarker(Guid.NewGuid(), "Width");

        var activeGroupStyle = PinchMarkerPreviewLayerRenderer.ResolveStyle(activeGroupMarker, selectedGroupId, "Height");
        var activeAxisStyle = PinchMarkerPreviewLayerRenderer.ResolveStyle(activeAxisMarker, selectedGroupId, "Height");
        var inactiveStyle = PinchMarkerPreviewLayerRenderer.ResolveStyle(inactiveMarker, selectedGroupId, "Height");

        Assert.Equal(Colors.SeaGreen, activeGroupStyle.Fill);
        Assert.Equal(5d, activeGroupStyle.Radius);
        Assert.Equal(Colors.DodgerBlue, activeAxisStyle.Fill);
        Assert.Equal(4d, activeAxisStyle.Radius);
        Assert.Equal(Colors.SlateGray, inactiveStyle.Fill);
        Assert.Equal(4d, inactiveStyle.Radius);
    }

    private static PinchMarkerDto CreatePinchMarker(Guid pinchGroupId, string axisTag)
    {
        return new PinchMarkerDto(
            Guid.NewGuid(),
            pinchGroupId,
            axisTag,
            Guid.NewGuid(),
            Guid.NewGuid(),
            axisTag,
            0.5m,
            120m,
            1);
    }
}
