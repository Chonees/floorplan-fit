using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.ViewModels;
using FloorplanFit.Domain.Documents;
using FloorplanFit.Domain.FloorPlans;
using FloorplanFit.Application.FloorPlans.Import;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FloorplanFit.Desktop.Tests.ViewModels;

public sealed class MeasurementBindingFloorPlanReviewViewModelTests
{
    [Fact]
    public async Task MeasurementBindingCopy_is_exposed_in_spanish()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var services = BuildServices(
            template,
            CreateSession(templateId),
            new InMemoryMeasurementCorridorRepository(),
            new InMemoryMeasurementNodeRepository(),
            new InMemoryDimensionIntervalBindingRepository());

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);
        await viewModel.LoadAsync(CancellationToken.None);

        Assert.Equal("Elegir punto", viewModel.AddMeasurementNodeButtonLabel);
        Assert.True(viewModel.ArePreviewDimensionsVisible);
        Assert.Equal("Seleccion\u00E1 una cota para definir qu\u00E9 mide.", viewModel.SelectedDimensionIntervalBindingSummary);
        Assert.Equal("Seleccion\u00E1 un grupo de ajuste para ver qu\u00E9 franjas de medida toca.", viewModel.SelectedPinchGroupImpactSummary);
        Assert.Equal("Seleccion\u00E1 una cota para ver qu\u00E9 grupos de ajuste podr\u00EDan afectarla.", viewModel.SelectedDimensionImpactSummary);
    }

    [Fact]
    public async Task AddMeasurementCorridorAsync_uses_selected_preview_geometry_and_refreshes_corridors()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var geometryPathId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var session = CreateSession(templateId);
        var corridorRepository = new InMemoryMeasurementCorridorRepository();
        var services = BuildServices(template, session, corridorRepository, new InMemoryMeasurementNodeRepository(), new InMemoryDimensionIntervalBindingRepository());

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);
        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.NewMeasurementCorridorName = "Patio-Width";
        viewModel.SelectedPinchAxis = nameof(PinchAxisTag.Width);
        Assert.True(viewModel.SelectPreviewPath(geometryPathId));

        await viewModel.AddMeasurementCorridorAsync(CancellationToken.None);

        var saved = Assert.Single(corridorRepository.Items);
        Assert.Equal("Patio-Width", saved.Name);
        Assert.Equal(geometryPathId, saved.GuideGeometryPathId);
        Assert.Equal(PinchAxisTag.Width, saved.AxisTag);
    }

    [Fact]
    public async Task HandlePreviewInteractionAsync_when_measurement_node_placement_is_armed_persists_a_node_for_the_selected_corridor()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var geometryPathId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var corridorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var corridor = new MeasurementCorridorDto(corridorId, "Patio-Width", nameof(PinchAxisTag.Width), geometryPathId, 95m, 145m, "Verified", 1);
        var session = CreateSession(templateId, measurementCorridors: [corridor]);
        var nodeRepository = new InMemoryMeasurementNodeRepository();
        var corridorRepository = new InMemoryMeasurementCorridorRepository(
            new MeasurementCorridor(
                corridorId,
                Guid.Empty,
                "Patio-Width",
                PinchAxisTag.Width,
                geometryPathId,
                95m,
                145m,
                "Verified",
                1));
        var services = BuildServices(template, session, corridorRepository,
            nodeRepository,
            new InMemoryDimensionIntervalBindingRepository());

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);
        await viewModel.LoadAsync(CancellationToken.None);
        corridorRepository.Items.Clear();
        corridorRepository.Items.Add(new MeasurementCorridor(
            corridorId,
            viewModel.DraftCurationId,
            "Patio-Width",
            PinchAxisTag.Width,
            geometryPathId,
            95m,
            145m,
            "Verified",
            1));
        viewModel.SelectedMeasurementCorridor = Assert.Single(viewModel.MeasurementCorridors);
        viewModel.ToggleMeasurementNodePlacement();

        await viewModel.HandlePreviewInteractionAsync(geometryPathId, 0.5m, CancellationToken.None);

        var saved = Assert.Single(nodeRepository.Items);
        Assert.Equal(corridorId, saved.CorridorId);
        Assert.Equal(FloorPlanArtifactSourceKinds.WallCandidate, saved.SourceArtifactKind);
        Assert.Equal(geometryPathId, saved.GeometryPathId);
        Assert.Equal(0.5m, saved.PositionRatio);
        Assert.Equal(100m, saved.AxisCoordinate);
    }

    [Fact]
    public async Task ToggleMeasurementNodePlacement_uses_spanish_guidance_messages()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var geometryPathId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var corridorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var corridor = new MeasurementCorridorDto(corridorId, "Patio-Width", nameof(PinchAxisTag.Width), geometryPathId, 95m, 145m, "Verified", 1);
        var session = CreateSession(templateId, measurementCorridors: [corridor]);
        var services = BuildServices(
            template,
            session,
            new InMemoryMeasurementCorridorRepository(),
            new InMemoryMeasurementNodeRepository(),
            new InMemoryDimensionIntervalBindingRepository());

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);
        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.SelectedMeasurementCorridor = Assert.Single(viewModel.MeasurementCorridors);

        viewModel.ToggleMeasurementNodePlacement();
        Assert.Equal("Ahora hac\u00E9 click en una l\u00EDnea o punto v\u00E1lido del preview para marcar un punto de medida en Patio-Width.", viewModel.StatusMessage);

        viewModel.ToggleMeasurementNodePlacement();
        Assert.Equal("Selecci\u00F3n de punto cancelada.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task TogglePinchPlacement_uses_spanish_guidance_messages()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var geometryPathId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var pinchGroupId = Guid.Parse("12121212-1212-1212-1212-121212121212");
        var session = CreateSession(
            templateId,
            pinchGroups:
            [
                new PinchGroupDto(pinchGroupId, "Patio", nameof(PinchAxisTag.Width), 1)
            ]);
        var services = BuildServices(
            template,
            session,
            new InMemoryMeasurementCorridorRepository(),
            new InMemoryMeasurementNodeRepository(),
            new InMemoryDimensionIntervalBindingRepository());

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);
        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.SelectedPinchGroup = Assert.Single(viewModel.PinchGroups);
        Assert.True(viewModel.SelectPreviewPath(geometryPathId));

        Assert.Equal("Agregar ajuste", viewModel.AddPinchButtonLabel);

        viewModel.TogglePinchPlacement();
        Assert.Equal("Ahora hac\u00E9 click en el preview para marcar un ajuste de Patio.", viewModel.StatusMessage);

        viewModel.TogglePinchPlacement();
        Assert.Equal("Selecci\u00F3n de ajuste cancelada.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task HandlePreviewInteractionAsync_does_not_crash_when_refresh_clears_selected_measurement_corridor()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var geometryPathId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var corridorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var initialSession = CreateSession(
            templateId,
            measurementCorridors:
            [
                new MeasurementCorridorDto(corridorId, "Patio-Width", nameof(PinchAxisTag.Width), geometryPathId, 95m, 145m, "Verified", 1)
            ]);
        var refreshedSession = CreateSession(templateId);
        var reader = new SequencedFloorPlanReviewSessionReader(initialSession, refreshedSession);
        var nodeRepository = new InMemoryMeasurementNodeRepository();
        var corridorRepository = new InMemoryMeasurementCorridorRepository(
            new MeasurementCorridor(
                corridorId,
                Guid.Empty,
                "Patio-Width",
                PinchAxisTag.Width,
                geometryPathId,
                95m,
                145m,
                "Verified",
                1));
        var services = BuildServices(
            template,
            reader,
            corridorRepository,
            nodeRepository,
            new InMemoryDimensionIntervalBindingRepository());

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);
        await viewModel.LoadAsync(CancellationToken.None);
        corridorRepository.Items.Clear();
        corridorRepository.Items.Add(new MeasurementCorridor(
            corridorId,
            viewModel.DraftCurationId,
            "Patio-Width",
            PinchAxisTag.Width,
            geometryPathId,
            95m,
            145m,
            "Verified",
            1));
        viewModel.SelectedMeasurementCorridor = Assert.Single(viewModel.MeasurementCorridors);
        viewModel.ToggleMeasurementNodePlacement();

        var exception = await Record.ExceptionAsync(() => viewModel.HandlePreviewInteractionAsync(geometryPathId, 0.5m, CancellationToken.None));

        Assert.Null(exception);
        Assert.Single(nodeRepository.Items);
    }

    [Fact]
    public async Task SaveSelectedDimensionIntervalBindingAsync_persists_manual_verified_binding_for_the_selected_dimension()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var geometryPathId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var corridorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var startNodeId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var endNodeId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var dimension = CreateDimension(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));
        var corridor = new MeasurementCorridorDto(corridorId, "Patio-Width", nameof(PinchAxisTag.Width), geometryPathId, 95m, 145m, "Verified", 1);
        var startNode = new MeasurementNodeDto(startNodeId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), geometryPathId, "Projected", 100m, 120m, 100m, 0m, 0m, 0.5m);
        var endNode = new MeasurementNodeDto(endNodeId, corridorId, 2, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), geometryPathId, "Projected", 224m, 120m, 224m, 0m, 0m, 1m);
        var session = CreateSession(templateId, dimensions: [dimension], measurementCorridors: [corridor], measurementNodes: [startNode, endNode]);
        var bindingRepository = new InMemoryDimensionIntervalBindingRepository();
        var corridorRepository = new InMemoryMeasurementCorridorRepository(
            new MeasurementCorridor(
                corridorId,
                Guid.Empty,
                "Patio-Width",
                PinchAxisTag.Width,
                geometryPathId,
                95m,
                145m,
                "Verified",
                1));
        var nodeRepository = new InMemoryMeasurementNodeRepository(
            new MeasurementNode(
                startNodeId,
                Guid.Empty,
                corridorId,
                1,
                "ProjectedGeometry",
                FloorPlanArtifactSourceKinds.WallCandidate,
                startNode.SourceArtifactId,
                geometryPathId,
                "Projected",
                100m,
                120m,
                100m,
                0m,
                0m,
                0.5m),
            new MeasurementNode(
                endNodeId,
                Guid.Empty,
                corridorId,
                2,
                "ProjectedGeometry",
                FloorPlanArtifactSourceKinds.WallCandidate,
                endNode.SourceArtifactId,
                geometryPathId,
                "Projected",
                224m,
                120m,
                224m,
                0m,
                0m,
                1m));
        var services = BuildServices(
            template,
            session,
            corridorRepository,
            nodeRepository,
            bindingRepository);

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);
        await viewModel.LoadAsync(CancellationToken.None);
        corridorRepository.Items.Clear();
        corridorRepository.Items.Add(new MeasurementCorridor(
            corridorId,
            viewModel.DraftCurationId,
            "Patio-Width",
            PinchAxisTag.Width,
            geometryPathId,
            95m,
            145m,
            "Verified",
            1));
        nodeRepository.Items.Clear();
        nodeRepository.Items.Add(new MeasurementNode(
            startNodeId,
            viewModel.DraftCurationId,
            corridorId,
            1,
            "ProjectedGeometry",
            FloorPlanArtifactSourceKinds.WallCandidate,
            startNode.SourceArtifactId,
            geometryPathId,
            "Projected",
            100m,
            120m,
            100m,
            0m,
            0m,
            0.5m));
        nodeRepository.Items.Add(new MeasurementNode(
            endNodeId,
            viewModel.DraftCurationId,
            corridorId,
            2,
            "ProjectedGeometry",
            FloorPlanArtifactSourceKinds.WallCandidate,
            endNode.SourceArtifactId,
            geometryPathId,
            "Projected",
            224m,
            120m,
            224m,
            0m,
            0m,
            1m));
        viewModel.SelectDimension(dimension.DimensionId);
        viewModel.SelectedMeasurementCorridor = Assert.Single(viewModel.MeasurementCorridors);
        viewModel.SelectedMeasurementStartNode = viewModel.MeasurementNodes.Single(item => item.NodeId == startNodeId);
        viewModel.SelectedMeasurementEndNode = viewModel.MeasurementNodes.Single(item => item.NodeId == endNodeId);

        await viewModel.SaveSelectedDimensionIntervalBindingAsync(CancellationToken.None);

        var saved = Assert.Single(bindingRepository.Items);
        Assert.Equal(dimension.DimensionId, saved.DimensionId);
        Assert.Equal(corridorId, saved.CorridorId);
        Assert.Equal("ManualVerified", saved.BindingStatus);
        Assert.Equal(100m, saved.IntervalStartCoordinate);
        Assert.Equal(224m, saved.IntervalEndCoordinate);
    }

    [Fact]
    public async Task Free_angle_dimensions_can_be_saved_against_the_manual_two_node_line()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var geometryPathId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var corridorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var startNodeId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var endNodeId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var dimensionId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var dimension = CreateDimension(dimensionId) with
        {
            Angle = 25m,
            DefPoint2Y = 160m
        };
        var corridor = new MeasurementCorridorDto(corridorId, "Patio-Width", nameof(PinchAxisTag.Width), geometryPathId, 95m, 145m, "Verified", 1);
        var startNode = new MeasurementNodeDto(startNodeId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), geometryPathId, "Projected", 100m, 120m, 100m, 0m, 0m, 0.5m);
        var endNode = new MeasurementNodeDto(endNodeId, corridorId, 2, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), geometryPathId, "Projected", 224m, 120m, 224m, 0m, 0m, 1m);
        var session = CreateSession(templateId, dimensions: [dimension], measurementCorridors: [corridor], measurementNodes: [startNode, endNode]);
        var services = BuildServices(
            template,
            session,
            new InMemoryMeasurementCorridorRepository(),
            new InMemoryMeasurementNodeRepository(),
            new InMemoryDimensionIntervalBindingRepository());

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);
        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.SelectDimension(dimensionId);
        viewModel.SelectedMeasurementCorridor = Assert.Single(viewModel.MeasurementCorridors);

        Assert.True(viewModel.CanSaveSelectedDimensionIntervalBinding);
        Assert.Equal("Estado: medida fija (lista para guardar relación manual)", viewModel.SelectedDimensionIntervalBindingSummary);
    }

    [Fact]
    public async Task SaveSelectedDimensionIntervalBindingAsync_does_not_crash_when_refresh_clears_measurement_selection()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var geometryPathId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var corridorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var startNodeId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var endNodeId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var dimension = CreateDimension(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));
        var corridor = new MeasurementCorridorDto(corridorId, "Patio-Width", nameof(PinchAxisTag.Width), geometryPathId, 95m, 145m, "Verified", 1);
        var startNode = new MeasurementNodeDto(startNodeId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), geometryPathId, "Projected", 100m, 120m, 100m, 0m, 0m, 0.5m);
        var endNode = new MeasurementNodeDto(endNodeId, corridorId, 2, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), geometryPathId, "Projected", 224m, 120m, 224m, 0m, 0m, 1m);
        var initialSession = CreateSession(templateId, dimensions: [dimension], measurementCorridors: [corridor], measurementNodes: [startNode, endNode]);
        var refreshedSession = CreateSession(templateId, dimensions: [dimension]);
        var reader = new SequencedFloorPlanReviewSessionReader(initialSession, refreshedSession);
        var bindingRepository = new InMemoryDimensionIntervalBindingRepository();
        var corridorRepository = new InMemoryMeasurementCorridorRepository(
            new MeasurementCorridor(
                corridorId,
                Guid.Empty,
                "Patio-Width",
                PinchAxisTag.Width,
                geometryPathId,
                95m,
                145m,
                "Verified",
                1));
        var nodeRepository = new InMemoryMeasurementNodeRepository(
            new MeasurementNode(
                startNodeId,
                Guid.Empty,
                corridorId,
                1,
                "ProjectedGeometry",
                FloorPlanArtifactSourceKinds.WallCandidate,
                startNode.SourceArtifactId,
                geometryPathId,
                "Projected",
                100m,
                120m,
                100m,
                0m,
                0m,
                0.5m),
            new MeasurementNode(
                endNodeId,
                Guid.Empty,
                corridorId,
                2,
                "ProjectedGeometry",
                FloorPlanArtifactSourceKinds.WallCandidate,
                endNode.SourceArtifactId,
                geometryPathId,
                "Projected",
                224m,
                120m,
                224m,
                0m,
                0m,
                1m));
        var services = BuildServices(
            template,
            reader,
            corridorRepository,
            nodeRepository,
            bindingRepository);

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);
        await viewModel.LoadAsync(CancellationToken.None);
        corridorRepository.Items.Clear();
        corridorRepository.Items.Add(new MeasurementCorridor(
            corridorId,
            viewModel.DraftCurationId,
            "Patio-Width",
            PinchAxisTag.Width,
            geometryPathId,
            95m,
            145m,
            "Verified",
            1));
        nodeRepository.Items.Clear();
        nodeRepository.Items.Add(new MeasurementNode(
            startNodeId,
            viewModel.DraftCurationId,
            corridorId,
            1,
            "ProjectedGeometry",
            FloorPlanArtifactSourceKinds.WallCandidate,
            startNode.SourceArtifactId,
            geometryPathId,
            "Projected",
            100m,
            120m,
            100m,
            0m,
            0m,
            0.5m));
        nodeRepository.Items.Add(new MeasurementNode(
            endNodeId,
            viewModel.DraftCurationId,
            corridorId,
            2,
            "ProjectedGeometry",
            FloorPlanArtifactSourceKinds.WallCandidate,
            endNode.SourceArtifactId,
            geometryPathId,
            "Projected",
            224m,
            120m,
            224m,
            0m,
            0m,
            1m));
        viewModel.SelectDimension(dimension.DimensionId);
        viewModel.SelectedMeasurementCorridor = Assert.Single(viewModel.MeasurementCorridors);
        viewModel.SelectedMeasurementStartNode = viewModel.MeasurementNodes.Single(item => item.NodeId == startNodeId);
        viewModel.SelectedMeasurementEndNode = viewModel.MeasurementNodes.Single(item => item.NodeId == endNodeId);

        var exception = await Record.ExceptionAsync(() => viewModel.SaveSelectedDimensionIntervalBindingAsync(CancellationToken.None));

        Assert.Null(exception);
        var saved = Assert.Single(bindingRepository.Items);
        Assert.Equal(dimension.DimensionId, saved.DimensionId);
    }

    [Fact]
    public async Task Selecting_dimension_and_corridor_with_exactly_two_nodes_auto_assigns_start_and_end_and_enables_save()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var geometryPathId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var corridorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var startNodeId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var endNodeId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var dimension = CreateDimension(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));
        var corridor = new MeasurementCorridorDto(corridorId, "Patio-Width", nameof(PinchAxisTag.Width), geometryPathId, 95m, 145m, "Verified", 1);
        var startNode = new MeasurementNodeDto(startNodeId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), geometryPathId, "Projected", 100m, 120m, 100m, 0m, 0m, 0.5m);
        var endNode = new MeasurementNodeDto(endNodeId, corridorId, 2, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), geometryPathId, "Projected", 224m, 120m, 224m, 0m, 0m, 1m);
        var session = CreateSession(templateId, dimensions: [dimension], measurementCorridors: [corridor], measurementNodes: [startNode, endNode]);
        var services = BuildServices(
            template,
            session,
            new InMemoryMeasurementCorridorRepository(),
            new InMemoryMeasurementNodeRepository(),
            new InMemoryDimensionIntervalBindingRepository());

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);
        await viewModel.LoadAsync(CancellationToken.None);

        viewModel.SelectDimension(dimension.DimensionId);
        viewModel.SelectedMeasurementCorridor = Assert.Single(viewModel.MeasurementCorridors);

        Assert.Equal(startNodeId, viewModel.SelectedMeasurementStartNode?.NodeId);
        Assert.Equal(endNodeId, viewModel.SelectedMeasurementEndNode?.NodeId);
        Assert.True(viewModel.CanSaveSelectedDimensionIntervalBinding);
    }

    [Fact]
    public async Task RestoreSelectedDimensionIntervalBindingAsync_deletes_the_manual_verified_binding_for_the_selected_dimension()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var geometryPathId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var corridorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var startNodeId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var endNodeId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var dimension = CreateDimension(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));
        var corridor = new MeasurementCorridorDto(corridorId, "Patio-Width", nameof(PinchAxisTag.Width), geometryPathId, 95m, 145m, "Verified", 1);
        var startNode = new MeasurementNodeDto(startNodeId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), geometryPathId, "Projected", 100m, 120m, 100m, 0m, 0m, 0.5m);
        var endNode = new MeasurementNodeDto(endNodeId, corridorId, 2, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), geometryPathId, "Projected", 224m, 120m, 224m, 0m, 0m, 1m);
        var binding = new DimensionIntervalBindingDto(dimension.DimensionId, corridorId, startNodeId, endNodeId, "ManualVerified", 100m, 224m);
        var session = CreateSession(templateId, dimensions: [dimension], measurementCorridors: [corridor], measurementNodes: [startNode, endNode], dimensionIntervalBindings: [binding]);
        var bindingRepository = new InMemoryDimensionIntervalBindingRepository(
            new DimensionIntervalBinding(Guid.Empty, dimension.DimensionId, corridorId, startNodeId, endNodeId, "ManualVerified", 100m, 224m, DateTime.UtcNow));
        var services = BuildServices(
            template,
            session,
            new InMemoryMeasurementCorridorRepository(),
            new InMemoryMeasurementNodeRepository(),
            bindingRepository);

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);
        await viewModel.LoadAsync(CancellationToken.None);
        bindingRepository.Items.Clear();
        bindingRepository.Items.Add(new DimensionIntervalBinding(
            viewModel.DraftCurationId,
            dimension.DimensionId,
            corridorId,
            startNodeId,
            endNodeId,
            "ManualVerified",
            100m,
            224m,
            DateTime.UtcNow));
        viewModel.SelectDimension(dimension.DimensionId);

        await viewModel.RestoreSelectedDimensionIntervalBindingAsync(CancellationToken.None);

        Assert.Empty(bindingRepository.Items);
    }

    [Fact]
    public async Task RemoveSelectedMeasurementCorridorAsync_deletes_the_selected_corridor_and_clears_related_measurement_selection()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var geometryPathId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var corridorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var startNodeId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var endNodeId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var dimensionId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var initialSession = CreateSession(
            templateId,
            dimensions: [CreateDimension(dimensionId)],
            measurementCorridors:
            [
                new MeasurementCorridorDto(corridorId, "Patio-Width", nameof(PinchAxisTag.Width), geometryPathId, 95m, 145m, "Verified", 1)
            ],
            measurementNodes:
            [
                new MeasurementNodeDto(startNodeId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), geometryPathId, "Projected", 100m, 120m, 100m, 0m, 0m, 0.5m),
                new MeasurementNodeDto(endNodeId, corridorId, 2, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), geometryPathId, "Projected", 224m, 120m, 224m, 0m, 0m, 1m)
            ],
            dimensionIntervalBindings:
            [
                new DimensionIntervalBindingDto(dimensionId, corridorId, startNodeId, endNodeId, "ManualVerified", 100m, 224m)
            ]);
        var refreshedSession = CreateSession(templateId, dimensions: [CreateDimension(dimensionId)]);
        var reader = new SequencedFloorPlanReviewSessionReader(initialSession, refreshedSession);
        var corridorRepository = new InMemoryMeasurementCorridorRepository(
            new MeasurementCorridor(
                corridorId,
                Guid.Empty,
                "Patio-Width",
                PinchAxisTag.Width,
                geometryPathId,
                95m,
                145m,
                "Verified",
                1));
        var nodeRepository = new InMemoryMeasurementNodeRepository(
            new MeasurementNode(
                startNodeId,
                Guid.Empty,
                corridorId,
                1,
                "ProjectedGeometry",
                FloorPlanArtifactSourceKinds.WallCandidate,
                Guid.NewGuid(),
                geometryPathId,
                "Projected",
                100m,
                120m,
                100m,
                0m,
                0m,
                0.5m),
            new MeasurementNode(
                endNodeId,
                Guid.Empty,
                corridorId,
                2,
                "ProjectedGeometry",
                FloorPlanArtifactSourceKinds.WallCandidate,
                Guid.NewGuid(),
                geometryPathId,
                "Projected",
                224m,
                120m,
                224m,
                0m,
                0m,
                1m));
        var bindingRepository = new InMemoryDimensionIntervalBindingRepository(
            new DimensionIntervalBinding(Guid.Empty, dimensionId, corridorId, startNodeId, endNodeId, "ManualVerified", 100m, 224m, DateTime.UtcNow));
        var services = BuildServices(template, reader, corridorRepository, nodeRepository, bindingRepository);

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);
        await viewModel.LoadAsync(CancellationToken.None);
        corridorRepository.Items.Clear();
        corridorRepository.Items.Add(new MeasurementCorridor(
            corridorId,
            viewModel.DraftCurationId,
            "Patio-Width",
            PinchAxisTag.Width,
            geometryPathId,
            95m,
            145m,
            "Verified",
            1));
        nodeRepository.Items.Clear();
        nodeRepository.Items.Add(new MeasurementNode(
            startNodeId,
            viewModel.DraftCurationId,
            corridorId,
            1,
            "ProjectedGeometry",
            FloorPlanArtifactSourceKinds.WallCandidate,
            Guid.NewGuid(),
            geometryPathId,
            "Projected",
            100m,
            120m,
            100m,
            0m,
            0m,
            0.5m));
        nodeRepository.Items.Add(new MeasurementNode(
            endNodeId,
            viewModel.DraftCurationId,
            corridorId,
            2,
            "ProjectedGeometry",
            FloorPlanArtifactSourceKinds.WallCandidate,
            Guid.NewGuid(),
            geometryPathId,
            "Projected",
            224m,
            120m,
            224m,
            0m,
            0m,
            1m));
        bindingRepository.Items.Clear();
        bindingRepository.Items.Add(new DimensionIntervalBinding(
            viewModel.DraftCurationId,
            dimensionId,
            corridorId,
            startNodeId,
            endNodeId,
            "ManualVerified",
            100m,
            224m,
            DateTime.UtcNow));
        viewModel.SelectDimension(dimensionId);
        viewModel.SelectedMeasurementCorridor = Assert.Single(viewModel.MeasurementCorridors);
        viewModel.SelectedMeasurementStartNode = viewModel.MeasurementNodes.Single(item => item.NodeId == startNodeId);
        viewModel.SelectedMeasurementEndNode = viewModel.MeasurementNodes.Single(item => item.NodeId == endNodeId);

        await viewModel.RemoveSelectedMeasurementCorridorAsync(CancellationToken.None);

        Assert.Empty(corridorRepository.Items);
        Assert.Empty(nodeRepository.Items);
        Assert.Empty(bindingRepository.Items);
        Assert.Null(viewModel.SelectedMeasurementCorridor);
        Assert.Null(viewModel.SelectedMeasurementStartNode);
        Assert.Null(viewModel.SelectedMeasurementEndNode);
        Assert.False(viewModel.CanRestoreSelectedDimensionIntervalBinding);
    }

    [Fact]
    public async Task SelectedPinchGroupImpactSummary_lists_overlapping_corridors_for_the_selected_band()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var overlappingCorridorId = Guid.NewGuid();
        var nonOverlappingCorridorId = Guid.NewGuid();
        var geometryPathId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var session = CreateSession(
            templateId,
            pinchGroups:
            [
                new PinchGroupDto(pinchGroupId, "Patio Width", nameof(PinchAxisTag.Width), 1)
            ],
            measurementCorridors:
            [
                new MeasurementCorridorDto(overlappingCorridorId, "Patio-Width", nameof(PinchAxisTag.Width), geometryPathId, 95m, 145m, "Verified", 1),
                new MeasurementCorridorDto(nonOverlappingCorridorId, "Bath-Height", nameof(PinchAxisTag.Height), geometryPathId, 95m, 145m, "Verified", 2)
            ],
            articulationBands:
            [
                new ArticulationBandDto(pinchGroupId, "Patio Width", nameof(PinchAxisTag.Width), 100m, 140m, 80m, "Verified")
            ]);
        var services = BuildServices(
            template,
            session,
            new InMemoryMeasurementCorridorRepository(),
            new InMemoryMeasurementNodeRepository(),
            new InMemoryDimensionIntervalBindingRepository());

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);
        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.SelectedPinchGroup = Assert.Single(viewModel.PinchGroups);

        Assert.Contains("Patio-Width", viewModel.SelectedPinchGroupImpactSummary);
        Assert.DoesNotContain("Bath-Height", viewModel.SelectedPinchGroupImpactSummary);
    }

    [Fact]
    public async Task SelectedDimensionImpactSummary_lists_overlapping_pinch_groups_for_the_manual_verified_binding()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var dimensionId = Guid.NewGuid();
        var corridorId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var otherPinchGroupId = Guid.NewGuid();
        var geometryPathId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var session = CreateSession(
            templateId,
            dimensions: [CreateDimension(dimensionId)],
            pinchGroups:
            [
                new PinchGroupDto(pinchGroupId, "Patio Width", nameof(PinchAxisTag.Width), 1),
                new PinchGroupDto(otherPinchGroupId, "Far Width", nameof(PinchAxisTag.Width), 2)
            ],
            measurementCorridors:
            [
                new MeasurementCorridorDto(corridorId, "Patio-Width", nameof(PinchAxisTag.Width), geometryPathId, 95m, 145m, "Verified", 1)
            ],
            measurementNodes:
            [
                new MeasurementNodeDto(startNodeId, corridorId, 1, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), geometryPathId, "Projected", 100m, 120m, 100m, 0m, 0m, 0.5m),
                new MeasurementNodeDto(endNodeId, corridorId, 2, "ProjectedGeometry", FloorPlanArtifactSourceKinds.WallCandidate, Guid.NewGuid(), geometryPathId, "Projected", 224m, 120m, 224m, 0m, 0m, 1m)
            ],
            dimensionIntervalBindings:
            [
                new DimensionIntervalBindingDto(dimensionId, corridorId, startNodeId, endNodeId, "ManualVerified", 100m, 224m)
            ],
            articulationBands:
            [
                new ArticulationBandDto(pinchGroupId, "Patio Width", nameof(PinchAxisTag.Width), 150m, 200m, 80m, "Verified"),
                new ArticulationBandDto(otherPinchGroupId, "Far Width", nameof(PinchAxisTag.Width), 260m, 320m, 80m, "Verified")
            ]);
        var services = BuildServices(
            template,
            session,
            new InMemoryMeasurementCorridorRepository(),
            new InMemoryMeasurementNodeRepository(),
            new InMemoryDimensionIntervalBindingRepository());

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);
        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.SelectDimension(dimensionId);

        Assert.Contains("Patio Width", viewModel.SelectedDimensionImpactSummary);
        Assert.DoesNotContain("Far Width", viewModel.SelectedDimensionImpactSummary);
    }

    private static ServiceCollection BuildServices(
        FloorPlanTemplate template,
        FloorPlanReviewSessionDto session,
        InMemoryMeasurementCorridorRepository corridorRepository,
        InMemoryMeasurementNodeRepository nodeRepository,
        InMemoryDimensionIntervalBindingRepository bindingRepository)
        => BuildServices(
            template,
            new FakeFloorPlanReviewSessionReader(session),
            corridorRepository,
            nodeRepository,
            bindingRepository);

    private static ServiceCollection BuildServices(
        FloorPlanTemplate template,
        IFloorPlanReviewSessionReader sessionReader,
        InMemoryMeasurementCorridorRepository corridorRepository,
        InMemoryMeasurementNodeRepository nodeRepository,
        InMemoryDimensionIntervalBindingRepository bindingRepository)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 15, 18, 30, 0, DateTimeKind.Utc)));
        services.AddSingleton(sessionReader);
        services.AddSingleton<IMeasurementCorridorRepository>(corridorRepository);
        services.AddSingleton<IMeasurementNodeRepository>(nodeRepository);
        services.AddSingleton<IDimensionIntervalBindingRepository>(bindingRepository);
        services.AddSingleton<IFloorPlanDimensionOverrideRepository>(new NoOpDimensionOverrideRepository());
        services.AddSingleton<IFloorPlanDimensionBindingOverrideRepository>(new NoOpDimensionBindingOverrideRepository());
        services.AddSingleton<ExportAdjustedDxfHandler>(new FakeExportAdjustedDxfHandler());
        services.AddTransient<AddMeasurementCorridorHandler>();
        services.AddTransient<AddMeasurementNodeHandler>();
        services.AddTransient<SaveDimensionIntervalBindingHandler>();
        services.AddTransient<RestoreDimensionIntervalBindingHandler>();
        services.AddTransient<RemoveMeasurementCorridorHandler>();
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();
        return services;
    }

    private static FloorPlanReviewSessionDto CreateSession(
        Guid templateId,
        IReadOnlyList<DimensionDto>? dimensions = null,
        IReadOnlyList<PinchGroupDto>? pinchGroups = null,
        IReadOnlyList<MeasurementCorridorDto>? measurementCorridors = null,
        IReadOnlyList<MeasurementNodeDto>? measurementNodes = null,
        IReadOnlyList<DimensionIntervalBindingDto>? dimensionIntervalBindings = null,
        IReadOnlyList<ArticulationBandDto>? articulationBands = null)
    {
        return new FloorPlanReviewSessionDto(
            templateId,
            "seminole2000",
            "SEMINOLE2000",
            "Curated Draft",
            1,
            null,
            [
                new GeometryPathDto(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), false, [new GeometrySegmentDto(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), 1, 100m, 100m, 100m, 140m)])
            ],
            [],
            [],
            [],
            [],
            [],
            [
                new WallCandidateDto(Guid.Parse("11111111-1111-1111-1111-111111111111"), "LINE:WALL", "WALLS", "Accepted", 0.95m, null, null, Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), 1)
            ],
            pinchGroups ?? [],
            [],
            [])
        {
            Dimensions = dimensions ?? [],
            MeasurementCorridors = measurementCorridors ?? [],
            MeasurementNodes = measurementNodes ?? [],
            DimensionIntervalBindings = dimensionIntervalBindings ?? [],
            ArticulationBands = articulationBands ?? []
        };
    }

    private static DimensionDto CreateDimension(Guid dimensionId)
    {
        return new DimensionDto(
            dimensionId,
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
            1);
    }

    private sealed class FakeFloorPlanReviewSessionReader : IFloorPlanReviewSessionReader
    {
        private readonly FloorPlanReviewSessionDto session;

        public FakeFloorPlanReviewSessionReader(FloorPlanReviewSessionDto session) => this.session = session;

        public Task<FloorPlanReviewSessionDto?> GetByTemplateAsync(Guid templateId, CancellationToken cancellationToken) => Task.FromResult<FloorPlanReviewSessionDto?>(session);

        public Task<FloorPlanReviewSessionDto?> GetByVersionAsync(Guid templateId, Guid floorPlanVersionId, CancellationToken cancellationToken) => Task.FromResult<FloorPlanReviewSessionDto?>(session);
    }

    private sealed class SequencedFloorPlanReviewSessionReader : IFloorPlanReviewSessionReader
    {
        private readonly Queue<FloorPlanReviewSessionDto> sessions;

        public SequencedFloorPlanReviewSessionReader(params FloorPlanReviewSessionDto[] sessions)
        {
            this.sessions = new Queue<FloorPlanReviewSessionDto>(sessions);
        }

        public Task<FloorPlanReviewSessionDto?> GetByTemplateAsync(Guid templateId, CancellationToken cancellationToken)
        {
            var session = sessions.Count > 1 ? sessions.Dequeue() : sessions.Peek();
            return Task.FromResult<FloorPlanReviewSessionDto?>(session);
        }

        public Task<FloorPlanReviewSessionDto?> GetByVersionAsync(Guid templateId, Guid floorPlanVersionId, CancellationToken cancellationToken)
            => GetByTemplateAsync(templateId, cancellationToken);
    }

    private sealed class InMemoryFloorPlanTemplateRepository : IFloorPlanTemplateRepository
    {
        private readonly FloorPlanTemplate template;
        public InMemoryFloorPlanTemplateRepository(FloorPlanTemplate template) => this.template = template;
        public Task<FloorPlanTemplate?> GetByCodeAsync(string code, CancellationToken cancellationToken) => Task.FromResult(template.Code == code ? template : null);
        public Task<FloorPlanTemplate?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken) => Task.FromResult(template.Id == templateId ? template : null);
        public Task AddAsync(FloorPlanTemplate template, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateAsync(FloorPlanTemplate template, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class InMemoryFloorPlanCurationRepository : IFloorPlanCurationRepository
    {
        private readonly List<FloorPlanCuration> items = [];
        public Task<FloorPlanCuration?> GetByIdAsync(Guid curationId, CancellationToken cancellationToken) => Task.FromResult(items.SingleOrDefault(item => item.Id == curationId));
        public Task<FloorPlanCuration?> GetDraftAsync(Guid floorPlanVersionId, CancellationToken cancellationToken) => Task.FromResult(items.SingleOrDefault(item => item.FloorPlanVersionId == floorPlanVersionId && item.Status == FloorPlanCurationStatus.Draft));
        public Task<FloorPlanCuration?> GetPublishedAsync(Guid floorPlanVersionId, CancellationToken cancellationToken) => Task.FromResult<FloorPlanCuration?>(null);
        public Task<int> GetNextCurationVersionAsync(Guid floorPlanVersionId, CancellationToken cancellationToken) => Task.FromResult(1);
        public Task AddAsync(FloorPlanCuration curation, CancellationToken cancellationToken) { items.Add(curation); return Task.CompletedTask; }
        public Task UpdateAsync(FloorPlanCuration curation, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class InMemoryMeasurementCorridorRepository : IMeasurementCorridorRepository
    {
        public InMemoryMeasurementCorridorRepository(params MeasurementCorridor[] items)
        {
            Items.AddRange(items);
        }

        public List<MeasurementCorridor> Items { get; } = [];
        public Task AddAsync(MeasurementCorridor corridor, CancellationToken cancellationToken) { Items.Add(corridor); return Task.CompletedTask; }
        public Task DeleteAsync(Guid corridorId, CancellationToken cancellationToken) { Items.RemoveAll(item => item.Id == corridorId); return Task.CompletedTask; }
        public Task<MeasurementCorridor?> GetByIdAsync(Guid corridorId, CancellationToken cancellationToken) => Task.FromResult(Items.SingleOrDefault(item => item.Id == corridorId));
        public Task<IReadOnlyList<MeasurementCorridor>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<MeasurementCorridor>>(Items.Where(item => item.FloorPlanCurationId == curationId).ToArray());
    }

    private sealed class InMemoryMeasurementNodeRepository : IMeasurementNodeRepository
    {
        public InMemoryMeasurementNodeRepository(params MeasurementNode[] items)
        {
            Items.AddRange(items);
        }

        public List<MeasurementNode> Items { get; } = [];
        public Task AddAsync(MeasurementNode node, CancellationToken cancellationToken) { Items.Add(node); return Task.CompletedTask; }
        public Task<MeasurementNode?> GetByIdAsync(Guid nodeId, CancellationToken cancellationToken) => Task.FromResult(Items.SingleOrDefault(item => item.Id == nodeId));
        public Task<IReadOnlyList<MeasurementNode>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<MeasurementNode>>(Items.Where(item => item.FloorPlanCurationId == curationId).ToArray());
        public Task<IReadOnlyList<MeasurementNode>> ListByCorridorAsync(Guid corridorId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<MeasurementNode>>(Items.Where(item => item.CorridorId == corridorId).ToArray());
        public Task DeleteByCorridorAsync(Guid corridorId, CancellationToken cancellationToken) { Items.RemoveAll(item => item.CorridorId == corridorId); return Task.CompletedTask; }
    }

    private sealed class InMemoryDimensionIntervalBindingRepository : IDimensionIntervalBindingRepository
    {
        public InMemoryDimensionIntervalBindingRepository(params DimensionIntervalBinding[] items)
        {
            Items.AddRange(items);
        }

        public List<DimensionIntervalBinding> Items { get; } = [];
        public Task<IReadOnlyList<DimensionIntervalBinding>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<DimensionIntervalBinding>>(Items.Where(item => item.FloorPlanCurationId == curationId).ToArray());
        public Task UpsertAsync(DimensionIntervalBinding binding, CancellationToken cancellationToken) { Items.RemoveAll(item => item.FloorPlanCurationId == binding.FloorPlanCurationId && item.DimensionId == binding.DimensionId); Items.Add(binding); return Task.CompletedTask; }
        public Task DeleteAsync(Guid floorPlanCurationId, Guid dimensionId, CancellationToken cancellationToken) { Items.RemoveAll(item => item.FloorPlanCurationId == floorPlanCurationId && item.DimensionId == dimensionId); return Task.CompletedTask; }
        public Task DeleteByCorridorAsync(Guid floorPlanCurationId, Guid corridorId, CancellationToken cancellationToken) { Items.RemoveAll(item => item.FloorPlanCurationId == floorPlanCurationId && item.CorridorId == corridorId); return Task.CompletedTask; }
    }

    private sealed class NoOpDimensionOverrideRepository : IFloorPlanDimensionOverrideRepository
    {
        public Task<IReadOnlyList<FloorPlanDimensionOverride>> ListByCurationAsync(Guid floorPlanCurationId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<FloorPlanDimensionOverride>>([]);
        public Task UpsertAsync(FloorPlanDimensionOverride dimensionOverride, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteAsync(Guid floorPlanCurationId, string sourceDimensionKey, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task MarkExportedAsync(Guid floorPlanCurationId, IReadOnlyList<string> sourceDimensionKeys, DateTime exportedAtUtc, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class NoOpDimensionBindingOverrideRepository : IFloorPlanDimensionBindingOverrideRepository
    {
        public Task<IReadOnlyList<FloorPlanDimensionBindingOverride>> ListByCurationAsync(Guid floorPlanCurationId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<FloorPlanDimensionBindingOverride>>([]);
        public Task UpsertAsync(FloorPlanDimensionBindingOverride bindingOverride, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteAsync(Guid floorPlanCurationId, string sourceDimensionKey, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeExportAdjustedDxfHandler : ExportAdjustedDxfHandler
    {
        public FakeExportAdjustedDxfHandler()
            : base(new StubExtractionSourceReader(), new StubReviewReader(), new NoOpDimensionOverrideRepository(), new StubImportedDocumentRepository(), new StubManagedFileStorage(), new StubAdjustedDxfExporter(), new StubFileHashService(), new FakeUnitOfWork(), new FakeClock(DateTime.UtcNow))
        {
        }

        public override Task<ExportAdjustedDxfResponse> HandleAsync(Guid templateId, Guid? floorPlanVersionId, Guid curationId, CancellationToken cancellationToken)
            => Task.FromResult(new ExportAdjustedDxfResponse(Guid.NewGuid(), @"C:\workspace\library\adjusted.dxf", 1));
    }

    private sealed class FakeUnitOfWork : IUnitOfWork { public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask; }
    private sealed class FakeClock : IClock { public FakeClock(DateTime utcNow) { UtcNow = utcNow; } public DateTime UtcNow { get; } }
    private sealed class StubExtractionSourceReader : IFloorPlanExtractionSourceReader
    {
        public Task<FloorPlanExtractionSource?> GetCurrentSourceAsync(Guid templateId, CancellationToken cancellationToken) => Task.FromResult<FloorPlanExtractionSource?>(null);
        public Task<FloorPlanExtractionSource?> GetByVersionAsync(Guid floorPlanVersionId, CancellationToken cancellationToken) => Task.FromResult<FloorPlanExtractionSource?>(null);
    }
    private sealed class StubReviewReader : IFloorPlanReviewSessionReader { public Task<FloorPlanReviewSessionDto?> GetByTemplateAsync(Guid templateId, CancellationToken cancellationToken) => Task.FromResult<FloorPlanReviewSessionDto?>(null); public Task<FloorPlanReviewSessionDto?> GetByVersionAsync(Guid templateId, Guid floorPlanVersionId, CancellationToken cancellationToken) => Task.FromResult<FloorPlanReviewSessionDto?>(null); }
    private sealed class StubImportedDocumentRepository : IImportedDocumentRepository { public Task AddAsync(ImportedDocument document, CancellationToken cancellationToken) => Task.CompletedTask; public Task<ImportedDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<ImportedDocument?>(null); }
    private sealed class StubManagedFileStorage : IManagedFileStorage
    {
        public Task<string> CopyIntoLibraryAsync(string sourceFilePath, CancellationToken cancellationToken) => Task.FromResult(sourceFilePath);
        public Task<string> ReserveAdjustedDxfPathAsync(string sourceFileName, CancellationToken cancellationToken) => Task.FromResult(sourceFileName);
    }

    private sealed class StubAdjustedDxfExporter : IAdjustedDxfExporter
    {
        public Task ExportAsync(string sourceFilePath, string outputFilePath, IReadOnlyList<DimensionDto> dimensions, CancellationToken cancellationToken) => Task.CompletedTask;
    }
    private sealed class StubFileHashService : IFileHashService { public Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken) => Task.FromResult(string.Empty); }
}
