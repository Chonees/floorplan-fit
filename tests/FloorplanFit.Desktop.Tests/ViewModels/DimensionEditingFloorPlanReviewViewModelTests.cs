using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls;
using FloorplanFit.Desktop.ViewModels;
using FloorplanFit.Domain.Documents;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FloorplanFit.Desktop.Tests.ViewModels;

public sealed class DimensionEditingFloorPlanReviewViewModelTests
{
    [Fact]
    public async Task SaveEditedDimensionAsync_persists_snapshot_and_keeps_dimension_selected()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var dimension = CreateDimension();
        var session = CreateSession(templateId, dimension);
        var repository = new InMemoryFloorPlanDimensionOverrideRepository();
        var services = BuildServices(template, session, repository, new FakeExportAdjustedDxfHandler());

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);
        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.SelectDimension(dimension.DimensionId);

        await viewModel.SaveEditedDimensionAsync(
            new FloorPlanPreviewControl.DimensionEditedEventArgs(
                dimension.DimensionId,
                dimension.SourceHandle ?? dimension.SourceEntityRef,
                dimension with { DisplayText = "11'-0\"", RenderTextX = 180m, IsEdited = true, IsDirty = true }),
            CancellationToken.None);

        var saved = Assert.Single(repository.Items);
        Assert.Equal("AB12", saved.SourceDimensionKey);
        Assert.Equal("11'-0\"", saved.DisplayText);
        Assert.Equal(dimension.DimensionId, viewModel.SelectedDimension?.DimensionId);
    }

    [Fact]
    public async Task ExportAdjustedDxfAsync_runs_handler_refreshes_review_and_reports_status()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var dimension = CreateDimension() with { IsEdited = true, IsDirty = true };
        var session = CreateSession(templateId, dimension);
        var exportHandler = new FakeExportAdjustedDxfHandler();
        var services = BuildServices(template, session, new InMemoryFloorPlanDimensionOverrideRepository(), exportHandler);

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);
        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.SelectDimension(dimension.DimensionId);

        await viewModel.ExportAdjustedDxfAsync(CancellationToken.None);

        Assert.Single(exportHandler.Calls);
        Assert.Contains("adjusted DXF", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadAsync_surfaces_dimension_association_summary_for_selected_dimension()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var dimension = CreateDimension();
        var association = new DimensionAssociationDto(
            dimension.DimensionId,
            "MeasuredEndpointAnchors",
            true,
            1m,
            "Resolved both dimension endpoints to measurable-edge anchors.")
        {
            StartAnchor = new DimensionAnchorReferenceDto(
                "WallCandidate:wall-a:path-a:1",
                FloorPlanArtifactSourceKinds.WallCandidate,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Start",
                100m,
                100m,
                0m),
            EndAnchor = new DimensionAnchorReferenceDto(
                "OpeningCandidate:opening-b:path-b:1",
                FloorPlanArtifactSourceKinds.OpeningCandidate,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Start",
                224m,
                100m,
                0m)
        };
        var session = CreateSession(templateId, dimension, [association]);
        var services = BuildServices(template, session, new InMemoryFloorPlanDimensionOverrideRepository(), new FakeExportAdjustedDxfHandler());

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);
        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.SelectDimension(dimension.DimensionId);

        Assert.Contains("Association: WallCandidate -> OpeningCandidate", viewModel.SelectedArtifactDetails, StringComparison.Ordinal);
        Assert.Contains("assoc: start=WallCandidate:wall-a:path-a:1/Start", viewModel.SelectedArtifactPositionSummary, StringComparison.Ordinal);
    }

    private static ServiceCollection BuildServices(
        FloorPlanTemplate template,
        FloorPlanReviewSessionDto session,
        InMemoryFloorPlanDimensionOverrideRepository dimensionOverrideRepository,
        FakeExportAdjustedDxfHandler exportHandler)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 11, 23, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(session));
        services.AddSingleton<IFloorPlanDimensionOverrideRepository>(dimensionOverrideRepository);
        services.AddSingleton<ExportAdjustedDxfHandler>(exportHandler);
        services.AddTransient<SaveFloorPlanDimensionOverrideHandler>();
        services.AddTransient<RestoreFloorPlanDimensionOverrideHandler>();
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();
        return services;
    }

    private static FloorPlanReviewSessionDto CreateSession(
        Guid templateId,
        DimensionDto dimension,
        IReadOnlyList<DimensionAssociationDto>? associations = null)
    {
        return new FloorPlanReviewSessionDto(
            templateId,
            "seminole2000",
            "SEMINOLE2000",
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
            [],
            [])
        {
            Dimensions = [dimension],
            DimensionAssociations = associations ?? []
        };
    }

    private static DimensionDto CreateDimension()
    {
        return new DimensionDto(
            Guid.NewGuid(),
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
            1)
        {
            SourceHandle = "AB12",
            RenderTextX = 162m,
            RenderTextY = 148m,
            RenderTextHeight = 3.5m,
            RenderTextStyleName = "ARCH",
            RenderTextAttachmentPoint = "MiddleCenter",
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto("LINE-1", 1, 100m, 140m, 100m, 100m),
                new DimensionLinePrimitiveDto("LINE-2", 2, 224m, 140m, 224m, 100m),
                new DimensionLinePrimitiveDto("LINE-3", 3, 100m, 140m, 224m, 140m)
            ],
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto("TEXT-1", 1, "10'-4\"", 162m, 148m, 3.5m, 0m)
                {
                    StyleName = "ARCH",
                    AttachmentPoint = "MiddleCenter"
                }
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("INSERT-1", 1, "_Dot", 100m, 140m, 0m),
                new DimensionInsertPrimitiveDto("INSERT-2", 2, "_Dot", 224m, 140m, 0m)
            ]
        };
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

    private sealed class InMemoryFloorPlanDimensionOverrideRepository : IFloorPlanDimensionOverrideRepository
    {
        public List<FloorPlanDimensionOverride> Items { get; } = [];

        public Task<IReadOnlyList<FloorPlanDimensionOverride>> ListByCurationAsync(Guid floorPlanCurationId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<FloorPlanDimensionOverride>>(Items.Where(item => item.FloorPlanCurationId == floorPlanCurationId).ToArray());

        public Task UpsertAsync(FloorPlanDimensionOverride dimensionOverride, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item =>
                item.FloorPlanCurationId == dimensionOverride.FloorPlanCurationId &&
                string.Equals(item.SourceDimensionKey, dimensionOverride.SourceDimensionKey, StringComparison.Ordinal));
            Items.Add(dimensionOverride);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid floorPlanCurationId, string sourceDimensionKey, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item =>
                item.FloorPlanCurationId == floorPlanCurationId &&
                string.Equals(item.SourceDimensionKey, sourceDimensionKey, StringComparison.Ordinal));
            return Task.CompletedTask;
        }

        public Task MarkExportedAsync(Guid floorPlanCurationId, IReadOnlyList<string> sourceDimensionKeys, DateTime exportedAtUtc, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeExportAdjustedDxfHandler : ExportAdjustedDxfHandler
    {
        public FakeExportAdjustedDxfHandler()
            : base(
                new StubExtractionSourceReader(),
                new StubReviewReader(),
                new StubDimensionOverrideRepository(),
                new StubImportedDocumentRepository(),
                new StubManagedFileStorage(),
                new StubAdjustedDxfExporter(),
                new StubFileHashService(),
                new FakeUnitOfWork(),
                new FakeClock(DateTime.UtcNow))
        {
        }

        public List<(Guid TemplateId, Guid? VersionId, Guid CurationId)> Calls { get; } = [];

        public override Task<ExportAdjustedDxfResponse> HandleAsync(Guid templateId, Guid? floorPlanVersionId, Guid curationId, CancellationToken cancellationToken)
        {
            Calls.Add((templateId, floorPlanVersionId, curationId));
            return Task.FromResult(new ExportAdjustedDxfResponse(Guid.NewGuid(), @"C:\workspace\library\adjusted-dxf\SEMINOLE2000-adjusted.dxf", 1));
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

    private sealed class StubExtractionSourceReader : IFloorPlanExtractionSourceReader
    {
        public Task<FloorPlanExtractionSource?> GetCurrentSourceAsync(Guid templateId, CancellationToken cancellationToken) => Task.FromResult<FloorPlanExtractionSource?>(null);

        public Task<FloorPlanExtractionSource?> GetByVersionAsync(Guid floorPlanVersionId, CancellationToken cancellationToken) => Task.FromResult<FloorPlanExtractionSource?>(null);
    }

    private sealed class StubReviewReader : IFloorPlanReviewSessionReader
    {
        public Task<FloorPlanReviewSessionDto?> GetByTemplateAsync(Guid templateId, CancellationToken cancellationToken) => Task.FromResult<FloorPlanReviewSessionDto?>(null);

        public Task<FloorPlanReviewSessionDto?> GetByVersionAsync(Guid templateId, Guid floorPlanVersionId, CancellationToken cancellationToken) => Task.FromResult<FloorPlanReviewSessionDto?>(null);
    }

    private sealed class StubDimensionOverrideRepository : IFloorPlanDimensionOverrideRepository
    {
        public Task<IReadOnlyList<FloorPlanDimensionOverride>> ListByCurationAsync(Guid floorPlanCurationId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<FloorPlanDimensionOverride>>([]);

        public Task UpsertAsync(FloorPlanDimensionOverride dimensionOverride, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task DeleteAsync(Guid floorPlanCurationId, string sourceDimensionKey, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task MarkExportedAsync(Guid floorPlanCurationId, IReadOnlyList<string> sourceDimensionKeys, DateTime exportedAtUtc, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class StubImportedDocumentRepository : IImportedDocumentRepository
    {
        public Task AddAsync(ImportedDocument document, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class StubManagedFileStorage : IManagedFileStorage
    {
        public Task<string> CopyIntoLibraryAsync(string sourceFilePath, CancellationToken cancellationToken) => Task.FromResult(string.Empty);

        public Task<string> ReserveAdjustedDxfPathAsync(string sourceFileName, CancellationToken cancellationToken) => Task.FromResult(string.Empty);
    }

    private sealed class StubAdjustedDxfExporter : IAdjustedDxfExporter
    {
        public Task ExportAsync(string sourceFilePath, string outputFilePath, IReadOnlyList<DimensionDto> dimensions, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class StubFileHashService : IFileHashService
    {
        public Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken) => Task.FromResult(string.Empty);
    }
}
