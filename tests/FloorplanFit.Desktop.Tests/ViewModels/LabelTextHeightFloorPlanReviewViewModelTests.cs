using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.ViewModels;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FloorplanFit.Desktop.Tests.ViewModels;

public sealed class LabelTextHeightFloorPlanReviewViewModelTests
{
    [Fact]
    public async Task SaveSelectedLabelTextHeightAsync_persists_manual_room_label_height()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var labelId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var session = new FloorPlanReviewSessionDto(
            templateId,
            "seminole2000",
            "SEMINOLE2000",
            "Curated Draft",
            1,
            null,
            [],
            [new RoomLabelDto(labelId, "TEXT:1", "ROOM LBLS", "KITCHEN", 240m, 180m, 0.95m, null, 1, TextHeight: 8m, DetectedTextHeight: 8m)],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            []);
        var overrideRepository = new InMemoryFloorPlanLabelOverrideRepository();
        var services = BuildServices(template, session, overrideRepository);

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);
        await viewModel.LoadAsync(CancellationToken.None);

        viewModel.SelectRoomLabel(labelId);
        viewModel.EditableSelectedLabelTextHeight = "6";
        await viewModel.SaveSelectedLabelTextHeightAsync(CancellationToken.None);

        var saved = Assert.Single(overrideRepository.Items);
        Assert.Equal(FloorPlanLabelOverrideSourceKinds.RoomLabel, saved.SourceArtifactKind);
        Assert.Equal(labelId, saved.SourceArtifactId);
        Assert.Equal(6m, saved.ResolvedTextHeight);
        Assert.Equal("KITCHEN", viewModel.SelectedRoomLabel?.Text);
    }

    [Fact]
    public async Task RestoreSelectedLabelTextHeightAsync_writes_detected_default_row()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var labelId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var session = new FloorPlanReviewSessionDto(
            templateId,
            "seminole2000",
            "SEMINOLE2000",
            "Curated Draft",
            1,
            null,
            [],
            [],
            [],
            [new OpeningLabelDto(labelId, "TEXT:2", "DOORTEXT", "Door", "2668", 600m, 120m, 0.95m, null, 1, TextHeight: 3m, HasManualTextHeight: true, DetectedTextHeight: 5m)],
            [],
            [],
            [],
            [],
            [],
            []);
        var overrideRepository = new InMemoryFloorPlanLabelOverrideRepository();
        var services = BuildServices(template, session, overrideRepository);

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);
        await viewModel.LoadAsync(CancellationToken.None);

        viewModel.SelectOpeningLabel(labelId);
        await viewModel.RestoreSelectedLabelTextHeightAsync(CancellationToken.None);

        var saved = Assert.Single(overrideRepository.Items);
        Assert.Equal(FloorPlanLabelOverrideSourceKinds.OpeningLabel, saved.SourceArtifactKind);
        Assert.Equal(labelId, saved.SourceArtifactId);
        Assert.Null(saved.ResolvedTextHeight);
    }

    [Fact]
    public async Task Selecting_and_clearing_room_label_syncs_and_clears_text_height_editor()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var labelId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var session = new FloorPlanReviewSessionDto(
            templateId,
            "seminole2000",
            "SEMINOLE2000",
            "Curated Draft",
            1,
            null,
            [],
            [new RoomLabelDto(labelId, "TEXT:7", "ROOM LBLS", "KITCHEN", 240m, 180m, 0.95m, null, 1, TextHeight: 7.5m, DetectedTextHeight: 7.5m)],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            []);
        var services = BuildServices(template, session, new InMemoryFloorPlanLabelOverrideRepository());

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);
        await viewModel.LoadAsync(CancellationToken.None);

        viewModel.SelectRoomLabel(labelId);
        Assert.Equal("7.5", viewModel.EditableSelectedLabelTextHeight);

        viewModel.SelectedRoomLabel = null;
        Assert.Equal(string.Empty, viewModel.EditableSelectedLabelTextHeight);
    }

    private static ServiceCollection BuildServices(
        FloorPlanTemplate template,
        FloorPlanReviewSessionDto session,
        InMemoryFloorPlanLabelOverrideRepository overrideRepository)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 12, 0, 10, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(session));
        services.AddSingleton<IFloorPlanLabelOverrideRepository>(overrideRepository);
        services.AddTransient<SaveFloorPlanLabelTextHeightHandler>();
        services.AddTransient<RestoreFloorPlanLabelTextHeightHandler>();
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

    private sealed class InMemoryFloorPlanLabelOverrideRepository : IFloorPlanLabelOverrideRepository
    {
        public List<FloorPlanLabelOverride> Items { get; } = [];

        public Task<IReadOnlyList<FloorPlanLabelOverride>> ListByCurationAsync(Guid floorPlanCurationId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<FloorPlanLabelOverride>>(Items.Where(item => item.FloorPlanCurationId == floorPlanCurationId).ToArray());
        }

        public Task UpsertAsync(FloorPlanLabelOverride labelOverride, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item =>
                item.FloorPlanCurationId == labelOverride.FloorPlanCurationId &&
                item.SourceArtifactKind == labelOverride.SourceArtifactKind &&
                item.SourceArtifactId == labelOverride.SourceArtifactId);
            Items.Add(labelOverride);
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
