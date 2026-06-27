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
    public void Preview_control_exposes_dimensions_for_canvas_overlay()
    {
        var changedDimensionId = Guid.NewGuid();
        DimensionDto[] dimensions =
        [
            new(
                changedDimensionId,
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
        ];
        Guid[] changedNumberDimensionIds = [changedDimensionId];

        var control = new FloorPlanPreviewControl
        {
            Dimensions = dimensions,
            ChangedNumberDimensionIds = changedNumberDimensionIds
        };

        Assert.Same(dimensions, control.Dimensions);
        Assert.Same(changedNumberDimensionIds, control.ChangedNumberDimensionIds);
    }

    [Fact]
    public void Preview_control_exposes_site_plan_underlay_geometry()
    {
        var pathId = Guid.NewGuid();
        GeometryPathDto[] sitePlanPaths =
        [
            new(
                pathId,
                IsClosed: true,
                [
                    new GeometrySegmentDto(pathId, 1, 0m, 0m, 100m, 0m),
                    new GeometrySegmentDto(pathId, 2, 100m, 0m, 100m, 100m),
                    new GeometrySegmentDto(pathId, 3, 100m, 100m, 0m, 100m),
                    new GeometrySegmentDto(pathId, 4, 0m, 100m, 0m, 0m)
                ])
        ];

        var control = new FloorPlanPreviewControl
        {
            SitePlanGeometryPaths = sitePlanPaths
        };

        Assert.Same(sitePlanPaths, control.SitePlanGeometryPaths);
    }

    [Fact]
    public void Preview_control_exposes_colored_site_plan_paths_and_texts_for_full_site_plan_preview()
    {
        var pathId = Guid.NewGuid();
        SitePlanRenderPathDto[] sitePlanPaths =
        [
            new(
                pathId,
                "SETBACK",
                "LWPOLYLINE",
                IsClosed: true,
                [
                    new GeometrySegmentDto(pathId, 1, 0m, 0m, 100m, 0m),
                    new GeometrySegmentDto(pathId, 2, 100m, 0m, 100m, 100m)
                ],
                "#FFFFB000",
                IsSetback: true)
        ];
        SitePlanTextDto[] sitePlanTexts =
        [
            new(Guid.NewGuid(), "NOTES", "TEXT", "SITE PLAN", 10m, 20m, 2.5m, 0m, "#FF00FFFF", IsSetback: false)
        ];

        var control = new FloorPlanPreviewControl
        {
            SitePlanRenderPaths = sitePlanPaths,
            SitePlanTexts = sitePlanTexts
        };

        Assert.Same(sitePlanPaths, control.SitePlanRenderPaths);
        Assert.Same(sitePlanTexts, control.SitePlanTexts);
    }

    [Fact]
    public void SitePlanPreviewLayerRenderer_preserves_site_plan_source_colors()
    {
        var nonSetback = SitePlanPreviewLayerRenderer.ResolveColor("#FF00FFFF", isSetback: false);
        var setback = SitePlanPreviewLayerRenderer.ResolveColor("#FFFF00FF", isSetback: true);
        var fallbackNonSetback = SitePlanPreviewLayerRenderer.ResolveColor(null, isSetback: false);
        var fallbackSetback = SitePlanPreviewLayerRenderer.ResolveColor(null, isSetback: true);

        Assert.Equal(Color.Parse("#FF00FFFF"), nonSetback);
        Assert.Equal(Color.Parse("#FFFF00FF"), setback);
        Assert.Equal(Color.FromArgb(210, 148, 163, 184), fallbackNonSetback);
        Assert.Equal(Color.Parse("#FFFFB000"), fallbackSetback);
    }

    [Fact]
    public void Dimension_renderers_use_red_for_dimensions_whose_visible_number_changed()
    {
        Assert.Equal(
            PreviewSemanticPalette.DimensionChangedMeasurement,
            DimensionPreviewLayerRenderer.ResolveDimensionStrokeColor(
                isHighlighted: false,
                isNodeBound: true,
                hasChangedNumber: true));
        Assert.Equal(
            PreviewSemanticPalette.DimensionChangedMeasurementArgb,
            CadTextPreviewLayerRenderer.ResolveDimensionTextColorArgb(
                isSelected: false,
                isNodeBound: true,
                hasChangedNumber: true));
    }

    [Fact]
    public void Dimension_renderer_keeps_selection_priority_over_changed_number_red()
    {
        Assert.Equal(
            PreviewSemanticPalette.SelectionHighlight,
            DimensionPreviewLayerRenderer.ResolveDimensionStrokeColor(
                isHighlighted: true,
                isNodeBound: true,
                hasChangedNumber: true));
        Assert.Equal(
            PreviewSemanticPalette.SelectionHighlightArgb,
            CadTextPreviewLayerRenderer.ResolveDimensionTextColorArgb(
                isSelected: true,
                isNodeBound: true,
                hasChangedNumber: true));
    }

    [Fact]
    public void Preview_control_exposes_the_dimensions_visibility_toggle_state()
    {
        var control = new FloorPlanPreviewControl
        {
            AreDimensionsVisible = false
        };

        Assert.False(control.AreDimensionsVisible);
        Assert.False(FloorPlanPreviewControl.CanResolveDimensionInteractions(areDimensionsVisible: false, isPinchPlacementArmed: false));
        Assert.False(FloorPlanPreviewControl.CanResolveDimensionInteractions(areDimensionsVisible: true, isPinchPlacementArmed: true));
        Assert.True(FloorPlanPreviewControl.CanResolveDimensionInteractions(areDimensionsVisible: true, isPinchPlacementArmed: false));
    }

    [Fact]
    public void Preview_control_exposes_floor_plan_move_tool_state_and_delta_conversion()
    {
        var control = new FloorPlanPreviewControl
        {
            IsFloorPlanMoveToolActive = true
        };
        var viewport = new FloorPlanPreviewGeometry.PreviewViewport(
            new Rect(0d, 0d, 200d, 200d),
            MinX: 0d,
            MinY: 0d,
            Scale: 10d,
            OffsetX: 0d,
            OffsetY: 0d);

        var delta = FloorPlanPreviewControl.CalculateFloorPlanMoveDelta(
            viewport,
            previousPointerPosition: new Point(20d, 120d),
            currentPointerPosition: new Point(50d, 80d));

        Assert.True(control.IsFloorPlanMoveToolActive);
        Assert.Equal(3m, delta.DeltaX);
        Assert.Equal(4m, delta.DeltaY);
    }

    [Fact]
    public void CalculateFloorPlanMoveDelta_preserves_high_zoom_precision()
    {
        var viewport = new FloorPlanPreviewGeometry.PreviewViewport(
            new Rect(0d, 0d, 200d, 200d),
            MinX: 0d,
            MinY: 0d,
            Scale: 10_000_000d,
            OffsetX: 0d,
            OffsetY: 0d);

        var delta = FloorPlanPreviewControl.CalculateFloorPlanMoveDelta(
            viewport,
            previousPointerPosition: new Point(20d, 120d),
            currentPointerPosition: new Point(25d, 115d));

        Assert.Equal(0.000001m, delta.DeltaX);
        Assert.Equal(0.000001m, delta.DeltaY);
    }

    [Fact]
    public void CalculateFloorPlanMoveDispatchDelta_accumulates_high_zoom_pointer_movement_from_drag_start()
    {
        var viewport = new FloorPlanPreviewGeometry.PreviewViewport(
            new Rect(0d, 0d, 200d, 200d),
            MinX: 0d,
            MinY: 0d,
            Scale: 10_000_000d,
            OffsetX: 0d,
            OffsetY: 0d);
        var start = new Point(20d, 120d);

        var firstPixel = FloorPlanPreviewControl.CalculateFloorPlanMoveDispatchDelta(
            viewport,
            dragStartPointerPosition: start,
            currentPointerPosition: new Point(21d, 119d),
            appliedDeltaX: 0m,
            appliedDeltaY: 0m);
        var accumulatedPixels = FloorPlanPreviewControl.CalculateFloorPlanMoveDispatchDelta(
            viewport,
            dragStartPointerPosition: start,
            currentPointerPosition: new Point(25d, 115d),
            appliedDeltaX: firstPixel.DeltaX,
            appliedDeltaY: firstPixel.DeltaY);

        Assert.Equal(0m, firstPixel.DeltaX);
        Assert.Equal(0m, firstPixel.DeltaY);
        Assert.Equal(0.000001m, accumulatedPixels.DeltaX);
        Assert.Equal(0.000001m, accumulatedPixels.DeltaY);
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
    public void FloorPlanPreviewControl_control_helpers_delegate_to_preview_interaction_coordinator()
    {
        var zoomedIn = FloorPlanPreviewControl.CalculateWheelZoomFactor(1d, wheelDeltaY: 1d);
        var panned = FloorPlanPreviewControl.ResolvePanStateForDrag(
            new FloorPlanPreviewControl.PreviewZoomState(1.5d, new Vector(10d, 15d)),
            new Point(50d, 50d),
            new Point(70d, 80d));

        Assert.True(zoomedIn > 1d);
        Assert.Equal(1.5d, panned.ZoomFactor);
        Assert.Equal(new Vector(30d, 45d), panned.PanOffset);
    }

    [Fact]
    public void FloorPlanPreviewControl_sources_collection_observer_lifecycle_through_the_hub()
    {
        var controlPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "FloorplanFit.Desktop", "Controls", "FloorPlanPreviewControl.cs");
        var source = File.ReadAllText(controlPath);

        Assert.Contains("PreviewCollectionObserverHub", source, StringComparison.Ordinal);
        Assert.DoesNotContain("private void GeometryPathsCollectionChanged(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("private void RoomLabelsCollectionChanged(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void FloorPlanPreviewControl_sources_preview_render_through_the_composer()
    {
        var controlPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "FloorplanFit.Desktop", "Controls", "FloorPlanPreviewControl.cs");
        var source = File.ReadAllText(controlPath);

        Assert.Contains("PreviewRenderComposer.Render(context, scene);", source, StringComparison.Ordinal);
        Assert.DoesNotContain("context.DrawLine(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CuratedArtifactPreviewLayerRenderer.Render(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void FloorPlanPreviewControl_clips_entire_render_to_local_bounds()
    {
        var controlPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "FloorplanFit.Desktop", "Controls", "FloorPlanPreviewControl.cs");
        var source = File.ReadAllText(controlPath);

        Assert.Contains("context.PushClip(bounds)", source, StringComparison.Ordinal);
        Assert.Contains("PreviewRenderComposer.Render(context, scene);", source, StringComparison.Ordinal);
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
    public void OpeningPreviewLayerRenderer_uses_requested_semantic_opening_palette_and_highlight()
    {
        var windowPen = OpeningPreviewLayerRenderer.CreatePen("Window", isHighlighted: false);
        var doorPen = OpeningPreviewLayerRenderer.CreatePen("Door", isHighlighted: false);
        var highlightedWindowPen = OpeningPreviewLayerRenderer.CreatePen("Window", isHighlighted: true);
        var highlightedDoorPen = OpeningPreviewLayerRenderer.CreatePen("Door", isHighlighted: true);

        Assert.Equal(Color.FromRgb(0, 188, 212), Assert.IsType<SolidColorBrush>(windowPen.Brush).Color);
        Assert.Equal(Color.Parse("#FF455668"), Assert.IsType<SolidColorBrush>(doorPen.Brush).Color);
        Assert.Equal(Colors.SeaGreen, Assert.IsType<SolidColorBrush>(highlightedWindowPen.Brush).Color);
        Assert.Equal(Colors.SeaGreen, Assert.IsType<SolidColorBrush>(highlightedDoorPen.Brush).Color);
        Assert.True(highlightedWindowPen.Thickness > windowPen.Thickness);
        Assert.True(highlightedDoorPen.Thickness > doorPen.Thickness);
    }

    [Fact]
    public void FixedPlanComponentPreviewLayerRenderer_uses_red_palette_even_when_dxf_color_is_available()
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
        Assert.Equal(Color.FromRgb(220, 38, 38), brush.Color);
    }

    [Fact]
    public void FixedPlanComponentPreviewLayerRenderer_uses_cyan_palette_for_cabinets()
    {
        var component = new FixedPlanComponentDto(
            Guid.NewGuid(),
            "LINE:1",
            "CABS",
            "Cabinet",
            "LINE",
            SourceBlockName: null,
            [Guid.NewGuid()],
            0.95m,
            null,
            1,
            ColorArgb: "#FFFF0000");

        var pen = FixedPlanComponentPreviewLayerRenderer.CreatePen(component, isHighlighted: false);

        var brush = Assert.IsType<SolidColorBrush>(pen.Brush);
        Assert.Equal(Color.FromRgb(0, 188, 212), brush.Color);
    }

    [Fact]
    public void FixedPlanComponentPreviewLayerRenderer_falls_back_to_red_palette_when_dxf_color_is_missing()
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
        Assert.Equal(Color.FromRgb(220, 38, 38), brush.Color);
    }

    [Fact]
    public void FixedPlanComponentPreviewLayerRenderer_uses_selection_green_when_highlighted()
    {
        var component = new FixedPlanComponentDto(
            Guid.NewGuid(),
            "INSERT:1",
            "FIXTURES",
            "Tub",
            "INSERT",
            "TUB",
            [Guid.NewGuid()],
            0.95m,
            null,
            1);

        var normalPen = FixedPlanComponentPreviewLayerRenderer.CreatePen(component, isHighlighted: false);
        var highlightedPen = FixedPlanComponentPreviewLayerRenderer.CreatePen(component, isHighlighted: true);

        Assert.Equal(Colors.SeaGreen, Assert.IsType<SolidColorBrush>(highlightedPen.Brush).Color);
        Assert.True(highlightedPen.Thickness > normalPen.Thickness);
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
    public void ProtectedDetailPreviewLayerRenderer_uses_selection_green_when_highlighted()
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
            1);

        var normalPen = ProtectedDetailPreviewLayerRenderer.CreatePen(assembly, isHighlighted: false);
        var highlightedPen = ProtectedDetailPreviewLayerRenderer.CreatePen(assembly, isHighlighted: true);

        Assert.Equal(Colors.SeaGreen, Assert.IsType<SolidColorBrush>(highlightedPen.Brush).Color);
        Assert.True(highlightedPen.Thickness > normalPen.Thickness);
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
    public void ResolvePreviewFontSize_clamps_extreme_zoom_text_to_safe_render_size()
    {
        var fontSize = CadTextPreviewLayerRenderer.ResolvePreviewFontSize(3.5m, viewportScale: 1_000_000d);

        Assert.Equal(CadTextPreviewLayerRenderer.MaxPreviewFontSize, fontSize);
    }

    [Fact]
    public void CanRenderText_rejects_far_offscreen_text_after_extreme_zoom()
    {
        var canRender = CadTextPreviewLayerRenderer.CanRenderText(
            new Point(10_000_000d, 0d),
            CadTextPreviewLayerRenderer.MaxPreviewFontSize);

        Assert.False(canRender);
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

        var plan = CadTextPreviewLayerRenderer.CreateRoomLabelRenderPlan(roomLabel, viewport.Value, isSelected: false);

        Assert.Equal("#FF000000", plan.ColorArgb);
    }

    [Fact]
    public void CreateRoomLabelRenderPlan_uses_selection_green_when_selected()
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

        var plan = CadTextPreviewLayerRenderer.CreateRoomLabelRenderPlan(roomLabel, viewport.Value, isSelected: true);

        Assert.Equal("#FF2E8B57", plan.ColorArgb);
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

        var plan = CadTextPreviewLayerRenderer.CreateOpeningLabelRenderPlan(label, viewport.Value, isSelected: false);

        Assert.Equal("#FF000000", plan.ColorArgb);
    }

    [Fact]
    public void CreateOpeningLabelRenderPlan_uses_selection_green_when_selected()
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

        var plan = CadTextPreviewLayerRenderer.CreateOpeningLabelRenderPlan(label, viewport.Value, isSelected: true);

        Assert.Equal("#FF2E8B57", plan.ColorArgb);
    }

    [Fact]
    public void CreateDimensionRenderPlan_projects_dimension_text_to_the_dimension_line_midpoint()
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

        var plan = CadTextPreviewLayerRenderer.CreateDimensionRenderPlan(dimension, viewport.Value);

        Assert.Equal("10'-4\"", plan.Text);
        Assert.Equal(viewport.Value.Project(50m, 20m), plan.Anchor);
        Assert.Equal(0d, plan.RotationDegrees);
    }

    [Fact]
    public void CreateDimensionRenderPlan_uses_precise_geometry_block_text_position_when_available()
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
        var dimension = new DimensionDto(
            Guid.NewGuid(),
            "DIMENSION:1",
            "DIMS",
            "DIMENSION",
            "*D169",
            "5'-8\"",
            "GeometryBlock",
            string.Empty,
            68m,
            1727.2m,
            "Inch",
            160,
            0m,
            0m,
            440.5741888255912m,
            521.7784891962232m,
            0m,
            372.5741888256203m,
            526.9408070879157m,
            0m,
            372.5741888256203m,
            516.9566494662152m,
            0m,
            0.99m,
            null,
            1)
        {
            RenderTextX = 408.8391899621098m,
            RenderTextY = 518.9677806582538m,
            RenderTextHeight = 3.5m,
            RenderTextRotationDegrees = 0m,
            RenderTextStyleName = "ARCH",
            RenderTextAttachmentPoint = "MiddleCenter"
        };

        var plan = CadTextPreviewLayerRenderer.CreateDimensionRenderPlan(dimension, viewport.Value);
        var expectedAnchor = viewport.Value.Project(408.8391899621098m, 518.9677806582538m);

        Assert.InRange(Math.Abs(expectedAnchor.X - plan.Anchor.X), 0d, 0.000001d);
        Assert.InRange(Math.Abs(expectedAnchor.Y - plan.Anchor.Y), 0d, 0.000001d);
        Assert.Equal(3.5d * viewport.Value.Scale, plan.FontSize);
        Assert.Equal(0d, plan.RotationDegrees);
        Assert.Equal("MiddleCenter", plan.AttachmentPoint);
    }

    [Fact]
    public void DimensionPreviewLayerRenderer_projects_exact_dimension_segments_to_preview_lines()
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
        var dimension = new DimensionDto(
            Guid.NewGuid(),
            "DIMENSION:1",
            "DIMS",
            "DIMENSION",
            "*D169",
            "5'-8\"",
            "GeometryBlock",
            string.Empty,
            68m,
            1727.2m,
            "Inch",
            160,
            0m,
            0m,
            440.5741888255912m,
            521.7784891962232m,
            0m,
            372.5741888256203m,
            526.9408070879157m,
            0m,
            372.5741888256203m,
            516.9566494662152m,
            0m,
            0.99m,
            null,
            1)
        {
            LineSegments =
            [
                new DimensionLineSegmentDto(440.5741888255912m, 519.7784891962231m, 440.5741888255912m, 512.9566494662152m),
                new DimensionLineSegmentDto(372.5741888256203m, 524.9408070879156m, 372.5741888256203m, 512.9566494662152m),
                new DimensionLineSegmentDto(437.0741888255913m, 516.9566494662152m, 376.0741888256204m, 516.9566494662152m)
            ]
        };

        var projected = DimensionPreviewLayerRenderer.CreateProjectedSegments(dimension, viewport.Value);

        Assert.Equal(3, projected.Count);
        Assert.Equal(viewport.Value.Project(440.5741888255912m, 519.7784891962231m), projected[0].Start);
        Assert.Equal(viewport.Value.Project(440.5741888255912m, 512.9566494662152m), projected[0].End);
        Assert.Equal(viewport.Value.Project(437.0741888255913m, 516.9566494662152m), projected[2].Start);
        Assert.Equal(viewport.Value.Project(376.0741888256204m, 516.9566494662152m), projected[2].End);
    }

    [Fact]
    public void CreateProjectedSegments_preserves_all_authored_line_primitives_without_truncating_dimension_extras()
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
        var dimension = new DimensionDto(
            Guid.NewGuid(),
            "DIMENSION:EXTRAS",
            "DIMS",
            "DIMENSION",
            "*D171",
            "10'",
            "GeometryBlock",
            string.Empty,
            120m,
            3048m,
            "Inch",
            0,
            0m,
            0m,
            100m,
            100m,
            0m,
            220m,
            100m,
            0m,
            100m,
            140m,
            0m,
            0.99m,
            null,
            1)
        {
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto("LEFT-LEG", 1, 100m, 140m, 100m, 100m),
                new DimensionLinePrimitiveDto("RIGHT-LEG", 2, 220m, 140m, 220m, 100m),
                new DimensionLinePrimitiveDto("ROOF", 3, 100m, 140m, 220m, 140m),
                new DimensionLinePrimitiveDto("TICK-MARK", 4, 95m, 145m, 105m, 135m),
                new DimensionLinePrimitiveDto("CENTERLINE", 5, 160m, 90m, 160m, 150m)
            ]
        };

        var projected = DimensionPreviewLayerRenderer.CreateProjectedSegments(dimension, viewport.Value);

        Assert.Equal(5, projected.Count);
        Assert.Equal(viewport.Value.Project(100m, 140m), projected[0].Start);
        Assert.Equal(viewport.Value.Project(220m, 100m), projected[1].End);
        Assert.Equal(viewport.Value.Project(220m, 140m), projected[2].End);
        Assert.Equal(viewport.Value.Project(95m, 145m), projected[3].Start);
        Assert.Equal(viewport.Value.Project(160m, 90m), projected[4].Start);
    }

    [Fact]
    public void CreateProjectedSegments_preserves_authored_duplicate_lines_instead_of_blind_deduping()
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
        var dimension = new DimensionDto(
            Guid.NewGuid(),
            "DIMENSION:DUP",
            "DIMS",
            "DIMENSION",
            "*D170",
            "10'",
            "GeometryBlock",
            string.Empty,
            120m,
            3048m,
            "Inch",
            0,
            0m,
            0m,
            100m,
            100m,
            0m,
            220m,
            100m,
            0m,
            100m,
            140m,
            0m,
            0.99m,
            null,
            1)
        {
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto("LINE-1", 1, 100m, 140m, 100m, 100m),
                new DimensionLinePrimitiveDto("LINE-1-DUP", 2, 100m, 140m, 100m, 100m),
                new DimensionLinePrimitiveDto("LINE-2", 3, 220m, 140m, 220m, 100m),
                new DimensionLinePrimitiveDto("LINE-3", 4, 100m, 140m, 220m, 140m),
                new DimensionLinePrimitiveDto("LINE-3-REV", 5, 220m, 140m, 100m, 140m)
            ]
        };

        var projected = DimensionPreviewLayerRenderer.CreateProjectedSegments(dimension, viewport.Value);

        Assert.Equal(5, projected.Count);
        Assert.Equal(viewport.Value.Project(100m, 140m), projected[0].Start);
        Assert.Equal(viewport.Value.Project(100m, 140m), projected[1].Start);
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
        var clampedMaximum = FloorPlanPreviewControl.CalculateWheelZoomFactor(100d, wheelDeltaY: 20d);

        Assert.Equal(2d, zoomedIn);
        Assert.Equal(0.5d, zoomedOut, precision: 12);
        Assert.Equal(FloorPlanPreviewControl.MinimumUserZoomFactor, clampedMinimum);
        Assert.Equal(FloorPlanPreviewControl.MaximumUserZoomFactor, clampedMaximum);
    }

    [Fact]
    public void CalculateWheelZoomFactor_reaches_wall_inspection_zoom_with_few_wheel_ticks()
    {
        var zoomAfterTenTicks = FloorPlanPreviewControl.CalculateWheelZoomFactor(1d, wheelDeltaY: 10d);

        Assert.True(zoomAfterTenTicks > 1000d);
    }

    [Fact]
    public void MaximumUserZoomFactor_allows_practically_unbounded_floor_plan_zoom()
    {
        Assert.Equal(1_000_000d, FloorPlanPreviewControl.MaximumUserZoomFactor);
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
    public void PreserveZoomStateForBaseViewportChange_keeps_the_anchor_world_point_on_screen()
    {
        var oldBaseViewport = new FloorPlanPreviewGeometry.PreviewViewport(
            new Rect(0, 0, 1200, 700),
            MinX: 0d,
            MinY: 0d,
            Scale: 2d,
            OffsetX: 120d,
            OffsetY: 90d);
        var newBaseViewport = new FloorPlanPreviewGeometry.PreviewViewport(
            new Rect(0, 0, 1200, 700),
            MinX: 1d,
            MinY: 0d,
            Scale: 2.1d,
            OffsetX: 118d,
            OffsetY: 90d);
        var anchorScreenPoint = new Point(600d, 350d);
        var currentState = new FloorPlanPreviewControl.PreviewZoomState(
            ZoomFactor: 1.35d,
            PanOffset: new Vector(24d, -16d));
        var worldAtAnchorBefore = oldBaseViewport
            .WithUserTransform(currentState.ZoomFactor, currentState.PanOffset)
            .Unproject(anchorScreenPoint);

        var nextState = FloorPlanPreviewControl.PreserveZoomStateForBaseViewportChange(
            oldBaseViewport,
            newBaseViewport,
            currentState,
            anchorScreenPoint);
        var projectedAfter = newBaseViewport
            .WithUserTransform(nextState.ZoomFactor, nextState.PanOffset)
            .Project(worldAtAnchorBefore.X, worldAtAnchorBefore.Y);

        Assert.Equal(currentState.ZoomFactor, nextState.ZoomFactor);
        Assert.InRange(Math.Abs(anchorScreenPoint.X - projectedAfter.X), 0d, 0.001d);
        Assert.InRange(Math.Abs(anchorScreenPoint.Y - projectedAfter.Y), 0d, 0.001d);
    }

    [Fact]
    public void ShouldPreserveViewportOnBoundsChange_only_when_render_size_changes()
    {
        var original = new Rect(0, 0, 1200, 700);
        var movedOnly = new Rect(10, 20, 1200, 700);
        var resized = new Rect(0, 0, 1200, 640);

        Assert.False(FloorPlanPreviewControl.ShouldPreserveViewportOnBoundsChange(original, original));
        Assert.False(FloorPlanPreviewControl.ShouldPreserveViewportOnBoundsChange(original, movedOnly));
        Assert.True(FloorPlanPreviewControl.ShouldPreserveViewportOnBoundsChange(original, resized));
    }

    [Fact]
    public void CalculateChangePreviewGhostOpacity_fades_old_geometry_out()
    {
        var duration = TimeSpan.FromMilliseconds(260);

        Assert.Equal(1d, FloorPlanPreviewControl.CalculateChangePreviewGhostOpacity(TimeSpan.Zero, duration));
        Assert.Equal(0.5d, FloorPlanPreviewControl.CalculateChangePreviewGhostOpacity(TimeSpan.FromMilliseconds(130), duration));
        Assert.Equal(0d, FloorPlanPreviewControl.CalculateChangePreviewGhostOpacity(TimeSpan.FromMilliseconds(300), duration));
    }

    [Fact]
    public void ShouldStartChangePreviewAnimation_suppresses_manual_floor_plan_move_geometry_changes()
    {
        var shouldAnimate = FloorPlanPreviewControl.ShouldStartChangePreviewAnimation(
            PreviewCollectionObserverHub.PreviewObservedCollectionSlot.GeometryPaths,
            hasActiveFloorPlanMove: true);

        Assert.False(shouldAnimate);
    }

    [Fact]
    public void ShouldStartChangePreviewAnimation_keeps_auto_fit_geometry_changes_animated()
    {
        Assert.True(FloorPlanPreviewControl.ShouldStartChangePreviewAnimation(
            PreviewCollectionObserverHub.PreviewObservedCollectionSlot.GeometryPaths,
            hasActiveFloorPlanMove: false));
        Assert.False(FloorPlanPreviewControl.ShouldStartChangePreviewAnimation(
            PreviewCollectionObserverHub.PreviewObservedCollectionSlot.Dimensions,
            hasActiveFloorPlanMove: false));
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
    public void CadViewportContext_create_reports_world_units_per_pixel_and_default_1_2_5_grid_spacing()
    {
        var viewport = new FloorPlanPreviewGeometry.PreviewViewport(
            new Rect(0, 0, 800, 600),
            MinX: 0d,
            MinY: 0d,
            Scale: 2d,
            OffsetX: 100d,
            OffsetY: 80d);

        var context = CadViewportContext.Create(viewport);

        Assert.Equal(0.5d, context.WorldUnitsPerPixel);
        Assert.Equal(10d, context.MinorGridSpacingWorld);
        Assert.Equal(50d, context.MajorGridSpacingWorld);
        Assert.Equal(4d, context.SnappingToleranceWorld);
    }

    [Fact]
    public void GetWorldLinePositions_returns_origin_anchored_grid_coordinates()
    {
        var positions = PreviewWorkspaceRenderer.GetWorldLinePositions(minWorld: -12d, maxWorld: 27d, spacingWorld: 10d);

        Assert.Equal([-10d, 0d, 10d, 20d], positions);
    }

    [Fact]
    public void CompressionHandlePreviewLayerRenderer_hides_handles_when_pinch_placement_is_armed()
    {
        var selectedGroupId = Guid.NewGuid();
        var marker = CreatePinchMarker(selectedGroupId, "Height");

        var handles = CompressionHandlePreviewLayerRenderer.GetVisibleHandles(
            new Rect(0, 0, 1000, 700),
            Domain.FloorPlans.PinchAxisTag.Height,
            isPinchPlacementArmed: true,
            [marker],
            selectedGroupId);

        Assert.Empty(handles);
    }

    [Fact]
    public void CompressionHandlePreviewLayerRenderer_hides_handles_when_no_selected_group_markers_can_drive_preview()
    {
        var selectedGroupId = Guid.NewGuid();
        var otherGroupMarker = CreatePinchMarker(Guid.NewGuid(), "Height");

        var handles = CompressionHandlePreviewLayerRenderer.GetVisibleHandles(
            new Rect(0, 0, 1000, 700),
            Domain.FloorPlans.PinchAxisTag.Height,
            isPinchPlacementArmed: false,
            [otherGroupMarker],
            selectedGroupId);

        Assert.Empty(handles);
    }

    [Fact]
    public void CompressionHandlePreviewLayerRenderer_returns_height_handles_when_selected_group_has_axis_markers()
    {
        var selectedGroupId = Guid.NewGuid();
        var marker = CreatePinchMarker(selectedGroupId, "Height");

        var handles = CompressionHandlePreviewLayerRenderer.GetVisibleHandles(
            new Rect(0, 0, 1000, 700),
            Domain.FloorPlans.PinchAxisTag.Height,
            isPinchPlacementArmed: false,
            [marker],
            selectedGroupId);

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
    public void PinchMarkerPreviewLayerRenderer_returns_no_preview_markers_without_selected_group()
    {
        var marker = CreatePinchMarker(Guid.NewGuid(), "Height");

        var filtered = PinchMarkerPreviewLayerRenderer.FilterForPreviewGroup(
            [marker],
            previewPinchGroupId: null);

        Assert.Empty(filtered!);
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
