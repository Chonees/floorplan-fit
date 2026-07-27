using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.ViewModels;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FloorplanFit.Desktop.Tests.ViewModels;

public sealed class FloorPlanReviewViewModelTests
{
    [Fact]
    public async Task LoadAsync_loads_the_review_session_with_candidates_and_pinch_markers_only()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var geometryPathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "santa-barbara", "SANTA-BARBARA", isActive: true);
        template.SetCurrentVersion(versionId);
        var openingPathId = Guid.NewGuid();

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 4, 30, 19, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(
            new FloorPlanReviewSessionDto(
                templateId,
                "santa-barbara",
                "SANTA-BARBARA",
                "Curated Draft",
                1,
                null,
                [
                    new GeometryPathDto(
                        geometryPathId,
                        false,
                        [new GeometrySegmentDto(geometryPathId, 1, 0m, 0m, 120m, 0m)]),
                    new GeometryPathDto(
                        openingPathId,
                        false,
                        [new GeometrySegmentDto(openingPathId, 1, 40m, 0m, 76m, 0m)])
                ],
                [
                    new RoomLabelDto(Guid.NewGuid(), "TEXT:1", "ROOM LBLS", "KITCHEN", 125m, 784m, 0.95m, null, 1)
                ],
                [
                    new OpeningCandidateDto(Guid.NewGuid(), "LINE:1", "DOORS", "Door", "LINE", openingPathId, 0.95m, null, 1)
                ],
                [
                    new OpeningLabelDto(Guid.NewGuid(), "TEXT:1", "DOORTEXT", "Door", "2668", 50m, 20m, 0.95m, null, 1)
                ],
                [],
                [],
                [
                    new WallCandidateDto(
                        Guid.NewGuid(),
                        "LINE:1",
                        "WALLS",
                        "Accepted",
                        0.95m,
                        null,
                        null,
                        geometryPathId,
                        1)
                ],
                [
                    new PinchGroupDto(pinchGroupId, "Patio", nameof(PinchAxisTag.Width), 1)
                ],
                [
                    new PinchMarkerDto(
                        Guid.NewGuid(),
                        pinchGroupId,
                        "Patio",
                        Guid.NewGuid(),
                        geometryPathId,
                        nameof(PinchAxisTag.Width),
                        0.5m,
                        120m,
                        1)
                ])
            {
                Dimensions =
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
                        123.810387305188m,
                        3144.7838375517752m,
                        "Inch",
                        0,
                        0m,
                        0m,
                        94.5741888255622m,
                        516.95664946623m,
                        0m,
                        218.38457613075m,
                        524.795084103958m,
                        0m,
                        94.5741888255622m,
                        537.195356591169m,
                        0.0000000000000074m,
                        0.99m,
                        null,
                        1)
                ]
            }));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);

        Assert.Equal("santa-barbara", viewModel.Code);
        Assert.Equal("SANTA-BARBARA", viewModel.Name);
        Assert.Equal("Curated Draft", viewModel.Status);
        Assert.Single(viewModel.WallCandidates);
        Assert.Single(viewModel.RoomLabels);
        Assert.Equal("KITCHEN", viewModel.RoomLabels.Single().Text);
        Assert.Single(viewModel.OpeningCandidates);
        Assert.Single(viewModel.OpeningLabels);
        Assert.Single(viewModel.Dimensions);
        Assert.Single(viewModel.VisibleDimensions);
        Assert.Equal("10'-4\"", viewModel.Dimensions.Single().DisplayText);
        Assert.Equal(1, viewModel.DimensionCount);
        Assert.Equal("Door", viewModel.OpeningCandidates.Single().Kind);
        Assert.Equal("2668", viewModel.OpeningLabels.Single().Text);
        Assert.Equal(1, viewModel.DoorOpeningCount);
        Assert.Equal(0, viewModel.WindowOpeningCount);
        Assert.Single(viewModel.PinchGroups);
        Assert.Single(viewModel.PinchMarkers);
        Assert.Single(viewModel.SelectedPinchGroupMarkers);
        Assert.Equal(viewModel.PinchMarkers.Single().PinchMarkerId, viewModel.SelectedPinchGroupMarkers.Single().PinchMarkerId);
        Assert.Equal("Patio", viewModel.SelectedPinchGroup?.Name);
        Assert.Contains(nameof(PinchAxisTag.Width), viewModel.PinchAxisOptions);
        Assert.Contains(nameof(PinchAxisTag.Height), viewModel.PinchAxisOptions);
        Assert.NotNull(viewModel.SelectedCandidate);
        Assert.Equal(geometryPathId, viewModel.HighlightGeometryPathId);
        Assert.Equal("Previewing candidate: LINE:1", viewModel.PreviewSelectionLabel);
    }

    [Fact]
    public async Task Review_queue_filters_and_search_keep_navigation_minimal_without_touching_preview_data()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var wallPathId = Guid.NewGuid();
        var curatedPathId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 11, 21, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(
            new FloorPlanReviewSessionDto(
                templateId,
                "seminole2000",
                "SEMINOLE2000",
                "Curated Draft",
                1,
                null,
                [
                    new GeometryPathDto(wallPathId, false, [new GeometrySegmentDto(wallPathId, 1, 0m, 0m, 120m, 0m)]),
                    new GeometryPathDto(curatedPathId, false, [new GeometrySegmentDto(curatedPathId, 1, 40m, 0m, 76m, 0m)])
                ],
                [
                    new RoomLabelDto(Guid.NewGuid(), "TEXT:ROOM:1", "ROOM LBLS", "KITCHEN", 125m, 784m, 0.95m, null, 1)
                ],
                [],
                [
                    new OpeningLabelDto(Guid.NewGuid(), "TEXT:OPENING:1", "DOORTEXT", "Door", "2668", 50m, 20m, 0.95m, null, 1)
                ],
                [
                    new FixedPlanComponentDto(
                        Guid.NewGuid(),
                        "INSERT:TUB:1",
                        "FIXTURES",
                        "Tub",
                        "INSERT",
                        "TUB1",
                        [curatedPathId],
                        0.95m,
                        null,
                        1)
                ],
                [],
                [
                    new WallCandidateDto(Guid.NewGuid(), "LINE:WALL:1", "WALLS", "Accepted", 0.95m, null, null, wallPathId, 1)
                ],
                [],
                [])));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.SelectedReviewQueueFilter = "Text & Notes";
        viewModel.ReviewQueueSearchText = "kitch";

        Assert.Empty(viewModel.VisibleWallCandidates);
        Assert.Single(viewModel.VisibleRoomLabels);
        Assert.Equal("KITCHEN", viewModel.VisibleRoomLabels.Single().Text);
        Assert.Empty(viewModel.VisibleOpeningLabels);
        Assert.Single(viewModel.VisibleCuratedPlanArtifacts);
        Assert.Equal(1, viewModel.VisibleQueueItemCount);
        Assert.Equal("Showing 1 of 4 review items", viewModel.QueueSummary);
        Assert.Equal(2, viewModel.GeometryPaths.Count);
    }

    [Fact]
    public async Task Queue_expanders_behave_like_a_single_open_folder_stack()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var wallPathId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 11, 21, 10, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(
            new FloorPlanReviewSessionDto(
                templateId,
                "seminole2000",
                "SEMINOLE2000",
                "Curated Draft",
                1,
                null,
                [
                    new GeometryPathDto(wallPathId, false, [new GeometrySegmentDto(wallPathId, 1, 0m, 0m, 120m, 0m)])
                ],
                [
                    new RoomLabelDto(Guid.NewGuid(), "TEXT:ROOM:1", "ROOM LBLS", "KITCHEN", 125m, 784m, 0.95m, null, 1)
                ],
                [],
                [],
                [],
                [],
                [
                    new WallCandidateDto(Guid.NewGuid(), "LINE:WALL:1", "WALLS", "Accepted", 0.95m, null, null, wallPathId, 1)
                ],
                [],
                [])));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.IsRoomNamesQueueExpanded = true;

        Assert.False(viewModel.IsCuratedObjectsQueueExpanded);
        Assert.True(viewModel.IsRoomNamesQueueExpanded);

        viewModel.IsStructureQueueExpanded = true;

        Assert.True(viewModel.IsStructureQueueExpanded);
        Assert.False(viewModel.IsRoomNamesQueueExpanded);
        Assert.False(viewModel.IsOpeningCodesQueueExpanded);
        Assert.False(viewModel.IsCuratedObjectsQueueExpanded);
    }

    [Fact]
    public async Task Review_queue_filter_changes_rehome_the_open_expander_to_the_first_visible_section()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var wallPathId = Guid.NewGuid();
        var curatedPathId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 11, 21, 15, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(
            new FloorPlanReviewSessionDto(
                templateId,
                "seminole2000",
                "SEMINOLE2000",
                "Curated Draft",
                1,
                null,
                [
                    new GeometryPathDto(wallPathId, false, [new GeometrySegmentDto(wallPathId, 1, 0m, 0m, 120m, 0m)]),
                    new GeometryPathDto(curatedPathId, false, [new GeometrySegmentDto(curatedPathId, 1, 40m, 0m, 76m, 0m)])
                ],
                [
                    new RoomLabelDto(Guid.NewGuid(), "TEXT:ROOM:1", "ROOM LBLS", "KITCHEN", 125m, 784m, 0.95m, null, 1)
                ],
                [],
                [],
                [
                    new FixedPlanComponentDto(
                        Guid.NewGuid(),
                        "INSERT:TUB:1",
                        "FIXTURES",
                        "Tub",
                        "INSERT",
                        "TUB1",
                        [curatedPathId],
                        0.95m,
                        null,
                        1)
                ],
                [],
                [
                    new WallCandidateDto(Guid.NewGuid(), "LINE:WALL:1", "WALLS", "Accepted", 0.95m, null, null, wallPathId, 1)
                ],
                [],
                [])));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);

        Assert.True(viewModel.IsCuratedObjectsQueueExpanded);
        Assert.False(viewModel.IsRoomNamesQueueExpanded);

        viewModel.SelectedReviewQueueFilter = "Text & Notes";
        viewModel.ReviewQueueSearchText = "kitch";

        Assert.False(viewModel.IsCuratedObjectsQueueExpanded);
        Assert.True(viewModel.IsRoomNamesQueueExpanded);
        Assert.Empty(viewModel.CuratedArtifactGroups);
        Assert.Single(viewModel.VisibleRoomLabels);
    }

    [Fact]
    public async Task Inspector_tool_selection_falls_back_to_overview_when_the_active_tool_is_no_longer_valid()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "santa-barbara", "SANTA-BARBARA", isActive: true);
        template.SetCurrentVersion(versionId);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 11, 21, 20, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(
            new FloorPlanReviewSessionDto(
                templateId,
                "santa-barbara",
                "SANTA-BARBARA",
                "Curated Draft",
                1,
                null,
                [],
                [
                    new RoomLabelDto(Guid.NewGuid(), "TEXT:1", "ROOM LBLS", "KITCHEN", 125m, 784m, 0.95m, null, 1)
                ],
                [],
                [],
                [],
                [],
                [],
                [],
                [])));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.SelectedRoomLabel = viewModel.RoomLabels.Single();
        viewModel.SelectInspectorTool("Text");

        Assert.True(viewModel.IsTextToolSelected);

        viewModel.SelectedRoomLabel = null;

        Assert.True(viewModel.IsOverviewToolSelected);
        Assert.False(viewModel.CanUseTextTool);
    }

    [Fact]
    public async Task Selecting_a_dimension_does_not_enable_the_text_tool()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "santa-barbara", "SANTA-BARBARA", isActive: true);
        template.SetCurrentVersion(versionId);
        var dimensionId = Guid.NewGuid();

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 13, 20, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(
            new FloorPlanReviewSessionDto(
                templateId,
                "santa-barbara",
                "SANTA-BARBARA",
                "Curated Draft",
                1,
                null,
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                [])
            {
                Dimensions =
                [
                    new DimensionDto(
                        dimensionId,
                        "DIMENSION:1",
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
                ]
            }));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.SelectDimension(dimensionId);

        Assert.NotNull(viewModel.SelectedDimension);
        Assert.False(viewModel.CanUseTextTool);
    }

    [Fact]
    public async Task SelectPreviewPath_selects_the_matching_candidate_line()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var firstPathId = Guid.NewGuid();
        var secondPathId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 3, 18, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(
            new FloorPlanReviewSessionDto(
                templateId,
                "seminole2000",
                "SEMINOLE2000",
                "Curated Draft",
                1,
                null,
                [
                    new GeometryPathDto(firstPathId, false, [new GeometrySegmentDto(firstPathId, 1, 0m, 0m, 120m, 0m)]),
                    new GeometryPathDto(secondPathId, false, [new GeometrySegmentDto(secondPathId, 1, 0m, 10m, 120m, 10m)])
                ],
                [],
                [],
                [],
                [],
                [],
                [
                    new WallCandidateDto(Guid.NewGuid(), "LINE:1", "WALLS", "Accepted", 0.95m, null, null, firstPathId, 1),
                    new WallCandidateDto(Guid.NewGuid(), "LINE:2", "WALLS", "Accepted", 0.90m, null, null, secondPathId, 2)
                ],
                [],
                [])));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);

        var selected = viewModel.SelectPreviewPath(secondPathId);

        Assert.True(selected);
        Assert.Equal("LINE:2", viewModel.SelectedCandidate?.SourceEntityRef);
        Assert.Equal(secondPathId, viewModel.HighlightGeometryPathId);
        Assert.Equal("Previewing candidate: LINE:2", viewModel.PreviewSelectionLabel);
    }

    [Fact]
    public async Task SelectPreviewPath_selects_the_matching_opening_geometry()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var wallPathId = Guid.NewGuid();
        var openingPathId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 7, 19, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(
            new FloorPlanReviewSessionDto(
                templateId,
                "seminole2000",
                "SEMINOLE2000",
                "Curated Draft",
                1,
                null,
                [
                    new GeometryPathDto(wallPathId, false, [new GeometrySegmentDto(wallPathId, 1, 0m, 0m, 120m, 0m)]),
                    new GeometryPathDto(openingPathId, false, [new GeometrySegmentDto(openingPathId, 1, 40m, 0m, 76m, 0m)])
                ],
                [],
                [
                    new OpeningCandidateDto(Guid.NewGuid(), "LINE:DOOR:1", "DOORS", "Door", "LINE", openingPathId, 0.95m, null, 1)
                ],
                [],
                [],
                [],
                [
                    new WallCandidateDto(Guid.NewGuid(), "LINE:WALL:1", "WALLS", "Accepted", 0.95m, null, null, wallPathId, 1)
                ],
                [],
                [])));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);

        var selected = viewModel.SelectPreviewPath(openingPathId);

        Assert.True(selected);
        Assert.Null(viewModel.SelectedCandidate);
        Assert.Equal("LINE:DOOR:1", viewModel.SelectedCuratedArtifact?.SourceEntityRef);
        Assert.Equal(FloorPlanArtifactTaxonomy.OpeningFamily, viewModel.SelectedCuratedArtifact?.ResolvedFamily);
        Assert.Equal(FloorPlanArtifactTaxonomy.DoorType, viewModel.SelectedCuratedArtifact?.ResolvedType);
        Assert.Equal(openingPathId, viewModel.HighlightGeometryPathId);
        Assert.Equal("Previewing curated object: LINE:DOOR:1", viewModel.PreviewSelectionLabel);
        Assert.Contains("Exclude from Curation", viewModel.InteractionHint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SelectPreviewPath_selects_the_matching_fixed_plan_component_geometry()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var wallPathId = Guid.NewGuid();
        var toiletPathId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 7, 20, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(
            new FloorPlanReviewSessionDto(
                templateId,
                "seminole2000",
                "SEMINOLE2000",
                "Curated Draft",
                1,
                null,
                [
                    new GeometryPathDto(wallPathId, false, [new GeometrySegmentDto(wallPathId, 1, 0m, 0m, 120m, 0m)]),
                    new GeometryPathDto(toiletPathId, false, [new GeometrySegmentDto(toiletPathId, 1, 40m, 10m, 76m, 10m)])
                ],
                [],
                [],
                [],
                [
                    new FixedPlanComponentDto(
                        Guid.NewGuid(),
                        "INSERT:1",
                        "FIXTURES",
                        "Toilet",
                        "INSERT",
                        "TOILET1",
                        [toiletPathId],
                        0.95m,
                        null,
                        1)
                ],
                [],
                [
                    new WallCandidateDto(Guid.NewGuid(), "LINE:WALL:1", "WALLS", "Accepted", 0.95m, null, null, wallPathId, 1)
                ],
                [],
                [])));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);

        var selected = viewModel.SelectPreviewPath(toiletPathId);

        Assert.True(selected);
        Assert.Null(viewModel.SelectedCandidate);
        Assert.Equal("INSERT:1", viewModel.SelectedCuratedArtifact?.SourceEntityRef);
        Assert.Equal(FloorPlanArtifactTaxonomy.FixedFamily, viewModel.SelectedCuratedArtifact?.ResolvedFamily);
        Assert.Equal(FloorPlanArtifactTaxonomy.ToiletType, viewModel.SelectedCuratedArtifact?.ResolvedType);
        Assert.Equal(toiletPathId, viewModel.HighlightGeometryPathId);
        Assert.Equal("Previewing curated object: INSERT:1", viewModel.PreviewSelectionLabel);
        Assert.Contains("Exclude from Curation", viewModel.InteractionHint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SelectPreviewPath_selects_the_matching_protected_detail_assembly_geometry()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var wallPathId = Guid.NewGuid();
        var protectedPathId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 7, 21, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(
            new FloorPlanReviewSessionDto(
                templateId,
                "seminole2000",
                "SEMINOLE2000",
                "Curated Draft",
                1,
                null,
                [
                    new GeometryPathDto(wallPathId, false, [new GeometrySegmentDto(wallPathId, 1, 0m, 0m, 120m, 0m)]),
                    new GeometryPathDto(protectedPathId, false, [new GeometrySegmentDto(protectedPathId, 1, 40m, 10m, 76m, 10m)])
                ],
                [],
                [],
                [],
                [],
                [
                    new ProtectedDetailAssemblyDto(
                        Guid.NewGuid(),
                        "DETAIL:MISC:1",
                        "MISC",
                        "WetAreaDetail",
                        "DETAIL-GROUP",
                        [protectedPathId],
                        0.90m,
                        null,
                        1)
                ],
                [
                    new WallCandidateDto(Guid.NewGuid(), "LINE:WALL:1", "WALLS", "Accepted", 0.95m, null, null, wallPathId, 1)
                ],
                [],
                [])));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);

        var selected = viewModel.SelectPreviewPath(protectedPathId);

        Assert.True(selected);
        Assert.Null(viewModel.SelectedCandidate);
        Assert.Equal("DETAIL:MISC:1", viewModel.SelectedCuratedArtifact?.SourceEntityRef);
        Assert.Equal(FloorPlanArtifactTaxonomy.ProtectedFamily, viewModel.SelectedCuratedArtifact?.ResolvedFamily);
        Assert.Equal(FloorPlanArtifactTaxonomy.UnknownWetAssemblyType, viewModel.SelectedCuratedArtifact?.ResolvedType);
        Assert.Equal(protectedPathId, viewModel.HighlightGeometryPathId);
        Assert.Equal("Previewing curated object: DETAIL:MISC:1", viewModel.PreviewSelectionLabel);
        Assert.Contains("Exclude from Curation", viewModel.InteractionHint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Selecting_room_label_populates_selected_item_summary()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "santa-barbara", "SANTA-BARBARA", isActive: true);
        template.SetCurrentVersion(versionId);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 10, 18, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(
            new FloorPlanReviewSessionDto(
                templateId,
                "santa-barbara",
                "SANTA-BARBARA",
                "Curated Draft",
                1,
                null,
                [],
                [
                    new RoomLabelDto(Guid.NewGuid(), "TEXT:1", "ROOM LBLS", "KITCHEN", 125m, 784m, 0.95m, null, 1)
                ],
                [],
                [],
                [],
                [],
                [],
                [],
                [])));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.SelectedRoomLabel = viewModel.RoomLabels.Single();

        Assert.True(viewModel.HasSelectedArtifact);
        Assert.Equal("Room Name", viewModel.SelectedArtifactTypeLabel);
        Assert.Equal("KITCHEN", viewModel.SelectedArtifactTitle);
        Assert.Contains("ROOM LBLS", viewModel.SelectedArtifactSubtitle, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Exclude from Curation", viewModel.ExcludeSelectedArtifactLabel);
        Assert.Equal("Previewing room label: KITCHEN", viewModel.PreviewSelectionLabel);
    }

    [Fact]
    public async Task ExcludeSelectedArtifactAsync_removes_selected_room_label()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var roomLabel = new ExtractedRoomLabel(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "TEXT:1",
            "ROOM LBLS",
            "KITCHEN",
            125m,
            784m,
            0.95m,
            null,
            1);
        var template = new FloorPlanTemplate(templateId, "santa-barbara", "SANTA-BARBARA", isActive: true);
        template.SetCurrentVersion(versionId);
        var roomLabelRepository = new InMemoryExtractedRoomLabelRepository(roomLabel);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IExtractedRoomLabelRepository>(roomLabelRepository);
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 10, 18, 30, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(
            new FloorPlanReviewSessionDto(
                templateId,
                "santa-barbara",
                "SANTA-BARBARA",
                "Curated Draft",
                1,
                null,
                [],
                [
                    new RoomLabelDto(roomLabel.Id, "TEXT:1", "ROOM LBLS", "KITCHEN", 125m, 784m, 0.95m, null, 1)
                ],
                [],
                [],
                [],
                [],
                [],
                [],
                [])));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();
        services.AddTransient<RemoveRoomLabelHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.SelectedRoomLabel = viewModel.RoomLabels.Single();

        await viewModel.ExcludeSelectedArtifactAsync(CancellationToken.None);

        Assert.False(roomLabelRepository.Items.Any());
    }

    [Fact]
    public async Task RemoveSelectedOpeningLabelAsync_removes_selected_opening_label()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var openingLabel = new ExtractedOpeningLabel(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "TEXT:9",
            "DOORTEXT",
            "Door",
            "24\"DR.",
            100m,
            200m,
            0.95m,
            null,
            1);
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var openingLabelRepository = new InMemoryExtractedOpeningLabelRepository(openingLabel);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IExtractedOpeningLabelRepository>(openingLabelRepository);
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 10, 18, 45, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(
            new FloorPlanReviewSessionDto(
                templateId,
                "seminole2000",
                "SEMINOLE2000",
                "Curated Draft",
                1,
                null,
                [],
                [],
                [],
                [
                    new OpeningLabelDto(openingLabel.Id, "TEXT:9", "DOORTEXT", "Door", "24\"DR.", 100m, 200m, 0.95m, null, 1)
                ],
                [],
                [],
                [],
                [],
                [])));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();
        services.AddTransient<RemoveOpeningLabelHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.SelectedOpeningLabel = viewModel.OpeningLabels.Single();

        await viewModel.RemoveSelectedOpeningLabelAsync(CancellationToken.None);

        Assert.False(openingLabelRepository.Items.Any());
        Assert.Null(viewModel.SelectedOpeningLabel);
    }

    [Fact]
    public async Task RemoveSelectedPinchAsync_removes_selected_pinch_marker()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var geometryPathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var pinchMarker = new PinchMarker(
            Guid.NewGuid(),
            Guid.NewGuid(),
            pinchGroupId,
            candidateId,
            geometryPathId,
            0.55m,
            120m,
            1);
        var template = new FloorPlanTemplate(templateId, "santa-barbara", "SANTA-BARBARA", isActive: true);
        template.SetCurrentVersion(versionId);
        var pinchMarkerRepository = new InMemoryPinchMarkerRepository([pinchMarker]);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IPinchMarkerRepository>(pinchMarkerRepository);
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 10, 19, 10, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(
            new FloorPlanReviewSessionDto(
                templateId,
                "santa-barbara",
                "SANTA-BARBARA",
                "Curated Draft",
                1,
                null,
                [
                    new GeometryPathDto(geometryPathId, false, [new GeometrySegmentDto(geometryPathId, 1, 0m, 0m, 120m, 0m)])
                ],
                [],
                [],
                [],
                [],
                [],
                [
                    new WallCandidateDto(candidateId, "LINE:68", "WALLS", "Accepted", 0.95m, 101.6m, null, geometryPathId, 1)
                ],
                [
                    new PinchGroupDto(pinchGroupId, "Patio", nameof(PinchAxisTag.Width), 1)
                ],
                [
                    new PinchMarkerDto(pinchMarker.Id, pinchGroupId, "Patio", candidateId, geometryPathId, nameof(PinchAxisTag.Width), 0.55m, 120m, 1)
                ])));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();
        services.AddTransient<RemovePinchMarkerHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.SelectedPinchMarker = viewModel.PinchMarkers.Single();

        await viewModel.RemoveSelectedPinchAsync(CancellationToken.None);

        Assert.False(pinchMarkerRepository.Items.Any());
    }

    [Fact]
    public async Task Selected_pinch_max_trim_edit_adjusts_by_half_inch_without_persisting_or_changing_marker_identity()
    {
        using var fixture = await CreateSelectedPinchEditFixtureAsync(152.4m);

        fixture.ViewModel.BeginEditSelectedPinchMaxTrim();

        Assert.Equal("6\"", fixture.ViewModel.EditableSelectedPinchMaxTrim);

        fixture.ViewModel.AdjustEditableSelectedPinchMaxTrim(-0.5m);

        Assert.Equal("5 1/2\"", fixture.ViewModel.EditableSelectedPinchMaxTrim);

        fixture.ViewModel.AdjustEditableSelectedPinchMaxTrim(0.5m);

        Assert.Equal("6\"", fixture.ViewModel.EditableSelectedPinchMaxTrim);
        Assert.Equal(152.4m, Assert.Single(fixture.MarkerRepository.Items).MaxTrimMm);
        Assert.Equal(0, fixture.MarkerRepository.UpdateCallCount);
        Assert.Equal(fixture.PinchMarkerId, fixture.ViewModel.SelectedPinchMarker?.PinchMarkerId);
    }

    [Fact]
    public async Task Selected_pinch_max_trim_edit_rejects_non_positive_half_inch_decrement_without_changing_text()
    {
        using var fixture = await CreateSelectedPinchEditFixtureAsync(12.7m);

        fixture.ViewModel.BeginEditSelectedPinchMaxTrim();
        fixture.ViewModel.AdjustEditableSelectedPinchMaxTrim(-0.5m);

        Assert.Equal("1/2\"", fixture.ViewModel.EditableSelectedPinchMaxTrim);
        Assert.Equal("Escribí una capacidad positiva válida para ajustar.", fixture.ViewModel.StatusMessage);

        fixture.ViewModel.EditableSelectedPinchMaxTrim = "invalid";
        fixture.ViewModel.AdjustEditableSelectedPinchMaxTrim(0.5m);

        Assert.Equal("invalid", fixture.ViewModel.EditableSelectedPinchMaxTrim);
        Assert.Equal("Escribí una capacidad positiva válida para ajustar.", fixture.ViewModel.StatusMessage);
    }

    [Fact]
    public async Task Successful_pinch_max_trim_adjustment_clears_previous_error_and_requires_guardar()
    {
        using var fixture = await CreateSelectedPinchEditFixtureAsync(12.7m);

        fixture.ViewModel.BeginEditSelectedPinchMaxTrim();
        fixture.ViewModel.AdjustEditableSelectedPinchMaxTrim(-0.5m);

        Assert.Equal("Escribí una capacidad positiva válida para ajustar.", fixture.ViewModel.StatusMessage);

        fixture.ViewModel.AdjustEditableSelectedPinchMaxTrim(0.5m);

        Assert.Equal("1\"", fixture.ViewModel.EditableSelectedPinchMaxTrim);
        Assert.Equal("Valor preparado; todavía falta Guardar.", fixture.ViewModel.StatusMessage);
    }

    [Fact]
    public async Task Adjusting_selected_pinch_max_trim_while_not_editing_leaves_text_unchanged()
    {
        using var fixture = await CreateSelectedPinchEditFixtureAsync(152.4m);

        fixture.ViewModel.EditableSelectedPinchMaxTrim = "6\"";
        fixture.ViewModel.AdjustEditableSelectedPinchMaxTrim(-0.5m);

        Assert.False(fixture.ViewModel.IsEditingSelectedPinchMaxTrim);
        Assert.Equal("6\"", fixture.ViewModel.EditableSelectedPinchMaxTrim);
    }

    [Fact]
    public async Task Selecting_another_pinch_updates_exact_capacity_and_abandons_the_previous_draft_without_collection_churn()
    {
        using var fixture = await CreateSelectedPinchEditFixtureAsync(152.4m, 254m);
        var propertyChanges = new List<string?>();
        fixture.ViewModel.PropertyChanged += (_, args) => propertyChanges.Add(args.PropertyName);

        fixture.ViewModel.BeginEditSelectedPinchMaxTrim();
        fixture.ViewModel.EditableSelectedPinchMaxTrim = "7\"";

        Assert.True(fixture.ViewModel.SelectPinchMarker(fixture.SecondPinchMarkerId));

        Assert.Equal(fixture.SecondPinchMarkerId, fixture.ViewModel.SelectedPinchMarkerId);
        Assert.Equal("10\"", fixture.ViewModel.SelectedPinchMaxTrimDisplay);
        Assert.False(fixture.ViewModel.IsEditingSelectedPinchMaxTrim);
        Assert.Empty(fixture.ViewModel.EditableSelectedPinchMaxTrim);
        Assert.Equal(0, fixture.MarkerRepository.UpdateCallCount);
        Assert.Equal(152.4m, fixture.MarkerRepository.Items.Single(item => item.Id == fixture.PinchMarkerId).MaxTrimMm);
        Assert.Equal(254m, fixture.MarkerRepository.Items.Single(item => item.Id == fixture.SecondPinchMarkerId).MaxTrimMm);
        Assert.Contains(nameof(FloorPlanReviewViewModel.SelectedPinchMarkerId), propertyChanges);
        Assert.DoesNotContain(nameof(FloorPlanReviewViewModel.SelectedPinchGroupMarkers), propertyChanges);
    }

    [Fact]
    public async Task Save_selected_pinch_max_trim_fails_safe_when_the_editing_identity_no_longer_matches_selection()
    {
        using var fixture = await CreateSelectedPinchEditFixtureAsync(152.4m, 254m);

        fixture.ViewModel.BeginEditSelectedPinchMaxTrim();
        fixture.ViewModel.EditableSelectedPinchMaxTrim = "7\"";
        fixture.ViewModel.SelectedPinchMarker = fixture.ViewModel.PinchMarkers.Single(
            marker => marker.PinchMarkerId == fixture.SecondPinchMarkerId);

        // Simulate a stale UI attempting to submit after selection already cancelled the original edit.
        fixture.ViewModel.IsEditingSelectedPinchMaxTrim = true;
        fixture.ViewModel.EditableSelectedPinchMaxTrim = "12\"";
        await fixture.ViewModel.SaveSelectedPinchMaxTrimAsync(CancellationToken.None);

        Assert.False(fixture.ViewModel.IsEditingSelectedPinchMaxTrim);
        Assert.Equal(0, fixture.MarkerRepository.UpdateCallCount);
        Assert.Equal(152.4m, fixture.MarkerRepository.Items.Single(item => item.Id == fixture.PinchMarkerId).MaxTrimMm);
        Assert.Equal(254m, fixture.MarkerRepository.Items.Single(item => item.Id == fixture.SecondPinchMarkerId).MaxTrimMm);
    }

    [Fact]
    public async Task Save_selected_pinch_max_trim_refreshes_and_restores_the_exact_marker_id()
    {
        using var fixture = await CreateSelectedPinchEditFixtureAsync(152.4m, 254m);
        fixture.ViewModel.SelectedPinchMarker = fixture.ViewModel.PinchMarkers.Single(
            marker => marker.PinchMarkerId == fixture.SecondPinchMarkerId);

        fixture.ViewModel.BeginEditSelectedPinchMaxTrim();
        fixture.ViewModel.EditableSelectedPinchMaxTrim = "11\"";
        await fixture.ViewModel.SaveSelectedPinchMaxTrimAsync(CancellationToken.None);

        Assert.Equal(1, fixture.MarkerRepository.UpdateCallCount);
        Assert.Equal(152.4m, fixture.MarkerRepository.Items.Single(item => item.Id == fixture.PinchMarkerId).MaxTrimMm);
        Assert.Equal(279.4m, fixture.MarkerRepository.Items.Single(item => item.Id == fixture.SecondPinchMarkerId).MaxTrimMm);
        Assert.Equal(fixture.SecondPinchMarkerId, fixture.ViewModel.SelectedPinchMarkerId);
        Assert.Equal(279.4m, fixture.ViewModel.SelectedPinchMarker?.MaxTrimMm);
        Assert.Equal("11\"", fixture.ViewModel.SelectedPinchMaxTrimDisplay);
    }

    [Fact]
    public async Task Selected_pinch_max_trim_edit_preserves_noop_exact_value_and_refreshes_saved_marker_and_band()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var geometryPathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var pinchMarkerId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "santa-barbara", "SANTA-BARBARA", isActive: true);
        template.SetCurrentVersion(versionId);
        var curationRepository = new InMemoryFloorPlanCurationRepository();
        var markerRepository = new InMemoryPinchMarkerRepository([]);

        FloorPlanReviewSessionDto CreateSession()
        {
            var maxTrimMm = markerRepository.Items.SingleOrDefault()?.MaxTrimMm ?? 120m;
            return new FloorPlanReviewSessionDto(
                templateId,
                "santa-barbara",
                "SANTA-BARBARA",
                "Curated Draft",
                1,
                null,
                [
                    new GeometryPathDto(geometryPathId, false, [new GeometrySegmentDto(geometryPathId, 1, 0m, 0m, 120m, 0m)])
                ],
                [],
                [],
                [],
                [],
                [],
                [
                    new WallCandidateDto(candidateId, "LINE:68", "WALLS", "Accepted", 0.95m, 101.6m, null, geometryPathId, 1)
                ],
                [
                    new PinchGroupDto(pinchGroupId, "Patio", nameof(PinchAxisTag.Width), 1)
                ],
                [
                    new PinchMarkerDto(pinchMarkerId, pinchGroupId, "Patio", candidateId, geometryPathId, nameof(PinchAxisTag.Width), 0.55m, maxTrimMm, 1)
                ])
            {
                ArticulationBands =
                [
                    new ArticulationBandDto(pinchGroupId, "Patio", nameof(PinchAxisTag.Width), 60m, 60m, maxTrimMm, "Suggested")
                ]
            };
        }

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(curationRepository);
        services.AddSingleton<IPinchMarkerRepository>(markerRepository);
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 7, 16, 21, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(CreateSession));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();
        services.AddTransient<UpdatePinchMarkerMaxTrimHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);
        markerRepository.Items.Add(new PinchMarker(
            pinchMarkerId,
            viewModel.DraftCurationId,
            pinchGroupId,
            candidateId,
            geometryPathId,
            0.55m,
            120m,
            1));
        viewModel.SelectedPinchGroup = viewModel.PinchGroups.Single();
        viewModel.SelectedPinchMarker = viewModel.PinchMarkers.Single();

        Assert.Equal("4 185/256\"", viewModel.SelectedPinchMaxTrimDisplay);

        viewModel.BeginEditSelectedPinchMaxTrim();

        Assert.True(viewModel.IsEditingSelectedPinchMaxTrim);
        Assert.Equal("4 185/256\"", viewModel.EditableSelectedPinchMaxTrim);

        viewModel.CancelEditSelectedPinchMaxTrim();

        Assert.False(viewModel.IsEditingSelectedPinchMaxTrim);

        viewModel.BeginEditSelectedPinchMaxTrim();
        viewModel.EditableSelectedPinchMaxTrim += "   ";
        await viewModel.SaveSelectedPinchMaxTrimAsync(CancellationToken.None);

        Assert.Equal(120m, Assert.Single(markerRepository.Items).MaxTrimMm);
        Assert.Equal(0, markerRepository.UpdateCallCount);
        Assert.Equal(pinchMarkerId, viewModel.SelectedPinchMarker?.PinchMarkerId);

        viewModel.BeginEditSelectedPinchMaxTrim();
        viewModel.EditableSelectedPinchMaxTrim = "1/256\"";
        await viewModel.SaveSelectedPinchMaxTrimAsync(CancellationToken.None);

        Assert.False(viewModel.IsEditingSelectedPinchMaxTrim);
        Assert.Equal(1, markerRepository.UpdateCallCount);
        Assert.Equal(pinchMarkerId, Assert.Single(markerRepository.Items).Id);
        Assert.Equal(0.099218750m, markerRepository.Items.Single().MaxTrimMm);
        Assert.Equal(pinchMarkerId, Assert.Single(viewModel.PinchMarkers).PinchMarkerId);
        Assert.Equal(0.099218750m, viewModel.PinchMarkers.Single().MaxTrimMm);
        Assert.Equal(pinchMarkerId, viewModel.SelectedPinchMarker?.PinchMarkerId);
        Assert.Equal(0.099218750m, Assert.Single(viewModel.ArticulationBands).MaxTrimMm);
        Assert.Equal("1/256\"", viewModel.SelectedPinchMaxTrimDisplay);

        viewModel.BeginEditSelectedPinchMaxTrim();
        viewModel.EditableSelectedPinchMaxTrim = "0.00390625";
        await viewModel.SaveSelectedPinchMaxTrimAsync(CancellationToken.None);

        Assert.Equal(1, markerRepository.UpdateCallCount);
        Assert.Equal(0.099218750m, Assert.Single(markerRepository.Items).MaxTrimMm);
    }

    [Theory]
    [InlineData("0.00390625")]
    [InlineData("1/256\"")]
    public async Task HandlePreviewInteractionAsync_saves_decimal_and_architectural_pinch_limits_without_rounding_drift(
        string maxTrimText)
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var geometryPathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var candidate = new ExtractedWallCandidate(
            candidateId,
            Guid.NewGuid(),
            "LINE:68",
            "WALLS",
            geometryPathId,
            120m,
            0.95m,
            null,
            ExtractedWallCandidateStatus.Accepted,
            1);
        var template = new FloorPlanTemplate(templateId, "santa-barbara", "SANTA-BARBARA", isActive: true);
        template.SetCurrentVersion(versionId);
        var curationRepository = new InMemoryFloorPlanCurationRepository();
        var groupRepository = new InMemoryPinchGroupRepository([]);
        var markerRepository = new InMemoryPinchMarkerRepository([]);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(curationRepository);
        services.AddSingleton<IExtractedWallCandidateRepository>(new InMemoryExtractedWallCandidateRepository(candidate));
        services.AddSingleton<IPinchGroupRepository>(groupRepository);
        services.AddSingleton<IPinchMarkerRepository>(markerRepository);
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 6, 6, 21, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(
            new FloorPlanReviewSessionDto(
                templateId,
                "santa-barbara",
                "SANTA-BARBARA",
                "Curated Draft",
                1,
                null,
                [
                    new GeometryPathDto(geometryPathId, false, [new GeometrySegmentDto(geometryPathId, 1, 0m, 0m, 120m, 0m)])
                ],
                [],
                [],
                [],
                [],
                [],
                [
                    new WallCandidateDto(candidateId, "LINE:68", "WALLS", "Accepted", 0.95m, 120m, null, geometryPathId, 1)
                ],
                [
                    new PinchGroupDto(pinchGroupId, "Patio", nameof(PinchAxisTag.Width), 1)
                ],
                [])));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();
        services.AddTransient<AddPinchMarkerHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);
        groupRepository.Items.Add(new PinchGroup(pinchGroupId, viewModel.DraftCurationId, "Patio", PinchAxisTag.Width, 1));
        viewModel.SelectedPinchGroup = viewModel.PinchGroups.Single();

        viewModel.NewPinchMaxTrimInches = maxTrimText;
        Assert.True(viewModel.SelectPreviewPath(geometryPathId));

        viewModel.TogglePinchPlacement();
        await viewModel.HandlePreviewInteractionAsync(geometryPathId, 0.5m, CancellationToken.None);

        var marker = Assert.Single(markerRepository.Items);
        Assert.Equal(0.099218750m, marker.MaxTrimMm);
    }

    [Fact]
    public async Task PublishAsync_without_pinch_markers_keeps_button_enabled_and_shows_validation_message()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var geometryPathId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "santa-barbara", "SANTA-BARBARA", isActive: true);
        template.SetCurrentVersion(versionId);
        var markerRepository = new InMemoryPinchMarkerRepository([]);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IPinchMarkerRepository>(markerRepository);
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 6, 4, 22, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(
            new FloorPlanReviewSessionDto(
                templateId,
                "santa-barbara",
                "SANTA-BARBARA",
                "Curated Draft",
                1,
                null,
                [
                    new GeometryPathDto(geometryPathId, false, [new GeometrySegmentDto(geometryPathId, 1, 0m, 0m, 120m, 0m)])
                ],
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                [])));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();
        services.AddTransient<PublishFloorPlanCurationHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);

        var exception = await Record.ExceptionAsync(() => viewModel.PublishAsync(CancellationToken.None));

        Assert.Null(exception);
        Assert.True(viewModel.CanPublishCuration);
        Assert.Equal("Agregá al menos un pinche antes de publicar.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task PublishAsync_uses_persisted_pinch_markers_even_when_the_session_projection_is_stale()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var geometryPathId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "santa-barbara", "SANTA-BARBARA", isActive: true);
        template.SetCurrentVersion(versionId);
        var markerRepository = new InMemoryPinchMarkerRepository([]);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IPinchMarkerRepository>(markerRepository);
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 6, 4, 22, 10, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(
            new FloorPlanReviewSessionDto(
                templateId,
                "santa-barbara",
                "SANTA-BARBARA",
                "Curated Draft",
                1,
                null,
                [
                    new GeometryPathDto(geometryPathId, false, [new GeometrySegmentDto(geometryPathId, 1, 0m, 0m, 120m, 0m)])
                ],
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                [])));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();
        services.AddTransient<PublishFloorPlanCurationHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);
        markerRepository.Items.Add(new PinchMarker(
            Guid.NewGuid(),
            viewModel.DraftCurationId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            geometryPathId,
            0.5m,
            120m,
            1));

        var exception = await Record.ExceptionAsync(() => viewModel.PublishAsync(CancellationToken.None));

        Assert.Null(exception);
        Assert.Empty(viewModel.PinchMarkers);
        Assert.True(viewModel.CanPublishCuration);
        Assert.Equal(viewModel.DraftCurationId, template.ActivePublishedCurationId);
    }

    [Fact]
    public async Task PublishAsync_commissions_and_persists_the_exact_published_curation_identity()
    {
        using var fixture = CreateCommissioningPublishFixture(includeDepth: true);
        await fixture.ViewModel.LoadAsync(CancellationToken.None);
        var publishedCurationId = fixture.ViewModel.DraftCurationId;
        fixture.SeedPersistedMarkers(publishedCurationId);

        await fixture.ViewModel.PublishAsync(CancellationToken.None);

        // CommissionPublishedCurationAsync computes the exact guard it refused on and reports
        // it through StatusMessage. Assert that before asserting persistence, otherwise a
        // refusal only ever surfaces as "the collection was empty" and the reason production
        // already knows is thrown away.
        Assert.False(
            // Matched without the accented character on purpose: the needle must not depend on
            // how this file is encoded, or a mangled literal would make the assertion vacuous.
            fixture.ViewModel.StatusMessage.Contains("Publicado, pero no qued", StringComparison.Ordinal),
            $"Commissioning refused to persist a profile: {fixture.ViewModel.StatusMessage}");
        var profile = Assert.Single(fixture.ProfileRepository.Items);
        Assert.Equal(fixture.FloorPlanVersionId, profile.FloorPlanVersionId);
        Assert.Equal(publishedCurationId, profile.PublishedCurationId);
        Assert.Contains(profile.Variables, variable => variable.Axis == HouseAdaptationAxis.Width);
        Assert.Contains(profile.Variables, variable => variable.Axis == HouseAdaptationAxis.Depth);
        Assert.Contains(profile.AuxiliaryEntityBindings, binding => binding.SourceEntityRef == "LABEL:COVERAGE");
        var openingBinding = Assert.Single(
            profile.AuxiliaryEntityBindings,
            binding => binding.SourceEntityRef == "OPENING:COVERAGE");
        Assert.Equal(fixture.OpeningGeometryPathId, openingBinding.GeometryPathId);
        Assert.Equal(fixture.OpeningHostWallPathId, openingBinding.HostGeometryPathId);
        Assert.Equal(0, openingBinding.HostSegmentSortOrder);
        Assert.NotEqual(openingBinding.GeometryPathId, openingBinding.HostGeometryPathId);
        var openingRoles = profile.Variables
            .SelectMany(variable => variable.ActionTemplates)
            .SelectMany(action => action.CanonicalEntityRoles)
            .Where(role => role.EntityRef == "OPENING:COVERAGE")
            .ToArray();
        Assert.NotEmpty(openingRoles);
        Assert.All(openingRoles, role =>
        {
            Assert.NotEqual("Stretch", role.Role);
            Assert.Equal(fixture.OpeningGeometryPathId, role.GeometryPathId);
            Assert.Equal(fixture.OpeningHostWallPathId, role.HostGeometryPathId);
            Assert.Equal(0, role.HostSegmentSortOrder);
            Assert.Empty(role.VertexIndices);
        });
        var dimensionBinding = Assert.Single(
            profile.AuxiliaryEntityBindings,
            binding => binding.SourceEntityRef == "DIMENSION:COVERAGE");
        Assert.Equal(CommissionExistingCurationAuxiliaryEntityKind.Label, dimensionBinding.Kind);
        Assert.Null(dimensionBinding.GeometryPathId);
        Assert.Null(dimensionBinding.SegmentSortOrder);
        Assert.Equal(new AdjustmentRecipeBoundsDto(1m, 1m, 2m, 2.125m), dimensionBinding.SourceBounds);
        Assert.Contains(
            profile.AuxiliaryEntityBindings,
            binding => binding.SourceEntityRef == "PROTECTED:COVERAGE" && binding.GeometryPathId.HasValue);
        Assert.Contains("Auto-fit ready", fixture.ViewModel.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublishAsync_exposes_one_actionable_reason_when_commissioning_cannot_close()
    {
        using var fixture = CreateCommissioningPublishFixture(includeDepth: false);
        await fixture.ViewModel.LoadAsync(CancellationToken.None);
        fixture.SeedPersistedMarkers(fixture.ViewModel.DraftCurationId);

        await fixture.ViewModel.PublishAsync(CancellationToken.None);

        Assert.Empty(fixture.ProfileRepository.Items);
        Assert.Contains("Publicado, pero no qued\u00F3 Auto-fit ready", fixture.ViewModel.StatusMessage, StringComparison.Ordinal);
        Assert.Contains("Height", fixture.ViewModel.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublishAsync_leaves_setup_required_for_an_unsupported_dimension_identity()
    {
        using var fixture = CreateCommissioningPublishFixture(
            includeDepth: true,
            dimensionSourceKind: "ARC");
        await fixture.ViewModel.LoadAsync(CancellationToken.None);
        fixture.SeedPersistedMarkers(fixture.ViewModel.DraftCurationId);

        await fixture.ViewModel.PublishAsync(CancellationToken.None);

        Assert.Empty(fixture.ProfileRepository.Items);
        Assert.Contains("Publicado, pero no qued\u00F3 Auto-fit ready", fixture.ViewModel.StatusMessage, StringComparison.Ordinal);
        Assert.Contains("DIMENSION:COVERAGE", fixture.ViewModel.StatusMessage, StringComparison.Ordinal);
        Assert.Contains("no soportado", fixture.ViewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(CommissioningOpeningHostScenario.Missing, "0 candidatos")]
    [InlineData(CommissioningOpeningHostScenario.Multiple, "2 candidatos")]
    [InlineData(CommissioningOpeningHostScenario.Crossing, "cruza")]
    [InlineData(CommissioningOpeningHostScenario.SupportedAndCrossing, "además cruza")]
    public async Task PublishAsync_leaves_setup_required_when_opening_host_evidence_is_not_unique_support(
        CommissioningOpeningHostScenario openingHostScenario,
        string expectedReason)
    {
        using var fixture = CreateCommissioningPublishFixture(
            includeDepth: true,
            openingHostScenario: openingHostScenario);
        await fixture.ViewModel.LoadAsync(CancellationToken.None);
        fixture.SeedPersistedMarkers(fixture.ViewModel.DraftCurationId);

        await fixture.ViewModel.PublishAsync(CancellationToken.None);

        Assert.Empty(fixture.ProfileRepository.Items);
        Assert.Contains("Publicado, pero no qued\u00F3 Auto-fit ready", fixture.ViewModel.StatusMessage, StringComparison.Ordinal);
        Assert.Contains("OPENING:COVERAGE", fixture.ViewModel.StatusMessage, StringComparison.Ordinal);
        Assert.Contains(expectedReason, fixture.ViewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RemoveSelectedPinchGroupAsync_removes_selected_group_and_all_its_markers()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var geometryPathId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var otherGroupId = Guid.NewGuid();
        var markerId = Guid.NewGuid();
        var secondMarkerId = Guid.NewGuid();
        var otherMarkerId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "santa-barbara", "SANTA-BARBARA", isActive: true);
        template.SetCurrentVersion(versionId);
        var initialSession = new FloorPlanReviewSessionDto(
            templateId,
            "santa-barbara",
            "SANTA-BARBARA",
            "Curated Draft",
            1,
            null,
            [
                new GeometryPathDto(geometryPathId, false, [new GeometrySegmentDto(geometryPathId, 1, 0m, 0m, 120m, 0m)])
            ],
            [],
            [],
            [],
            [],
            [],
            [
                new WallCandidateDto(candidateId, "LINE:68", "WALLS", "Accepted", 0.95m, 101.6m, null, geometryPathId, 1)
            ],
            [
                new PinchGroupDto(groupId, "Patio", nameof(PinchAxisTag.Width), 1),
                new PinchGroupDto(otherGroupId, "Hall", nameof(PinchAxisTag.Width), 2)
            ],
            [
                new PinchMarkerDto(markerId, groupId, "Patio", candidateId, geometryPathId, nameof(PinchAxisTag.Width), 0.55m, 120m, 1),
                new PinchMarkerDto(secondMarkerId, groupId, "Patio", candidateId, geometryPathId, nameof(PinchAxisTag.Width), 0.65m, 90m, 2),
                new PinchMarkerDto(otherMarkerId, otherGroupId, "Hall", candidateId, geometryPathId, nameof(PinchAxisTag.Width), 0.25m, 80m, 3)
            ]);
        var refreshedSession = initialSession with
        {
            PinchGroups = [new PinchGroupDto(otherGroupId, "Hall", nameof(PinchAxisTag.Width), 2)],
            PinchMarkers = [new PinchMarkerDto(otherMarkerId, otherGroupId, "Hall", candidateId, geometryPathId, nameof(PinchAxisTag.Width), 0.25m, 80m, 3)]
        };
        var curationRepository = new InMemoryFloorPlanCurationRepository();
        var groupRepository = new InMemoryPinchGroupRepository([]);
        var markerRepository = new InMemoryPinchMarkerRepository([]);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(curationRepository);
        services.AddSingleton<IPinchGroupRepository>(groupRepository);
        services.AddSingleton<IPinchMarkerRepository>(markerRepository);
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 10, 19, 10, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new SequencedFloorPlanReviewSessionReader(initialSession, refreshedSession));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();
        services.AddTransient<RemovePinchGroupHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);
        groupRepository.Items.Add(new PinchGroup(groupId, viewModel.DraftCurationId, "Patio", PinchAxisTag.Width, 1));
        groupRepository.Items.Add(new PinchGroup(otherGroupId, viewModel.DraftCurationId, "Hall", PinchAxisTag.Width, 2));
        markerRepository.Items.Add(new PinchMarker(markerId, viewModel.DraftCurationId, groupId, candidateId, geometryPathId, 0.55m, 120m, 1));
        markerRepository.Items.Add(new PinchMarker(secondMarkerId, viewModel.DraftCurationId, groupId, candidateId, geometryPathId, 0.65m, 90m, 2));
        markerRepository.Items.Add(new PinchMarker(otherMarkerId, viewModel.DraftCurationId, otherGroupId, candidateId, geometryPathId, 0.25m, 80m, 3));
        viewModel.SelectedPinchGroup = viewModel.PinchGroups.Single(item => item.PinchGroupId == groupId);

        await viewModel.RemoveSelectedPinchGroupAsync(CancellationToken.None);

        Assert.DoesNotContain(groupRepository.Items, item => item.Id == groupId);
        Assert.Contains(groupRepository.Items, item => item.Id == otherGroupId);
        Assert.DoesNotContain(markerRepository.Items, item => item.PinchGroupId == groupId);
        Assert.Contains(markerRepository.Items, item => item.Id == otherMarkerId);
        Assert.Null(viewModel.SelectedPinchGroup);
        Assert.Null(viewModel.SelectedPinchMarker);
        Assert.Equal([otherGroupId], viewModel.PinchGroups.Select(item => item.PinchGroupId).ToArray());
        Assert.Equal([otherMarkerId], viewModel.PinchMarkers.Select(item => item.PinchMarkerId).ToArray());
        Assert.False(viewModel.CanRemoveSelectedPinchGroup);
    }

    [Fact]
    public async Task AddPinchGroupAsync_uses_the_name_entered_by_the_operator()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var geometryPathId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var groupRepository = new InMemoryPinchGroupRepository([]);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IPinchGroupRepository>(groupRepository);
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 6, 10, 22, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new RepositoryBackedFloorPlanReviewSessionReader(
            templateId,
            "seminole2000",
            "SEMINOLE2000",
            geometryPathId,
            groupRepository));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();
        services.AddTransient<AddPinchGroupHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.SelectedPinchAxis = nameof(PinchAxisTag.Height);

        await viewModel.AddPinchGroupAsync("  Patio trasero  ", CancellationToken.None);

        var group = Assert.Single(groupRepository.Items);
        Assert.Equal("Patio trasero", group.Name);
        Assert.Equal(PinchAxisTag.Height, group.AxisTag);
        Assert.Equal(group.Id, viewModel.SelectedPinchGroupId);
        Assert.Equal("Patio trasero", viewModel.SelectedPinchGroup?.Name);
    }

    [Fact]
    public async Task RenameSelectedPinchGroupAsync_updates_only_the_selected_group_name()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var geometryPathId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var otherGroupId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var groupRepository = new InMemoryPinchGroupRepository(
        [
            new PinchGroup(groupId, Guid.Empty, "Ajuste 1", PinchAxisTag.Width, 1),
            new PinchGroup(otherGroupId, Guid.Empty, "Porche", PinchAxisTag.Height, 2)
        ]);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IPinchGroupRepository>(groupRepository);
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 6, 10, 22, 5, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new RepositoryBackedFloorPlanReviewSessionReader(
            templateId,
            "seminole2000",
            "SEMINOLE2000",
            geometryPathId,
            groupRepository));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();
        services.AddTransient<RenamePinchGroupHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);
        groupRepository.Items[0] = new PinchGroup(groupId, viewModel.DraftCurationId, "Ajuste 1", PinchAxisTag.Width, 1);
        groupRepository.Items[1] = new PinchGroup(otherGroupId, viewModel.DraftCurationId, "Porche", PinchAxisTag.Height, 2);
        viewModel.SelectedPinchGroup = viewModel.PinchGroups.Single(item => item.PinchGroupId == groupId);

        await viewModel.RenameSelectedPinchGroupAsync("  Patio principal  ", CancellationToken.None);

        Assert.Equal("Patio principal", groupRepository.Items.Single(item => item.Id == groupId).Name);
        Assert.Equal("Porche", groupRepository.Items.Single(item => item.Id == otherGroupId).Name);
        Assert.Equal(groupId, viewModel.SelectedPinchGroupId);
        Assert.Equal("Patio principal", viewModel.SelectedPinchGroup?.Name);
        Assert.True(viewModel.CanRenameSelectedPinchGroup);
    }

    [Fact]
    public async Task ExcludeSelectedArtifactAsync_rejects_selected_wall_candidate()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var geometryPathId = Guid.NewGuid();
        var candidate = new ExtractedWallCandidate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "LINE:68",
            "WALLS",
            geometryPathId,
            101.6m,
            0.95m,
            null,
            ExtractedWallCandidateStatus.Accepted,
            1);
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var candidateRepository = new InMemoryExtractedWallCandidateRepository(candidate);
        var markerRepository = new InMemoryPinchMarkerRepository([]);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IExtractedWallCandidateRepository>(candidateRepository);
        services.AddSingleton<IPinchMarkerRepository>(markerRepository);
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 10, 19, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(
            new FloorPlanReviewSessionDto(
                templateId,
                "seminole2000",
                "SEMINOLE2000",
                "Curated Draft",
                1,
                null,
                [
                    new GeometryPathDto(geometryPathId, false, [new GeometrySegmentDto(geometryPathId, 1, 0m, 0m, 120m, 0m)])
                ],
                [],
                [],
                [],
                [],
                [],
                [
                    new WallCandidateDto(candidate.Id, "LINE:68", "WALLS", "Accepted", 0.95m, 101.6m, null, geometryPathId, 1)
                ],
                [],
                [])));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();
        services.AddTransient<RejectWallCandidateHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);
        await viewModel.ExcludeSelectedArtifactAsync(CancellationToken.None);

        Assert.Equal(ExtractedWallCandidateStatus.Rejected, candidate.Status);
        Assert.True(candidateRepository.UpdateCalled);
    }

    [Fact]
    public async Task Selecting_a_height_pinch_marker_switches_the_active_preview_axis_to_height()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var geometryPathId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var pinchMarkerId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 5, 15, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(
            new FloorPlanReviewSessionDto(
                templateId,
                "seminole2000",
                "SEMINOLE2000",
                "Curated Draft",
                1,
                null,
                [
                    new GeometryPathDto(geometryPathId, false, [new GeometrySegmentDto(geometryPathId, 1, 0m, 0m, 0m, 120m)])
                ],
                [],
                [],
                [],
                [],
                [],
                [
                    new WallCandidateDto(candidateId, "LINE:68", "WALLS", "Accepted", 0.95m, null, null, geometryPathId, 1)
                ],
                [
                    new PinchGroupDto(pinchGroupId, "Patio", nameof(PinchAxisTag.Height), 1)
                ],
                [
                    new PinchMarkerDto(pinchMarkerId, pinchGroupId, "Patio", candidateId, geometryPathId, nameof(PinchAxisTag.Height), 0.62m, 120m, 1)
                ])));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.SelectedPinchMarker = viewModel.PinchMarkers.Single();

        Assert.Equal(nameof(PinchAxisTag.Height), viewModel.SelectedPinchAxis);
        Assert.Equal("Previewing Height pinch", viewModel.PreviewSelectionLabel);
        Assert.Contains("top or bottom", viewModel.InteractionHint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Height_group_without_height_pinches_guides_user_to_place_a_pinch_instead_of_promising_handles()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var geometryPathId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 5, 15, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(
            new FloorPlanReviewSessionDto(
                templateId,
                "seminole2000",
                "SEMINOLE2000",
                "Curated Draft",
                1,
                null,
                [
                    new GeometryPathDto(geometryPathId, false, [new GeometrySegmentDto(geometryPathId, 1, 0m, 0m, 0m, 120m)])
                ],
                [],
                [],
                [],
                [],
                [],
                [
                    new WallCandidateDto(candidateId, "LINE:68", "WALLS", "Accepted", 0.95m, null, null, geometryPathId, 1)
                ],
                [
                    new PinchGroupDto(pinchGroupId, "Ajuste 3", nameof(PinchAxisTag.Height), 1)
                ],
                [])));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);

        Assert.Equal(nameof(PinchAxisTag.Height), viewModel.SelectedPinchAxis);
        Assert.Empty(viewModel.SelectedPinchGroupMarkers);
        Assert.Contains("marc", viewModel.InteractionHint, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("drag the green top or bottom handle", viewModel.InteractionHint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Changing_pinch_axis_to_height_selects_height_group_so_preview_handles_have_a_driver()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var geometryPathId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var widthGroupId = Guid.NewGuid();
        var heightGroupId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 5, 15, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(
            new FloorPlanReviewSessionDto(
                templateId,
                "seminole2000",
                "SEMINOLE2000",
                "Curated Draft",
                1,
                null,
                [
                    new GeometryPathDto(geometryPathId, false, [new GeometrySegmentDto(geometryPathId, 1, 0m, 0m, 0m, 120m)])
                ],
                [],
                [],
                [],
                [],
                [],
                [
                    new WallCandidateDto(candidateId, "LINE:68", "WALLS", "Accepted", 0.95m, null, null, geometryPathId, 1)
                ],
                [
                    new PinchGroupDto(widthGroupId, "Ajuste Width", nameof(PinchAxisTag.Width), 1),
                    new PinchGroupDto(heightGroupId, "Ajuste Height", nameof(PinchAxisTag.Height), 2)
                ],
                [
                    new PinchMarkerDto(Guid.NewGuid(), widthGroupId, "Ajuste Width", candidateId, geometryPathId, nameof(PinchAxisTag.Width), 0.25m, 80m, 1),
                    new PinchMarkerDto(Guid.NewGuid(), heightGroupId, "Ajuste Height", candidateId, geometryPathId, nameof(PinchAxisTag.Height), 0.62m, 120m, 2)
                ])));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);
        Assert.Equal(widthGroupId, viewModel.SelectedPinchGroupId);

        viewModel.SelectedPinchAxis = nameof(PinchAxisTag.Height);

        Assert.Equal(heightGroupId, viewModel.SelectedPinchGroupId);
        Assert.All(viewModel.SelectedPinchGroupMarkers, marker => Assert.Equal(nameof(PinchAxisTag.Height), marker.AxisTag));
    }

    [Fact]
    public async Task SelectPinchMarker_preserves_exact_marker_and_group_when_crossing_to_axis_with_multiple_groups()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var geometryPathId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var widthGroupId = Guid.NewGuid();
        var firstHeightGroupId = Guid.NewGuid();
        var targetHeightGroupId = Guid.NewGuid();
        var targetMarkerId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 5, 15, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(
            new FloorPlanReviewSessionDto(
                templateId,
                "seminole2000",
                "SEMINOLE2000",
                "Curated Draft",
                1,
                null,
                [
                    new GeometryPathDto(geometryPathId, false, [new GeometrySegmentDto(geometryPathId, 1, 0m, 0m, 0m, 120m)])
                ],
                [],
                [],
                [],
                [],
                [],
                [
                    new WallCandidateDto(candidateId, "LINE:68", "WALLS", "Accepted", 0.95m, null, null, geometryPathId, 1)
                ],
                [
                    new PinchGroupDto(widthGroupId, "Ajuste Width", nameof(PinchAxisTag.Width), 1),
                    new PinchGroupDto(firstHeightGroupId, "Ajuste Height H1", nameof(PinchAxisTag.Height), 2),
                    new PinchGroupDto(targetHeightGroupId, "Ajuste Height H2", nameof(PinchAxisTag.Height), 3)
                ],
                [
                    new PinchMarkerDto(Guid.NewGuid(), widthGroupId, "Ajuste Width", candidateId, geometryPathId, nameof(PinchAxisTag.Width), 0.25m, 80m, 1),
                    new PinchMarkerDto(Guid.NewGuid(), firstHeightGroupId, "Ajuste Height H1", candidateId, geometryPathId, nameof(PinchAxisTag.Height), 0.45m, 100m, 2),
                    new PinchMarkerDto(targetMarkerId, targetHeightGroupId, "Ajuste Height H2", candidateId, geometryPathId, nameof(PinchAxisTag.Height), 0.65m, 120m, 3)
                ])));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);
        Assert.Equal(widthGroupId, viewModel.SelectedPinchGroupId);

        Assert.True(viewModel.SelectPinchMarker(targetMarkerId));

        Assert.Equal(targetMarkerId, viewModel.SelectedPinchMarkerId);
        Assert.Equal(targetHeightGroupId, viewModel.SelectedPinchGroupId);
    }

    [Fact]
    public async Task Changing_measurement_corridor_axis_to_height_updates_the_preview_axis_and_height_group()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var geometryPathId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var corridorId = Guid.NewGuid();
        var widthGroupId = Guid.NewGuid();
        var heightGroupId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 5, 15, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(
            new FloorPlanReviewSessionDto(
                templateId,
                "seminole2000",
                "SEMINOLE2000",
                "Curated Draft",
                1,
                null,
                [
                    new GeometryPathDto(geometryPathId, false, [new GeometrySegmentDto(geometryPathId, 1, 0m, 0m, 0m, 120m)])
                ],
                [],
                [],
                [],
                [],
                [],
                [
                    new WallCandidateDto(candidateId, "LINE:68", "WALLS", "Accepted", 0.95m, null, null, geometryPathId, 1)
                ],
                [
                    new PinchGroupDto(widthGroupId, "Ajuste Width", nameof(PinchAxisTag.Width), 1),
                    new PinchGroupDto(heightGroupId, "Ajuste Height", nameof(PinchAxisTag.Height), 2)
                ],
                [
                    new PinchMarkerDto(Guid.NewGuid(), widthGroupId, "Ajuste Width", candidateId, geometryPathId, nameof(PinchAxisTag.Width), 0.25m, 80m, 1),
                    new PinchMarkerDto(Guid.NewGuid(), heightGroupId, "Ajuste Height", candidateId, geometryPathId, nameof(PinchAxisTag.Height), 0.62m, 120m, 2)
                ])
            {
                MeasurementCorridors =
                [
                    new MeasurementCorridorDto(corridorId, "Franja 1", nameof(PinchAxisTag.Width), geometryPathId, 0m, 120m, "Verified", 1)
                ]
            }));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.SelectedMeasurementCorridor = Assert.Single(viewModel.MeasurementCorridors);

        Assert.Equal(nameof(PinchAxisTag.Width), viewModel.SelectedPinchAxis);
        Assert.Equal(widthGroupId, viewModel.SelectedPinchGroupId);

        viewModel.SelectedMeasurementCorridorAxis = nameof(PinchAxisTag.Height);

        Assert.Equal(nameof(PinchAxisTag.Height), viewModel.SelectedPinchAxis);
        Assert.Equal(heightGroupId, viewModel.SelectedPinchGroupId);
        Assert.All(viewModel.SelectedPinchGroupMarkers, marker => Assert.Equal(nameof(PinchAxisTag.Height), marker.AxisTag));
    }

    [Fact]
    public async Task RejectSelectedCandidateAsync_rejects_selected_accepted_candidate()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var geometryPathId = Guid.NewGuid();
        var candidate = new ExtractedWallCandidate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "LINE:68",
            "WALLS",
            geometryPathId,
            101.6m,
            0.95m,
            null,
            ExtractedWallCandidateStatus.Accepted,
            1);
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var candidateRepository = new InMemoryExtractedWallCandidateRepository(candidate);
        var markerRepository = new InMemoryPinchMarkerRepository([]);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IExtractedWallCandidateRepository>(candidateRepository);
        services.AddSingleton<IPinchMarkerRepository>(markerRepository);
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 9, 22, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(
            new FloorPlanReviewSessionDto(
                templateId,
                "seminole2000",
                "SEMINOLE2000",
                "Curated Draft",
                1,
                null,
                [
                    new GeometryPathDto(geometryPathId, false, [new GeometrySegmentDto(geometryPathId, 1, 0m, 0m, 120m, 0m)])
                ],
                [],
                [],
                [],
                [],
                [],
                [
                    new WallCandidateDto(candidate.Id, "LINE:68", "WALLS", "Accepted", 0.95m, 101.6m, null, geometryPathId, 1)
                ],
                [],
                [])));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();
        services.AddTransient<RejectWallCandidateHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);
        await viewModel.RejectSelectedCandidateAsync(CancellationToken.None);

        Assert.Equal(ExtractedWallCandidateStatus.Rejected, candidate.Status);
        Assert.True(candidateRepository.UpdateCalled);
        Assert.Equal(viewModel.DraftCurationId, markerRepository.LastRemovedCurationId);
        Assert.Equal(candidate.Id, markerRepository.LastRemovedSourceCandidateId);
    }

    private static async Task<SelectedPinchEditFixture> CreateSelectedPinchEditFixtureAsync(
        decimal maxTrimMm,
        decimal? secondMaxTrimMm = null)
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var geometryPathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        Guid[] pinchMarkerIds = secondMaxTrimMm is null
            ? [Guid.NewGuid()]
            : [Guid.NewGuid(), Guid.NewGuid()];
        decimal[] maxTrimValues = secondMaxTrimMm is null
            ? [maxTrimMm]
            : [maxTrimMm, secondMaxTrimMm.Value];
        var template = new FloorPlanTemplate(templateId, "santa-barbara", "SANTA-BARBARA", isActive: true);
        template.SetCurrentVersion(versionId);
        var markerRepository = new InMemoryPinchMarkerRepository([]);

        FloorPlanReviewSessionDto CreateSession()
        {
            var markers = pinchMarkerIds
                .Select((pinchMarkerId, index) => new PinchMarkerDto(
                    pinchMarkerId,
                    pinchGroupId,
                    "Patio",
                    candidateId,
                    geometryPathId,
                    nameof(PinchAxisTag.Width),
                    0.35m + (0.3m * index),
                    markerRepository.Items.FirstOrDefault(item => item.Id == pinchMarkerId)?.MaxTrimMm ?? maxTrimValues[index],
                    index + 1))
                .ToArray();

            return new FloorPlanReviewSessionDto(
                templateId,
                "santa-barbara",
                "SANTA-BARBARA",
                "Curated Draft",
                1,
                null,
                [
                    new GeometryPathDto(geometryPathId, false, [new GeometrySegmentDto(geometryPathId, 1, 0m, 0m, 120m, 0m)])
                ],
                [],
                [],
                [],
                [],
                [],
                [
                    new WallCandidateDto(candidateId, "LINE:68", "WALLS", "Accepted", 0.95m, 101.6m, null, geometryPathId, 1)
                ],
                [
                    new PinchGroupDto(pinchGroupId, "Patio", nameof(PinchAxisTag.Width), 1)
                ],
                markers);
        }

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IPinchMarkerRepository>(markerRepository);
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 7, 16, 21, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(CreateSession));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();
        services.AddTransient<UpdatePinchMarkerMaxTrimHandler>();

        var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);
        for (var index = 0; index < pinchMarkerIds.Length; index++)
        {
            markerRepository.Items.Add(new PinchMarker(
                pinchMarkerIds[index],
                viewModel.DraftCurationId,
                pinchGroupId,
                candidateId,
                geometryPathId,
                0.35m + (0.3m * index),
                maxTrimValues[index],
                index + 1));
        }

        viewModel.SelectedPinchGroup = viewModel.PinchGroups.Single();
        viewModel.SelectedPinchMarker = viewModel.PinchMarkers.OrderBy(marker => marker.SortOrder).First();

        return new SelectedPinchEditFixture(provider, viewModel, markerRepository, pinchMarkerIds);
    }

    private static CommissioningPublishFixture CreateCommissioningPublishFixture(
        bool includeDepth,
        string dimensionSourceKind = "DIMENSION",
        CommissioningOpeningHostScenario openingHostScenario = CommissioningOpeningHostScenario.Valid)
    {
        var templateId = Guid.NewGuid();
        var floorPlanVersionId = Guid.NewGuid();
        var widthGroupId = Guid.NewGuid();
        var depthGroupId = Guid.NewGuid();
        var widthAPathId = Guid.NewGuid();
        var widthBPathId = Guid.NewGuid();
        var widthClosingPathId = Guid.NewGuid();
        var depthAPathId = Guid.NewGuid();
        var depthBPathId = Guid.NewGuid();
        var depthClosingPathId = Guid.NewGuid();
        var openingPathId = Guid.NewGuid();
        var duplicateOpeningHostPathId = Guid.NewGuid();
        var crossingOpeningHostPathId = Guid.NewGuid();
        var protectedPathId = Guid.NewGuid();
        var widthACandidateId = Guid.NewGuid();
        var widthBCandidateId = Guid.NewGuid();
        var widthClosingCandidateId = Guid.NewGuid();
        var depthACandidateId = Guid.NewGuid();
        var depthBCandidateId = Guid.NewGuid();
        var depthClosingCandidateId = Guid.NewGuid();
        var duplicateOpeningHostCandidateId = Guid.NewGuid();
        var crossingOpeningHostCandidateId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "commissioned-house", "COMMISSIONED HOUSE", isActive: true);
        template.SetCurrentVersion(floorPlanVersionId);
        var version = new FloorPlanVersion(
            floorPlanVersionId,
            templateId,
            Guid.NewGuid(),
            "commissioned-geometry",
            1,
            new DateTime(2026, 7, 21, 14, 0, 0, DateTimeKind.Utc));

        var openingGeometry = openingHostScenario switch
        {
            CommissioningOpeningHostScenario.Missing => (StartX: 12m, StartY: 1m, EndX: 12m, EndY: 2m),
            CommissioningOpeningHostScenario.Crossing => (StartX: 9m, StartY: 2m, EndX: 11m, EndY: 2m),
            _ => (StartX: 10m, StartY: 1m, EndX: 10m, EndY: 2m)
        };
        var paths = new List<GeometryPathDto>
        {
            Path(widthAPathId, 0m, 0m, 10m, 0m),
            Path(widthBPathId, 0m, 4m, 10m, 4m),
            Path(widthClosingPathId, 10m, 0m, 10m, 4m),
            Path(depthAPathId, 20m, 0m, 20m, 10m),
            Path(depthBPathId, 24m, 0m, 24m, 10m),
            Path(depthClosingPathId, 20m, 10m, 24m, 10m),
            Path(
                openingPathId,
                openingGeometry.StartX,
                openingGeometry.StartY,
                openingGeometry.EndX,
                openingGeometry.EndY),
            Path(protectedPathId, 1m, 2m, 2m, 2m)
        };
        if (openingHostScenario == CommissioningOpeningHostScenario.Multiple)
        {
            paths.Add(Path(duplicateOpeningHostPathId, 10m, 0m, 10m, 4m));
        }
        else if (openingHostScenario == CommissioningOpeningHostScenario.SupportedAndCrossing)
        {
            paths.Add(Path(crossingOpeningHostPathId, 9m, 1.5m, 11m, 1.5m));
        }

        var candidates = new List<WallCandidateDto>
        {
            Candidate(widthACandidateId, "WIDTH:A", widthAPathId, 1),
            Candidate(widthBCandidateId, "WIDTH:B", widthBPathId, 2),
            Candidate(widthClosingCandidateId, "WIDTH:CLOSING", widthClosingPathId, 3),
            Candidate(depthACandidateId, "DEPTH:A", depthAPathId, 4),
            Candidate(depthBCandidateId, "DEPTH:B", depthBPathId, 5),
            Candidate(depthClosingCandidateId, "DEPTH:CLOSING", depthClosingPathId, 6)
        };
        if (openingHostScenario == CommissioningOpeningHostScenario.Multiple)
        {
            candidates.Add(Candidate(
                duplicateOpeningHostCandidateId,
                "WIDTH:CLOSING:DUPLICATE",
                duplicateOpeningHostPathId,
                7));
        }
        else if (openingHostScenario == CommissioningOpeningHostScenario.SupportedAndCrossing)
        {
            candidates.Add(Candidate(
                crossingOpeningHostCandidateId,
                "OPENING:CROSSING",
                crossingOpeningHostPathId,
                7));
        }
        // The closing edge is the operator's commissioned decision about which side of the house
        // absorbs the reduction, so the fixture states it rather than leaving commissioning to
        // infer it from where a door happens to sit.
        PinchGroupDto[] groups = includeDepth
            ?
            [
                new(widthGroupId, "Width room", nameof(PinchAxisTag.Width), 1, "Right"),
                new(depthGroupId, "Depth room", nameof(PinchAxisTag.Height), 2, "Top")
            ]
            : [new(widthGroupId, "Width room", nameof(PinchAxisTag.Width), 1, "Right")];
        var markers = new List<PinchMarkerDto>
        {
            new(Guid.NewGuid(), widthGroupId, "Width room", widthACandidateId, widthAPathId, nameof(PinchAxisTag.Width), 0.5m, 101.6m, 1),
            new(Guid.NewGuid(), widthGroupId, "Width room", widthBCandidateId, widthBPathId, nameof(PinchAxisTag.Width), 0.5m, 152.4m, 2)
        };
        if (includeDepth)
        {
            markers.Add(new(Guid.NewGuid(), depthGroupId, "Depth room", depthACandidateId, depthAPathId, nameof(PinchAxisTag.Height), 0.5m, 76.2m, 1));
            markers.Add(new(Guid.NewGuid(), depthGroupId, "Depth room", depthBCandidateId, depthBPathId, nameof(PinchAxisTag.Height), 0.5m, 127m, 2));
        }

        var session = new FloorPlanReviewSessionDto(
            templateId,
            template.Code,
            template.Name,
            "Curated Draft",
            1,
            null,
            paths,
            [
                new RoomLabelDto(
                    Guid.NewGuid(),
                    "LABEL:COVERAGE",
                    "ROOM LABELS",
                    "ROOM",
                    1m,
                    1m,
                    1m,
                    null,
                    1,
                    SourceEntityKind: "TEXT",
                    TextHeight: 0.5m)
            ],
            [
                new OpeningCandidateDto(
                    Guid.NewGuid(),
                    "OPENING:COVERAGE",
                    "OPENINGS",
                    "Door",
                    "LINE",
                    openingPathId,
                    1m,
                    null,
                    1)
            ],
            [],
            [],
            [
                new ProtectedDetailAssemblyDto(
                    Guid.NewGuid(),
                    "PROTECTED:COVERAGE",
                    "PROTECTED",
                    "Core",
                    "LINE",
                    [protectedPathId],
                    1m,
                    null,
                    1)
            ],
            candidates,
            groups,
            markers)
        {
            MeasurementContext = new MeasurementContextDto("inch", 25.4m, 0.01m, 0.1m),
            Dimensions = [CommissioningDimension(dimensionSourceKind)]
        };
        var markerRepository = new InMemoryPinchMarkerRepository([]);
        var profileRepository = new InMemoryCommissionedHouseAdaptationProfileRepository();
        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanVersionRepository>(new InMemoryFloorPlanVersionRepository(version));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IPinchMarkerRepository>(markerRepository);
        services.AddSingleton<ICommissionedHouseAdaptationProfileRepository>(profileRepository);
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 7, 21, 15, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(session));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();
        services.AddTransient<PublishFloorPlanCurationHandler>();
        services.AddTransient<SaveCommissionedHouseAdaptationProfileHandler>();

        var provider = services.BuildServiceProvider();
        return new CommissioningPublishFixture(
            provider,
            new FloorPlanReviewViewModel(
                provider.GetRequiredService<IServiceScopeFactory>(),
                templateId,
                floorPlanVersionId),
            floorPlanVersionId,
            markerRepository,
            profileRepository,
            markers,
            openingPathId,
            widthClosingPathId);
    }

    private static GeometryPathDto Path(Guid id, decimal x1, decimal y1, decimal x2, decimal y2)
        => new(id, false, [new GeometrySegmentDto(id, 0, x1, y1, x2, y2)]);

    private static WallCandidateDto Candidate(Guid id, string sourceRef, Guid pathId, int sortOrder)
        => new(id, sourceRef, "WALL", "Accepted", 1m, 101.6m, null, pathId, sortOrder);

    private static DimensionDto CommissioningDimension(string sourceEntityKind)
        => new(
            Guid.NewGuid(),
            "DIMENSION:COVERAGE",
            "DIMS",
            sourceEntityKind,
            "*D1",
            "1'-0\"",
            "GeometryBlock",
            string.Empty,
            1m,
            25.4m,
            "Inch",
            0,
            0m,
            0m,
            1m,
            1m,
            0m,
            2m,
            1m,
            0m,
            1m,
            2m,
            0m,
            1m,
            null,
            1)
        {
            SourceHandle = "D1",
            RenderTextX = 1.5m,
            RenderTextY = 2m,
            RenderTextHeight = 0.25m,
            LinePrimitives = [new DimensionLinePrimitiveDto("LINE:1", 1, 1m, 2m, 2m, 2m)],
            TextPrimitives = [new DimensionTextPrimitiveDto("TEXT:1", 1, "1'-0\"", 1.5m, 2m, 0.25m, 0m)]
        };

    private sealed class CommissioningPublishFixture(
        ServiceProvider provider,
        FloorPlanReviewViewModel viewModel,
        Guid floorPlanVersionId,
        InMemoryPinchMarkerRepository markerRepository,
        InMemoryCommissionedHouseAdaptationProfileRepository profileRepository,
        IReadOnlyList<PinchMarkerDto> markers,
        Guid openingGeometryPathId,
        Guid openingHostWallPathId) : IDisposable
    {
        public FloorPlanReviewViewModel ViewModel => viewModel;

        public Guid FloorPlanVersionId => floorPlanVersionId;

        public InMemoryCommissionedHouseAdaptationProfileRepository ProfileRepository => profileRepository;

        public Guid OpeningGeometryPathId => openingGeometryPathId;

        public Guid OpeningHostWallPathId => openingHostWallPathId;

        public void SeedPersistedMarkers(Guid curationId)
        {
            markerRepository.Items.AddRange(markers.Select(marker => new PinchMarker(
                marker.PinchMarkerId,
                curationId,
                marker.PinchGroupId,
                marker.SourceCandidateId,
                marker.GeometryPathId,
                marker.PositionRatio,
                marker.MaxTrimMm,
                marker.SortOrder)));
        }

        public void Dispose() => provider.Dispose();
    }

    // Public because a public [Theory] method takes it as a parameter, and a method may not
    // expose a less accessible type in its signature.
    public enum CommissioningOpeningHostScenario
    {
        Valid,
        Missing,
        Multiple,
        Crossing,
        SupportedAndCrossing
    }

    private sealed class InMemoryCommissionedHouseAdaptationProfileRepository
        : ICommissionedHouseAdaptationProfileRepository
    {
        public List<CommissionedHouseAdaptationProfile> Items { get; } = [];

        public Task UpsertAsync(
            CommissionedHouseAdaptationProfile profile,
            CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.FloorPlanVersionId == profile.FloorPlanVersionId);
            Items.Add(profile);
            return Task.CompletedTask;
        }

        public Task<CommissionedHouseAdaptationProfile?> GetByFloorPlanVersionIdAsync(
            Guid floorPlanVersionId,
            Guid expectedPublishedCurationId,
            CancellationToken cancellationToken)
            => Task.FromResult(Items.SingleOrDefault(item =>
                item.FloorPlanVersionId == floorPlanVersionId &&
                item.PublishedCurationId == expectedPublishedCurationId));

        public Task RemoveByFloorPlanVersionIdAsync(
            Guid floorPlanVersionId,
            CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.FloorPlanVersionId == floorPlanVersionId);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryFloorPlanVersionRepository : IFloorPlanVersionRepository
    {
        private readonly List<FloorPlanVersion> items;

        public InMemoryFloorPlanVersionRepository(params FloorPlanVersion[] items)
        {
            this.items = items.ToList();
        }

        public Task<FloorPlanVersion?> GetByIdAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
            => Task.FromResult(items.SingleOrDefault(item => item.Id == floorPlanVersionId));

        public Task<int> GetNextVersionNumberAsync(Guid floorPlanTemplateId, CancellationToken cancellationToken)
            => Task.FromResult(
                items.Where(item => item.FloorPlanTemplateId == floorPlanTemplateId)
                    .Select(item => item.VersionNumber)
                    .DefaultIfEmpty(0)
                    .Max() + 1);

        public Task AddAsync(FloorPlanVersion version, CancellationToken cancellationToken)
        {
            items.Add(version);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
        {
            items.RemoveAll(item => item.Id == floorPlanVersionId);
            return Task.CompletedTask;
        }
    }

    private sealed class SelectedPinchEditFixture(
        ServiceProvider provider,
        FloorPlanReviewViewModel viewModel,
        InMemoryPinchMarkerRepository markerRepository,
        IReadOnlyList<Guid> pinchMarkerIds) : IDisposable
    {
        public FloorPlanReviewViewModel ViewModel => viewModel;

        public InMemoryPinchMarkerRepository MarkerRepository => markerRepository;

        public Guid PinchMarkerId => pinchMarkerIds[0];

        public Guid SecondPinchMarkerId => pinchMarkerIds[1];

        public void Dispose() => provider.Dispose();
    }

    private sealed class FakeFloorPlanReviewSessionReader : IFloorPlanReviewSessionReader
    {
        private readonly Func<FloorPlanReviewSessionDto> readSession;

        public FakeFloorPlanReviewSessionReader(FloorPlanReviewSessionDto session)
            : this(() => session)
        {
        }

        public FakeFloorPlanReviewSessionReader(Func<FloorPlanReviewSessionDto> readSession)
        {
            this.readSession = readSession;
        }

        public Task<FloorPlanReviewSessionDto?> GetByTemplateAsync(Guid templateId, CancellationToken cancellationToken)
        {
            return Task.FromResult<FloorPlanReviewSessionDto?>(readSession());
        }

        public Task<FloorPlanReviewSessionDto?> GetByVersionAsync(
            Guid templateId,
            Guid floorPlanVersionId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<FloorPlanReviewSessionDto?>(readSession());
        }
    }

    private sealed class SequencedFloorPlanReviewSessionReader : IFloorPlanReviewSessionReader
    {
        private readonly Queue<FloorPlanReviewSessionDto> sessions;
        private bool pendingNonPublishedOpenProbe;

        public SequencedFloorPlanReviewSessionReader(params FloorPlanReviewSessionDto[] sessions)
        {
            this.sessions = new Queue<FloorPlanReviewSessionDto>(sessions);
        }

        public Task<FloorPlanReviewSessionDto?> GetByTemplateAsync(Guid templateId, CancellationToken cancellationToken)
        {
            var session = ReadNextSession();
            return Task.FromResult<FloorPlanReviewSessionDto?>(session);
        }

        public Task<FloorPlanReviewSessionDto?> GetByVersionAsync(
            Guid templateId,
            Guid floorPlanVersionId,
            CancellationToken cancellationToken)
        {
            return GetByTemplateAsync(templateId, cancellationToken);
        }

        public Task<FloorPlanReviewSessionDto?> GetByCurationAsync(
            Guid templateId,
            Guid curationId,
            CancellationToken cancellationToken)
        {
            var session = ConsumeNextSession();
            return Task.FromResult<FloorPlanReviewSessionDto?>(session);
        }

        public Task<FloorPlanReviewSessionDto?> GetByCurationAsync(
            Guid templateId,
            Guid floorPlanVersionId,
            Guid curationId,
            CancellationToken cancellationToken)
        {
            return GetByCurationAsync(templateId, curationId, cancellationToken);
        }

        private FloorPlanReviewSessionDto ReadNextSession()
        {
            var session = sessions.Peek();
            if (session.ActivePublishedCurationId is null && !pendingNonPublishedOpenProbe)
            {
                pendingNonPublishedOpenProbe = true;
                return session;
            }

            pendingNonPublishedOpenProbe = false;
            return ConsumeNextSession();
        }

        private FloorPlanReviewSessionDto ConsumeNextSession()
        {
            pendingNonPublishedOpenProbe = false;
            return sessions.Count > 1 ? sessions.Dequeue() : sessions.Peek();
        }
    }

    private sealed class RepositoryBackedFloorPlanReviewSessionReader : IFloorPlanReviewSessionReader
    {
        private readonly Guid templateId;
        private readonly string templateCode;
        private readonly string templateName;
        private readonly Guid geometryPathId;
        private readonly InMemoryPinchGroupRepository groupRepository;

        public RepositoryBackedFloorPlanReviewSessionReader(
            Guid templateId,
            string templateCode,
            string templateName,
            Guid geometryPathId,
            InMemoryPinchGroupRepository groupRepository)
        {
            this.templateId = templateId;
            this.templateCode = templateCode;
            this.templateName = templateName;
            this.geometryPathId = geometryPathId;
            this.groupRepository = groupRepository;
        }

        public Task<FloorPlanReviewSessionDto?> GetByTemplateAsync(Guid templateId, CancellationToken cancellationToken)
            => Task.FromResult<FloorPlanReviewSessionDto?>(CreateSession());

        public Task<FloorPlanReviewSessionDto?> GetByVersionAsync(
            Guid templateId,
            Guid floorPlanVersionId,
            CancellationToken cancellationToken)
            => Task.FromResult<FloorPlanReviewSessionDto?>(CreateSession());

        public Task<FloorPlanReviewSessionDto?> GetByCurationAsync(
            Guid templateId,
            Guid curationId,
            CancellationToken cancellationToken)
            => Task.FromResult<FloorPlanReviewSessionDto?>(CreateSession());

        public Task<FloorPlanReviewSessionDto?> GetByCurationAsync(
            Guid templateId,
            Guid floorPlanVersionId,
            Guid curationId,
            CancellationToken cancellationToken)
            => Task.FromResult<FloorPlanReviewSessionDto?>(CreateSession());

        private FloorPlanReviewSessionDto CreateSession()
        {
            return new FloorPlanReviewSessionDto(
                templateId,
                templateCode,
                templateName,
                "Curated Draft",
                1,
                null,
                [
                    new GeometryPathDto(geometryPathId, false, [new GeometrySegmentDto(geometryPathId, 1, 0m, 0m, 120m, 0m)])
                ],
                [],
                [],
                [],
                [],
                [],
                [],
                groupRepository.Items
                    .OrderBy(item => item.SortOrder)
                    .Select(item => new PinchGroupDto(
                        item.Id,
                        item.Name,
                        item.AxisTag.ToString(),
                        item.SortOrder,
                        item.ClosingEdge))
                    .ToArray(),
                []);
        }
    }

    private sealed class InMemoryFloorPlanTemplateRepository : IFloorPlanTemplateRepository
    {
        private readonly List<FloorPlanTemplate> items;

        public InMemoryFloorPlanTemplateRepository(params FloorPlanTemplate[] items)
        {
            this.items = items.ToList();
        }

        public Task<FloorPlanTemplate?> GetByCodeAsync(string code, CancellationToken cancellationToken)
        {
            return Task.FromResult(items.SingleOrDefault(item => item.Code == code));
        }

        public Task<FloorPlanTemplate?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken)
        {
            return Task.FromResult(items.SingleOrDefault(item => item.Id == templateId));
        }

        public Task AddAsync(FloorPlanTemplate template, CancellationToken cancellationToken)
        {
            items.Add(template);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(FloorPlanTemplate template, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryExtractedWallCandidateRepository : IExtractedWallCandidateRepository
    {
        private readonly ExtractedWallCandidate candidate;

        public InMemoryExtractedWallCandidateRepository(ExtractedWallCandidate candidate)
        {
            this.candidate = candidate;
        }

        public bool UpdateCalled { get; private set; }

        public Task AddRangeAsync(
            IReadOnlyList<ExtractedWallCandidate> domainCandidates,
            IReadOnlyList<DetectedWallCandidate> detectedCandidates,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task AddAsync(
            ExtractedWallCandidate domainCandidate,
            DetectedWallCandidate detectedCandidate,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<int> GetNextSortOrderAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<ExtractedWallCandidate?> GetByIdAsync(Guid candidateId, CancellationToken cancellationToken)
            => Task.FromResult(candidate.Id == candidateId ? candidate : null);

        public Task UpdateAsync(ExtractedWallCandidate candidate, CancellationToken cancellationToken)
        {
            UpdateCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryExtractedRoomLabelRepository : IExtractedRoomLabelRepository
    {
        public InMemoryExtractedRoomLabelRepository(params ExtractedRoomLabel[] seed)
        {
            Items = [.. seed];
        }

        public List<ExtractedRoomLabel> Items { get; }

        public Task AddRangeAsync(IReadOnlyList<ExtractedRoomLabel> labels, CancellationToken cancellationToken)
        {
            Items.AddRange(labels);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ExtractedRoomLabel>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ExtractedRoomLabel>>(Items.Where(item => item.WallExtractionRunId == wallExtractionRunId).ToArray());
        }

        public Task RemoveAsync(Guid roomLabelId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.Id == roomLabelId);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryExtractedOpeningLabelRepository : IExtractedOpeningLabelRepository
    {
        public InMemoryExtractedOpeningLabelRepository(params ExtractedOpeningLabel[] seed)
        {
            Items = [.. seed];
        }

        public List<ExtractedOpeningLabel> Items { get; }

        public Task AddRangeAsync(IReadOnlyList<ExtractedOpeningLabel> labels, CancellationToken cancellationToken)
        {
            Items.AddRange(labels);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ExtractedOpeningLabel>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ExtractedOpeningLabel>>(Items.Where(item => item.WallExtractionRunId == wallExtractionRunId).ToArray());
        }

        public Task RemoveAsync(Guid openingLabelId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.Id == openingLabelId);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryPinchGroupRepository : IPinchGroupRepository
    {
        public InMemoryPinchGroupRepository(IReadOnlyList<PinchGroup> seed)
        {
            Items = [.. seed];
        }

        public List<PinchGroup> Items { get; }

        public Task AddAsync(PinchGroup group, CancellationToken cancellationToken)
        {
            Items.Add(group);
            return Task.CompletedTask;
        }

        public Task<PinchGroup?> GetByIdAsync(Guid pinchGroupId, CancellationToken cancellationToken)
            => Task.FromResult(Items.SingleOrDefault(item => item.Id == pinchGroupId));

        public Task<IReadOnlyList<PinchGroup>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<PinchGroup>>(Items.Where(item => item.FloorPlanCurationId == curationId).ToArray());

        public Task RemoveAsync(Guid pinchGroupId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.Id == pinchGroupId);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(PinchGroup group, CancellationToken cancellationToken)
        {
            var index = Items.FindIndex(item => item.Id == group.Id);
            if (index >= 0)
            {
                Items[index] = group;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryPinchMarkerRepository : IPinchMarkerRepository
    {
        public InMemoryPinchMarkerRepository(IReadOnlyList<PinchMarker> seed)
        {
            Items = [.. seed];
        }

        public List<PinchMarker> Items { get; }

        public Guid? LastRemovedCurationId { get; private set; }

        public Guid? LastRemovedSourceCandidateId { get; private set; }

        public int UpdateCallCount { get; private set; }

        public Task AddAsync(PinchMarker marker, CancellationToken cancellationToken)
        {
            Items.Add(marker);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(PinchMarker marker, CancellationToken cancellationToken)
        {
            UpdateCallCount++;
            var index = Items.FindIndex(item => item.Id == marker.Id);
            if (index >= 0)
            {
                Items[index] = marker;
            }

            return Task.CompletedTask;
        }

        public Task<PinchMarker?> GetByIdAsync(Guid pinchMarkerId, CancellationToken cancellationToken)
            => Task.FromResult(Items.SingleOrDefault(item => item.Id == pinchMarkerId));

        public Task<IReadOnlyList<PinchMarker>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<PinchMarker>>(Items.Where(item => item.FloorPlanCurationId == curationId).ToArray());

        public Task RemoveAsync(Guid pinchMarkerId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.Id == pinchMarkerId);
            return Task.CompletedTask;
        }

        public Task RemoveByGroupAsync(Guid curationId, Guid pinchGroupId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.FloorPlanCurationId == curationId && item.PinchGroupId == pinchGroupId);
            return Task.CompletedTask;
        }

        public Task RemoveBySourceCandidateAsync(Guid curationId, Guid sourceCandidateId, CancellationToken cancellationToken)
        {
            LastRemovedCurationId = curationId;
            LastRemovedSourceCandidateId = sourceCandidateId;
            Items.RemoveAll(item => item.FloorPlanCurationId == curationId && item.SourceCandidateId == sourceCandidateId);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryFloorPlanCurationRepository : IFloorPlanCurationRepository
    {
        private readonly List<FloorPlanCuration> items = [];

        public Task<FloorPlanCuration?> GetByIdAsync(Guid curationId, CancellationToken cancellationToken)
        {
            return Task.FromResult(items.SingleOrDefault(item => item.Id == curationId));
        }

        public Task<FloorPlanCuration?> GetDraftAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
        {
            return Task.FromResult(items.SingleOrDefault(item =>
                item.FloorPlanVersionId == floorPlanVersionId &&
                item.Status == FloorPlanCurationStatus.Draft));
        }

        public Task<FloorPlanCuration?> GetPublishedAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
        {
            return Task.FromResult(items.SingleOrDefault(item =>
                item.FloorPlanVersionId == floorPlanVersionId &&
                item.Status == FloorPlanCurationStatus.Published));
        }

        public Task<int> GetNextCurationVersionAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
        {
            var nextVersion = items
                .Where(item => item.FloorPlanVersionId == floorPlanVersionId)
                .Select(item => item.CurationVersion)
                .DefaultIfEmpty(0)
                .Max() + 1;

            return Task.FromResult(nextVersion);
        }

        public Task AddAsync(FloorPlanCuration curation, CancellationToken cancellationToken)
        {
            items.Add(curation);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(FloorPlanCuration curation, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
