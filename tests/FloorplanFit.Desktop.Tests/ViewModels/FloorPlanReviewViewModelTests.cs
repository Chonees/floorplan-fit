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
    public async Task LoadAsync_loads_the_review_session_and_selects_the_first_candidate()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var geometryPathId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "santa-barbara", "SANTA-BARBARA", isActive: true);
        template.SetCurrentVersion(versionId);

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
                        [new GeometrySegmentDto(geometryPathId, 1, 0m, 0m, 120m, 0m)])
                ],
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
                [])));
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
        Assert.NotNull(viewModel.SelectedCandidate);
        Assert.Equal(geometryPathId, viewModel.HighlightGeometryPathId);
        Assert.Equal("Previewing candidate: LINE:1", viewModel.PreviewSelectionLabel);
    }

    [Fact]
    public async Task Selecting_curated_wall_updates_preview_selection_label_and_highlight()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var candidatePathId = Guid.NewGuid();
        var curatedPathId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "santa-barbara", "SANTA-BARBARA", isActive: true);
        template.SetCurrentVersion(versionId);

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
                        candidatePathId,
                        false,
                        [new GeometrySegmentDto(candidatePathId, 1, 0m, 0m, 120m, 0m)]),
                    new GeometryPathDto(
                        curatedPathId,
                        false,
                        [new GeometrySegmentDto(curatedPathId, 1, 0m, 10m, 120m, 10m)])
                ],
                [
                    new WallCandidateDto(
                        candidateId,
                        "LINE:1",
                        "WALLS",
                        "Accepted",
                        0.95m,
                        null,
                        null,
                        candidatePathId,
                        1)
                ],
                [
                    new CuratedWallDto(
                        Guid.NewGuid(),
                        "W-001",
                        candidateId,
                        nameof(WallRole.Partition),
                        nameof(WallMobilityLevel.Flexible),
                        nameof(WallProtectionLevel.None),
                        101.6m,
                        "2x4",
                        null,
                        IsExterior: false,
                        IsStructuralHint: false,
                        curatedPathId,
                        1,
                        null)
                ])));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);

        viewModel.SelectedCuratedWall = viewModel.CuratedWalls.Single();

        Assert.Equal(curatedPathId, viewModel.HighlightGeometryPathId);
        Assert.Equal("Previewing curated wall: W-001 (LINE:1)", viewModel.PreviewSelectionLabel);
    }

    [Fact]
    public async Task SelectPreviewPath_selects_pending_candidate_when_no_curated_wall_exists()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var candidatePathId = Guid.NewGuid();
        var otherPathId = Guid.NewGuid();
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
                    new GeometryPathDto(candidatePathId, false, [new GeometrySegmentDto(candidatePathId, 1, 0m, 0m, 120m, 0m)]),
                    new GeometryPathDto(otherPathId, false, [new GeometrySegmentDto(otherPathId, 1, 0m, 10m, 120m, 10m)])
                ],
                [
                    new WallCandidateDto(Guid.NewGuid(), "LINE:1", "WALLS", "Pending", 0.95m, null, null, candidatePathId, 1),
                    new WallCandidateDto(Guid.NewGuid(), "LINE:2", "WALLS", "Pending", 0.90m, null, null, otherPathId, 2)
                ],
                [])));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);

        var selected = viewModel.SelectPreviewPath(otherPathId);

        Assert.True(selected);
        Assert.Equal("LINE:2", viewModel.SelectedCandidate?.SourceEntityRef);
        Assert.Null(viewModel.SelectedCuratedWall);
        Assert.Equal(otherPathId, viewModel.HighlightGeometryPathId);
        Assert.Equal("Previewing candidate: LINE:2", viewModel.PreviewSelectionLabel);
    }

    [Fact]
    public async Task SelectPreviewPath_prioritizes_curated_wall_and_syncs_the_source_candidate()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var curatedPathId = Guid.NewGuid();
        var acceptedCandidateId = Guid.NewGuid();
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
                    new GeometryPathDto(curatedPathId, false, [new GeometrySegmentDto(curatedPathId, 1, 0m, 0m, 120m, 0m)])
                ],
                [
                    new WallCandidateDto(acceptedCandidateId, "LINE:7", "WALLS", "Accepted", 0.95m, null, null, curatedPathId, 1)
                ],
                [
                    new CuratedWallDto(
                        Guid.NewGuid(),
                        "W-007",
                        acceptedCandidateId,
                        nameof(WallRole.Partition),
                        nameof(WallMobilityLevel.Flexible),
                        nameof(WallProtectionLevel.None),
                        101.6m,
                        "2x4",
                        null,
                        IsExterior: false,
                        IsStructuralHint: false,
                        curatedPathId,
                        1,
                        null)
                ])));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);

        var selected = viewModel.SelectPreviewPath(curatedPathId);

        Assert.True(selected);
        Assert.Equal("LINE:7", viewModel.SelectedCandidate?.SourceEntityRef);
        Assert.Equal("W-007", viewModel.SelectedCuratedWall?.StableWallId);
        Assert.Equal(curatedPathId, viewModel.HighlightGeometryPathId);
        Assert.Equal("Previewing curated wall: W-007 (LINE:7)", viewModel.PreviewSelectionLabel);
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
