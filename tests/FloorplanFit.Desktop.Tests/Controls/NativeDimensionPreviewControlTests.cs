using Avalonia;
using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls;
using FloorplanFit.Desktop.Controls.Preview;
using FloorplanFit.Domain.FloorPlans;
using Xunit;

namespace FloorplanFit.Desktop.Tests.Controls;

public sealed class NativeDimensionPreviewControlTests
{
    [Fact]
    public void TryResolveDimensionHit_prefers_dimension_text_and_lines_before_generic_geometry()
    {
        var pathId = Guid.NewGuid();
        var viewport = FloorPlanPreviewGeometry.CalculateViewport(
            [new GeometryPathDto(pathId, false, [new GeometrySegmentDto(pathId, 1, 0m, 0m, 300m, 0m)])],
            new Rect(0, 0, 800, 600),
            48d)!.Value;
        var dimension = CreateDimension();
        var pointer = viewport.Project(162m, 148m);

        var hit = FloorPlanPreviewControl.TryResolveDimensionHit(
            [dimension],
            viewport,
            pointer,
            hitTolerancePixels: 8d);

        Assert.NotNull(hit);
        Assert.Equal(dimension.DimensionId, hit.Value.Dimension.DimensionId);
        Assert.Equal(FloorPlanPreviewControl.DimensionHandleKind.DimensionLinePoint, hit.Value.SuggestedHandle);
    }

    [Fact]
    public void ApplyDimensionHandleDelta_moves_native_dimension_in_world_units_and_updates_primitives()
    {
        var dimension = CreateDimension();

        var edited = FloorPlanPreviewControl.ApplyDimensionHandleDelta(
            dimension,
            FloorPlanPreviewControl.DimensionHandleKind.SecondDefinitionPoint,
            24m,
            0m);

        Assert.Equal(248m, edited.DefPoint2X);
        Assert.Equal(148m, edited.MeasurementSourceUnits);
        Assert.Equal(3759.2m, edited.MeasurementMillimeters);
        Assert.Equal("12'-4\"", edited.DisplayText);
        Assert.Equal(248m, edited.LinePrimitives[1].StartX);
        Assert.Equal(248m, edited.LinePrimitives[1].EndX);
        Assert.Equal(248m, edited.LinePrimitives[2].EndX);
        Assert.Equal("12'-4\"", edited.TextPrimitives[0].Text);
        Assert.Equal(248m, edited.InsertPrimitives[1].X);
    }

    [Fact]
    public void ApplyDimensionHandleDelta_replaces_dimension_placeholder_override_and_preserves_suffix()
    {
        var dimension = CreateDimension() with
        {
            RawTextOverride = "<> TO CL. OF EXH. VENT",
            DisplayText = "10'-4\" TO CL. OF EXH. VENT",
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto("TEXT-1", 1, "10'-4\" TO CL. OF EXH. VENT", 162m, 148m, 3.5m, 0m)
                {
                    StyleName = "ARCH",
                    AttachmentPoint = "MiddleCenter"
                }
            ]
        };

        var edited = FloorPlanPreviewControl.ApplyDimensionHandleDelta(
            dimension,
            FloorPlanPreviewControl.DimensionHandleKind.SecondDefinitionPoint,
            24m,
            0m);

        Assert.Equal(148m, edited.MeasurementSourceUnits);
        Assert.Equal("12'-4\" TO CL. OF EXH. VENT", edited.DisplayText);
        Assert.Equal("12'-4\" TO CL. OF EXH. VENT", edited.TextPrimitives[0].Text);
    }

    [Fact]
    public void ResolveHandles_exposes_only_the_two_visible_terminal_extents_without_a_center_grip()
    {
        var dimension = CreateDimension();

        var handles = NativeDimensionEditor.ResolveHandles(dimension);

        Assert.Equal(2, handles.Count);
        Assert.DoesNotContain(handles, item => item.HandleKind == FloorPlanPreviewControl.DimensionHandleKind.DimensionLinePoint);
        Assert.Contains(handles, item => item.WorldPoint == new Point(100d, 140d));
        Assert.Contains(handles, item => item.WorldPoint == new Point(224d, 140d));
    }

    [Fact]
    public void ApplyDimensionHandleDelta_preserves_split_linear_family_when_dragging_the_dimension_body()
    {
        var dimension = CreateSplitLinearDimension();

        var edited = FloorPlanPreviewControl.ApplyDimensionHandleDelta(
            dimension,
            FloorPlanPreviewControl.DimensionHandleKind.DimensionLinePoint,
            12m,
            0m);

        Assert.Equal(4, edited.LinePrimitives.Count);
        Assert.Equal(dimension.MeasurementSourceUnits, edited.MeasurementSourceUnits);
        Assert.Equal(dimension.MeasurementMillimeters, edited.MeasurementMillimeters);
        Assert.Equal(dimension.DisplayText, edited.DisplayText);
        AssertLineExists(edited.LinePrimitives, 357.574189m, 183.378754m, 442.377243m, 183.378754m);
        AssertLineExists(edited.LinePrimitives, 363.244655m, 177.378754m, 442.377243m, 177.378754m);
        AssertLineExists(edited.LinePrimitives, 438.377243m, 186.878754m, 438.377243m, 190.378754m);
        AssertLineExists(edited.LinePrimitives, 438.377243m, 173.878754m, 438.377243m, 170.378754m);
        Assert.Equal(438.377243m, edited.InsertPrimitives[0].X);
        Assert.Equal(438.377243m, edited.InsertPrimitives[1].X);
        Assert.Equal(433.016422m, edited.TextPrimitives[0].X);
    }

    [Fact]
    public void ApplyDimensionHandleDelta_preserves_leader_linear_family_when_dragging_the_dimension_body()
    {
        var dimension = CreateLeaderLinearDimension();

        var edited = FloorPlanPreviewControl.ApplyDimensionHandleDelta(
            dimension,
            FloorPlanPreviewControl.DimensionHandleKind.DimensionLinePoint,
            10m,
            0m);

        Assert.Equal(5, edited.LinePrimitives.Count);
        Assert.Equal(dimension.MeasurementSourceUnits, edited.MeasurementSourceUnits);
        Assert.Equal(dimension.MeasurementMillimeters, edited.MeasurementMillimeters);
        Assert.Equal(dimension.DisplayText, edited.DisplayText);
        AssertLineExists(edited.LinePrimitives, 437.602465m, 631.378754m, 439.753330m, 631.378754m);
        AssertLineExists(edited.LinePrimitives, 437.602465m, 645.378754m, 439.753330m, 645.378754m);
        AssertLineExists(edited.LinePrimitives, 435.753330m, 634.878754m, 435.753330m, 641.878754m);
        AssertLineExists(edited.LinePrimitives, 435.753330m, 638.378754m, 427.375883m, 645.331944m);
        AssertLineExists(edited.LinePrimitives, 427.375883m, 645.331944m, 427.375883m, 655.665277m);
        Assert.Equal(435.753330m, edited.InsertPrimitives[0].X);
        Assert.Equal(435.753330m, edited.InsertPrimitives[1].X);
        Assert.Equal(423.792550m, edited.TextPrimitives[0].X);
    }

    [Fact]
    public void ApplyDimensionHandleDelta_preserves_c_shape_when_line_primitives_are_in_non_canonical_order()
    {
        // DXF stores the line primitives in a non-canonical order: roof first,
        // then right leg, then left leg. The handles must still resolve to the real
        // C-shape corners, the drag must preserve topology, and the resulting line
        // primitives must remain visually anchored to the original geometry.
        var dimension = new DimensionDto(
            Guid.NewGuid(),
            "DIMENSION:NONCANON",
            "DIMS",
            "DIMENSION",
            "*D200",
            "10'-4\"",
            "GeometryBlock",
            string.Empty,
            124m,
            3149.6m,
            "Inch",
            0,
            0m,
            0m,
            999m, // intentionally wrong DefPoint1 — must be ignored in favor of geometry
            999m,
            0m,
            999m,
            999m,
            0m,
            999m,
            999m,
            0m,
            0.99m,
            null,
            1)
        {
            SourceHandle = "NONCANON",
            RenderTextX = 162m,
            RenderTextY = 148m,
            RenderTextHeight = 3.5m,
            RenderTextStyleName = "ARCH",
            RenderTextAttachmentPoint = "MiddleCenter",
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto("ROOF", 1, 100m, 140m, 224m, 140m),
                new DimensionLinePrimitiveDto("RIGHT-LEG", 2, 224m, 140m, 224m, 100m),
                new DimensionLinePrimitiveDto("LEFT-LEG", 3, 100m, 140m, 100m, 100m)
            ],
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto("TEXT-1", 1, "10'-4\"", 162m, 148m, 3.5m, 0m)
                {
                    StyleName = "ARCH",
                    AttachmentPoint = "MiddleCenter"
                }
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("INSERT-1", 1, "_Dot", 100m, 140m, 0m),
                new DimensionInsertPrimitiveDto("INSERT-2", 2, "_Dot", 224m, 140m, 0m)
            ]
        };

        var edited = FloorPlanPreviewControl.ApplyDimensionHandleDelta(
            dimension,
            FloorPlanPreviewControl.DimensionHandleKind.SecondDefinitionPoint,
            24m,
            0m);

        // After the drag, the right wall point moved by +24 along the measure axis.
        Assert.Equal(248m, edited.DefPoint2X);
        Assert.Equal(100m, edited.DefPoint2Y);
        Assert.Equal(100m, edited.DefPointX); // BaseLeft synced from real geometry
        Assert.Equal(100m, edited.DefPointY);
        Assert.Equal(148m, edited.MeasurementSourceUnits);

        Assert.Equal(3, edited.LinePrimitives.Count);
        var roof = edited.LinePrimitives[0];
        var rightLeg = edited.LinePrimitives[1];
        var leftLeg = edited.LinePrimitives[2];

        Assert.Contains(100m, new[] { roof.StartX, roof.EndX });
        Assert.Contains(248m, new[] { roof.StartX, roof.EndX });
        Assert.True(rightLeg.StartX == 248m && rightLeg.EndX == 248m, $"Right leg X did not follow drag: {rightLeg.StartX}, {rightLeg.EndX}");
        Assert.True(leftLeg.StartX == 100m && leftLeg.EndX == 100m, $"Left leg X drifted: {leftLeg.StartX}, {leftLeg.EndX}");
    }

    [Fact]
    public void ApplyDimensionHandleDelta_preserves_c_shape_when_extras_precede_the_c_shape_lines()
    {
        // Simulates a DXF block where AutoCAD wrote tick marks first and the actual
        // C-shape lines later. The previous algorithm only inspected the first 3
        // line primitives and failed to identify the C; this test guards against
        // that regression by placing two short "tick marks" at the head of the
        // LinePrimitives array.
        var dimension = new DimensionDto(
            Guid.NewGuid(),
            "DIMENSION:EXTRAS-FIRST",
            "DIMS",
            "DIMENSION",
            "*D300",
            "10'-4\"",
            "GeometryBlock",
            string.Empty,
            124m,
            3149.6m,
            "Inch",
            0,
            0m,
            0m,
            999m, // intentionally wrong DefPoint — should be ignored
            999m,
            0m,
            999m,
            999m,
            0m,
            999m,
            999m,
            0m,
            0.99m,
            null,
            1)
        {
            SourceHandle = "EXTRAS",
            RenderTextX = 162m,
            RenderTextY = 148m,
            RenderTextHeight = 3.5m,
            RenderTextStyleName = "ARCH",
            RenderTextAttachmentPoint = "MiddleCenter",
            LinePrimitives =
            [
                // Tick marks first (extras AutoCAD wrote into the block)
                new DimensionLinePrimitiveDto("TICK-A", 1, 95m, 145m, 105m, 135m),
                new DimensionLinePrimitiveDto("TICK-B", 2, 219m, 145m, 229m, 135m),
                // Then the C-shape lines, out of canonical order
                new DimensionLinePrimitiveDto("ROOF", 3, 100m, 140m, 224m, 140m),
                new DimensionLinePrimitiveDto("RIGHT-LEG", 4, 224m, 140m, 224m, 100m),
                new DimensionLinePrimitiveDto("LEFT-LEG", 5, 100m, 140m, 100m, 100m)
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("INSERT-RIGHT", 1, "_Dot", 224m, 140m, 0m),
                new DimensionInsertPrimitiveDto("INSERT-LEFT", 2, "_Dot", 100m, 140m, 0m)
            ]
        };

        var edited = FloorPlanPreviewControl.ApplyDimensionHandleDelta(
            dimension,
            FloorPlanPreviewControl.DimensionHandleKind.SecondDefinitionPoint,
            24m,
            0m);

        // Right wall must have moved +24 along the measure axis, left wall stays.
        Assert.Equal(248m, edited.DefPoint2X);
        Assert.Equal(100m, edited.DefPoint2Y);
        Assert.Equal(100m, edited.DefPointX);
        Assert.Equal(100m, edited.DefPointY);

        Assert.Equal(5, edited.LinePrimitives.Count);
        var tickA = edited.LinePrimitives[0];
        var tickB = edited.LinePrimitives[1];
        var roof = edited.LinePrimitives[2];
        var rightLeg = edited.LinePrimitives[3];
        var leftLeg = edited.LinePrimitives[4];

        Assert.Equal(95m, tickA.StartX);
        Assert.Equal(105m, tickA.EndX);
        Assert.Equal(243m, tickB.StartX);
        Assert.Equal(253m, tickB.EndX);
        Assert.True((leftLeg.StartX == 100m && leftLeg.EndX == 100m), $"Left leg moved unexpectedly: {leftLeg.StartX},{leftLeg.EndX}");
        Assert.True((rightLeg.StartX == 248m && rightLeg.EndX == 248m), $"Right leg did not follow drag: {rightLeg.StartX},{rightLeg.EndX}");
        Assert.Contains(100m, new[] { roof.StartX, roof.EndX });
        Assert.Contains(248m, new[] { roof.StartX, roof.EndX });

        Assert.Equal(248m, edited.InsertPrimitives[0].X);
        Assert.Equal(100m, edited.InsertPrimitives[1].X);
    }

    [Fact]
    public void BuildRenderedDimensions_preserves_authored_dimension_geometry_when_there_is_no_manual_edit()
    {
        var dimension = CreateDimension();

        var rendered = DimensionPreviewProjector.BuildRenderedDimensions([dimension], activeEdit: null);

        var preserved = Assert.Single(rendered);
        Assert.Same(dimension, preserved);
        Assert.Equal(100m, preserved.LinePrimitives[0].StartX);
        Assert.Equal(224m, preserved.LinePrimitives[2].EndX);
        Assert.Equal(162m, preserved.TextPrimitives[0].X);
        Assert.Equal(100m, preserved.InsertPrimitives[0].X);
    }

    [Fact]
    public void BuildRenderedDimensions_preserves_high_zoom_dimension_edit_precision()
    {
        var dimension = CreateDimension();

        var rendered = DimensionPreviewProjector.BuildRenderedDimensions(
            [dimension],
            new DimensionPreviewProjector.DimensionPreviewEditRequest(
                dimension.DimensionId,
                dimension,
                FloorPlanPreviewControl.DimensionHandleKind.SecondDefinitionPoint,
                new Point(224d, 140d),
                new Point(224.000001d, 140d)));

        var edited = Assert.Single(rendered);
        Assert.Equal(224.000001m, edited.DefPoint2X);
        Assert.Equal(224.000001m, edited.LinePrimitives[1].StartX);
        Assert.Equal(224.000001m, edited.InsertPrimitives[1].X);
        Assert.Equal("10'-4\"", edited.DisplayText);
    }

    [Fact]
    public void BuildRenderedDimensionsForPreview_keeps_authored_linear_dimensions_static_even_when_preview_geometry_moves()
    {
        var wallPathId = Guid.NewGuid();
        var openingPathId = Guid.NewGuid();
        var wallCandidateId = Guid.NewGuid();
        var openingCandidateId = Guid.NewGuid();
        IReadOnlyList<GeometryPathDto> baseGeometry =
        [
            new(wallPathId, false, [new GeometrySegmentDto(wallPathId, 1, 100m, 100m, 100m, 140m)]),
            new(openingPathId, false, [new GeometrySegmentDto(openingPathId, 1, 224m, 100m, 224m, 140m)])
        ];
        IReadOnlyList<GeometryPathDto> previewGeometry =
        [
            new(wallPathId, false, [new GeometrySegmentDto(wallPathId, 1, 100m, 100m, 100m, 140m)]),
            new(openingPathId, false, [new GeometrySegmentDto(openingPathId, 1, 260m, 100m, 260m, 140m)])
        ];
        IReadOnlyList<WallCandidateDto> wallCandidates =
        [
            new(wallCandidateId, "LINE:WALL", "WALLS", "Accepted", 0.95m, null, null, wallPathId, 1)
        ];
        IReadOnlyList<OpeningCandidateDto> openingCandidates =
        [
            new(openingCandidateId, "LINE:OPENING", "DOORS", "Door", "LINE", openingPathId, 0.95m, null, 1)
        ];
        var measurementContext = new MeasurementContextDto("Inch", 25.4m, 1m, 1m);
        var dimension = CreateDimension();
        var baseEdges = MeasurableEdgeProjector.Build(baseGeometry, wallCandidates, openingCandidates, measurementContext);
        var associations = DimensionAssociationProjector.Build([dimension], baseEdges, measurementContext);
        var bindings = DimensionBindingProjector.Build([dimension], associations);

        var rendered = FloorPlanPreviewControl.BuildRenderedDimensionsForPreview(
            [dimension],
            associations,
            bindings,
            previewGeometry,
            wallCandidates,
            openingCandidates,
            measurementContext,
            useReactivePreview: true,
            activeEdit: null);

        var updated = Assert.Single(rendered);
        Assert.Equal(100m, updated.DefPointX);
        Assert.Equal(224m, updated.DefPoint2X);
        Assert.Equal(124m, updated.MeasurementSourceUnits);
        Assert.Equal("10'-4\"", updated.DisplayText);
        Assert.Equal(224m, updated.LinePrimitives[1].StartX);
        Assert.Equal(224m, updated.InsertPrimitives[1].X);
    }

    [Fact]
    public void BuildRenderedDimensionsForPreview_keeps_authored_linear_dimensions_static_even_when_preview_path_splits()
    {
        var wallPathId = Guid.NewGuid();
        var openingPathId = Guid.NewGuid();
        var wallCandidateId = Guid.NewGuid();
        var openingCandidateId = Guid.NewGuid();
        IReadOnlyList<GeometryPathDto> baseGeometry =
        [
            new(wallPathId, false, [new GeometrySegmentDto(wallPathId, 1, 100m, 100m, 100m, 140m)]),
            new(openingPathId, false, [new GeometrySegmentDto(openingPathId, 1, 224m, 100m, 224m, 140m)])
        ];
        IReadOnlyList<GeometryPathDto> previewGeometry =
        [
            new(wallPathId, false, [new GeometrySegmentDto(wallPathId, 1, 90m, 100m, 90m, 140m)]),
            new(openingPathId, false,
            [
                new GeometrySegmentDto(openingPathId, 1, 240m, 100m, 240m, 120m),
                new GeometrySegmentDto(openingPathId, 2, 240m, 120m, 240m, 160m)
            ])
        ];
        IReadOnlyList<WallCandidateDto> wallCandidates =
        [
            new(wallCandidateId, "LINE:WALL", "WALLS", "Accepted", 0.95m, null, null, wallPathId, 1)
        ];
        IReadOnlyList<OpeningCandidateDto> openingCandidates =
        [
            new(openingCandidateId, "LINE:OPENING", "DOORS", "Door", "LINE", openingPathId, 0.95m, null, 1)
        ];
        var measurementContext = new MeasurementContextDto("Inch", 25.4m, 1m, 1m);
        var dimension = CreateDimension() with
        {
            DefPointY = 120m,
            DefPoint2Y = 120m,
            DefPoint3Y = 160m,
            RenderTextY = 168m,
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto("LINE-1", 1, 100m, 160m, 100m, 120m),
                new DimensionLinePrimitiveDto("LINE-2", 2, 224m, 160m, 224m, 120m),
                new DimensionLinePrimitiveDto("LINE-3", 3, 100m, 160m, 224m, 160m)
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("INSERT-1", 1, "_Dot", 100m, 160m, 0m),
                new DimensionInsertPrimitiveDto("INSERT-2", 2, "_Dot", 224m, 160m, 0m)
            ],
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto("TEXT-1", 1, "10'-4\"", 162m, 168m, 3.5m, 0m)
                {
                    StyleName = "ARCH",
                    AttachmentPoint = "MiddleCenter"
                }
            ]
        };
        var baseEdges = MeasurableEdgeProjector.Build(baseGeometry, wallCandidates, openingCandidates, measurementContext);
        var associations = DimensionAssociationProjector.Build([dimension], baseEdges, measurementContext);
        var bindings = DimensionBindingProjector.Build([dimension], associations);

        var rendered = FloorPlanPreviewControl.BuildRenderedDimensionsForPreview(
            [dimension],
            associations,
            bindings,
            previewGeometry,
            wallCandidates,
            openingCandidates,
            measurementContext,
            useReactivePreview: true,
            activeEdit: null);

        var updated = Assert.Single(rendered);
        Assert.Equal(100m, updated.DefPointX);
        Assert.Equal(224m, updated.DefPoint2X);
        Assert.Equal(124m, updated.MeasurementSourceUnits);
        Assert.Equal("10'-4\"", updated.DisplayText);
        Assert.Equal(224m, updated.LinePrimitives[1].StartX);
        Assert.Equal(224m, updated.InsertPrimitives[1].X);
    }

    [Fact]
    public void BuildRenderedDimensionsForPreview_keeps_unresolved_dimensions_authored_static()
    {
        var wallPathId = Guid.NewGuid();
        var openingPathId = Guid.NewGuid();
        var wallCandidateId = Guid.NewGuid();
        var openingCandidateId = Guid.NewGuid();
        IReadOnlyList<GeometryPathDto> previewGeometry =
        [
            new(wallPathId, false, [new GeometrySegmentDto(wallPathId, 1, 90m, 100m, 90m, 140m)]),
            new(openingPathId, false, [new GeometrySegmentDto(openingPathId, 1, 260m, 100m, 260m, 140m)])
        ];
        IReadOnlyList<WallCandidateDto> wallCandidates =
        [
            new(wallCandidateId, "LINE:WALL", "WALLS", "Accepted", 0.95m, null, null, wallPathId, 1)
        ];
        IReadOnlyList<OpeningCandidateDto> openingCandidates =
        [
            new(openingCandidateId, "LINE:OPENING", "DOORS", "Door", "LINE", openingPathId, 0.95m, null, 1)
        ];
        var measurementContext = new MeasurementContextDto("Inch", 25.4m, 1m, 1m);
        var dimension = CreateDimension();
        IReadOnlyList<DimensionAssociationDto> unresolvedAssociations =
        [
            new(
                dimension.DimensionId,
                "MeasuredEndpointAnchors",
                false,
                0.35m,
                "start ambiguous; end unresolved.")
            {
                StartAnchor = new DimensionAnchorReferenceDto("edge-a", "WallCandidate", wallCandidateId, wallPathId, "Start", 100m, 100m, 0m)
            }
        ];

        var rendered = FloorPlanPreviewControl.BuildRenderedDimensionsForPreview(
            [dimension],
            unresolvedAssociations,
            [],
            previewGeometry,
            wallCandidates,
            openingCandidates,
            measurementContext,
            useReactivePreview: true,
            activeEdit: null);

        var preserved = Assert.Single(rendered);
        Assert.Same(dimension, preserved);
        Assert.Equal(224m, preserved.DefPoint2X);
        Assert.Equal(124m, preserved.MeasurementSourceUnits);
        Assert.Equal("10'-4\"", preserved.DisplayText);
    }

    [Fact]
    public void BuildRenderedDimensionsForPreview_keeps_authored_ordinate_x_dimensions_static_even_when_preview_geometry_moves()
    {
        var wallPathId = Guid.NewGuid();
        var openingPathId = Guid.NewGuid();
        var wallCandidateId = Guid.NewGuid();
        var openingCandidateId = Guid.NewGuid();
        IReadOnlyList<GeometryPathDto> baseGeometry =
        [
            new(wallPathId, false, [new GeometrySegmentDto(wallPathId, 1, 100m, 100m, 100m, 140m)]),
            new(openingPathId, false, [new GeometrySegmentDto(openingPathId, 1, 260m, 140m, 260m, 180m)])
        ];
        IReadOnlyList<GeometryPathDto> previewGeometry =
        [
            new(wallPathId, false, [new GeometrySegmentDto(wallPathId, 1, 100m, 100m, 100m, 140m)]),
            new(openingPathId, false, [new GeometrySegmentDto(openingPathId, 1, 320m, 140m, 320m, 180m)])
        ];
        IReadOnlyList<WallCandidateDto> wallCandidates =
        [
            new(wallCandidateId, "LINE:WALL", "WALLS", "Accepted", 0.95m, null, null, wallPathId, 1)
        ];
        IReadOnlyList<OpeningCandidateDto> openingCandidates =
        [
            new(openingCandidateId, "LINE:OPENING", "DOORS", "Door", "LINE", openingPathId, 0.95m, null, 1)
        ];
        var measurementContext = new MeasurementContextDto("Inch", 25.4m, 1m, 1m);
        var dimension = CreateOrdinateXDimension();
        var baseEdges = MeasurableEdgeProjector.Build(baseGeometry, wallCandidates, openingCandidates, measurementContext);
        var associations = DimensionAssociationProjector.Build([dimension], baseEdges, measurementContext);
        var bindings = DimensionBindingProjector.Build([dimension], associations);

        var rendered = FloorPlanPreviewControl.BuildRenderedDimensionsForPreview(
            [dimension],
            associations,
            bindings,
            previewGeometry,
            wallCandidates,
            openingCandidates,
            measurementContext,
            useReactivePreview: true,
            activeEdit: null);

        var updated = Assert.Single(rendered);
        Assert.Equal(100m, updated.DefPointX);
        Assert.Equal(100m, updated.DefPointY);
        Assert.Equal(260m, updated.DefPoint2X);
        Assert.Equal(140m, updated.DefPoint2Y);
        Assert.Equal(290m, updated.DefPoint3X);
        Assert.Equal(170m, updated.DefPoint3Y);
        Assert.Equal(160m, updated.MeasurementSourceUnits);
        Assert.Equal("13'-4\"", updated.DisplayText);
        Assert.Equal(260m, updated.LinePrimitives[0].StartX);
        Assert.Equal(290m, updated.LinePrimitives[0].EndX);
        Assert.Equal(304m, updated.TextPrimitives[0].X);
    }

    [Fact]
    public void BuildRenderedDimensionsForPreview_keeps_authored_diameter_dimensions_static_even_when_preview_geometry_moves()
    {
        var leftPathId = Guid.NewGuid();
        var rightPathId = Guid.NewGuid();
        var leftCandidateId = Guid.NewGuid();
        var rightCandidateId = Guid.NewGuid();
        IReadOnlyList<GeometryPathDto> baseGeometry =
        [
            new(leftPathId, false, [new GeometrySegmentDto(leftPathId, 1, 80m, 100m, 80m, 100m)]),
            new(rightPathId, false, [new GeometrySegmentDto(rightPathId, 1, 120m, 100m, 120m, 100m)])
        ];
        IReadOnlyList<GeometryPathDto> previewGeometry =
        [
            new(leftPathId, false, [new GeometrySegmentDto(leftPathId, 1, 100m, 40m, 100m, 40m)]),
            new(rightPathId, false, [new GeometrySegmentDto(rightPathId, 1, 100m, 160m, 100m, 160m)])
        ];
        IReadOnlyList<WallCandidateDto> wallCandidates =
        [
            new(leftCandidateId, "LINE:LEFT", "WALLS", "Accepted", 0.95m, null, null, leftPathId, 1),
            new(rightCandidateId, "LINE:RIGHT", "WALLS", "Accepted", 0.95m, null, null, rightPathId, 1)
        ];
        IReadOnlyList<OpeningCandidateDto> openingCandidates = [];
        var measurementContext = new MeasurementContextDto("Inch", 25.4m, 1m, 1m);
        var dimension = CreateDiameterDimension();
        var baseEdges = MeasurableEdgeProjector.Build(baseGeometry, wallCandidates, openingCandidates, measurementContext);
        var associations = DimensionAssociationProjector.Build([dimension], baseEdges, measurementContext);
        var bindings = DimensionBindingProjector.Build([dimension], associations);

        var rendered = FloorPlanPreviewControl.BuildRenderedDimensionsForPreview(
            [dimension],
            associations,
            bindings,
            previewGeometry,
            wallCandidates,
            openingCandidates,
            measurementContext,
            useReactivePreview: true,
            activeEdit: null);

        var updated = Assert.Single(rendered);
        Assert.Equal(80m, updated.DefPointX);
        Assert.Equal(100m, updated.DefPointY);
        Assert.Equal(120m, updated.DefPoint2X);
        Assert.Equal(100m, updated.DefPoint2Y);
        Assert.Equal(110m, updated.DefPoint3X);
        Assert.Equal(120m, updated.DefPoint3Y);
        Assert.Equal(40m, updated.MeasurementSourceUnits);
        Assert.Equal("3'-4\"", updated.DisplayText);
        Assert.Equal(80m, updated.LinePrimitives[0].StartX);
        Assert.Equal(100m, updated.LinePrimitives[0].StartY);
        Assert.Equal(120m, updated.LinePrimitives[0].EndX);
        Assert.Equal(100m, updated.LinePrimitives[0].EndY);
        Assert.Equal(120m, updated.TextPrimitives[0].X);
        Assert.Equal(125m, updated.TextPrimitives[0].Y);
    }

    [Fact]
    public void BuildRenderedDimensionsForPreview_reprojects_manual_verified_interval_bindings_when_the_selected_pinch_group_overlaps()
    {
        var wallPathId = Guid.NewGuid();
        var openingPathId = Guid.NewGuid();
        var wallCandidateId = Guid.NewGuid();
        var openingCandidateId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var corridorId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        IReadOnlyList<GeometryPathDto> previewGeometry =
        [
            new(wallPathId, false, [new GeometrySegmentDto(wallPathId, 1, 100m, 100m, 100m, 140m)]),
            new(openingPathId, false, [new GeometrySegmentDto(openingPathId, 1, 260m, 100m, 260m, 140m)])
        ];
        IReadOnlyList<WallCandidateDto> wallCandidates =
        [
            new(wallCandidateId, "LINE:WALL", "WALLS", "Accepted", 0.95m, null, null, wallPathId, 1)
        ];
        IReadOnlyList<OpeningCandidateDto> openingCandidates =
        [
            new(openingCandidateId, "LINE:OPENING", "DOORS", "Door", "LINE", openingPathId, 0.95m, null, 1)
        ];
        IReadOnlyList<MeasurementCorridorDto> corridors =
        [
            new(corridorId, "Patio-Width", "Width", wallPathId, 95m, 145m, "Verified", 1)
        ];
        IReadOnlyList<MeasurementNodeDto> nodes =
        [
            new(startNodeId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, wallCandidateId, wallPathId, "Projected", 100m, 120m, 100m, 0m, 0m, 0.5m),
            new(endNodeId, corridorId, 2, "ProjectedGeometry", FloorPlanArtifactSourceKinds.OpeningCandidate, openingCandidateId, openingPathId, "Projected", 224m, 120m, 224m, 0m, 0m, 0.5m)
        ];
        IReadOnlyList<DimensionIntervalBindingDto> intervalBindings =
        [
            new(CreateDimension().DimensionId, corridorId, startNodeId, endNodeId, "ManualVerified", 100m, 224m)
        ];
        var dimension = CreateDimension() with
        {
            DimensionId = intervalBindings[0].DimensionId
        };
        IReadOnlyList<ArticulationBandDto> articulationBands =
        [
            new(pinchGroupId, "Patio", "Width", 150m, 240m, 120m, "Verified")
        ];

        var rendered = FloorPlanPreviewControl.BuildRenderedDimensionsForPreview(
            [dimension],
            [],
            [],
            previewGeometry,
            wallCandidates,
            openingCandidates,
            new MeasurementContextDto("Inch", 25.4m, 1m, 1m),
            useReactivePreview: true,
            activeEdit: null,
            measurementCorridors: corridors,
            measurementNodes: nodes,
            dimensionIntervalBindings: intervalBindings,
            articulationBands: articulationBands,
            previewPinchGroupId: pinchGroupId);

        var updated = Assert.Single(rendered);
        Assert.Equal(260m, updated.DefPoint2X);
        Assert.Equal(160m, updated.MeasurementSourceUnits);
        Assert.Equal("13'-4\"", updated.DisplayText);
        Assert.Equal(260m, updated.InsertPrimitives[1].X);
    }

    [Fact]
    public void ResolveSnappedWorldPoint_prefers_major_grid_when_within_snapping_tolerance()
    {
        var cadViewport = new CadViewportContext(
            WorldUnitsPerPixel: 1d,
            MinorGridSpacingWorld: 20d,
            MajorGridSpacingWorld: 100d,
            SnappingToleranceWorld: 1d);

        var snapped = FloorPlanPreviewControl.ResolveSnappedWorldPoint(
            new Point(99.4, 200.4),
            cadViewport,
            [new Point(99.7, 200.1)],
            enableGridSnap: true);

        Assert.Equal(100d, snapped.X);
        Assert.Equal(200d, snapped.Y);
    }

    private static DimensionDto CreateDimension()
    {
        return new DimensionDto(
            Guid.NewGuid(),
            "DIMENSION:AB12",
            "DIMS",
            "DIMENSION",
            "*D169",
            "10'-4\"",
            "GeometryBlock",
            string.Empty,
            124m,
            3149.6m,
            "Inch",
            0,
            0m,
            0m,
            100m,
            100m,
            0m,
            224m,
            100m,
            0m,
            100m,
            140m,
            0m,
            0.99m,
            null,
            1)
        {
            SourceHandle = "AB12",
            RenderTextX = 162m,
            RenderTextY = 148m,
            RenderTextHeight = 3.5m,
            RenderTextStyleName = "ARCH",
            RenderTextAttachmentPoint = "MiddleCenter",
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto("LINE-1", 1, 100m, 140m, 100m, 100m),
                new DimensionLinePrimitiveDto("LINE-2", 2, 224m, 140m, 224m, 100m),
                new DimensionLinePrimitiveDto("LINE-3", 3, 100m, 140m, 224m, 140m)
            ],
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto("TEXT-1", 1, "10'-4\"", 162m, 148m, 3.5m, 0m)
                {
                    StyleName = "ARCH",
                    AttachmentPoint = "MiddleCenter"
                }
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("INSERT-1", 1, "_Dot", 100m, 140m, 0m),
                new DimensionInsertPrimitiveDto("INSERT-2", 2, "_Dot", 224m, 140m, 0m)
            ]
        };
    }

    private static DimensionDto CreateOrdinateXDimension()
    {
        return new DimensionDto(
            Guid.NewGuid(),
            "DIMENSION:ORDX",
            "DIMS",
            "DIMENSION",
            "*D250",
            "13'-4\"",
            "GeometryBlock",
            string.Empty,
            160m,
            4064m,
            "Inch",
            6,
            0m,
            0m,
            100m,
            100m,
            0m,
            260m,
            140m,
            0m,
            290m,
            170m,
            0m,
            0.99m,
            null,
            1)
        {
            SourceHandle = "ORDX",
            RenderTextX = 304m,
            RenderTextY = 174m,
            RenderTextHeight = 3.5m,
            RenderTextStyleName = "ARCH",
            RenderTextAttachmentPoint = "MiddleLeft",
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto("ORDX-LINE-1", 1, 260m, 140m, 290m, 170m),
                new DimensionLinePrimitiveDto("ORDX-LINE-2", 2, 290m, 170m, 320m, 170m)
            ],
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto("ORDX-TEXT-1", 1, "13'-4\"", 304m, 174m, 3.5m, 0m)
                {
                    StyleName = "ARCH",
                    AttachmentPoint = "MiddleLeft"
                }
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("ORDX-INSERT-1", 1, "_Dot", 260m, 140m, 0m),
                new DimensionInsertPrimitiveDto("ORDX-INSERT-2", 2, "_Dot", 290m, 170m, 0m)
            ]
        };
    }

    private static DimensionDto CreateDiameterDimension()
    {
        return new DimensionDto(
            Guid.NewGuid(),
            "DIMENSION:DIAMX",
            "DIMS",
            "DIMENSION",
            "*D261",
            "3'-4\"",
            "GeometryBlock",
            string.Empty,
            40m,
            1016m,
            "Inch",
            3,
            0m,
            0m,
            80m,
            100m,
            0m,
            120m,
            100m,
            0m,
            110m,
            120m,
            0m,
            0.99m,
            null,
            1)
        {
            SourceHandle = "DIAMX",
            RenderTextX = 120m,
            RenderTextY = 125m,
            RenderTextHeight = 3.5m,
            RenderTextStyleName = "ARCH",
            RenderTextAttachmentPoint = "MiddleLeft",
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto("DIAMX-LINE-1", 1, 80m, 100m, 120m, 100m),
                new DimensionLinePrimitiveDto("DIAMX-LINE-2", 2, 120m, 100m, 110m, 120m)
            ],
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto("DIAMX-TEXT-1", 1, "3'-4\"", 120m, 125m, 3.5m, 0m)
                {
                    StyleName = "ARCH",
                    AttachmentPoint = "MiddleLeft"
                }
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("DIAMX-INSERT-1", 1, "_Dot", 80m, 100m, 0m),
                new DimensionInsertPrimitiveDto("DIAMX-INSERT-2", 2, "_Dot", 120m, 100m, 0m)
            ]
        };
    }

    private static DimensionDto CreateSplitLinearDimension()
    {
        return new DimensionDto(
            Guid.NewGuid(),
            "DIMENSION:1711",
            "DIMS",
            "DIMENSION",
            "*D314",
            "6\"",
            "GeometryBlock",
            string.Empty,
            6m,
            152.4m,
            "Inch",
            0,
            90m,
            0m,
            426.377243037711m,
            177.378753798520m,
            0m,
            355.574188825562m,
            183.378753798549m,
            0m,
            361.244654926439m,
            177.378753798520m,
            0m,
            0.99m,
            null,
            1)
        {
            SourceHandle = "D314",
            RenderTextX = 421.016421524873m,
            RenderTextY = 180.661034183330m,
            RenderTextHeight = 3.5m,
            RenderTextStyleName = "ARCH",
            RenderTextAttachmentPoint = "MiddleCenter",
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto("LINE-1", 1, 357.574188825562m, 183.378753798549m, 430.377243037711m, 183.378753798549m),
                new DimensionLinePrimitiveDto("LINE-2", 2, 363.244654926439m, 177.378753798520m, 430.377243037711m, 177.378753798520m),
                new DimensionLinePrimitiveDto("LINE-3", 3, 426.377243037711m, 186.878753798549m, 426.377243037711m, 190.378753798549m),
                new DimensionLinePrimitiveDto("LINE-4", 4, 426.377243037711m, 173.878753798520m, 426.377243037711m, 170.378753798520m)
            ],
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto("TEXT-1", 1, "6\"", 421.016421524873m, 180.661034183330m, 3.5m, 90m)
                {
                    StyleName = "ARCH",
                    AttachmentPoint = "MiddleCenter"
                }
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("INSERT-1", 1, "_Dot", 426.377243037711m, 183.378753798549m, 0m),
                new DimensionInsertPrimitiveDto("INSERT-2", 2, "_Dot", 426.377243037711m, 177.378753798520m, 0m)
            ]
        };
    }

    private static DimensionDto CreateLeaderLinearDimension()
    {
        return new DimensionDto(
            Guid.NewGuid(),
            "DIMENSION:1709",
            "DIMS",
            "DIMENSION",
            "*D249",
            "14\"",
            "GeometryBlock",
            string.Empty,
            14m,
            355.6m,
            "Inch",
            0,
            90m,
            0m,
            425.753329527016m,
            645.378753798433m,
            0m,
            425.602465130800m,
            631.378753798549m,
            0m,
            425.602465130800m,
            645.378753798433m,
            0m,
            0.99m,
            null,
            1)
        {
            SourceHandle = "D249",
            RenderTextX = 413.792549980876m,
            RenderTextY = 650.498610618040m,
            RenderTextHeight = 3.5m,
            RenderTextStyleName = "ARCH",
            RenderTextAttachmentPoint = "MiddleCenter",
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto("LINE-1", 1, 427.602465130801m, 631.378753798550m, 429.753329527016m, 631.378753798550m),
                new DimensionLinePrimitiveDto("LINE-2", 2, 427.602465130801m, 645.378753798433m, 429.753329527016m, 645.378753798433m),
                new DimensionLinePrimitiveDto("LINE-3", 3, 425.753329527016m, 634.878753798550m, 425.753329527016m, 641.878753798433m),
                new DimensionLinePrimitiveDto("LINE-4", 4, 425.753329527016m, 638.378753798491m, 417.375883314210m, 645.331943951374m),
                new DimensionLinePrimitiveDto("LINE-5", 5, 417.375883314210m, 645.331943951374m, 417.375883314210m, 655.665277284707m)
            ],
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto("TEXT-1", 1, "14\"", 413.792549980876m, 650.498610618040m, 3.5m, 90m)
                {
                    StyleName = "ARCH",
                    AttachmentPoint = "MiddleCenter"
                }
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("INSERT-1", 1, "_Dot", 425.753329527016m, 631.378753798550m, 0m),
                new DimensionInsertPrimitiveDto("INSERT-2", 2, "_Dot", 425.753329527016m, 645.378753798433m, 0m)
            ]
        };
    }

    private static void AssertLineExists(
        IReadOnlyList<DimensionLinePrimitiveDto> lines,
        decimal startX,
        decimal startY,
        decimal endX,
        decimal endY)
    {
        Assert.Contains(
            lines,
            line => line.StartX == startX &&
                    line.StartY == startY &&
                    line.EndX == endX &&
                    line.EndY == endY);
    }
}
