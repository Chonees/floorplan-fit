using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.ViewModels;
using Xunit;

namespace FloorplanFit.Desktop.Tests.ViewModels;

public sealed class SitePlanAdjustmentPreviewProjectorTests
{
    [Fact]
    public void Project_centers_floor_plan_inside_site_plan_buildable_area()
    {
        var floorPathId = Guid.NewGuid();
        var floorGeometry = new[]
        {
            CreateRectangle(floorPathId, minX: 0m, minY: 0m, maxX: 50m, maxY: 100m)
        };
        var sitePlan = new SitePlanPreviewDto(
            "site.dxf",
            "inch",
            25.4m,
            [CreateRectangle(Guid.NewGuid(), minX: 0m, minY: 0m, maxX: 400m, maxY: 400m)],
            new SitePlanBuildableAreaDto(100m, 100m, 300m, 300m));

        var projection = SitePlanAdjustmentPreviewProjector.Project(
            floorGeometry,
            roomLabels: [],
            openingLabels: [],
            dimensions: [],
            new MeasurementContextDto("inch", 25.4m, 0.1m, 1m),
            sitePlan);

        var projectedBounds = BoundsOf(projection.FloorPlanGeometryPaths);
        Assert.Equal(175m, projectedBounds.MinX);
        Assert.Equal(150m, projectedBounds.MinY);
        Assert.Equal(225m, projectedBounds.MaxX);
        Assert.Equal(250m, projectedBounds.MaxY);
    }

    [Fact]
    public void Project_converts_floor_plan_units_before_centering_on_site_plan()
    {
        var floorPathId = Guid.NewGuid();
        var floorGeometry = new[]
        {
            CreateRectangle(floorPathId, minX: 0m, minY: 0m, maxX: 120m, maxY: 60m)
        };
        var sitePlan = new SitePlanPreviewDto(
            "site.dxf",
            "foot",
            304.8m,
            [CreateRectangle(Guid.NewGuid(), minX: 0m, minY: 0m, maxX: 200m, maxY: 200m)],
            new SitePlanBuildableAreaDto(90m, 90m, 110m, 110m));

        var projection = SitePlanAdjustmentPreviewProjector.Project(
            floorGeometry,
            roomLabels: [],
            openingLabels: [],
            dimensions: [],
            new MeasurementContextDto("inch", 25.4m, 0.1m, 1m),
            sitePlan);

        var projectedBounds = BoundsOf(projection.FloorPlanGeometryPaths);
        Assert.InRange(projectedBounds.MaxX - projectedBounds.MinX, 9.999m, 10.001m);
        Assert.InRange(projectedBounds.MaxY - projectedBounds.MinY, 4.999m, 5.001m);
        Assert.InRange((projectedBounds.MinX + projectedBounds.MaxX) / 2m, 99.999m, 100.001m);
        Assert.InRange((projectedBounds.MinY + projectedBounds.MaxY) / 2m, 99.999m, 100.001m);
    }

    [Fact]
    public void Project_centers_floor_plan_by_structural_placement_geometry_not_fixture_outliers()
    {
        var wallPathId = Guid.NewGuid();
        var fixturePathId = Guid.NewGuid();
        var floorGeometry = new[]
        {
            CreateRectangle(wallPathId, minX: 0m, minY: 0m, maxX: 40m, maxY: 100m),
            CreateRectangle(fixturePathId, minX: 100m, minY: 40m, maxX: 120m, maxY: 60m)
        };
        var sitePlan = new SitePlanPreviewDto(
            "site.dxf",
            "inch",
            25.4m,
            [CreateRectangle(Guid.NewGuid(), minX: 0m, minY: 0m, maxX: 400m, maxY: 400m)],
            new SitePlanBuildableAreaDto(100m, 100m, 300m, 300m));

        var projection = SitePlanAdjustmentPreviewProjector.Project(
            floorGeometry,
            roomLabels: [],
            openingLabels: [],
            dimensions: [],
            new MeasurementContextDto("inch", 25.4m, 0.1m, 1m),
            sitePlan,
            floorPlanPlacementGeometryPathIds: new HashSet<Guid> { wallPathId });

        var projectedWallBounds = BoundsOf(projection.FloorPlanGeometryPaths.Where(path => path.Id == wallPathId).ToArray());
        Assert.Equal(200m, (projectedWallBounds.MinX + projectedWallBounds.MaxX) / 2m);
        Assert.Equal(200m, (projectedWallBounds.MinY + projectedWallBounds.MaxY) / 2m);
    }

    [Fact]
    public void Auto_fit_facts_use_structural_placement_geometry_not_fixture_outliers()
    {
        var wallPathId = Guid.NewGuid();
        var fixtureOutlierPathId = Guid.NewGuid();
        var floorGeometry = new[]
        {
            CreateRectangle(wallPathId, minX: 0m, minY: 0m, maxX: 100m, maxY: 100m),
            CreateRectangle(fixtureOutlierPathId, minX: -77.459m, minY: 40m, maxX: -20m, maxY: 60m)
        };
        var sitePlan = new SitePlanPreviewDto(
            "height-deficit-only.dxf",
            "inch",
            25.4m,
            [CreateRectangle(Guid.NewGuid(), minX: 0m, minY: 0m, maxX: 100m, maxY: 98m)],
            new SitePlanBuildableAreaDto(0m, 0m, 100m, 98m));

        var projection = SitePlanAdjustmentPreviewProjector.Project(
            floorGeometry,
            roomLabels: [],
            openingLabels: [],
            dimensions: [],
            new MeasurementContextDto("inch", 25.4m, 0.1m, 1m),
            sitePlan,
            floorPlanPlacementGeometryPathIds: new HashSet<Guid> { wallPathId });
        var autoFitGeometryPaths = SitePlanAdjustmentPreviewProjector.ResolveAutoFitGeometryPaths(
            projection.FloorPlanGeometryPaths,
            new HashSet<Guid> { wallPathId });

        var facts = AutoFitSuggestionFactBuilder.Build(
            autoFitGeometryPaths,
            sitePlan.BuildableArea,
            sitePlan.ToMillimetersFactor,
            pinchGroups: [],
            articulationBands: []);

        Assert.Equal(0m, facts.Deficit.WidthInches);
        Assert.Equal(2m, facts.Deficit.HeightInches);
    }

    [Fact]
    public void Project_moves_dimension_definition_text_and_line_segment_points_with_the_floor_plan()
    {
        var floorPathId = Guid.NewGuid();
        var floorGeometry = new[]
        {
            CreateRectangle(floorPathId, minX: 0m, minY: 0m, maxX: 100m, maxY: 100m)
        };
        var dimension = new DimensionDto(
            Guid.NewGuid(),
            "DIMENSION:1",
            "DIMS",
            "DIMENSION",
            "*D1",
            "8'-0\"",
            "GeometryBlock",
            string.Empty,
            96m,
            2438.4m,
            "inch",
            0,
            0m,
            0m,
            DefPointX: 0m,
            DefPointY: 0m,
            DefPointZ: 0m,
            DefPoint2X: 100m,
            DefPoint2Y: 0m,
            DefPoint2Z: 0m,
            DefPoint3X: 50m,
            DefPoint3Y: 12m,
            DefPoint3Z: 0m,
            0.99m,
            null,
            1)
        {
            RenderTextX = 50m,
            RenderTextY = 12m,
            LineSegments = [new DimensionLineSegmentDto(0m, 0m, 100m, 0m)]
        };
        var sitePlan = new SitePlanPreviewDto(
            "site.dxf",
            "inch",
            25.4m,
            [CreateRectangle(Guid.NewGuid(), minX: 0m, minY: 0m, maxX: 400m, maxY: 400m)],
            new SitePlanBuildableAreaDto(100m, 100m, 300m, 300m));

        var projection = SitePlanAdjustmentPreviewProjector.Project(
            floorGeometry,
            roomLabels: [],
            openingLabels: [],
            dimensions: [dimension],
            new MeasurementContextDto("inch", 25.4m, 0.1m, 1m),
            sitePlan);

        var projected = Assert.Single(projection.Dimensions);
        Assert.Equal(150m, projected.DefPointX);
        Assert.Equal(150m, projected.DefPointY);
        Assert.Equal(250m, projected.DefPoint2X);
        Assert.Equal(150m, projected.DefPoint2Y);
        Assert.Equal(200m, projected.RenderTextX);
        Assert.Equal(162m, projected.RenderTextY);
        var segment = Assert.Single(projected.LineSegments);
        Assert.Equal(new DimensionLineSegmentDto(150m, 150m, 250m, 150m), segment);
    }

    [Fact]
    public void MoveFloorPlanBy_moves_floor_overlay_without_moving_site_plan()
    {
        var sitePathId = Guid.NewGuid();
        var floorPathId = Guid.NewGuid();
        var dimensionId = Guid.NewGuid();
        var viewModel = new SitePlanAdjustmentViewModel(
            "Adjust",
            "Subtitle",
            "Selection",
            "Status",
            [CreateRectangle(sitePathId, minX: 0m, minY: 0m, maxX: 100m, maxY: 100m)],
            [],
            [],
            [CreateRectangle(floorPathId, minX: 10m, minY: 20m, maxX: 30m, maxY: 40m)],
            [new RoomLabelDto(Guid.NewGuid(), "TEXT:ROOM", "ROOMS", "KITCHEN", 12m, 22m, 0.95m, null, 1)],
            [new OpeningLabelDto(Guid.NewGuid(), "TEXT:DOOR", "DOORS", "Door", "2668", 14m, 24m, 0.95m, null, 1)],
            [
                new DimensionDto(
                    dimensionId,
                    "DIMENSION:1",
                    "DIMS",
                    "DIMENSION",
                    "*D1",
                    "8'-0\"",
                    "GeometryBlock",
                    string.Empty,
                    96m,
                    2438.4m,
                    "inch",
                    0,
                    0m,
                    0m,
                    DefPointX: 10m,
                    DefPointY: 20m,
                    DefPointZ: 0m,
                    DefPoint2X: 30m,
                    DefPoint2Y: 20m,
                    DefPoint2Z: 0m,
                    DefPoint3X: 20m,
                    DefPoint3Y: 28m,
                    DefPoint3Z: 0m,
                    0.99m,
                    null,
                    1)
                {
                    RenderTextX = 20m,
                    RenderTextY = 28m,
                    LineSegments = [new DimensionLineSegmentDto(10m, 20m, 30m, 20m)]
                }
            ]);

        viewModel.MoveFloorPlanBy(5m, -3m);

        var siteBounds = BoundsOf(viewModel.SitePlanGeometryPaths);
        Assert.Equal(0m, siteBounds.MinX);
        Assert.Equal(0m, siteBounds.MinY);
        var floorBounds = BoundsOf(viewModel.FloorPlanGeometryPaths);
        Assert.Equal(15m, floorBounds.MinX);
        Assert.Equal(17m, floorBounds.MinY);
        Assert.Equal(35m, floorBounds.MaxX);
        Assert.Equal(37m, floorBounds.MaxY);
        Assert.Equal(5m, viewModel.ManualOffsetX);
        Assert.Equal(-3m, viewModel.ManualOffsetY);
        Assert.Equal(17m, Assert.Single(viewModel.RoomLabels).X);
        Assert.Equal(19m, Assert.Single(viewModel.RoomLabels).Y);
        Assert.Equal(19m, Assert.Single(viewModel.OpeningLabels).X);
        Assert.Equal(21m, Assert.Single(viewModel.OpeningLabels).Y);
        var dimension = Assert.Single(viewModel.Dimensions);
        Assert.Equal(15m, dimension.DefPointX);
        Assert.Equal(17m, dimension.DefPointY);
        Assert.Equal(25m, dimension.RenderTextX);
        Assert.Equal(25m, dimension.RenderTextY);
        Assert.Equal(new DimensionLineSegmentDto(15m, 17m, 35m, 17m), Assert.Single(dimension.LineSegments));
    }

    [Fact]
    public async Task SuggestAutoFitPlanAsync_populates_ranked_fit_options()
    {
        var facts = new AutoFitSuggestionFacts(
            new AutoFitEnvelopeDeficitDto(WidthInches: 0m, HeightInches: 2m, LeftInches: 0m, RightInches: 0m, BottomInches: 1m, TopInches: 1m),
            [
                new AutoFitCandidateGroupDto(Guid.NewGuid(), "Ajuste 1", "Height", 4m, 10m, 20m, 0),
                new AutoFitCandidateGroupDto(Guid.NewGuid(), "Ajuste 2", "Height", 4m, 30m, 40m, 0)
            ],
            []);
        var plan = new AutoFitSuggestionPlan(
            "Use Ajuste 2 exactly.",
            [new AutoFitSuggestionStep("Ajuste 2", "Height", 2m, "Matches the exact height deficit.")],
            "No over-trim.");
        var response = new AutoFitSuggestionPlanResponse(
            true,
            plan,
            AutoFitSuggestionPlanValidator.Validate(facts, plan),
            null)
        {
            Plans = [plan]
        };
        var fakeSuggester = new FakeAutoFitPlanSuggester(response);
        var viewModel = new SitePlanAdjustmentViewModel(
            "Adjust",
            "Subtitle",
            "Selection",
            "Status",
            sitePlanGeometryPaths: [],
            sitePlanRenderPaths: [],
            sitePlanTexts: [],
            floorPlanGeometryPaths: [],
            roomLabels: [],
            openingLabels: [],
            dimensions: [],
            autoFitSuggestionFacts: facts,
            autoFitPlanSuggester: fakeSuggester);

        await viewModel.SuggestAutoFitPlanAsync(CancellationToken.None);

        Assert.Same(facts, fakeSuggester.ReceivedFacts);
        Assert.NotNull(fakeSuggester.ReceivedCandidatePlans);
        Assert.True(fakeSuggester.ReceivedCandidatePlans.Count >= 3);
        var option = Assert.Single(viewModel.AutoFitSuggestionOptions);
        Assert.Equal("Use Ajuste 2 exactly.", option.Title);
        Assert.Contains("Ajuste 2", option.Details, StringComparison.Ordinal);
        Assert.Contains("1 fit option", viewModel.AutoFitSuggestionSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplyAutoFitPlan_reduces_only_the_selected_pinch_group_in_preview()
    {
        var pathId = Guid.NewGuid();
        var selectedGroupId = Guid.NewGuid();
        var otherGroupId = Guid.NewGuid();
        var facts = new AutoFitSuggestionFacts(
            new AutoFitEnvelopeDeficitDto(WidthInches: 0m, HeightInches: 2m, LeftInches: 0m, RightInches: 0m, BottomInches: 0m, TopInches: 2m),
            [
                new AutoFitCandidateGroupDto(selectedGroupId, "Ajuste 1", "Height", 4m, 10m, 20m, 0),
                new AutoFitCandidateGroupDto(otherGroupId, "Ajuste 2", "Height", 4m, 30m, 40m, 0)
            ],
            []);
        var plan = new AutoFitSuggestionPlan(
            "Reduce selected group.",
            [new AutoFitSuggestionStep("Ajuste 1", "Height", 2m, "Selected human option.")],
            "Only Ajuste 1 should move.");
        var viewModel = new SitePlanAdjustmentViewModel(
            "Adjust",
            "Subtitle",
            "Selection",
            "Status",
            sitePlanGeometryPaths: [],
            sitePlanRenderPaths: [],
            sitePlanTexts: [],
            floorPlanGeometryPaths: [CreateRectangle(pathId, minX: 0m, minY: 0m, maxX: 10m, maxY: 10m)],
            roomLabels: [],
            openingLabels: [],
            dimensions: [],
            autoFitSuggestionFacts: facts,
            autoFitPlanSuggester: null,
            sitePlanToMillimetersFactor: 25.4m,
            pinchMarkers:
            [
                new PinchMarkerDto(Guid.NewGuid(), selectedGroupId, "Ajuste 1", Guid.NewGuid(), pathId, "Height", 0.375m, 101.6m, 1),
                new PinchMarkerDto(Guid.NewGuid(), otherGroupId, "Ajuste 2", Guid.NewGuid(), pathId, "Height", 0.375m, 101.6m, 2)
            ]);
        var option = new AutoFitSuggestionOptionViewModel(1, plan);

        viewModel.ApplyAutoFitPlan(option);

        var bounds = BoundsOf(viewModel.FloorPlanGeometryPaths);
        Assert.Equal(0m, bounds.MinY);
        Assert.Equal(8m, bounds.MaxY);
        Assert.Contains("Applied option 1", viewModel.AutoFitSuggestionStatus, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplyAutoFitPlan_respects_split_option_groups_on_opposite_sides()
    {
        var pathId = Guid.NewGuid();
        var leftGroupId = Guid.NewGuid();
        var rightGroupId = Guid.NewGuid();
        var facts = new AutoFitSuggestionFacts(
            new AutoFitEnvelopeDeficitDto(
                WidthInches: 2m,
                HeightInches: 0m,
                LeftInches: 1m,
                RightInches: 1m,
                BottomInches: 0m,
                TopInches: 0m),
            [
                new AutoFitCandidateGroupDto(leftGroupId, "Ajuste izquierda", "Width", 1m, 0m, 0m, 0),
                new AutoFitCandidateGroupDto(rightGroupId, "Ajuste derecha", "Width", 1m, 0m, 0m, 0)
            ],
            []);
        var plan = new AutoFitSuggestionPlan(
            "Split left and right.",
            [
                new AutoFitSuggestionStep("Ajuste izquierda", "Width", 1m, "Human chose left side."),
                new AutoFitSuggestionStep("Ajuste derecha", "Width", 1m, "Human chose right side.")
            ],
            "Both selected groups must be respected.");
        var viewModel = new SitePlanAdjustmentViewModel(
            "Adjust",
            "Subtitle",
            "Selection",
            "Status",
            sitePlanGeometryPaths: [],
            sitePlanRenderPaths: [],
            sitePlanTexts: [],
            floorPlanGeometryPaths: [CreateRectangle(pathId, minX: 0m, minY: 0m, maxX: 100m, maxY: 100m)],
            roomLabels: [],
            openingLabels: [],
            dimensions: [],
            autoFitSuggestionFacts: facts,
            autoFitPlanSuggester: null,
            sitePlanToMillimetersFactor: 25.4m,
            pinchMarkers:
            [
                new PinchMarkerDto(Guid.NewGuid(), leftGroupId, "Ajuste izquierda", Guid.NewGuid(), pathId, "Width", 0.05m, 25.4m, 1),
                new PinchMarkerDto(Guid.NewGuid(), rightGroupId, "Ajuste derecha", Guid.NewGuid(), pathId, "Width", 0.20m, 25.4m, 2)
            ]);
        var unselectedOption = new AutoFitSuggestionOptionViewModel(
            1,
            new AutoFitSuggestionPlan(
                "Use only left.",
                [new AutoFitSuggestionStep("Ajuste izquierda", "Width", 2m, "Different option.")],
                "Not selected."));
        var option = new AutoFitSuggestionOptionViewModel(3, plan);
        viewModel.AutoFitSuggestionOptions.Add(unselectedOption);
        viewModel.AutoFitSuggestionOptions.Add(option);

        viewModel.ApplyAutoFitPlan(option);

        var bounds = BoundsOf(viewModel.FloorPlanGeometryPaths);
        Assert.Equal(1m, bounds.MinX);
        Assert.Equal(99m, bounds.MaxX);
        Assert.False(unselectedOption.IsApplied);
        Assert.True(option.IsApplied);
        Assert.Contains("Applied option 3", viewModel.AutoFitSuggestionStatus, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplyAutoFitPlan_respects_split_option_groups_on_opposite_height_sides()
    {
        var pathId = Guid.NewGuid();
        var bottomGroupId = Guid.NewGuid();
        var topGroupId = Guid.NewGuid();
        var facts = new AutoFitSuggestionFacts(
            new AutoFitEnvelopeDeficitDto(
                WidthInches: 0m,
                HeightInches: 2m,
                LeftInches: 0m,
                RightInches: 0m,
                BottomInches: 1m,
                TopInches: 1m),
            [
                new AutoFitCandidateGroupDto(bottomGroupId, "Ajuste abajo", "Height", 1m, 0m, 0m, 0),
                new AutoFitCandidateGroupDto(topGroupId, "Ajuste arriba", "Height", 1m, 0m, 0m, 0)
            ],
            []);
        var plan = new AutoFitSuggestionPlan(
            "Split bottom and top.",
            [
                new AutoFitSuggestionStep("Ajuste abajo", "Height", 1m, "Human chose bottom side."),
                new AutoFitSuggestionStep("Ajuste arriba", "Height", 1m, "Human chose top side.")
            ],
            "Both selected height groups must be respected.");
        var viewModel = new SitePlanAdjustmentViewModel(
            "Adjust",
            "Subtitle",
            "Selection",
            "Status",
            sitePlanGeometryPaths: [],
            sitePlanRenderPaths: [],
            sitePlanTexts: [],
            floorPlanGeometryPaths: [CreateRectangle(pathId, minX: 0m, minY: 0m, maxX: 100m, maxY: 100m)],
            roomLabels: [],
            openingLabels: [],
            dimensions: [],
            autoFitSuggestionFacts: facts,
            autoFitPlanSuggester: null,
            sitePlanToMillimetersFactor: 25.4m,
            pinchMarkers:
            [
                new PinchMarkerDto(Guid.NewGuid(), bottomGroupId, "Ajuste abajo", Guid.NewGuid(), pathId, "Height", 0.125m, 25.4m, 1),
                new PinchMarkerDto(Guid.NewGuid(), topGroupId, "Ajuste arriba", Guid.NewGuid(), pathId, "Height", 0.625m, 25.4m, 2)
            ]);
        var option = new AutoFitSuggestionOptionViewModel(4, plan);

        viewModel.ApplyAutoFitPlan(option);

        var bounds = BoundsOf(viewModel.FloorPlanGeometryPaths);
        Assert.Equal(1m, bounds.MinY);
        Assert.Equal(99m, bounds.MaxY);
        Assert.Contains("Applied option 4", viewModel.AutoFitSuggestionStatus, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplyAutoFitPlan_reapplying_same_option_uses_initial_preview_baseline()
    {
        var pathId = Guid.NewGuid();
        var rightGroupId = Guid.NewGuid();
        var facts = new AutoFitSuggestionFacts(
            new AutoFitEnvelopeDeficitDto(
                WidthInches: 1m,
                HeightInches: 0m,
                LeftInches: 0m,
                RightInches: 1m,
                BottomInches: 0m,
                TopInches: 0m),
            [new AutoFitCandidateGroupDto(rightGroupId, "Ajuste derecha", "Width", 2m, 0m, 0m, 0)],
            []);
        var plan = new AutoFitSuggestionPlan(
            "Reduce right side.",
            [new AutoFitSuggestionStep("Ajuste derecha", "Width", 1m, "Human chose right side.")],
            "Repeated apply should not stack reductions.");
        var viewModel = new SitePlanAdjustmentViewModel(
            "Adjust",
            "Subtitle",
            "Selection",
            "Status",
            sitePlanGeometryPaths: [],
            sitePlanRenderPaths: [],
            sitePlanTexts: [],
            floorPlanGeometryPaths: [CreateRectangle(pathId, minX: 0m, minY: 0m, maxX: 100m, maxY: 100m)],
            roomLabels: [],
            openingLabels: [],
            dimensions: [],
            autoFitSuggestionFacts: facts,
            autoFitPlanSuggester: null,
            sitePlanToMillimetersFactor: 25.4m,
            pinchMarkers:
            [
                new PinchMarkerDto(Guid.NewGuid(), rightGroupId, "Ajuste derecha", Guid.NewGuid(), pathId, "Width", 0.20m, 50.8m, 1)
            ]);
        var option = new AutoFitSuggestionOptionViewModel(1, plan);

        viewModel.ApplyAutoFitPlan(option);
        var firstApplyBounds = BoundsOf(viewModel.FloorPlanGeometryPaths);
        viewModel.ApplyAutoFitPlan(option);
        var secondApplyBounds = BoundsOf(viewModel.FloorPlanGeometryPaths);

        Assert.Equal(0m, firstApplyBounds.MinX);
        Assert.Equal(99m, firstApplyBounds.MaxX);
        Assert.Equal(firstApplyBounds, secondApplyBounds);
    }

    [Fact]
    public void ApplyAutoFitPlan_switches_options_from_manually_moved_baseline()
    {
        var pathId = Guid.NewGuid();
        var leftGroupId = Guid.NewGuid();
        var rightGroupId = Guid.NewGuid();
        var facts = new AutoFitSuggestionFacts(
            new AutoFitEnvelopeDeficitDto(
                WidthInches: 1m,
                HeightInches: 0m,
                LeftInches: 1m,
                RightInches: 1m,
                BottomInches: 0m,
                TopInches: 0m),
            [
                new AutoFitCandidateGroupDto(leftGroupId, "Ajuste izquierda", "Width", 2m, 0m, 0m, 0),
                new AutoFitCandidateGroupDto(rightGroupId, "Ajuste derecha", "Width", 2m, 0m, 0m, 0)
            ],
            []);
        var leftPlan = new AutoFitSuggestionPlan(
            "Reduce left side.",
            [new AutoFitSuggestionStep("Ajuste izquierda", "Width", 1m, "Human chose left side.")],
            "Switching should reset before applying.");
        var rightPlan = new AutoFitSuggestionPlan(
            "Reduce right side.",
            [new AutoFitSuggestionStep("Ajuste derecha", "Width", 1m, "Human chose right side.")],
            "First preview option.");
        var viewModel = new SitePlanAdjustmentViewModel(
            "Adjust",
            "Subtitle",
            "Selection",
            "Status",
            sitePlanGeometryPaths: [],
            sitePlanRenderPaths: [],
            sitePlanTexts: [],
            floorPlanGeometryPaths: [CreateRectangle(pathId, minX: 0m, minY: 0m, maxX: 100m, maxY: 100m)],
            roomLabels: [],
            openingLabels: [],
            dimensions: [],
            autoFitSuggestionFacts: facts,
            autoFitPlanSuggester: null,
            sitePlanToMillimetersFactor: 25.4m,
            pinchMarkers:
            [
                new PinchMarkerDto(Guid.NewGuid(), leftGroupId, "Ajuste izquierda", Guid.NewGuid(), pathId, "Width", 0.05m, 50.8m, 1),
                new PinchMarkerDto(Guid.NewGuid(), rightGroupId, "Ajuste derecha", Guid.NewGuid(), pathId, "Width", 0.20m, 50.8m, 2)
            ]);
        var leftOption = new AutoFitSuggestionOptionViewModel(1, leftPlan);
        var rightOption = new AutoFitSuggestionOptionViewModel(2, rightPlan);

        viewModel.MoveFloorPlanBy(10m, 0m);
        viewModel.ApplyAutoFitPlan(rightOption);
        viewModel.ApplyAutoFitPlan(leftOption);

        var bounds = BoundsOf(viewModel.FloorPlanGeometryPaths);
        Assert.Equal(11m, bounds.MinX);
        Assert.Equal(110m, bounds.MaxX);
        Assert.Equal(10m, viewModel.ManualOffsetX);
    }

    [Fact]
    public void ApplyAutoFitPlan_rebuilds_related_dimensions_with_interval_reactive_projector()
    {
        var leftWallPathId = Guid.NewGuid();
        var rightWallPathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var corridorId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var dimension = CreateLinearDimension();
        var unaffectedDimension = CreateLinearDimension() with
        {
            SourceEntityRef = "DIMENSION:UNAFFECTED",
            DisplayText = "8'-0\"",
            MeasurementSourceUnits = 96m,
            MeasurementMillimeters = 2438.4m
        };
        var facts = new AutoFitSuggestionFacts(
            new AutoFitEnvelopeDeficitDto(
                WidthInches: 2m,
                HeightInches: 0m,
                LeftInches: 0m,
                RightInches: 2m,
                BottomInches: 0m,
                TopInches: 0m),
            [new AutoFitCandidateGroupDto(pinchGroupId, "Ajuste 1", "Width", 4m, 150m, 240m, 1)],
            []);
        var plan = new AutoFitSuggestionPlan(
            "Reduce right side.",
            [new AutoFitSuggestionStep("Ajuste 1", "Width", 2m, "Selected human option.")],
            "The bound dimension should be recalculated.");
        var viewModel = new SitePlanAdjustmentViewModel(
            "Adjust",
            "Subtitle",
            "Selection",
            "Status",
            sitePlanGeometryPaths: [],
            sitePlanRenderPaths: [],
            sitePlanTexts: [],
            floorPlanGeometryPaths:
            [
                new GeometryPathDto(leftWallPathId, false, [new GeometrySegmentDto(leftWallPathId, 1, 100m, 100m, 100m, 140m)]),
                new GeometryPathDto(rightWallPathId, false, [new GeometrySegmentDto(rightWallPathId, 1, 224m, 100m, 224m, 140m)])
            ],
            roomLabels: [],
            openingLabels: [],
            dimensions: [dimension, unaffectedDimension],
            autoFitSuggestionFacts: facts,
            autoFitPlanSuggester: null,
            sitePlanToMillimetersFactor: 25.4m,
            pinchMarkers:
            [
                new PinchMarkerDto(Guid.NewGuid(), pinchGroupId, "Ajuste 1", Guid.NewGuid(), rightWallPathId, "Width", 0.5m, 101.6m, 1)
            ],
            measurementCorridors:
            [
                new MeasurementCorridorDto(corridorId, "Facade width", "Width", leftWallPathId, 95m, 145m, "Verified", 1)
            ],
            measurementNodes:
            [
                new MeasurementNodeDto(startNodeId, corridorId, 1, "ProjectedGeometry", "WallCandidate", Guid.NewGuid(), leftWallPathId, "Projected", 100m, 120m, 100m, 0m, 0m, 0.5m),
                new MeasurementNodeDto(endNodeId, corridorId, 2, "ProjectedGeometry", "WallCandidate", Guid.NewGuid(), rightWallPathId, "Projected", 224m, 120m, 224m, 0m, 0m, 0.5m)
            ],
            dimensionIntervalBindings:
            [
                new DimensionIntervalBindingDto(dimension.DimensionId, corridorId, startNodeId, endNodeId, "ManualVerified", 100m, 224m)
            ],
            articulationBands:
            [
                new ArticulationBandDto(pinchGroupId, "Ajuste 1", "Width", 150m, 240m, 101.6m, "Verified")
            ]);
        var option = new AutoFitSuggestionOptionViewModel(1, plan);

        viewModel.ApplyAutoFitPlan(option);

        var updated = Assert.Single(viewModel.Dimensions, item => item.DimensionId == dimension.DimensionId);
        var unaffected = Assert.Single(viewModel.Dimensions, item => item.DimensionId == unaffectedDimension.DimensionId);
        Assert.Equal(100m, updated.DefPointX);
        Assert.Equal(222m, updated.DefPoint2X);
        Assert.Equal(122m, updated.MeasurementSourceUnits);
        Assert.Equal("10'-2\"", updated.DisplayText);
        Assert.Equal("ReactiveAssociatedMeasurement", updated.DisplayTextSource);
        Assert.Equal("8'-0\"", unaffected.DisplayText);
        Assert.Contains(dimension.DimensionId, viewModel.ChangedNumberDimensionIds);
        Assert.DoesNotContain(unaffectedDimension.DimensionId, viewModel.ChangedNumberDimensionIds);
    }

    [Fact]
    public void ApplyAutoFitPlan_rebuilds_fractional_related_dimensions_instead_of_visually_hiding_the_change()
    {
        var leftWallPathId = Guid.NewGuid();
        var rightWallPathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var corridorId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var dimension = CreateLinearDimension();
        var facts = new AutoFitSuggestionFacts(
            new AutoFitEnvelopeDeficitDto(
                WidthInches: 0.5m,
                HeightInches: 0m,
                LeftInches: 0m,
                RightInches: 0.5m,
                BottomInches: 0m,
                TopInches: 0m),
            [new AutoFitCandidateGroupDto(pinchGroupId, "Ajuste fino", "Width", 1m, 150m, 240m, 1)],
            []);
        var plan = new AutoFitSuggestionPlan(
            "Reduce right side by half an inch.",
            [new AutoFitSuggestionStep("Ajuste fino", "Width", 0.5m, "Selected human option.")],
            "The bound dimension should show the fractional reduction.");
        var viewModel = new SitePlanAdjustmentViewModel(
            "Adjust",
            "Subtitle",
            "Selection",
            "Status",
            sitePlanGeometryPaths: [],
            sitePlanRenderPaths: [],
            sitePlanTexts: [],
            floorPlanGeometryPaths:
            [
                new GeometryPathDto(leftWallPathId, false, [new GeometrySegmentDto(leftWallPathId, 1, 100m, 100m, 100m, 140m)]),
                new GeometryPathDto(rightWallPathId, false, [new GeometrySegmentDto(rightWallPathId, 1, 224m, 100m, 224m, 140m)])
            ],
            roomLabels: [],
            openingLabels: [],
            dimensions: [dimension],
            autoFitSuggestionFacts: facts,
            autoFitPlanSuggester: null,
            sitePlanToMillimetersFactor: 25.4m,
            pinchMarkers:
            [
                new PinchMarkerDto(Guid.NewGuid(), pinchGroupId, "Ajuste fino", Guid.NewGuid(), rightWallPathId, "Width", 0.5m, 25.4m, 1)
            ],
            measurementCorridors:
            [
                new MeasurementCorridorDto(corridorId, "Facade width", "Width", leftWallPathId, 95m, 145m, "Verified", 1)
            ],
            measurementNodes:
            [
                new MeasurementNodeDto(startNodeId, corridorId, 1, "ProjectedGeometry", "WallCandidate", Guid.NewGuid(), leftWallPathId, "Projected", 100m, 120m, 100m, 0m, 0m, 0.5m),
                new MeasurementNodeDto(endNodeId, corridorId, 2, "ProjectedGeometry", "WallCandidate", Guid.NewGuid(), rightWallPathId, "Projected", 224m, 120m, 224m, 0m, 0m, 0.5m)
            ],
            dimensionIntervalBindings:
            [
                new DimensionIntervalBindingDto(dimension.DimensionId, corridorId, startNodeId, endNodeId, "ManualVerified", 100m, 224m)
            ],
            articulationBands:
            [
                new ArticulationBandDto(pinchGroupId, "Ajuste fino", "Width", 150m, 240m, 25.4m, "Verified")
            ]);
        var option = new AutoFitSuggestionOptionViewModel(1, plan);

        viewModel.ApplyAutoFitPlan(option);

        var updated = Assert.Single(viewModel.Dimensions);
        Assert.Equal(100m, updated.DefPointX);
        Assert.Equal(223.5m, updated.DefPoint2X);
        Assert.Equal(123.5m, updated.MeasurementSourceUnits);
        Assert.Equal("10'-3 1/2\"", updated.DisplayText);
        Assert.Contains(dimension.DimensionId, viewModel.ChangedNumberDimensionIds);
    }

    [Fact]
    public void ApplyAutoFitPlan_does_not_mark_bound_dimensions_outside_selected_band_as_changed()
    {
        var verticalWallPathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var corridorId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var dimension = CreateVerticalDimension(100m, 208m, "9'-0\"");
        var facts = new AutoFitSuggestionFacts(
            new AutoFitEnvelopeDeficitDto(
                WidthInches: 0m,
                HeightInches: 0.5m,
                LeftInches: 0m,
                RightInches: 0m,
                BottomInches: 0m,
                TopInches: 0.5m),
            [new AutoFitCandidateGroupDto(pinchGroupId, "Ajuste 1", "Height", 0.5m, 300m, 340m, 1)],
            []);
        var plan = new AutoFitSuggestionPlan(
            "Reduce top by half an inch.",
            [new AutoFitSuggestionStep("Ajuste 1", "Height", 0.5m, "Selected human option.")],
            "Dimensions outside the selected band should not turn red.");
        var viewModel = new SitePlanAdjustmentViewModel(
            "Adjust",
            "Subtitle",
            "Selection",
            "Status",
            sitePlanGeometryPaths: [],
            sitePlanRenderPaths: [],
            sitePlanTexts: [],
            floorPlanGeometryPaths:
            [
                new GeometryPathDto(verticalWallPathId, false, [new GeometrySegmentDto(verticalWallPathId, 1, 100m, 100m, 100m, 340m)])
            ],
            roomLabels: [],
            openingLabels: [],
            dimensions: [dimension],
            autoFitSuggestionFacts: facts,
            autoFitPlanSuggester: null,
            sitePlanToMillimetersFactor: 25.4m,
            pinchMarkers:
            [
                new PinchMarkerDto(Guid.NewGuid(), pinchGroupId, "Ajuste 1", Guid.NewGuid(), verticalWallPathId, "Height", 0.8333333333333333333333333333m, 12.7m, 1)
            ],
            measurementCorridors:
            [
                new MeasurementCorridorDto(corridorId, "Sink height", "Height", verticalWallPathId, 95m, 215m, "Verified", 1)
            ],
            measurementNodes:
            [
                new MeasurementNodeDto(startNodeId, corridorId, 1, "ProjectedGeometry", "WallCandidate", Guid.NewGuid(), verticalWallPathId, "Projected", 100m, 100m, 100m, 0m, 0m, 0m),
                new MeasurementNodeDto(endNodeId, corridorId, 2, "ProjectedGeometry", "WallCandidate", Guid.NewGuid(), verticalWallPathId, "Projected", 100m, 208m, 100m, 0m, 0m, 0.45m)
            ],
            dimensionIntervalBindings:
            [
                new DimensionIntervalBindingDto(dimension.DimensionId, corridorId, startNodeId, endNodeId, "ManualVerified", 100m, 208m)
            ],
            articulationBands:
            [
                new ArticulationBandDto(pinchGroupId, "Ajuste 1", "Height", 300m, 340m, 12.7m, "Verified")
            ]);
        var option = new AutoFitSuggestionOptionViewModel(1, plan);

        viewModel.ApplyAutoFitPlan(option);

        var bounds = BoundsOf(viewModel.FloorPlanGeometryPaths);
        var updated = Assert.Single(viewModel.Dimensions);
        Assert.Equal(339.5m, bounds.MaxY);
        Assert.Equal(108m, updated.MeasurementSourceUnits);
        Assert.Equal("9'-0\"", updated.DisplayText);
        Assert.DoesNotContain(dimension.DimensionId, viewModel.ChangedNumberDimensionIds);
    }

    [Fact]
    public async Task SuggestAutoFitPlanAsync_keeps_button_enabled_but_does_not_call_openai_when_required_axis_has_no_candidate_groups()
    {
        var facts = new AutoFitSuggestionFacts(
            new AutoFitEnvelopeDeficitDto(
                WidthInches: 78.459m,
                HeightInches: 0m,
                LeftInches: 39.229m,
                RightInches: 39.23m,
                BottomInches: 0m,
                TopInches: 0m),
            [],
            ["Width deficit 78.459 inches exceeds available capacity 0 inches."]);
        var fakeSuggester = new FakeAutoFitPlanSuggester(
            new AutoFitSuggestionPlanResponse(
                true,
                new AutoFitSuggestionPlan("Should not be used", [], "No call expected."),
                new AutoFitSuggestionValidationResult(true, []),
                null));
        var viewModel = new SitePlanAdjustmentViewModel(
            "Adjust",
            "Subtitle",
            "Selection",
            "Status",
            sitePlanGeometryPaths: [],
            sitePlanRenderPaths: [],
            sitePlanTexts: [],
            floorPlanGeometryPaths: [],
            roomLabels: [],
            openingLabels: [],
            dimensions: [],
            autoFitSuggestionFacts: facts,
            autoFitPlanSuggester: fakeSuggester);

        Assert.True(viewModel.CanSuggestAutoFitPlan);

        await viewModel.SuggestAutoFitPlanAsync(CancellationToken.None);

        Assert.Null(fakeSuggester.ReceivedFacts);
        Assert.Contains("pinch", viewModel.AutoFitSuggestionStatus, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Width deficit 78.459", viewModel.AutoFitSuggestionPlanDetails, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FilterSitePlanForAdjustment_keeps_only_terrain_and_setbacks()
    {
        var terrainPathId = Guid.NewGuid();
        var setbackPathId = Guid.NewGuid();
        var streetPathId = Guid.NewGuid();
        var sitePlan = new SitePlanPreviewDto(
            "site.dxf",
            "foot",
            304.8m,
            [],
            new SitePlanBuildableAreaDto(0m, 0m, 100m, 100m),
            [
                new SitePlanRenderPathDto(
                    terrainPathId,
                    "2312-001-BM$0$C-PROP-SUBD",
                    "LINE",
                    IsClosed: false,
                    [new GeometrySegmentDto(terrainPathId, 1, 0m, 0m, 100m, 0m)],
                    ColorArgb: null,
                    IsSetback: false),
                new SitePlanRenderPathDto(
                    setbackPathId,
                    "SETBACKS",
                    "LINE",
                    IsClosed: false,
                    [new GeometrySegmentDto(setbackPathId, 1, 10m, 10m, 90m, 10m)],
                    ColorArgb: "#FFFFB000",
                    IsSetback: true),
                new SitePlanRenderPathDto(
                    streetPathId,
                    "E",
                    "LINE",
                    IsClosed: false,
                    [new GeometrySegmentDto(streetPathId, 1, -50m, -20m, 150m, -20m)],
                    ColorArgb: null,
                    IsSetback: false)
            ],
            [
                new SitePlanTextDto(Guid.NewGuid(), "E", "TEXT", "SITE PLAN", 0m, -30m, 2m, 0m, null, IsSetback: false),
                new SitePlanTextDto(Guid.NewGuid(), "SETBACK-TEXT", "TEXT", "20' REAR SETBACK LINE", 20m, 20m, 2m, 0m, null, IsSetback: true)
            ]);

        var filtered = SitePlanAdjustmentPreviewProjector.FilterSitePlanForAdjustment(sitePlan);

        Assert.Equal([terrainPathId, setbackPathId], filtered.RenderPaths.Select(path => path.Id).ToArray());
        Assert.Equal([terrainPathId, setbackPathId], filtered.GeometryPaths.Select(path => path.Id).ToArray());
        var text = Assert.Single(filtered.Texts);
        Assert.Contains("SETBACK", text.Text, StringComparison.OrdinalIgnoreCase);
    }

    private static GeometryPathDto CreateRectangle(Guid pathId, decimal minX, decimal minY, decimal maxX, decimal maxY)
    {
        return new GeometryPathDto(
            pathId,
            IsClosed: true,
            [
                new GeometrySegmentDto(pathId, 1, minX, minY, maxX, minY),
                new GeometrySegmentDto(pathId, 2, maxX, minY, maxX, maxY),
                new GeometrySegmentDto(pathId, 3, maxX, maxY, minX, maxY),
                new GeometrySegmentDto(pathId, 4, minX, maxY, minX, minY)
            ]);
    }

    private static GeometryBounds BoundsOf(IReadOnlyList<GeometryPathDto> paths)
    {
        var points = paths
            .SelectMany(path => path.Segments)
            .SelectMany(segment => new[]
            {
                (segment.StartX, segment.StartY),
                (segment.EndX, segment.EndY)
            })
            .ToArray();

        return new GeometryBounds(
            points.Min(point => point.Item1),
            points.Min(point => point.Item2),
            points.Max(point => point.Item1),
            points.Max(point => point.Item2));
    }

    private readonly record struct GeometryBounds(decimal MinX, decimal MinY, decimal MaxX, decimal MaxY);

    private static DimensionDto CreateLinearDimension()
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
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("INSERT-1", 1, "_Dot", 100m, 140m, 0m),
                new DimensionInsertPrimitiveDto("INSERT-2", 2, "_Dot", 224m, 140m, 0m)
            ]
        };
    }

    private static DimensionDto CreateVerticalDimension(decimal startY, decimal endY, string displayText)
    {
        var span = decimal.Abs(endY - startY);
        var midY = startY + ((endY - startY) / 2m);

        return new DimensionDto(
            Guid.NewGuid(),
            "DIMENSION:VERT",
            "DIMS",
            "DIMENSION",
            "*D170",
            displayText,
            "GeometryBlock",
            string.Empty,
            span,
            span * 25.4m,
            "Inch",
            90,
            0m,
            0m,
            100m,
            startY,
            0m,
            100m,
            endY,
            0m,
            140m,
            startY,
            0m,
            0.99m,
            null,
            1)
        {
            SourceHandle = "VERT",
            RenderTextX = 148m,
            RenderTextY = midY,
            RenderTextHeight = 3.5m,
            RenderTextStyleName = "ARCH",
            RenderTextAttachmentPoint = "MiddleCenter",
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto("LINE-1", 1, 140m, startY, 100m, startY),
                new DimensionLinePrimitiveDto("LINE-2", 2, 140m, endY, 100m, endY),
                new DimensionLinePrimitiveDto("LINE-3", 3, 140m, startY, 140m, endY)
            ],
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto("TEXT-1", 1, displayText, 148m, midY, 3.5m, 90m)
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("INSERT-1", 1, "_Dot", 140m, startY, 0m),
                new DimensionInsertPrimitiveDto("INSERT-2", 2, "_Dot", 140m, endY, 0m)
            ]
        };
    }

    private sealed class FakeAutoFitPlanSuggester(AutoFitSuggestionPlanResponse response) : IAutoFitPlanSuggester
    {
        public AutoFitSuggestionFacts? ReceivedFacts { get; private set; }

        public IReadOnlyList<AutoFitSuggestionPlan>? ReceivedCandidatePlans { get; private set; }

        public Task<AutoFitSuggestionPlanResponse> SuggestAsync(
            AutoFitSuggestionFacts facts,
            CancellationToken cancellationToken)
        {
            ReceivedFacts = facts;
            return Task.FromResult(response);
        }

        public Task<AutoFitSuggestionPlanResponse> SuggestAsync(
            AutoFitSuggestionFacts facts,
            IReadOnlyList<AutoFitSuggestionPlan> candidatePlans,
            CancellationToken cancellationToken)
        {
            ReceivedFacts = facts;
            ReceivedCandidatePlans = candidatePlans;
            return Task.FromResult(response);
        }
    }
}
