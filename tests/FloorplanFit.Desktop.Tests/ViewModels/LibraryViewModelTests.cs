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
    private static FloorPlanLibraryItemDto CreateLibraryItem(Guid templateId, Guid versionId, string status)
    {
        return new FloorPlanLibraryItemDto(
            templateId,
            "santa-barbara",
            "SANTA-BARBARA",
            VersionCount: 1,
            CurrentVersionId: versionId,
            CurrentVersionNumber: 1,
            Versions:
            [
                new FloorPlanLibraryVersionDto(
                    versionId,
                    VersionNumber: 1,
                    status,
                    new DateTime(2026, 4, 30, 18, 0, 0, DateTimeKind.Utc),
                    "inch",
                    IsCurrent: true)
            ]);
    }

    [Fact]
    public async Task ExtractSelectedAsync_uses_the_current_version_source_and_refreshes_the_library()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var extractionSource = new FloorPlanExtractionSource(templateId, versionId, @"C:\workspace\library\raw-dxf\SANTA-BARBARA.dxf");
        var unitOfWork = new FakeUnitOfWork();
        var runRepository = new InMemoryWallExtractionRunRepository();
        var candidateRepository = new InMemoryExtractedWallCandidateRepository();
        var roomLabelRepository = new InMemoryExtractedRoomLabelRepository();
        var fixedPlanComponentRepository = new InMemoryExtractedFixedPlanComponentRepository();
        var protectedDetailRepository = new InMemoryExtractedProtectedDetailAssemblyRepository();
        var dimensionRepository = new InMemoryExtractedDimensionRepository();

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
        services.AddSingleton<IRoomLabelExtractor>(new FakeRoomLabelExtractor(
        [
            new DetectedRoomLabel(
                "TEXT:1",
                "ROOM LBLS",
                "KITCHEN",
                10m,
                20m,
                0.95m,
                null)
        ]));
        services.AddSingleton<IOpeningExtractor>(new FakeOpeningExtractor(new DetectedOpeningExtraction([], [])));
        services.AddSingleton<IFixedPlanComponentExtractor>(new FakeFixedPlanComponentExtractor(
        [
            new DetectedFixedPlanComponent(
                "INSERT:1",
                "FIXTURES",
                "Toilet",
                "INSERT",
                "TOILET1",
                [[new GeometryPoint(40m, 10m), new GeometryPoint(76m, 10m)]],
                0.95m,
                null)
        ]));
        services.AddSingleton<IProtectedDetailAssemblyExtractor>(new FakeProtectedDetailAssemblyExtractor([]));
        services.AddSingleton<IDimensionExtractor>(new FakeDimensionExtractor(
        [
            new DetectedDimension(
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
                null)
        ]));
        services.AddSingleton<IWallExtractionRunRepository>(runRepository);
        services.AddSingleton<IExtractedWallCandidateRepository>(candidateRepository);
        services.AddSingleton<IExtractedRoomLabelRepository>(roomLabelRepository);
        services.AddSingleton<IExtractedOpeningCandidateRepository>(new InMemoryExtractedOpeningCandidateRepository());
        services.AddSingleton<IExtractedOpeningLabelRepository>(new InMemoryExtractedOpeningLabelRepository());
        services.AddSingleton<IExtractedFixedPlanComponentRepository>(fixedPlanComponentRepository);
        services.AddSingleton<IExtractedProtectedDetailAssemblyRepository>(protectedDetailRepository);
        services.AddSingleton<IExtractedDimensionRepository>(dimensionRepository);
        services.AddSingleton<IUnitOfWork>(unitOfWork);
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 4, 30, 19, 30, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanLibraryReader>(new FakeFloorPlanLibraryReader(
        [
            CreateLibraryItem(templateId, versionId, "Extracted")
        ]));
        services.AddTransient<ExtractWallCandidatesHandler>();
        services.AddTransient<GetFloorPlanLibraryHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new LibraryViewModel(provider.GetRequiredService<IServiceScopeFactory>())
        {
            SelectedItem = CreateLibraryItem(templateId, versionId, "Imported")
        };

        await viewModel.ExtractSelectedAsync(CancellationToken.None);

        Assert.True(unitOfWork.SaveChangesCalled);
        Assert.Single(runRepository.Items);
        Assert.Single(candidateRepository.Items);
        Assert.Single(roomLabelRepository.Items);
        Assert.Single(fixedPlanComponentRepository.Items);
        Assert.Single(dimensionRepository.Items);
        Assert.Single(viewModel.Items);
        Assert.Equal("Extracted", viewModel.Items[0].Status);
    }

    private sealed class FakeRoomLabelExtractor : IRoomLabelExtractor
    {
        private readonly IReadOnlyList<DetectedRoomLabel> items;

        public FakeRoomLabelExtractor(IReadOnlyList<DetectedRoomLabel> items)
        {
            this.items = items;
        }

        public Task<IReadOnlyList<DetectedRoomLabel>> ExtractAsync(string managedFilePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(items);
        }
    }

    private sealed class FakeOpeningExtractor : IOpeningExtractor
    {
        private readonly DetectedOpeningExtraction extraction;

        public FakeOpeningExtractor(DetectedOpeningExtraction extraction)
        {
            this.extraction = extraction;
        }

        public Task<DetectedOpeningExtraction> ExtractAsync(string managedFilePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(extraction);
        }
    }

    private sealed class FakeFixedPlanComponentExtractor : IFixedPlanComponentExtractor
    {
        private readonly IReadOnlyList<DetectedFixedPlanComponent> items;

        public FakeFixedPlanComponentExtractor(IReadOnlyList<DetectedFixedPlanComponent> items)
        {
            this.items = items;
        }

        public Task<IReadOnlyList<DetectedFixedPlanComponent>> ExtractAsync(string managedFilePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(items);
        }
    }

    private sealed class FakeProtectedDetailAssemblyExtractor : IProtectedDetailAssemblyExtractor
    {
        private readonly IReadOnlyList<DetectedProtectedDetailAssembly> items;

        public FakeProtectedDetailAssemblyExtractor(IReadOnlyList<DetectedProtectedDetailAssembly> items)
        {
            this.items = items;
        }

        public Task<IReadOnlyList<DetectedProtectedDetailAssembly>> ExtractAsync(string managedFilePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(items);
        }
    }

    private sealed class FakeDimensionExtractor : IDimensionExtractor
    {
        private readonly IReadOnlyList<DetectedDimension> items;

        public FakeDimensionExtractor(IReadOnlyList<DetectedDimension> items)
        {
            this.items = items;
        }

        public Task<IReadOnlyList<DetectedDimension>> ExtractAsync(string managedFilePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(items);
        }
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
        services.AddSingleton<IFloorPlanVersionRepository>(new InMemoryFloorPlanVersionRepository(
            new FloorPlanVersion(
                versionId,
                templateId,
                Guid.NewGuid(),
                "fingerprint",
                1,
                new DateTime(2026, 4, 30, 17, 0, 0, DateTimeKind.Utc))));
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
        var viewModel = new LibraryViewModel(provider.GetRequiredService<IServiceScopeFactory>())
        {
            SelectedItem = CreateLibraryItem(templateId, versionId, "Extracted")
        };

        var reviewViewModel = await viewModel.OpenSelectedReviewAsync(CancellationToken.None);

        Assert.NotNull(reviewViewModel);
        Assert.Equal("santa-barbara", reviewViewModel.Code);
        Assert.Equal("SANTA-BARBARA", reviewViewModel.Name);
    }

    [Fact]
    public async Task OpenSelectedReviewAsync_reextracts_when_loaded_review_has_stale_dimension_payload()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var extractionSource = new FloorPlanExtractionSource(templateId, versionId, @"C:\workspace\library\raw-dxf\SEMINOLE2000.dxf");
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var runRepository = new InMemoryWallExtractionRunRepository();
        var unitOfWork = new FakeUnitOfWork();

        var staleSession = new FloorPlanReviewSessionDto(
            templateId,
            "seminole2000",
            "SEMINOLE2000",
            "Extracted",
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
                    Guid.NewGuid(),
                    "DIMENSION:1",
                    "DIMS",
                    "DIMENSION",
                    "*D169",
                    "5'-8\"",
                    "GeometryBlock",
                    string.Empty,
                    68m,
                    1727.2m,
                    "Inch",
                    160,
                    0m,
                    0m,
                    440m,
                    520m,
                    0m,
                    372m,
                    526m,
                    0m,
                    372m,
                    516m,
                    0m,
                    0.99m,
                    null,
                    1)
            ]
        };

        var refreshedSession = staleSession with
        {
            Dimensions =
            [
                staleSession.Dimensions[0] with
                {
                    RenderTextX = 408.8m,
                    RenderTextY = 518.9m,
                    RenderTextHeight = 3.5m,
                    RenderTextAttachmentPoint = "MiddleCenter",
                    LineSegments =
                    [
                        new DimensionLineSegmentDto(440m, 520m, 440m, 513m),
                        new DimensionLineSegmentDto(372m, 525m, 372m, 513m),
                        new DimensionLineSegmentDto(437m, 517m, 376m, 517m)
                    ]
                }
            ]
        };

        var reviewReader = new SequencedFloorPlanReviewSessionReader(staleSession, refreshedSession);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanVersionRepository>(new InMemoryFloorPlanVersionRepository(
            new FloorPlanVersion(
                versionId,
                templateId,
                Guid.NewGuid(),
                "fingerprint",
                1,
                new DateTime(2026, 4, 30, 17, 0, 0, DateTimeKind.Utc))));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IFloorPlanExtractionSourceReader>(new FakeFloorPlanExtractionSourceReader(extractionSource));
        services.AddSingleton<IWallExtractor>(new FakeWallExtractor(
        [
            new DetectedWallCandidate("LINE:1", "WALLS", [new GeometryPoint(0m, 0m), new GeometryPoint(120m, 0m)], null, 0.95m, null)
        ]));
        services.AddSingleton<IRoomLabelExtractor>(new FakeRoomLabelExtractor([]));
        services.AddSingleton<IOpeningExtractor>(new FakeOpeningExtractor(new DetectedOpeningExtraction([], [])));
        services.AddSingleton<IFixedPlanComponentExtractor>(new FakeFixedPlanComponentExtractor([]));
        services.AddSingleton<IProtectedDetailAssemblyExtractor>(new FakeProtectedDetailAssemblyExtractor([]));
        services.AddSingleton<IDimensionExtractor>(new FakeDimensionExtractor(
        [
            new DetectedDimension(
                "DIMENSION:1",
                "DIMS",
                "DIMENSION",
                "*D169",
                "5'-8\"",
                "GeometryBlock",
                string.Empty,
                68m,
                1727.2m,
                "Inch",
                160,
                0m,
                0m,
                440m,
                520m,
                0m,
                372m,
                526m,
                0m,
                372m,
                516m,
                0m,
                0.99m,
                null)
            {
                RenderTextX = 408.8m,
                RenderTextY = 518.9m,
                RenderTextHeight = 3.5m,
                RenderTextAttachmentPoint = "MiddleCenter",
                LineSegments =
                [
                    new DetectedDimensionLineSegment(440m, 520m, 440m, 513m),
                    new DetectedDimensionLineSegment(372m, 525m, 372m, 513m),
                    new DetectedDimensionLineSegment(437m, 517m, 376m, 517m)
                ]
            }
        ]));
        services.AddSingleton<IWallExtractionRunRepository>(runRepository);
        services.AddSingleton<IExtractedWallCandidateRepository>(new InMemoryExtractedWallCandidateRepository());
        services.AddSingleton<IExtractedRoomLabelRepository>(new InMemoryExtractedRoomLabelRepository());
        services.AddSingleton<IExtractedOpeningCandidateRepository>(new InMemoryExtractedOpeningCandidateRepository());
        services.AddSingleton<IExtractedOpeningLabelRepository>(new InMemoryExtractedOpeningLabelRepository());
        services.AddSingleton<IExtractedFixedPlanComponentRepository>(new InMemoryExtractedFixedPlanComponentRepository());
        services.AddSingleton<IExtractedProtectedDetailAssemblyRepository>(new InMemoryExtractedProtectedDetailAssemblyRepository());
        services.AddSingleton<IExtractedDimensionRepository>(new InMemoryExtractedDimensionRepository());
        services.AddSingleton<IUnitOfWork>(unitOfWork);
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 4, 30, 20, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(reviewReader);
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();
        services.AddTransient<ExtractWallCandidatesHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new LibraryViewModel(provider.GetRequiredService<IServiceScopeFactory>())
        {
            SelectedItem = CreateLibraryItem(templateId, versionId, "Extracted")
        };

        var reviewViewModel = await viewModel.OpenSelectedReviewAsync(CancellationToken.None);

        Assert.NotNull(reviewViewModel);
        Assert.Equal(2, reviewReader.CallCount);
        Assert.Single(runRepository.Items);
        var dimension = Assert.Single(reviewViewModel.Dimensions);
        Assert.Equal(3, dimension.LineSegments.Count);
        Assert.Equal(408.8m, dimension.RenderTextX);
    }

    [Fact]
    public async Task DeleteSelectedVersionAsync_removes_the_selected_version_and_refreshes_the_library()
    {
        var templateId = Guid.NewGuid();
        var versionOneId = Guid.NewGuid();
        var versionTwoId = Guid.NewGuid();
        var versionRepository = new InMemoryFloorPlanVersionRepository(
            new FloorPlanVersion(
                versionOneId,
                templateId,
                Guid.NewGuid(),
                "fingerprint-one",
                1,
                new DateTime(2026, 5, 9, 12, 0, 0, DateTimeKind.Utc)),
            new FloorPlanVersion(
                versionTwoId,
                templateId,
                Guid.NewGuid(),
                "fingerprint-two",
                2,
                new DateTime(2026, 5, 9, 13, 0, 0, DateTimeKind.Utc)));
        var unitOfWork = new FakeUnitOfWork();

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanVersionRepository>(versionRepository);
        services.AddSingleton<IUnitOfWork>(unitOfWork);
        services.AddSingleton<IFloorPlanLibraryReader>(
            new RepositoryBackedFloorPlanLibraryReader(templateId, versionRepository));
        services.AddTransient<RemoveFloorPlanVersionHandler>();
        services.AddTransient<GetFloorPlanLibraryHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new LibraryViewModel(provider.GetRequiredService<IServiceScopeFactory>());
        await viewModel.LoadAsync(CancellationToken.None);

        Assert.Equal("Selected: santa-barbara v2", viewModel.SelectedVersionLabel);

        await viewModel.DeleteSelectedVersionAsync(CancellationToken.None);

        Assert.All(versionRepository.Items, item => Assert.NotEqual(versionTwoId, item.Id));
        Assert.True(unitOfWork.SaveChangesCalled);
        var remainingItem = Assert.Single(viewModel.Items);
        var remainingVersion = Assert.Single(remainingItem.Versions);
        Assert.Equal(versionOneId, remainingVersion.VersionId);
        Assert.Equal("Selected: santa-barbara v1", viewModel.SelectedVersionLabel);
        Assert.Equal("Deleted v2", viewModel.StatusMessage);
    }

    private sealed class InMemoryExtractedRoomLabelRepository : IExtractedRoomLabelRepository
    {
        public List<ExtractedRoomLabel> Items { get; } = [];

        public Task AddRangeAsync(IReadOnlyList<ExtractedRoomLabel> labels, CancellationToken cancellationToken)
        {
            Items.AddRange(labels);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ExtractedRoomLabel>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ExtractedRoomLabel>>(
                Items.Where(item => item.WallExtractionRunId == wallExtractionRunId).ToArray());
        }

        public Task RemoveAsync(Guid roomLabelId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.Id == roomLabelId);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryExtractedOpeningCandidateRepository : IExtractedOpeningCandidateRepository
    {
        public Task AddRangeAsync(
            IReadOnlyList<ExtractedOpeningCandidate> domainCandidates,
            IReadOnlyList<DetectedOpeningCandidate> detectedCandidates,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ExtractedOpeningCandidate>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ExtractedOpeningCandidate>>([]);
        }

        public Task RemoveAsync(Guid openingCandidateId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryExtractedOpeningLabelRepository : IExtractedOpeningLabelRepository
    {
        public Task AddRangeAsync(IReadOnlyList<ExtractedOpeningLabel> labels, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ExtractedOpeningLabel>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ExtractedOpeningLabel>>([]);
        }

        public Task RemoveAsync(Guid openingLabelId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryExtractedFixedPlanComponentRepository : IExtractedFixedPlanComponentRepository
    {
        public List<ExtractedFixedPlanComponent> Items { get; } = [];

        public Task AddRangeAsync(
            IReadOnlyList<ExtractedFixedPlanComponent> domainComponents,
            IReadOnlyList<DetectedFixedPlanComponent> detectedComponents,
            CancellationToken cancellationToken)
        {
            Items.AddRange(domainComponents);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ExtractedFixedPlanComponent>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ExtractedFixedPlanComponent>>(
                Items.Where(item => item.WallExtractionRunId == wallExtractionRunId).ToArray());
        }

        public Task RemoveAsync(Guid fixedPlanComponentId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.Id == fixedPlanComponentId);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryExtractedProtectedDetailAssemblyRepository : IExtractedProtectedDetailAssemblyRepository
    {
        public List<ExtractedProtectedDetailAssembly> Items { get; } = [];

        public Task AddRangeAsync(
            IReadOnlyList<ExtractedProtectedDetailAssembly> domainAssemblies,
            IReadOnlyList<DetectedProtectedDetailAssembly> detectedAssemblies,
            CancellationToken cancellationToken)
        {
            Items.AddRange(domainAssemblies);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ExtractedProtectedDetailAssembly>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ExtractedProtectedDetailAssembly>>(
                Items.Where(item => item.WallExtractionRunId == wallExtractionRunId).ToArray());
        }

        public Task RemoveAsync(Guid protectedDetailAssemblyId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.Id == protectedDetailAssemblyId);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryExtractedDimensionRepository : IExtractedDimensionRepository
    {
        public List<ExtractedDimension> Items { get; } = [];

        public Task AddRangeAsync(IReadOnlyList<ExtractedDimension> dimensions, CancellationToken cancellationToken)
        {
            Items.AddRange(dimensions);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ExtractedDimension>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ExtractedDimension>>(
                Items.Where(item => item.WallExtractionRunId == wallExtractionRunId).ToArray());
        }
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

        public Task<FloorPlanExtractionSource?> GetByVersionAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
        {
            return Task.FromResult<FloorPlanExtractionSource?>(
                source.FloorPlanVersionId == floorPlanVersionId ? source : null);
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

        public Task<FloorPlanReviewSessionDto?> GetByVersionAsync(
            Guid templateId,
            Guid floorPlanVersionId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<FloorPlanReviewSessionDto?>(session);
        }
    }

    private sealed class SequencedFloorPlanReviewSessionReader : IFloorPlanReviewSessionReader
    {
        private readonly Queue<FloorPlanReviewSessionDto> sessions;

        public SequencedFloorPlanReviewSessionReader(params FloorPlanReviewSessionDto[] sessions)
        {
            this.sessions = new Queue<FloorPlanReviewSessionDto>(sessions);
        }

        public int CallCount { get; private set; }

        public Task<FloorPlanReviewSessionDto?> GetByTemplateAsync(Guid templateId, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult<FloorPlanReviewSessionDto?>(sessions.Count > 1 ? sessions.Dequeue() : sessions.Peek());
        }

        public Task<FloorPlanReviewSessionDto?> GetByVersionAsync(
            Guid templateId,
            Guid floorPlanVersionId,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult<FloorPlanReviewSessionDto?>(sessions.Count > 1 ? sessions.Dequeue() : sessions.Peek());
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

    private sealed class InMemoryFloorPlanVersionRepository : IFloorPlanVersionRepository
    {
        public InMemoryFloorPlanVersionRepository(params FloorPlanVersion[] items)
        {
            Items = items.ToList();
        }

        public List<FloorPlanVersion> Items { get; }

        public Task<FloorPlanVersion?> GetByIdAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.SingleOrDefault(item => item.Id == floorPlanVersionId));
        }

        public Task<int> GetNextVersionNumberAsync(Guid floorPlanTemplateId, CancellationToken cancellationToken)
        {
            var nextVersion = Items
                .Where(item => item.FloorPlanTemplateId == floorPlanTemplateId)
                .Select(item => item.VersionNumber)
                .DefaultIfEmpty(0)
                .Max() + 1;

            return Task.FromResult(nextVersion);
        }

        public Task AddAsync(FloorPlanVersion version, CancellationToken cancellationToken)
        {
            Items.Add(version);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.Id == floorPlanVersionId);
            return Task.CompletedTask;
        }
    }

    private sealed class RepositoryBackedFloorPlanLibraryReader : IFloorPlanLibraryReader
    {
        private readonly Guid templateId;
        private readonly InMemoryFloorPlanVersionRepository versionRepository;

        public RepositoryBackedFloorPlanLibraryReader(
            Guid templateId,
            InMemoryFloorPlanVersionRepository versionRepository)
        {
            this.templateId = templateId;
            this.versionRepository = versionRepository;
        }

        public Task<IReadOnlyList<FloorPlanLibraryItemDto>> ListAsync(CancellationToken cancellationToken)
        {
            var versions = versionRepository.Items
                .Where(item => item.FloorPlanTemplateId == templateId)
                .OrderByDescending(item => item.VersionNumber)
                .Select((item, index) => new FloorPlanLibraryVersionDto(
                    item.Id,
                    item.VersionNumber,
                    "Imported",
                    item.CreatedAtUtc,
                    "inch",
                    IsCurrent: index == 0))
                .ToArray();

            if (versions.Length == 0)
            {
                return Task.FromResult<IReadOnlyList<FloorPlanLibraryItemDto>>([]);
            }

            return Task.FromResult<IReadOnlyList<FloorPlanLibraryItemDto>>(
            [
                new FloorPlanLibraryItemDto(
                    templateId,
                    "santa-barbara",
                    "SANTA-BARBARA",
                    versions.Length,
                    versions[0].VersionId,
                    versions[0].VersionNumber,
                    versions)
            ]);
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
