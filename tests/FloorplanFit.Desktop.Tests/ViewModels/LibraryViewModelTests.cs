using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Application.FloorPlans.Extraction;
using FloorplanFit.Application.FloorPlans.Library;
using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.ViewModels;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FloorplanFit.Desktop.Tests.ViewModels;

public sealed class LibraryViewModelTests
{
    [Fact]
    public async Task ExtractSelectedAsync_uses_the_current_version_source_and_refreshes_the_library()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var extractionSource = new FloorPlanExtractionSource(templateId, versionId, @"C:\workspace\library\raw-dxf\SANTA-BARBARA.dxf");
        var unitOfWork = new FakeUnitOfWork();
        var runRepository = new InMemoryWallExtractionRunRepository();
        var candidateRepository = new InMemoryExtractedWallCandidateRepository();

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanExtractionSourceReader>(new FakeFloorPlanExtractionSourceReader(extractionSource));
        services.AddSingleton<IWallExtractor>(new FakeWallExtractor(
        [
            new DetectedWallCandidate(
                "LINE:1",
                "WALLS",
                [new GeometryPoint(0m, 0m), new GeometryPoint(120m, 0m)],
                null,
                0.95m,
                null)
        ]));
        services.AddSingleton<IWallExtractionRunRepository>(runRepository);
        services.AddSingleton<IExtractedWallCandidateRepository>(candidateRepository);
        services.AddSingleton<IUnitOfWork>(unitOfWork);
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 4, 30, 19, 30, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanLibraryReader>(new FakeFloorPlanLibraryReader(
        [
            new FloorPlanLibraryItemDto(
                templateId,
                "santa-barbara",
                "SANTA-BARBARA",
                "Extracted",
                1,
                new DateTime(2026, 4, 30, 18, 0, 0, DateTimeKind.Utc),
                "inch")
        ]));
        services.AddTransient<ExtractWallCandidatesHandler>();
        services.AddTransient<GetFloorPlanLibraryHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new LibraryViewModel(provider.GetRequiredService<IServiceScopeFactory>())
        {
            SelectedItem = new FloorPlanLibraryItemDto(
                templateId,
                "santa-barbara",
                "SANTA-BARBARA",
                "Imported",
                1,
                new DateTime(2026, 4, 30, 18, 0, 0, DateTimeKind.Utc),
                "inch")
        };

        await viewModel.ExtractSelectedAsync(CancellationToken.None);

        Assert.True(unitOfWork.SaveChangesCalled);
        Assert.Single(runRepository.Items);
        Assert.Single(candidateRepository.Items);
        Assert.Single(viewModel.Items);
        Assert.Equal("Extracted", viewModel.Items[0].Status);
    }

    [Fact]
    public async Task OpenSelectedReviewAsync_returns_a_loaded_review_view_model()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "santa-barbara", "SANTA-BARBARA", isActive: true);
        template.SetCurrentVersion(versionId);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 4, 30, 20, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(
            new FloorPlanReviewSessionDto(
                templateId,
                "santa-barbara",
                "SANTA-BARBARA",
                "Extracted",
                1,
                null,
                [],
                [],
                [])));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new LibraryViewModel(provider.GetRequiredService<IServiceScopeFactory>())
        {
            SelectedItem = new FloorPlanLibraryItemDto(
                templateId,
                "santa-barbara",
                "SANTA-BARBARA",
                "Extracted",
                1,
                new DateTime(2026, 4, 30, 18, 0, 0, DateTimeKind.Utc),
                "inch")
        };

        var reviewViewModel = await viewModel.OpenSelectedReviewAsync(CancellationToken.None);

        Assert.NotNull(reviewViewModel);
        Assert.Equal("santa-barbara", reviewViewModel.Code);
        Assert.Equal("SANTA-BARBARA", reviewViewModel.Name);
    }

    private sealed class FakeFloorPlanExtractionSourceReader : IFloorPlanExtractionSourceReader
    {
        private readonly FloorPlanExtractionSource source;

        public FakeFloorPlanExtractionSourceReader(FloorPlanExtractionSource source)
        {
            this.source = source;
        }

        public Task<FloorPlanExtractionSource?> GetCurrentSourceAsync(Guid templateId, CancellationToken cancellationToken)
        {
            return Task.FromResult<FloorPlanExtractionSource?>(source);
        }
    }

    private sealed class FakeWallExtractor : IWallExtractor
    {
        private readonly IReadOnlyList<DetectedWallCandidate> items;

        public FakeWallExtractor(IReadOnlyList<DetectedWallCandidate> items)
        {
            this.items = items;
        }

        public Task<IReadOnlyList<DetectedWallCandidate>> ExtractAsync(string managedFilePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(items);
        }
    }

    private sealed class InMemoryWallExtractionRunRepository : IWallExtractionRunRepository
    {
        public List<WallExtractionRun> Items { get; } = [];

        public Task AddAsync(WallExtractionRun run, CancellationToken cancellationToken)
        {
            Items.Add(run);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryExtractedWallCandidateRepository : IExtractedWallCandidateRepository
    {
        public List<ExtractedWallCandidate> Items { get; } = [];

        public Task AddRangeAsync(IReadOnlyList<ExtractedWallCandidate> domainCandidates, IReadOnlyList<DetectedWallCandidate> detectedCandidates, CancellationToken cancellationToken)
        {
            Items.AddRange(domainCandidates);
            return Task.CompletedTask;
        }

        public Task<ExtractedWallCandidate?> GetByIdAsync(Guid candidateId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.SingleOrDefault(item => item.Id == candidateId));
        }

        public Task UpdateAsync(ExtractedWallCandidate candidate, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeFloorPlanLibraryReader : IFloorPlanLibraryReader
    {
        private readonly IReadOnlyList<FloorPlanLibraryItemDto> items;

        public FakeFloorPlanLibraryReader(IReadOnlyList<FloorPlanLibraryItemDto> items)
        {
            this.items = items;
        }

        public Task<IReadOnlyList<FloorPlanLibraryItemDto>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(items);
        }
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
        public bool SaveChangesCalled { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalled = true;
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
