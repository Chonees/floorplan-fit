using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FloorplanFit.Desktop.Tests.ViewModels;

public sealed class CommissionedSitePlanAdjustmentIntegrationTests
{
    [Fact]
    public void BuildCommissioned_projects_once_without_legacy_pinches_or_suggester_and_keeps_source_actions_for_export()
    {
        var scenario = Scenario.Create();

        var result = SitePlanAdjustmentPreviewProjector.BuildCommissioned(
            scenario.LibraryItem,
            scenario.Version,
            scenario.Review,
            scenario.Site,
            scenario.Profile,
            floorPlanSourcePath: "floor.dxf",
            sitePlanSourcePath: "site.dxf",
            adjustedSitePlanExporter: new NoOpAdjustedSitePlanExporter());

        Assert.True(result.Succeeded, result.RejectionReason);
        var viewModel = Assert.IsType<SitePlanAdjustmentViewModel>(result.ViewModel);
        var bounds = Bounds(viewModel.FloorPlanGeometryPaths);
        Assert.Equal(0m, bounds.MinX);
        Assert.Equal(16m, bounds.MaxX);
        Assert.Equal(16m, bounds.MaxX - bounds.MinX);
        Assert.Equal(20m, bounds.MaxY - bounds.MinY);
        var fixedLabel = Assert.Single(viewModel.RoomLabels);
        Assert.Equal("LABEL:A", fixedLabel.SourceEntityRef);
        Assert.Equal(3m, fixedLabel.X);
        Assert.Equal(3m, fixedLabel.Y);

        var placement = viewModel.BuildAdjustedSitePlanPlacement();
        Assert.Equal(2m, placement.FloorToSiteScale);
        Assert.Equal(0m, placement.SiteOffsetX);
        var exportedAction = Assert.Single(placement.StretchActions);
        Assert.Equal(scenario.WidthAction.ActionId, exportedAction.ActionId);
        Assert.Equal(10m, exportedAction.CutCoordinate);
        Assert.Equal(2m, exportedAction.DeltaSourceUnits);
        Assert.Equal(5m, exportedAction.MaxDeltaSourceUnits);
        Assert.True(viewModel.IsCommissionedAutoFit);
        Assert.False(viewModel.CanSuggestAutoFitPlan);
        Assert.False(viewModel.IsCommissionedComparisonAvailable);
        Assert.False(viewModel.CanExportAdjustedSitePlan);

        viewModel.ApplyCommissionedStructuralComparison(
            scenario.Review.GeometryPaths.ToArray(),
            "Confirmed whole-plan registration");

        Assert.True(viewModel.IsCommissionedComparisonAvailable);
        Assert.True(viewModel.CanExportAdjustedSitePlan);
        Assert.NotEmpty(viewModel.CommissionedBeforeFloorGeometry);
        Assert.NotEmpty(viewModel.CommissionedBeforeElectricalGeometry);
        Assert.NotEmpty(viewModel.CommissionedAfterFloorGeometry);
        Assert.NotEmpty(viewModel.CommissionedAfterElectricalGeometry);
        var beforeFloorBounds = Bounds(viewModel.CommissionedBeforeFloorGeometry);
        var beforeElectricalBounds = Bounds(viewModel.CommissionedBeforeElectricalGeometry);
        Assert.Equal(20m, beforeFloorBounds.MaxX - beforeFloorBounds.MinX);
        Assert.Equal(beforeFloorBounds, beforeElectricalBounds);
        Assert.Equal(
            SegmentCoordinates(viewModel.CommissionedAfterFloorGeometry),
            SegmentCoordinates(viewModel.CommissionedAfterElectricalGeometry));
        Assert.Contains("WALL", viewModel.CommissionedComparisonStatus, StringComparison.Ordinal);
        Assert.Contains("ArchitecturalBase", viewModel.CommissionedComparisonStatus, StringComparison.Ordinal);
        Assert.Contains("2\"", viewModel.AutoFitSuggestionSummary, StringComparison.Ordinal);
        Assert.DoesNotContain("pinch", viewModel.AutoFitSuggestionStatus, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Commissioned_comparison_unavailable_reason_is_explicit_and_blocks_confirmation_export()
    {
        var scenario = Scenario.Create();
        var result = SitePlanAdjustmentPreviewProjector.BuildCommissioned(
            scenario.LibraryItem,
            scenario.Version,
            scenario.Review,
            scenario.Site,
            scenario.Profile,
            floorPlanSourcePath: "floor.dxf",
            sitePlanSourcePath: "site.dxf",
            adjustedSitePlanExporter: new NoOpAdjustedSitePlanExporter());
        var viewModel = Assert.IsType<SitePlanAdjustmentViewModel>(result.ViewModel);

        viewModel.MarkCommissionedComparisonUnavailable(
            "Electrical no está disponible: el registro confirmado no coincide con los DXF resueltos.");

        Assert.False(viewModel.IsCommissionedComparisonAvailable);
        Assert.False(viewModel.CanExportAdjustedSitePlan);
        Assert.Empty(viewModel.CommissionedBeforeElectricalGeometry);
        Assert.Empty(viewModel.CommissionedAfterElectricalGeometry);
        Assert.Contains("Electrical", viewModel.CommissionedComparisonStatus, StringComparison.Ordinal);
        Assert.Contains("no está disponible", viewModel.CommissionedComparisonStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void Commissioned_recommendation_shows_exact_deltas_preservation_and_honest_electrical_labeling()
    {
        var scenario = Scenario.Create();
        var result = SitePlanAdjustmentPreviewProjector.BuildCommissioned(
            scenario.LibraryItem,
            scenario.Version,
            scenario.Review,
            scenario.Site,
            scenario.Profile,
            floorPlanSourcePath: "floor.dxf",
            sitePlanSourcePath: "site.dxf",
            adjustedSitePlanExporter: new NoOpAdjustedSitePlanExporter());
        Assert.True(result.Succeeded, result.RejectionReason);
        var viewModel = Assert.IsType<SitePlanAdjustmentViewModel>(result.ViewModel);

        Assert.Contains("Ajuste requerido: ancho 2", viewModel.AutoFitCandidateSummary, StringComparison.Ordinal);
        Assert.Contains("profundidad 0", viewModel.AutoFitCandidateSummary, StringComparison.Ordinal);
        Assert.Contains("Ancho -2", viewModel.AutoFitSuggestionSummary, StringComparison.Ordinal);
        Assert.Equal("Auto-fit seguro aplicado a la vista previa.", viewModel.AutoFitSuggestionStatus);
        Assert.Contains("Se aplicó 1 variable commissioned", viewModel.AutoFitSuggestionPlanDetails, StringComparison.Ordinal);
        Assert.Contains(
            "mantienen su rol Fixed/RigidMove",
            viewModel.AutoFitSuggestionPlanDetails,
            StringComparison.Ordinal);

        viewModel.ApplyCommissionedStructuralComparison(
            scenario.Review.GeometryPaths.ToArray(),
            "Confirmed whole-plan registration");

        Assert.Contains(
            "El WALL original de Electrical no se presenta como arquitectura proyectada.",
            viewModel.CommissionedComparisonStatus,
            StringComparison.Ordinal);
        Assert.Contains(
            "No se muestran dispositivos ni cableado.",
            viewModel.CommissionedComparisonStatus,
            StringComparison.Ordinal);
        Assert.Contains("ArchitecturalBase", viewModel.CommissionedComparisonStatus, StringComparison.Ordinal);
        Assert.True(viewModel.CanExportAdjustedSitePlan);

        viewModel.MarkCommissionedComparisonUnavailable(
            "Electrical no está disponible: evidencia de registro vencida.");

        Assert.False(viewModel.CanExportAdjustedSitePlan);
    }

    [Fact]
    public void BuildCommissioned_rejects_a_profile_from_a_different_published_curation()
    {
        var scenario = Scenario.Create();
        var staleProfile = scenario.Profile with { PublishedCurationId = Guid.NewGuid() };

        var result = SitePlanAdjustmentPreviewProjector.BuildCommissioned(
            scenario.LibraryItem,
            scenario.Version,
            scenario.Review,
            scenario.Site,
            staleProfile);

        Assert.False(result.Succeeded);
        Assert.Null(result.ViewModel);
        Assert.Contains("curaci", result.RejectionReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildCommissioned_requires_measurement_context_and_accepted_structural_geometry()
    {
        var missingMeasurement = Scenario.Create();
        missingMeasurement.Review.MeasurementContext = null;

        var measurementResult = SitePlanAdjustmentPreviewProjector.BuildCommissioned(
            missingMeasurement.LibraryItem,
            missingMeasurement.Version,
            missingMeasurement.Review,
            missingMeasurement.Site,
            missingMeasurement.Profile);

        Assert.False(measurementResult.Succeeded);
        Assert.Null(measurementResult.ViewModel);
        Assert.Contains("medici", measurementResult.RejectionReason, StringComparison.OrdinalIgnoreCase);

        var missingStructure = Scenario.Create(addAcceptedWalls: false);
        var structureResult = SitePlanAdjustmentPreviewProjector.BuildCommissioned(
            missingStructure.LibraryItem,
            missingStructure.Version,
            missingStructure.Review,
            missingStructure.Site,
            missingStructure.Profile);

        Assert.False(structureResult.Succeeded);
        Assert.Null(structureResult.ViewModel);
        Assert.Contains("estructural", structureResult.RejectionReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildCommissioned_returns_untouched_preview_and_blocks_export_when_preview_rejects_the_profile()
    {
        var scenario = Scenario.Create();
        var missingTop = Line(0m, 10m, 10m, 10m);
        var missingBottom = Line(0m, 0m, 10m, 0m);
        var unsafeWidthAction = Action(
            "missing-width",
            "Width",
            "Right",
            10m,
            5m,
            missingTop,
            missingBottom,
            closingVertexIndex: 1,
            [new AdjustmentRecipeEntityRoleDto("LABEL:A", null, null, "Fixed", [])]);
        var unsafeProfile = scenario.Profile with
        {
            Variables =
            [
                new CommissionedAdaptationVariable(
                    "width",
                    "Width",
                    HouseAdaptationAxis.Width,
                    1,
                    [unsafeWidthAction]),
                scenario.Profile.Variables.Single(variable => variable.Axis == HouseAdaptationAxis.Depth)
            ]
        };

        var result = SitePlanAdjustmentPreviewProjector.BuildCommissioned(
            scenario.LibraryItem,
            scenario.Version,
            scenario.Review,
            scenario.Site,
            unsafeProfile,
            floorPlanSourcePath: "floor.dxf",
            sitePlanSourcePath: "site.dxf",
            adjustedSitePlanExporter: new NoOpAdjustedSitePlanExporter());

        Assert.False(result.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(result.RejectionReason));
        var viewModel = Assert.IsType<SitePlanAdjustmentViewModel>(result.ViewModel);
        var bounds = Bounds(viewModel.FloorPlanGeometryPaths);
        Assert.Equal(20m, bounds.MaxX - bounds.MinX);
        Assert.Equal(20m, bounds.MaxY - bounds.MinY);
        Assert.Empty(viewModel.BuildAdjustedSitePlanPlacement().StretchActions);
        Assert.False(viewModel.CanExportAdjustedSitePlan);
        Assert.Contains("bloqueado", viewModel.AutoFitSuggestionStatus, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record Scenario(
        FloorPlanLibraryItemDto LibraryItem,
        FloorPlanLibraryVersionDto Version,
        FloorPlanReviewViewModel Review,
        SitePlanPreviewDto Site,
        CommissionedHouseAdaptationProfile Profile,
        AdjustmentRecipeStretchActionDto WidthAction)
    {
        public static Scenario Create(bool addAcceptedWalls = true)
        {
            var versionId = Guid.NewGuid();
            var curationId = Guid.NewGuid();
            var top = Line(0m, 10m, 10m, 10m);
            var bottom = Line(0m, 0m, 10m, 0m);
            var left = Line(0m, 0m, 0m, 10m);
            var right = Line(10m, 0m, 10m, 10m);
            const string protectedLabel = "LABEL:A";
            var widthAction = Action(
                "width",
                "Width",
                "Right",
                10m,
                5m,
                top,
                bottom,
                closingVertexIndex: 1,
                [
                    Role(right, "width:RIGHT", "RigidMove", []),
                    Role(left, "width:LEFT", "Fixed", []),
                    new AdjustmentRecipeEntityRoleDto(protectedLabel, null, null, "Fixed", [])
                ]);
            var depthAction = Action(
                "depth",
                "Height",
                "Top",
                10m,
                5m,
                left,
                right,
                closingVertexIndex: 1,
                [
                    Role(top, "depth:TOP", "RigidMove", []),
                    Role(bottom, "depth:BOTTOM", "Fixed", []),
                    new AdjustmentRecipeEntityRoleDto(protectedLabel, null, null, "Fixed", [])
                ]);
            var profile = new CommissionedHouseAdaptationProfile(
                versionId,
                curationId,
                25.4m,
                [
                    new CommissionedAdaptationVariable("width", "Width", HouseAdaptationAxis.Width, 1, [widthAction]),
                    new CommissionedAdaptationVariable("depth", "Depth", HouseAdaptationAxis.Depth, 1, [depthAction])
                ],
                [],
                [protectedLabel])
            {
                AuxiliaryEntityBindings =
                [
                    new CommissionExistingCurationAuxiliaryEntityBinding(
                        protectedLabel,
                        CommissionExistingCurationAuxiliaryEntityKind.Label,
                        new AdjustmentRecipeBoundsDto(1m, 1m, 2m, 2m),
                        GeometryPathId: null,
                        SegmentSortOrder: null,
                        IsImmutableSize: false,
                        IsProtected: true)
                ]
            };
            using var provider = new ServiceCollection().BuildServiceProvider();
            var review = new FloorPlanReviewViewModel(
                provider.GetRequiredService<IServiceScopeFactory>(),
                Guid.NewGuid());
            foreach (var path in new[] { top, bottom, left, right })
            {
                review.GeometryPaths.Add(path);
                if (addAcceptedWalls)
                {
                    review.WallCandidates.Add(new WallCandidateDto(
                        Guid.NewGuid(),
                        $"LINE:{path.Id:N}",
                        "WALLS",
                        "Accepted",
                        1m,
                        null,
                        null,
                        path.Id,
                        1));
                }
            }

            review.RoomLabels.Add(new RoomLabelDto(
                Guid.NewGuid(),
                protectedLabel,
                "ROOMS",
                "PROTECTED",
                1.5m,
                1.5m,
                1m,
                null,
                1,
                SourceEntityKind: "TEXT",
                TextHeight: 0.25m));

            review.MeasurementContext = new MeasurementContextDto("inch", 25.4m, 0.1m, 1m);
            var version = new FloorPlanLibraryVersionDto(
                versionId,
                1,
                "Published",
                DateTime.UtcNow,
                "inch",
                IsCurrent: true,
                ActivePublishedCurationId: curationId);
            var libraryItem = new FloorPlanLibraryItemDto(
                Guid.NewGuid(),
                "house",
                "House",
                1,
                versionId,
                1,
                [version]);
            var site = new SitePlanPreviewDto(
                "site.dxf",
                "inch",
                12.7m,
                [Rectangle(0m, 0m, 16m, 20m)],
                new SitePlanBuildableAreaDto(0m, 0m, 16m, 20m));

            return new(libraryItem, version, review, site, profile, widthAction);
        }
    }

    private static AdjustmentRecipeStretchActionDto Action(
        string id,
        string axis,
        string edge,
        decimal cut,
        decimal capacity,
        GeometryPathDto first,
        GeometryPathDto second,
        int closingVertexIndex,
        IReadOnlyList<AdjustmentRecipeEntityRoleDto>? additionalRoles = null)
        => new(
            id,
            axis,
            edge,
            cut,
            0m,
            capacity,
            0.001m,
            new AdjustmentRecipeBoundsDto(0m, 0m, 10m, 10m),
            [Span(first, $"{id}:A", closingVertexIndex), Span(second, $"{id}:B", closingVertexIndex)],
            [
                Role(first, $"{id}:A", "Stretch", [closingVertexIndex]),
                Role(second, $"{id}:B", "Stretch", [closingVertexIndex]),
                .. (additionalRoles ?? Array.Empty<AdjustmentRecipeEntityRoleDto>())
            ]);

    private static AdjustmentRecipeTargetSpanDto Span(
        GeometryPathDto path,
        string entityRef,
        int closingVertexIndex)
    {
        var segment = path.Segments[0];
        return new(
            entityRef,
            path.Id,
            segment.SortOrder,
            segment.StartX,
            segment.StartY,
            segment.EndX,
            segment.EndY,
            closingVertexIndex);
    }

    private static AdjustmentRecipeEntityRoleDto Role(
        GeometryPathDto path,
        string entityRef,
        string role,
        IReadOnlyList<int> vertexIndices)
        => new(entityRef, path.Id, path.Segments[0].SortOrder, role, vertexIndices);

    private static GeometryPathDto Line(decimal startX, decimal startY, decimal endX, decimal endY)
    {
        var id = Guid.NewGuid();
        return new(id, false, [new GeometrySegmentDto(id, 1, startX, startY, endX, endY)]);
    }

    private static GeometryPathDto Rectangle(decimal minX, decimal minY, decimal maxX, decimal maxY)
    {
        var id = Guid.NewGuid();
        return new(
            id,
            true,
            [
                new GeometrySegmentDto(id, 1, minX, minY, maxX, minY),
                new GeometrySegmentDto(id, 2, maxX, minY, maxX, maxY),
                new GeometrySegmentDto(id, 3, maxX, maxY, minX, maxY),
                new GeometrySegmentDto(id, 4, minX, maxY, minX, minY)
            ]);
    }

    private static (decimal MinX, decimal MinY, decimal MaxX, decimal MaxY) Bounds(
        IEnumerable<GeometryPathDto> paths)
    {
        var segments = paths.SelectMany(path => path.Segments).ToArray();
        return (
            segments.Min(segment => decimal.Min(segment.StartX, segment.EndX)),
            segments.Min(segment => decimal.Min(segment.StartY, segment.EndY)),
            segments.Max(segment => decimal.Max(segment.StartX, segment.EndX)),
            segments.Max(segment => decimal.Max(segment.StartY, segment.EndY)));
    }

    private static IReadOnlyList<(decimal StartX, decimal StartY, decimal EndX, decimal EndY)> SegmentCoordinates(
        IEnumerable<GeometryPathDto> paths)
        => paths
            .SelectMany(path => path.Segments)
            .Select(segment => (segment.StartX, segment.StartY, segment.EndX, segment.EndY))
            .ToArray();

    private sealed class NoOpAdjustedSitePlanExporter : IAdjustedSitePlanExporter
    {
        public Task<AdjustedSitePlanExportResult> ExportAsync(
            string floorPlanSourcePath,
            string sitePlanSourcePath,
            string outputFilePath,
            AdjustedSitePlanPlacementDto placement,
            CancellationToken cancellationToken)
            => Task.FromResult(new AdjustedSitePlanExportResult(outputFilePath, 0, []));
    }
}
