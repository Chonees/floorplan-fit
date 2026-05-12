using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.ViewModels;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FloorplanFit.Desktop.Tests.ViewModels;

public sealed class CuratedArtifactFloorPlanReviewViewModelTests
{
    [Fact]
    public async Task LoadAsync_groups_visible_curated_artifacts_and_keeps_excluded_items_out_of_the_active_groups()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var visiblePathId = Guid.NewGuid();
        var excludedPathId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);

        var services = BuildServices(
            template,
            new FloorPlanReviewSessionDto(
                templateId,
                "seminole2000",
                "SEMINOLE2000",
                "Curated Draft",
                1,
                null,
                [
                    new GeometryPathDto(visiblePathId, false, [new GeometrySegmentDto(visiblePathId, 1, 0m, 0m, 120m, 0m)]),
                    new GeometryPathDto(excludedPathId, false, [new GeometrySegmentDto(excludedPathId, 1, 0m, 10m, 120m, 10m)])
                ],
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                [
                    new CuratedPlanArtifactDto(
                        Guid.NewGuid(),
                        FloorPlanArtifactSourceKinds.OpeningCandidate,
                        "LINE:DOOR:1",
                        "DOORS",
                        "LINE",
                        null,
                        [visiblePathId],
                        0.95m,
                        null,
                        1,
                        FloorPlanArtifactTaxonomy.OpeningFamily,
                        FloorPlanArtifactTaxonomy.OpeningCategory,
                        FloorPlanArtifactTaxonomy.DoorType,
                        FloorPlanArtifactTaxonomy.FixedFamily,
                        FloorPlanArtifactTaxonomy.WetFixtureCategory,
                        FloorPlanArtifactTaxonomy.TubType,
                        FloorPlanArtifactDecisionState.Reclassified.ToString(),
                        "#FFDC2626"),
                    new CuratedPlanArtifactDto(
                        Guid.NewGuid(),
                        FloorPlanArtifactSourceKinds.ProtectedDetailAssembly,
                        "DETAIL:MISC:1",
                        "MISC",
                        "DETAIL-GROUP",
                        null,
                        [excludedPathId],
                        0.90m,
                        null,
                        2,
                        FloorPlanArtifactTaxonomy.ProtectedFamily,
                        FloorPlanArtifactTaxonomy.WetAssemblyCategory,
                        FloorPlanArtifactTaxonomy.UnknownWetAssemblyType,
                        FloorPlanArtifactTaxonomy.ProtectedFamily,
                        FloorPlanArtifactTaxonomy.WetAssemblyCategory,
                        FloorPlanArtifactTaxonomy.UnknownWetAssemblyType,
                        FloorPlanArtifactDecisionState.Excluded.ToString(),
                        "#FF8B5CF6")
                ]));

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);

        Assert.Equal(2, viewModel.CuratedPlanArtifacts.Count);
        Assert.Single(viewModel.VisibleCuratedPlanArtifacts);
        Assert.Single(viewModel.CuratedArtifactGroups);
        Assert.Equal("Plumbing Fixtures", viewModel.CuratedArtifactGroups.Single().Title);
        Assert.Equal("LINE:DOOR:1", viewModel.VisibleCuratedPlanArtifacts.Single().SourceEntityRef);
    }

    [Fact]
    public async Task SelectPreviewPath_selects_the_matching_curated_artifact_geometry()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var wallPathId = Guid.NewGuid();
        var curatedPathId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);

        var services = BuildServices(
            template,
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
                [],
                [],
                [],
                [],
                [],
                [
                    new WallCandidateDto(Guid.NewGuid(), "LINE:WALL:1", "WALLS", "Accepted", 0.95m, null, null, wallPathId, 1)
                ],
                [],
                [],
                [
                    new CuratedPlanArtifactDto(
                        Guid.NewGuid(),
                        FloorPlanArtifactSourceKinds.OpeningCandidate,
                        "LINE:DOOR:1",
                        "DOORS",
                        "LINE",
                        null,
                        [curatedPathId],
                        0.95m,
                        null,
                        1,
                        FloorPlanArtifactTaxonomy.OpeningFamily,
                        FloorPlanArtifactTaxonomy.OpeningCategory,
                        FloorPlanArtifactTaxonomy.DoorType,
                        FloorPlanArtifactTaxonomy.FixedFamily,
                        FloorPlanArtifactTaxonomy.WetFixtureCategory,
                        FloorPlanArtifactTaxonomy.TubType,
                        FloorPlanArtifactDecisionState.Reclassified.ToString(),
                        "#FFDC2626")
                ]));

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);

        var selected = viewModel.SelectPreviewPath(curatedPathId);

        Assert.True(selected);
        Assert.Equal("LINE:DOOR:1", viewModel.SelectedCuratedArtifact?.SourceEntityRef);
        Assert.Equal(curatedPathId, viewModel.HighlightGeometryPathId);
        Assert.Equal("Previewing curated object: LINE:DOOR:1", viewModel.PreviewSelectionLabel);
        Assert.Equal(FloorPlanArtifactTaxonomy.FixedFamily, viewModel.EditableCuratedArtifactFamily);
        Assert.Equal(FloorPlanArtifactTaxonomy.WetFixtureCategory, viewModel.EditableCuratedArtifactCategory);
        Assert.Equal(FloorPlanArtifactTaxonomy.TubType, viewModel.EditableCuratedArtifactType);
    }

    private static ServiceCollection BuildServices(FloorPlanTemplate template, FloorPlanReviewSessionDto session)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 11, 18, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(session));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();
        return services;
    }

    private sealed class FakeFloorPlanReviewSessionReader : IFloorPlanReviewSessionReader
    {
        private readonly FloorPlanReviewSessionDto session;

        public FakeFloorPlanReviewSessionReader(FloorPlanReviewSessionDto session)
        {
            this.session = session;
        }

        public Task<FloorPlanReviewSessionDto?> GetByTemplateAsync(Guid templateId, CancellationToken cancellationToken)
            => Task.FromResult<FloorPlanReviewSessionDto?>(session);

        public Task<FloorPlanReviewSessionDto?> GetByVersionAsync(Guid templateId, Guid floorPlanVersionId, CancellationToken cancellationToken)
            => Task.FromResult<FloorPlanReviewSessionDto?>(session);
    }

    private sealed class InMemoryFloorPlanTemplateRepository : IFloorPlanTemplateRepository
    {
        private readonly FloorPlanTemplate template;

        public InMemoryFloorPlanTemplateRepository(FloorPlanTemplate template)
        {
            this.template = template;
        }

        public Task<FloorPlanTemplate?> GetByCodeAsync(string code, CancellationToken cancellationToken)
            => Task.FromResult(template.Code == code ? template : null);

        public Task<FloorPlanTemplate?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken)
            => Task.FromResult(template.Id == templateId ? template : null);

        public Task AddAsync(FloorPlanTemplate template, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateAsync(FloorPlanTemplate template, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class InMemoryFloorPlanCurationRepository : IFloorPlanCurationRepository
    {
        private readonly List<FloorPlanCuration> items = [];

        public Task<FloorPlanCuration?> GetByIdAsync(Guid curationId, CancellationToken cancellationToken)
            => Task.FromResult(items.SingleOrDefault(item => item.Id == curationId));

        public Task<FloorPlanCuration?> GetDraftAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
            => Task.FromResult(items.SingleOrDefault(item => item.FloorPlanVersionId == floorPlanVersionId && item.Status == FloorPlanCurationStatus.Draft));

        public Task<FloorPlanCuration?> GetPublishedAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
            => Task.FromResult<FloorPlanCuration?>(null);

        public Task<int> GetNextCurationVersionAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
            => Task.FromResult(1);

        public Task AddAsync(FloorPlanCuration curation, CancellationToken cancellationToken)
        {
            items.Add(curation);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(FloorPlanCuration curation, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
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
