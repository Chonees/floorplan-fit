using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls;
using FloorplanFit.Desktop.ViewModels;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FloorplanFit.Desktop.Tests.ViewModels;

public sealed class MovableArtifactFloorPlanReviewViewModelTests
{
    [Fact]
    public async Task SaveMovedArtifactPositionAsync_persists_canonical_translation_and_keeps_curated_selection()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var artifactId = Guid.NewGuid();
        var pathId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var session = new FloorPlanReviewSessionDto(
            templateId,
            "seminole2000",
            "SEMINOLE2000",
            "Curated Draft",
            1,
            null,
            [new GeometryPathDto(pathId, false, [new GeometrySegmentDto(pathId, 1, 40m, 0m, 76m, 0m)])],
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
                    artifactId,
                    FloorPlanArtifactSourceKinds.OpeningCandidate,
                    "LINE:DOOR:1",
                    "DOORS",
                    "LINE",
                    null,
                    [pathId],
                    0.95m,
                    null,
                    1,
                    FloorPlanArtifactTaxonomy.OpeningFamily,
                    FloorPlanArtifactTaxonomy.OpeningCategory,
                    FloorPlanArtifactTaxonomy.DoorType,
                    FloorPlanArtifactTaxonomy.OpeningFamily,
                    FloorPlanArtifactTaxonomy.OpeningCategory,
                    FloorPlanArtifactTaxonomy.DoorType,
                    FloorPlanArtifactDecisionState.DetectedDefault.ToString(),
                    "#FF455668")
            ]);
        var positionRepository = new InMemoryFloorPlanArtifactPositionRepository();
        var services = BuildServices(template, session, positionRepository);

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);
        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.SelectPreviewPath(pathId);

        await viewModel.SaveMovedArtifactPositionAsync(
            new FloorPlanPreviewControl.MovableArtifactMovedEventArgs(
                FloorPlanArtifactSourceKinds.OpeningCandidate,
                artifactId,
                FloorPlanArtifactPositionMode.Translation,
                null,
                null,
                24m,
                -12m),
            CancellationToken.None);

        var saved = Assert.Single(positionRepository.Items);
        Assert.Equal(FloorPlanArtifactSourceKinds.OpeningCandidate, saved.SourceArtifactKind);
        Assert.Equal(artifactId, saved.SourceArtifactId);
        Assert.Equal(FloorPlanArtifactPositionMode.Translation, saved.PositionMode);
        Assert.Equal(24m, saved.TranslationDx);
        Assert.Equal(-12m, saved.TranslationDy);
        Assert.Equal("LINE:DOOR:1", viewModel.SelectedCuratedArtifact?.SourceEntityRef);
    }

    [Fact]
    public async Task RestoreSelectedArtifactPositionAsync_uses_detected_room_label_coordinates_or_zero_translation()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var labelId = Guid.NewGuid();
        var artifactId = Guid.NewGuid();
        var pathId = Guid.NewGuid();
        var session = new FloorPlanReviewSessionDto(
            templateId,
            "seminole2000",
            "SEMINOLE2000",
            "Curated Draft",
            1,
            null,
            [new GeometryPathDto(pathId, false, [new GeometrySegmentDto(pathId, 1, 40m, 0m, 76m, 0m)])],
            [new RoomLabelDto(labelId, "TEXT:1", "ROOM LBLS", "KITCHEN", 280m, 156m, 0.95m, null, 1, HasManualPosition: true, DetectedX: 240m, DetectedY: 180m)],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [
                new CuratedPlanArtifactDto(
                    artifactId,
                    FloorPlanArtifactSourceKinds.OpeningCandidate,
                    "LINE:DOOR:1",
                    "DOORS",
                    "LINE",
                    null,
                    [pathId],
                    0.95m,
                    null,
                    1,
                    FloorPlanArtifactTaxonomy.OpeningFamily,
                    FloorPlanArtifactTaxonomy.OpeningCategory,
                    FloorPlanArtifactTaxonomy.DoorType,
                    FloorPlanArtifactTaxonomy.OpeningFamily,
                    FloorPlanArtifactTaxonomy.OpeningCategory,
                    FloorPlanArtifactTaxonomy.DoorType,
                    FloorPlanArtifactDecisionState.DetectedDefault.ToString(),
                    "#FF455668",
                    HasManualPosition: true,
                    TranslationDx: 24m,
                    TranslationDy: -12m)
            ]);
        var positionRepository = new InMemoryFloorPlanArtifactPositionRepository();
        var services = BuildServices(template, session, positionRepository);

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);
        await viewModel.LoadAsync(CancellationToken.None);

        viewModel.SelectRoomLabel(labelId);
        await viewModel.RestoreSelectedArtifactPositionAsync(CancellationToken.None);
        var restoredLabel = Assert.Single(positionRepository.Items);
        Assert.Equal(FloorPlanArtifactPositionSourceKinds.RoomLabel, restoredLabel.SourceArtifactKind);
        Assert.Equal(FloorPlanArtifactPositionMode.AbsolutePoint, restoredLabel.PositionMode);
        Assert.Equal(240m, restoredLabel.ResolvedX);
        Assert.Equal(180m, restoredLabel.ResolvedY);

        viewModel.SelectPreviewPath(pathId);
        await viewModel.RestoreSelectedArtifactPositionAsync(CancellationToken.None);
        Assert.Equal(2, positionRepository.Items.Count);
        var restoredGeometry = positionRepository.Items.Last();
        Assert.Equal(FloorPlanArtifactSourceKinds.OpeningCandidate, restoredGeometry.SourceArtifactKind);
        Assert.Equal(FloorPlanArtifactPositionMode.Translation, restoredGeometry.PositionMode);
        Assert.Equal(0m, restoredGeometry.TranslationDx);
        Assert.Equal(0m, restoredGeometry.TranslationDy);
    }

    private static ServiceCollection BuildServices(
        FloorPlanTemplate template,
        FloorPlanReviewSessionDto session,
        InMemoryFloorPlanArtifactPositionRepository positionRepository)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 11, 23, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(session));
        services.AddSingleton<IFloorPlanArtifactPositionRepository>(positionRepository);
        services.AddTransient<SaveFloorPlanArtifactPositionHandler>();
        services.AddTransient<RestoreFloorPlanArtifactPositionHandler>();
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

    private sealed class InMemoryFloorPlanArtifactPositionRepository : IFloorPlanArtifactPositionRepository
    {
        public List<FloorPlanArtifactPosition> Items { get; } = [];

        public Task<IReadOnlyList<FloorPlanArtifactPosition>> ListByCurationAsync(Guid floorPlanCurationId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<FloorPlanArtifactPosition>>(Items.Where(item => item.FloorPlanCurationId == floorPlanCurationId).ToArray());
        }

        public Task UpsertAsync(FloorPlanArtifactPosition position, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item =>
                item.FloorPlanCurationId == position.FloorPlanCurationId &&
                item.SourceArtifactKind == position.SourceArtifactKind &&
                item.SourceArtifactId == position.SourceArtifactId);
            Items.Add(position);
            return Task.CompletedTask;
        }
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
