using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Application.FloorPlans.Review;
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

    private sealed class FakeFloorPlanReviewSessionReader : IFloorPlanReviewSessionReader
    {
        private readonly FloorPlanReviewSessionDto session;

        public FakeFloorPlanReviewSessionReader(FloorPlanReviewSessionDto session)
        {
            this.session = session;
        }

        public Task<FloorPlanReviewSessionDto?> GetByTemplateAsync(Guid templateId, CancellationToken cancellationToken)
        {
            return Task.FromResult<FloorPlanReviewSessionDto?>(session);
        }

        public Task<FloorPlanReviewSessionDto?> GetByVersionAsync(
            Guid templateId,
            Guid floorPlanVersionId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<FloorPlanReviewSessionDto?>(session);
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

    private sealed class InMemoryPinchMarkerRepository : IPinchMarkerRepository
    {
        public InMemoryPinchMarkerRepository(IReadOnlyList<PinchMarker> seed)
        {
            Items = [.. seed];
        }

        public List<PinchMarker> Items { get; }

        public Guid? LastRemovedCurationId { get; private set; }

        public Guid? LastRemovedSourceCandidateId { get; private set; }

        public Task AddAsync(PinchMarker marker, CancellationToken cancellationToken)
        {
            Items.Add(marker);
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
