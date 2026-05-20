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

    [Fact]
    public async Task SaveSelectedCuratedArtifactClassificationAsync_persists_reclassified_overlay()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var curatedArtifactId = Guid.NewGuid();
        var curatedPathId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var classificationRepository = new InMemoryFloorPlanArtifactClassificationRepository();

        var services = BuildServices(
            template,
            CreateCuratedArtifactSession(templateId, curatedArtifactId, curatedPathId),
            services =>
            {
                services.AddSingleton<IFloorPlanArtifactClassificationRepository>(classificationRepository);
                services.AddTransient<SaveCuratedArtifactClassificationHandler>();
            });

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.SelectedCuratedArtifact = viewModel.VisibleCuratedPlanArtifacts.Single();
        viewModel.EditableCuratedArtifactFamily = FloorPlanArtifactTaxonomy.ProtectedFamily;
        viewModel.EditableCuratedArtifactCategory = FloorPlanArtifactTaxonomy.WetAssemblyCategory;
        viewModel.EditableCuratedArtifactType = FloorPlanArtifactTaxonomy.UnknownWetAssemblyType;

        await viewModel.SaveSelectedCuratedArtifactClassificationAsync(CancellationToken.None);

        var saved = Assert.Single(classificationRepository.Items);
        Assert.Equal(viewModel.DraftCurationId, saved.FloorPlanCurationId);
        Assert.Equal(curatedArtifactId, saved.SourceArtifactId);
        Assert.Equal(FloorPlanArtifactTaxonomy.ProtectedFamily, saved.ResolvedFamily);
        Assert.Equal(FloorPlanArtifactTaxonomy.WetAssemblyCategory, saved.ResolvedCategory);
        Assert.Equal(FloorPlanArtifactTaxonomy.UnknownWetAssemblyType, saved.ResolvedType);
        Assert.Equal(FloorPlanArtifactDecisionState.Reclassified, saved.DecisionState);
    }

    [Fact]
    public async Task RestoreSelectedCuratedArtifactClassificationAsync_persists_detected_default_overlay()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var curatedArtifactId = Guid.NewGuid();
        var curatedPathId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var classificationRepository = new InMemoryFloorPlanArtifactClassificationRepository();

        var services = BuildServices(
            template,
            CreateCuratedArtifactSession(templateId, curatedArtifactId, curatedPathId),
            services =>
            {
                services.AddSingleton<IFloorPlanArtifactClassificationRepository>(classificationRepository);
                services.AddTransient<RestoreCuratedArtifactClassificationHandler>();
            });

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.SelectedCuratedArtifact = viewModel.VisibleCuratedPlanArtifacts.Single();

        await viewModel.RestoreSelectedCuratedArtifactClassificationAsync(CancellationToken.None);

        var saved = Assert.Single(classificationRepository.Items);
        Assert.Equal(viewModel.DraftCurationId, saved.FloorPlanCurationId);
        Assert.Equal(curatedArtifactId, saved.SourceArtifactId);
        Assert.Equal(FloorPlanArtifactTaxonomy.OpeningFamily, saved.ResolvedFamily);
        Assert.Equal(FloorPlanArtifactTaxonomy.OpeningCategory, saved.ResolvedCategory);
        Assert.Equal(FloorPlanArtifactTaxonomy.DoorType, saved.ResolvedType);
        Assert.Equal(FloorPlanArtifactDecisionState.DetectedDefault, saved.DecisionState);
    }

    [Fact]
    public async Task ExcludeSelectedCuratedArtifactAsync_persists_excluded_overlay()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var curatedArtifactId = Guid.NewGuid();
        var curatedPathId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var classificationRepository = new InMemoryFloorPlanArtifactClassificationRepository();

        var services = BuildServices(
            template,
            CreateCuratedArtifactSession(templateId, curatedArtifactId, curatedPathId),
            services =>
            {
                services.AddSingleton<IFloorPlanArtifactClassificationRepository>(classificationRepository);
                services.AddTransient<ExcludeCuratedArtifactHandler>();
            });

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);

        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.SelectedCuratedArtifact = viewModel.VisibleCuratedPlanArtifacts.Single();

        await viewModel.ExcludeSelectedCuratedArtifactAsync(CancellationToken.None);

        var saved = Assert.Single(classificationRepository.Items);
        Assert.Equal(viewModel.DraftCurationId, saved.FloorPlanCurationId);
        Assert.Equal(curatedArtifactId, saved.SourceArtifactId);
        Assert.Equal(FloorPlanArtifactTaxonomy.FixedFamily, saved.ResolvedFamily);
        Assert.Equal(FloorPlanArtifactTaxonomy.WetFixtureCategory, saved.ResolvedCategory);
        Assert.Equal(FloorPlanArtifactTaxonomy.TubType, saved.ResolvedType);
        Assert.Equal(FloorPlanArtifactDecisionState.Excluded, saved.DecisionState);
    }

    private static ServiceCollection BuildServices(
        FloorPlanTemplate template,
        FloorPlanReviewSessionDto session,
        Action<ServiceCollection>? configureExtraServices = null)
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
        configureExtraServices?.Invoke(services);
        return services;
    }

    private static FloorPlanReviewSessionDto CreateCuratedArtifactSession(Guid templateId, Guid curatedArtifactId, Guid curatedPathId)
        => new(
            templateId,
            "seminole2000",
            "SEMINOLE2000",
            "Curated Draft",
            1,
            null,
            [
                new GeometryPathDto(curatedPathId, false, [new GeometrySegmentDto(curatedPathId, 1, 40m, 0m, 76m, 0m)])
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
                    curatedArtifactId,
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
            ]);

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

    private sealed class InMemoryFloorPlanArtifactClassificationRepository : IFloorPlanArtifactClassificationRepository
    {
        public List<FloorPlanArtifactClassification> Items { get; } = [];

        public Task<IReadOnlyList<FloorPlanArtifactClassification>> ListByCurationAsync(Guid floorPlanCurationId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<FloorPlanArtifactClassification>>(Items.Where(item => item.FloorPlanCurationId == floorPlanCurationId).ToArray());

        public Task UpsertAsync(FloorPlanArtifactClassification classification, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item =>
                item.FloorPlanCurationId == classification.FloorPlanCurationId &&
                item.SourceArtifactKind == classification.SourceArtifactKind &&
                item.SourceArtifactId == classification.SourceArtifactId);
            Items.Add(classification);
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
