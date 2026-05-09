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
                        "Pending",
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
                ])));
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
                    new WallCandidateDto(Guid.NewGuid(), "LINE:1", "WALLS", "Pending", 0.95m, null, null, firstPathId, 1),
                    new WallCandidateDto(Guid.NewGuid(), "LINE:2", "WALLS", "Pending", 0.90m, null, null, secondPathId, 2)
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
                    new WallCandidateDto(Guid.NewGuid(), "LINE:WALL:1", "WALLS", "Pending", 0.95m, null, null, wallPathId, 1)
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
        Assert.Equal("LINE:DOOR:1", viewModel.SelectedOpeningCandidate?.SourceEntityRef);
        Assert.Equal(openingPathId, viewModel.HighlightGeometryPathId);
        Assert.Equal("Previewing opening: LINE:DOOR:1", viewModel.PreviewSelectionLabel);
        Assert.Contains("Remove Selected Opening", viewModel.InteractionHint, StringComparison.OrdinalIgnoreCase);
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
                    new WallCandidateDto(Guid.NewGuid(), "LINE:WALL:1", "WALLS", "Pending", 0.95m, null, null, wallPathId, 1)
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
        Assert.Equal("Toilet", viewModel.SelectedFixedPlanComponent?.Kind);
        Assert.Equal(toiletPathId, viewModel.HighlightGeometryPathId);
        Assert.Equal("Previewing fixed component: INSERT:1", viewModel.PreviewSelectionLabel);
        Assert.Contains("Remove Selected Component", viewModel.InteractionHint, StringComparison.OrdinalIgnoreCase);
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
                    new WallCandidateDto(Guid.NewGuid(), "LINE:WALL:1", "WALLS", "Pending", 0.95m, null, null, wallPathId, 1)
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
        Assert.Equal("WetAreaDetail", viewModel.SelectedProtectedDetailAssembly?.Kind);
        Assert.Equal(protectedPathId, viewModel.HighlightGeometryPathId);
        Assert.Equal("Previewing protected detail: DETAIL:MISC:1", viewModel.PreviewSelectionLabel);
        Assert.Contains("Remove Selected Detail", viewModel.InteractionHint, StringComparison.OrdinalIgnoreCase);
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
                    new WallCandidateDto(candidateId, "LINE:68", "WALLS", "Pending", 0.95m, null, null, geometryPathId, 1)
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

    private sealed class InMemoryFloorPlanCurationRepository : IFloorPlanCurationRepository
    {
        private readonly List<FloorPlanCuration> items = [];

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
