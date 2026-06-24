using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
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
    public void Build_uses_spanish_site_plan_context_copy_for_the_sidebar()
    {
        var templateId = Guid.NewGuid();
        var version = new FloorPlanLibraryVersionDto(
            Guid.NewGuid(),
            1,
            "Published",
            DateTime.UtcNow,
            "inch",
            IsCurrent: true,
            ActivePublishedCurationId: Guid.NewGuid());
        var libraryItem = new FloorPlanLibraryItemDto(
            templateId,
            "seminole2000",
            "Seminole",
            VersionCount: 1,
            CurrentVersionId: version.VersionId,
            CurrentVersionNumber: version.VersionNumber,
            Versions: [version]);
        using var provider = new ServiceCollection().BuildServiceProvider();
        var reviewViewModel = new FloorPlanReviewViewModel(
            provider.GetRequiredService<IServiceScopeFactory>(),
            templateId);
        var floorPathId = Guid.NewGuid();
        reviewViewModel.GeometryPaths.Add(CreateRectangle(floorPathId, minX: 0m, minY: 0m, maxX: 100m, maxY: 200m));
        reviewViewModel.MeasurementContext = new MeasurementContextDto("inch", 25.4m, 0.1m, 1m);
        var sitePlan = new SitePlanPreviewDto(
            "lote.dxf",
            "inch",
            25.4m,
            [CreateRectangle(Guid.NewGuid(), minX: 0m, minY: 0m, maxX: 700m, maxY: 1100m)],
            new SitePlanBuildableAreaDto(100m, 101m, 583.786m, 1029m));

        var viewModel = SitePlanAdjustmentPreviewProjector.Build(
            libraryItem,
            version,
            reviewViewModel,
            sitePlan);

        Assert.Equal("Ajustar a site plan", viewModel.Title);
        Assert.Equal("seminole2000 v1 sobre lote.dxf", viewModel.Subtitle);
        Assert.Contains("Centrado en área edificable: 100, 101 -> 583.786, 1029", viewModel.PreviewSelectionLabel, StringComparison.Ordinal);
        Assert.Contains("Vista previa", viewModel.StatusMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("Centered in buildable area", viewModel.PreviewSelectionLabel, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Preview only", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Move Floor Plan", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(" over ", viewModel.Subtitle, StringComparison.OrdinalIgnoreCase);
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
    public void Auto_fit_facts_do_not_require_width_trim_when_centered_structural_width_matches_buildable_width()
    {
        var wallPathId = Guid.NewGuid();
        var floorGeometry = new[]
        {
            CreateRectangle(wallPathId, minX: 0m, minY: 0m, maxX: 483.786m, maxY: 930m)
        };
        var sitePlan = new SitePlanPreviewDto(
            "alto-menos-2.dxf",
            "inch",
            25.4m,
            [CreateRectangle(Guid.NewGuid(), minX: 100m, minY: 101m, maxX: 583.786m, maxY: 1029m)],
            new SitePlanBuildableAreaDto(100m, 101m, 583.786m, 1029m));

        var projection = SitePlanAdjustmentPreviewProjector.Project(
            floorGeometry,
            roomLabels: [],
            openingLabels: [],
            dimensions: [],
            new MeasurementContextDto("inch", 25.4m, 0.1m, 1m),
            sitePlan,
            floorPlanPlacementGeometryPathIds: new HashSet<Guid> { wallPathId });
        var projectedBounds = BoundsOf(projection.FloorPlanGeometryPaths);
        var autoFitGeometryPaths = SitePlanAdjustmentPreviewProjector.ResolveAutoFitGeometryPaths(
            projection.FloorPlanGeometryPaths,
            new HashSet<Guid> { wallPathId });

        var facts = AutoFitSuggestionFactBuilder.Build(
            autoFitGeometryPaths,
            sitePlan.BuildableArea,
            sitePlan.ToMillimetersFactor,
            pinchGroups: [],
            articulationBands: []);

        Assert.Equal(sitePlan.BuildableArea.MinX, projectedBounds.MinX);
        Assert.Equal(sitePlan.BuildableArea.MaxX, projectedBounds.MaxX);
        Assert.Equal(0m, facts.Deficit.WidthInches);
        Assert.Equal(2m, facts.Deficit.HeightInches);
    }

    [Fact]
    public void Build_uses_wall_candidate_footprint_not_total_preview_outliers_for_width_deficit()
    {
        var templateId = Guid.NewGuid();
        var version = new FloorPlanLibraryVersionDto(
            Guid.NewGuid(),
            1,
            "Published",
            DateTime.UtcNow,
            "inch",
            IsCurrent: true,
            ActivePublishedCurationId: Guid.NewGuid());
        var libraryItem = new FloorPlanLibraryItemDto(
            templateId,
            "seminole2000",
            "Seminole",
            VersionCount: 1,
            CurrentVersionId: version.VersionId,
            CurrentVersionNumber: version.VersionNumber,
            Versions: [version]);
        using var provider = new ServiceCollection().BuildServiceProvider();
        var reviewViewModel = new FloorPlanReviewViewModel(
            provider.GetRequiredService<IServiceScopeFactory>(),
            templateId);
        var wallPathId = Guid.NewGuid();
        var rightOutlierPathId = Guid.NewGuid();
        reviewViewModel.GeometryPaths.Add(CreateRectangle(wallPathId, minX: 0m, minY: 0m, maxX: 100m, maxY: 100m));
        reviewViewModel.GeometryPaths.Add(CreateRectangle(rightOutlierPathId, minX: 100m, minY: 40m, maxX: 160m, maxY: 60m));
        reviewViewModel.WallCandidates.Add(new WallCandidateDto(
            Guid.NewGuid(),
            "LINE:WALL:1",
            "WALLS",
            "Accepted",
            0.95m,
            null,
            null,
            wallPathId,
            1));
        reviewViewModel.MeasurementContext = new MeasurementContextDto("inch", 25.4m, 0.1m, 1m);
        var sitePlan = new SitePlanPreviewDto(
            "ancho-menos-1.dxf",
            "inch",
            25.4m,
            [CreateRectangle(Guid.NewGuid(), minX: 100.5m, minY: 100m, maxX: 199.5m, maxY: 200m)],
            new SitePlanBuildableAreaDto(100.5m, 100m, 199.5m, 200m));

        var viewModel = SitePlanAdjustmentPreviewProjector.Build(
            libraryItem,
            version,
            reviewViewModel,
            sitePlan);

        var projectedWallBounds = BoundsOf(viewModel.FloorPlanGeometryPaths.Where(path => path.Id == wallPathId).ToArray());
        var renderedBounds = BoundsOf(viewModel.FloorPlanGeometryPaths);
        Assert.Equal(100m, projectedWallBounds.MinX);
        Assert.Equal(200m, projectedWallBounds.MaxX);
        Assert.Equal(260m, renderedBounds.MaxX);
        Assert.Contains("ancho 1\"", viewModel.AutoFitCandidateSummary, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_computes_auto_fit_deficit_from_structural_paths_not_visual_dimension_extents()
    {
        var templateId = Guid.NewGuid();
        var version = new FloorPlanLibraryVersionDto(
            Guid.NewGuid(),
            1,
            "Published",
            DateTime.UtcNow,
            "inch",
            IsCurrent: true,
            ActivePublishedCurationId: Guid.NewGuid());
        var libraryItem = new FloorPlanLibraryItemDto(
            templateId,
            "seminole2000",
            "Seminole",
            VersionCount: 1,
            CurrentVersionId: version.VersionId,
            CurrentVersionNumber: version.VersionNumber,
            Versions: [version]);
        using var provider = new ServiceCollection().BuildServiceProvider();
        var reviewViewModel = new FloorPlanReviewViewModel(
            provider.GetRequiredService<IServiceScopeFactory>(),
            templateId);
        var wallPathId = Guid.NewGuid();
        reviewViewModel.GeometryPaths.Add(CreateRectangle(wallPathId, minX: 0m, minY: 0m, maxX: 100m, maxY: 101m));
        reviewViewModel.WallCandidates.Add(WallCandidate(wallPathId, 1));
        reviewViewModel.Dimensions.Add(CreateVerticalDimension(-200m, 301m, "VISUAL OUTLIER"));
        reviewViewModel.MeasurementContext = new MeasurementContextDto("inch", 25.4m, 0.1m, 1m);
        var sitePlan = new SitePlanPreviewDto(
            "height-minus-one.dxf",
            "inch",
            25.4m,
            [CreateRectangle(Guid.NewGuid(), minX: 0m, minY: 0m, maxX: 100m, maxY: 100m)],
            new SitePlanBuildableAreaDto(0m, 0m, 100m, 100m));

        var viewModel = SitePlanAdjustmentPreviewProjector.Build(
            libraryItem,
            version,
            reviewViewModel,
            sitePlan);

        var projectedDimension = Assert.Single(viewModel.Dimensions);
        Assert.True(projectedDimension.DefPointY < sitePlan.BuildableArea.MinY - 100m);
        Assert.True(projectedDimension.DefPoint2Y > sitePlan.BuildableArea.MaxY + 100m);
        Assert.Contains("alto 1\"", viewModel.AutoFitCandidateSummary, StringComparison.Ordinal);
        Assert.DoesNotContain("501", viewModel.AutoFitCandidateSummary, StringComparison.Ordinal);
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
    public void MoveFloorPlanBy_recomputes_auto_fit_side_deficit_before_applying_plan()
    {
        var verticalPathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var facts = new AutoFitSuggestionFacts(
            new AutoFitEnvelopeDeficitDto(
                WidthInches: 0m,
                HeightInches: 1m,
                LeftInches: 0m,
                RightInches: 0m,
                BottomInches: 0.5m,
                TopInches: 0.5m),
            [new AutoFitCandidateGroupDto(pinchGroupId, "Ajuste alto", "Height", 2m, -0.5m, 100.5m, 0)],
            []);
        var plan = new AutoFitSuggestionPlan(
            "Trim the height by one inch.",
            [new AutoFitSuggestionStep("Ajuste alto", "Height", 1m, "The plan is one inch too tall.")],
            "Manual movement must update which side overflows.");
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
                new GeometryPathDto(verticalPathId, false, [new GeometrySegmentDto(verticalPathId, 1, 50m, -0.5m, 50m, 100.5m)])
            ],
            roomLabels: [],
            openingLabels: [],
            dimensions: [],
            autoFitSuggestionFacts: facts,
            sitePlanToMillimetersFactor: 25.4m,
            pinchMarkers:
            [
                new PinchMarkerDto(Guid.NewGuid(), pinchGroupId, "Ajuste alto", Guid.NewGuid(), verticalPathId, "Height", 0.5m, 25.4m, 1)
            ]);
        var option = new AutoFitSuggestionOptionViewModel(1, plan);

        viewModel.MoveFloorPlanBy(0m, -1m);
        viewModel.ApplyAutoFitPlan(option);

        var step = Assert.Single(viewModel.BuildAdjustedSitePlanPlacement().CompressionSteps);
        Assert.Equal("Bottom", step.Edge);
    }

    [Fact]
    public void ApplyAutoFitPlan_marks_adjustment_affected_dimensions_red_even_when_visible_text_is_unchanged()
    {
        var leftWallPathId = Guid.NewGuid();
        var rightWallPathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var dimension = CreateLinearDimension();
        var facts = new AutoFitSuggestionFacts(
            new AutoFitEnvelopeDeficitDto(
                WidthInches: 1m,
                HeightInches: 0m,
                LeftInches: 0m,
                RightInches: 1m,
                BottomInches: 0m,
                TopInches: 0m),
            [new AutoFitCandidateGroupDto(pinchGroupId, "Ajuste ancho", "Width", 1m, 150m, 240m, 0)],
            []);
        var plan = new AutoFitSuggestionPlan(
            "Reduce the right side by one inch.",
            [new AutoFitSuggestionStep("Ajuste ancho", "Width", 1m, "The dimension geometry is affected even if the label rounds the same.")],
            "Affected cotas must be highlighted.");
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
            sitePlanToMillimetersFactor: 25.4m,
            pinchMarkers:
            [
                new PinchMarkerDto(Guid.NewGuid(), pinchGroupId, "Ajuste ancho", Guid.NewGuid(), rightWallPathId, "Width", 0.5m, 25.4m, 1)
            ]);
        var option = new AutoFitSuggestionOptionViewModel(1, plan);

        viewModel.ApplyAutoFitPlan(option);

        var updated = Assert.Single(viewModel.Dimensions);
        Assert.Equal("10'-4\"", updated.DisplayText);
        Assert.Equal(223m, updated.DefPoint2X);
        Assert.Contains(dimension.DimensionId, viewModel.ChangedNumberDimensionIds);
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
        Assert.Equal("Alto: recortar 2\" en Ajuste 2.", option.Title);
        Assert.Contains("Ajuste 2", option.Details, StringComparison.Ordinal);
        Assert.DoesNotContain("Matches the exact height deficit", option.Details, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("No over-trim", option.Details, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Aplicar", option.ApplyLabel);
        Assert.Contains("1 opción disponible", viewModel.AutoFitSuggestionSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AI ordenó", viewModel.AutoFitSuggestionStatus, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("OpenAI", viewModel.AutoFitSuggestionStatus, StringComparison.Ordinal);
        Assert.DoesNotContain("Pick the option", viewModel.AutoFitSuggestionPlanDetails, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SuggestAutoFitPlanAsync_keeps_visible_suggestion_copy_in_spanish_even_when_openai_returns_english()
    {
        var facts = new AutoFitSuggestionFacts(
            new AutoFitEnvelopeDeficitDto(WidthInches: 0m, HeightInches: 2m, LeftInches: 0m, RightInches: 0m, BottomInches: 1m, TopInches: 1m),
            [
                new AutoFitCandidateGroupDto(Guid.NewGuid(), "alto del patio", "Height", 2m, 10m, 20m, 0),
                new AutoFitCandidateGroupDto(Guid.NewGuid(), "alto porche", "Height", 2m, 30m, 40m, 0)
            ],
            []);
        var englishPlan = new AutoFitSuggestionPlan(
            "Reduce height by 2 inches, splitting evenly between alto porche and alto del patio.",
            [
                new AutoFitSuggestionStep("alto porche", "Height", 1m, "Distributes the reduction, which may be preferable for balanced fit."),
                new AutoFitSuggestionStep("alto del patio", "Height", 1m, "Distributes the reduction, which may be preferable for visual reasons.")
            ],
            "This option distributes the required reduction evenly.");
        var response = new AutoFitSuggestionPlanResponse(
            true,
            englishPlan,
            AutoFitSuggestionPlanValidator.Validate(facts, englishPlan),
            null)
        {
            Plans = [englishPlan]
        };
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
            autoFitPlanSuggester: new FakeAutoFitPlanSuggester(response));

        await viewModel.SuggestAutoFitPlanAsync(CancellationToken.None);

        var option = Assert.Single(viewModel.AutoFitSuggestionOptions);
        var visibleCopy = string.Join(
            " ",
            viewModel.AutoFitCandidateSummary,
            viewModel.AutoFitSuggestionStatus,
            viewModel.AutoFitSuggestionSummary,
            viewModel.AutoFitSuggestionPlanDetails,
            option.Title,
            option.Details,
            option.ApplyLabel);
        Assert.Contains("Alto: recortar 2\" dividido entre alto porche y alto del patio.", option.Title);
        Assert.Contains("alto porche: 1\"", option.Details);
        Assert.Contains("alto del patio: 1\"", option.Details);
        Assert.DoesNotContain("Reduce height", visibleCopy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("inches", visibleCopy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("splitting evenly", visibleCopy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Distributes", visibleCopy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Apply option", visibleCopy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("OpenAI", visibleCopy, StringComparison.Ordinal);
        Assert.DoesNotContain("Height", visibleCopy, StringComparison.Ordinal);
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
        Assert.Contains("Opción 1 aplicada", viewModel.AutoFitSuggestionStatus, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplyAutoFitPlan_recenters_adjusted_floor_plan_inside_buildable_area()
    {
        var pathId = Guid.NewGuid();
        var rightGroupId = Guid.NewGuid();
        var facts = new AutoFitSuggestionFacts(
            new AutoFitEnvelopeDeficitDto(1m, 0m, 0m, 1m, 0m, 0m),
            [new AutoFitCandidateGroupDto(rightGroupId, "Ajuste derecha", "Width", 1m, 0m, 0m, 0)],
            []);
        var plan = new AutoFitSuggestionPlan(
            "Trim right.",
            [new AutoFitSuggestionStep("Ajuste derecha", "Width", 1m, "Right side trim.")],
            "Adjusted preview should be centered after trimming.");
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
                new PinchMarkerDto(Guid.NewGuid(), rightGroupId, "Ajuste derecha", Guid.NewGuid(), pathId, "Width", 0.20m, 25.4m, 1)
            ],
            autoFitBuildableArea: new SitePlanBuildableAreaDto(0m, 0m, 100m, 100m));
        var option = new AutoFitSuggestionOptionViewModel(1, plan);

        viewModel.ApplyAutoFitPlan(option);

        var bounds = BoundsOf(viewModel.FloorPlanGeometryPaths);
        var placement = viewModel.BuildAdjustedSitePlanPlacement();
        var marker = Assert.Single(Assert.Single(placement.CompressionSteps).Markers);
        Assert.Equal(0.5m, bounds.MinX);
        Assert.Equal(99.5m, bounds.MaxX);
        Assert.Equal(0.5m, viewModel.ManualOffsetX);
        Assert.Equal(0.5m, placement.SiteOffsetX);
        Assert.Equal(80m, marker.Coordinate);
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
        Assert.Contains("Opción 3 aplicada", viewModel.AutoFitSuggestionStatus, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_centers_floor_plan_by_structural_mass_not_by_a_stray_appendage()
    {
        var templateId = Guid.NewGuid();
        var version = new FloorPlanLibraryVersionDto(
            Guid.NewGuid(),
            1,
            "Published",
            DateTime.UtcNow,
            "inch",
            IsCurrent: true,
            ActivePublishedCurationId: Guid.NewGuid());
        var libraryItem = new FloorPlanLibraryItemDto(
            templateId,
            "seminole2000",
            "Seminole",
            VersionCount: 1,
            CurrentVersionId: version.VersionId,
            CurrentVersionNumber: version.VersionNumber,
            Versions: [version]);
        using var provider = new ServiceCollection().BuildServiceProvider();
        var reviewViewModel = new FloorPlanReviewViewModel(
            provider.GetRequiredService<IServiceScopeFactory>(),
            templateId);

        // Dense structural body in X[100..200]; a single stray wall line juts left to
        // X=70. The body must center in the buildable, ignoring the appendage.
        var bodyIds = new List<Guid>();
        var bodyRectId = Guid.NewGuid();
        bodyIds.Add(bodyRectId);
        reviewViewModel.GeometryPaths.Add(CreateRectangle(bodyRectId, minX: 100m, minY: 0m, maxX: 200m, maxY: 100m));
        reviewViewModel.WallCandidates.Add(WallCandidate(bodyRectId, 1));
        for (var index = 1; index < 80; index++)
        {
            var pathId = Guid.NewGuid();
            var x = 100m + (index * (100m / 80m));
            reviewViewModel.GeometryPaths.Add(new GeometryPathDto(pathId, false, [new GeometrySegmentDto(pathId, 1, x, 0m, x, 100m)]));
            reviewViewModel.WallCandidates.Add(WallCandidate(pathId, index + 1));
            bodyIds.Add(pathId);
        }

        var strayId = Guid.NewGuid();
        reviewViewModel.GeometryPaths.Add(new GeometryPathDto(strayId, false, [new GeometrySegmentDto(strayId, 1, 100m, 50m, 70m, 50m)]));
        reviewViewModel.WallCandidates.Add(WallCandidate(strayId, 999));

        reviewViewModel.MeasurementContext = new MeasurementContextDto("inch", 25.4m, 0.1m, 1m);
        var sitePlan = new SitePlanPreviewDto(
            "structural-center.dxf",
            "inch",
            25.4m,
            [CreateRectangle(Guid.NewGuid(), minX: 0m, minY: 0m, maxX: 100m, maxY: 100m)],
            new SitePlanBuildableAreaDto(0m, 0m, 100m, 100m));

        var viewModel = SitePlanAdjustmentPreviewProjector.Build(libraryItem, version, reviewViewModel, sitePlan);

        var bodyBounds = BoundsOf(viewModel.FloorPlanGeometryPaths.Where(path => bodyIds.Contains(path.Id)).ToArray());
        // Body (structural width 100) centered on the buildable center (50): X[0..100].
        Assert.Equal(0m, bodyBounds.MinX);
        Assert.Equal(100m, bodyBounds.MaxX);
    }

    [Fact]
    public void ApplyAutoFitPlan_records_compression_steps_in_floor_source_coordinates()
    {
        var pathId = Guid.NewGuid();
        var rightGroupId = Guid.NewGuid();
        var facts = new AutoFitSuggestionFacts(
            new AutoFitEnvelopeDeficitDto(1m, 0m, 0.5m, 0.5m, 0m, 0m),
            [new AutoFitCandidateGroupDto(rightGroupId, "Ajuste derecha", "Width", 1m, 0m, 0m, 0)],
            []);
        var plan = new AutoFitSuggestionPlan(
            "Trim right.",
            [new AutoFitSuggestionStep("Ajuste derecha", "Width", 1m, "Right half marker.")],
            "Recorded step must be invariant to manual moves.");
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
                new PinchMarkerDto(Guid.NewGuid(), rightGroupId, "Ajuste derecha", Guid.NewGuid(), pathId, "Width", 0.20m, 25.4m, 1)
            ]);

        // Manual move BEFORE applying: the recorded source coordinate must stay invariant.
        viewModel.MoveFloorPlanBy(5m, 0m);
        viewModel.ApplyAutoFitPlan(new AutoFitSuggestionOptionViewModel(1, plan));

        var placement = viewModel.BuildAdjustedSitePlanPlacement();
        var step = Assert.Single(placement.CompressionSteps);
        Assert.Equal("Width", step.AxisTag);
        Assert.Equal("Right", step.Edge);
        var marker = Assert.Single(step.Markers);
        // Marker sits at preview X=85 after the +5 move; back in floor source coords: 80.
        Assert.Equal(80m, marker.Coordinate);
        Assert.Equal(1m, marker.TrimSourceUnits);
        // Affine part reflects the accumulated manual offset.
        Assert.Equal(5m, placement.SiteOffsetX);
        Assert.Equal(0m, placement.SiteOffsetY);
        Assert.Equal(1m, placement.FloorToSiteScale);
    }

    [Fact]
    public async Task ExportAdjustedSitePlanAsync_invokes_exporter_with_current_placement_and_paths()
    {
        var pathId = Guid.NewGuid();
        var exporter = new FakeAdjustedSitePlanExporter();
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
            autoFitSuggestionFacts: null,
            autoFitPlanSuggester: null,
            sitePlanToMillimetersFactor: 25.4m,
            projectionScale: 1m,
            projectionOffsetX: 100m,
            projectionOffsetY: 50m,
            floorPlanSourcePath: @"C:\plans\floor.dxf",
            sitePlanSourcePath: @"C:\plans\site.dxf",
            adjustedSitePlanExporter: exporter);

        Assert.True(viewModel.CanExportAdjustedSitePlan);
        viewModel.MoveFloorPlanBy(10m, -4m);

        await viewModel.ExportAdjustedSitePlanAsync(@"C:\out\combined.dxf", CancellationToken.None);

        Assert.NotNull(exporter.LastCall);
        Assert.Equal(@"C:\plans\floor.dxf", exporter.LastCall!.FloorPlanSourcePath);
        Assert.Equal(@"C:\plans\site.dxf", exporter.LastCall.SitePlanSourcePath);
        Assert.Equal(@"C:\out\combined.dxf", exporter.LastCall.OutputFilePath);
        Assert.Equal(110m, exporter.LastCall.Placement.SiteOffsetX);
        Assert.Equal(46m, exporter.LastCall.Placement.SiteOffsetY);
        Assert.Contains("combined.dxf", viewModel.AutoFitSuggestionStatus, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeAdjustedSitePlanExporter : FloorplanFit.Application.Abstractions.IAdjustedSitePlanExporter
    {
        public sealed record Call(
            string FloorPlanSourcePath,
            string SitePlanSourcePath,
            string OutputFilePath,
            AdjustedSitePlanPlacementDto Placement);

        public Call? LastCall { get; private set; }

        public Task<FloorplanFit.Application.Abstractions.AdjustedSitePlanExportResult> ExportAsync(
            string floorPlanSourcePath,
            string sitePlanSourcePath,
            string outputFilePath,
            AdjustedSitePlanPlacementDto placement,
            CancellationToken cancellationToken)
        {
            LastCall = new Call(floorPlanSourcePath, sitePlanSourcePath, outputFilePath, placement);
            return Task.FromResult(new FloorplanFit.Application.Abstractions.AdjustedSitePlanExportResult(
                outputFilePath,
                4,
                []));
        }
    }

    [Fact]
    public void ApplyAutoFitPlan_edge_inference_ignores_degenerate_outlier_geometry()
    {
        var pathId = Guid.NewGuid();
        var degeneratePathId = Guid.NewGuid();
        var rightGroupId = Guid.NewGuid();
        var facts = new AutoFitSuggestionFacts(
            new AutoFitEnvelopeDeficitDto(
                WidthInches: 1m,
                HeightInches: 0m,
                LeftInches: 0.5m,
                RightInches: 0.5m,
                BottomInches: 0m,
                TopInches: 0m),
            [new AutoFitCandidateGroupDto(rightGroupId, "Ajuste derecha", "Width", 1m, 0m, 0m, 0)],
            []);
        var plan = new AutoFitSuggestionPlan(
            "Trim the right side group.",
            [new AutoFitSuggestionStep("Ajuste derecha", "Width", 1m, "Marker sits on the right half.")],
            "The degenerate outlier path must not shift the inferred center.");
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
                CreateRectangle(pathId, minX: 0m, minY: 0m, maxX: 100m, maxY: 100m),
                new GeometryPathDto(
                    degeneratePathId,
                    IsClosed: true,
                    [new GeometrySegmentDto(degeneratePathId, 1, 1000m, 50m, 1000m, 50m)])
            ],
            roomLabels: [],
            openingLabels: [],
            dimensions: [],
            autoFitSuggestionFacts: facts,
            autoFitPlanSuggester: null,
            sitePlanToMillimetersFactor: 25.4m,
            pinchMarkers:
            [
                new PinchMarkerDto(Guid.NewGuid(), rightGroupId, "Ajuste derecha", Guid.NewGuid(), pathId, "Width", 0.20m, 25.4m, 1)
            ]);
        var option = new AutoFitSuggestionOptionViewModel(1, plan);

        viewModel.ApplyAutoFitPlan(option);

        var bounds = BoundsOf(viewModel.FloorPlanGeometryPaths.Where(path => path.Id == pathId).ToArray());
        Assert.Equal(0m, bounds.MinX);
        Assert.Equal(99m, bounds.MaxX);
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
        Assert.Contains("Opción 4 aplicada", viewModel.AutoFitSuggestionStatus, StringComparison.OrdinalIgnoreCase);
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

        var placement = viewModel.BuildAdjustedSitePlanPlacement();
        var exportedDimensionPatch = Assert.Single(placement.AdjustedDimensions);
        Assert.Equal(dimension.DimensionId, exportedDimensionPatch.DimensionId);
        Assert.Equal("10'-2\"", exportedDimensionPatch.DisplayText);
        Assert.Equal("10'-2\"", Assert.Single(exportedDimensionPatch.TextPrimitives).Text);
        Assert.Equal(222m, exportedDimensionPatch.DefPoint2X);
    }

    [Fact]
    public void BuildAdjustedSitePlanPlacement_exports_reactive_dimension_patch_from_projected_preview_coordinates()
    {
        var leftWallPathId = Guid.NewGuid();
        var rightWallPathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var corridorId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var dimension = CreateLinearDimension() with
        {
            DefPointX = 1100m,
            DefPoint2X = 1224m,
            DefPoint3X = 1100m,
            RenderTextX = 1162m,
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto("LINE-1", 1, 1100m, 140m, 1100m, 100m),
                new DimensionLinePrimitiveDto("LINE-2", 2, 1224m, 140m, 1224m, 100m),
                new DimensionLinePrimitiveDto("LINE-3", 3, 1100m, 140m, 1224m, 140m)
            ],
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto("TEXT-1", 1, "10'-4\"", 1162m, 148m, 3.5m, 0m)
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("INSERT-1", 1, "_Dot", 1100m, 140m, 0m),
                new DimensionInsertPrimitiveDto("INSERT-2", 2, "_Dot", 1224m, 140m, 0m)
            ]
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
            "The bound dimension should be exported with recalculated text.");
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
                new GeometryPathDto(leftWallPathId, false, [new GeometrySegmentDto(leftWallPathId, 1, 1100m, 100m, 1100m, 140m)]),
                new GeometryPathDto(rightWallPathId, false, [new GeometrySegmentDto(rightWallPathId, 1, 1224m, 100m, 1224m, 140m)])
            ],
            roomLabels: [],
            openingLabels: [],
            dimensions: [dimension],
            autoFitSuggestionFacts: facts,
            autoFitPlanSuggester: null,
            sitePlanToMillimetersFactor: 25.4m,
            pinchMarkers:
            [
                new PinchMarkerDto(Guid.NewGuid(), pinchGroupId, "Ajuste 1", Guid.NewGuid(), rightWallPathId, "Width", 0.5m, 50.8m, 1)
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
            ],
            projectionScale: 1m,
            projectionOffsetX: 1000m);
        var option = new AutoFitSuggestionOptionViewModel(1, plan);

        viewModel.ApplyAutoFitPlan(option);

        var exportedDimensionPatch = Assert.Single(viewModel.BuildAdjustedSitePlanPlacement().AdjustedDimensions);
        Assert.Equal("10'-2\"", exportedDimensionPatch.DisplayText);
        Assert.Equal("10'-2\"", Assert.Single(exportedDimensionPatch.TextPrimitives).Text);
        Assert.Equal(222m, exportedDimensionPatch.DefPoint2X);
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
        Assert.Contains("Déficit de ancho 78.459", viewModel.AutoFitSuggestionPlanDetails, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FilterSitePlanForAdjustment_keeps_full_site_plan_content_for_visual_fidelity()
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

        Assert.Equal([terrainPathId, setbackPathId, streetPathId], filtered.RenderPaths.Select(path => path.Id).ToArray());
        Assert.Equal([terrainPathId, setbackPathId, streetPathId], filtered.GeometryPaths.Select(path => path.Id).ToArray());
        Assert.Equal(["SITE PLAN", "20' REAR SETBACK LINE"], filtered.Texts.Select(text => text.Text).ToArray());
    }

    [Fact]
    public void FilterSitePlanForAdjustment_keeps_synthetic_title_block_text_because_preview_matches_the_dxf()
    {
        var sitePlan = new SitePlanPreviewDto(
            "SYNTH FALTA 2 ALTO - RECTANGULAR - RIO.dxf",
            "inch",
            25.4m,
            [],
            new SitePlanBuildableAreaDto(0m, 0m, 100m, 100m),
            [],
            [
                new SitePlanTextDto(Guid.NewGuid(), "E", "TEXT", "57", 0m, -30m, 2m, 0m, null, IsSetback: false),
                new SitePlanTextDto(Guid.NewGuid(), "E", "TEXT", "RIO DRIVE", 0m, -35m, 2m, 0m, null, IsSetback: false),
                new SitePlanTextDto(Guid.NewGuid(), "E", "TEXT", "SITE PLAN", 0m, -40m, 2m, 0m, null, IsSetback: false),
                new SitePlanTextDto(Guid.NewGuid(), "SETBACKS", "TEXT", "20' REAR SETBACK LINE", 20m, 20m, 2m, 0m, null, IsSetback: true)
            ]);

        var filtered = SitePlanAdjustmentPreviewProjector.FilterSitePlanForAdjustment(sitePlan);

        Assert.Equal(["57", "RIO DRIVE", "SITE PLAN", "20' REAR SETBACK LINE"], filtered.Texts.Select(text => text.Text).ToArray());
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

    private static WallCandidateDto WallCandidate(Guid geometryPathId, int sortOrder)
        => new(
            Guid.NewGuid(),
            $"LINE:WALL:{sortOrder}",
            "WALLS",
            "Accepted",
            0.95m,
            null,
            null,
            geometryPathId,
            sortOrder);

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
