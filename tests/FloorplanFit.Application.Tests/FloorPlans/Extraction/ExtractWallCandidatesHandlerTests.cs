using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Extraction;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Extraction;

public sealed class ExtractWallCandidatesHandlerTests
{
    [Fact]
    public async Task HandleAsync_persists_accepted_candidates_and_returns_extracted_status()
    {
        var versionId = Guid.NewGuid();
        var extractor = new FakeWallExtractor(
        [
            new DetectedWallCandidate(
                "LINE:12",
                "A-WALL",
                [new GeometryPoint(0, 0), new GeometryPoint(1200, 0)],
                ThicknessMm: 101.6m,
                Confidence: 0.95m,
                DetectionNotes: null)
        ]);
        var runs = new InMemoryWallExtractionRunRepository();
        var candidates = new InMemoryExtractedWallCandidateRepository();
        var roomLabels = new InMemoryExtractedRoomLabelRepository();
        var openings = new InMemoryExtractedOpeningCandidateRepository();
        var openingLabels = new InMemoryExtractedOpeningLabelRepository();
        var fixedComponents = new InMemoryExtractedFixedPlanComponentRepository();
        var protectedDetails = new InMemoryExtractedProtectedDetailAssemblyRepository();
        var unitOfWork = new FakeUnitOfWork();

        var handler = new ExtractWallCandidatesHandler(
            extractor,
            new FakeRoomLabelExtractor(
            [
                new DetectedRoomLabel(
                    "TEXT:1",
                    "ROOM LBLS",
                    "KITCHEN",
                    125m,
                    784m,
                    0.95m,
                    "Detected from ROOM LBLS text entity.")
            ]),
            new FakeOpeningExtractor(new DetectedOpeningExtraction(
                [
                    new DetectedOpeningCandidate(
                        "LINE:1",
                        "DOORS",
                        "Door",
                        "LINE",
                        [new GeometryPoint(0m, 0m), new GeometryPoint(36m, 0m)],
                        0.95m,
                        "Detected from DOORS line entity.")
                ],
                [
                    new DetectedOpeningLabel(
                        "TEXT:1",
                        "DOORTEXT",
                        "Door",
                        "2668",
                        10m,
                        12m,
                        0.95m,
                        "Detected from DOORTEXT text entity.",
                        "TEXT",
                        3.5m,
                        90m,
                        "TEXT1")
                ])),
            new FakeFixedPlanComponentExtractor(
            [
                new DetectedFixedPlanComponent(
                    "INSERT:1",
                    "FIXTURES",
                    "Toilet",
                    "INSERT",
                    "TOILET1",
                    [
                        [new GeometryPoint(0m, 0m), new GeometryPoint(12m, 0m)],
                        [new GeometryPoint(0m, 4m), new GeometryPoint(12m, 4m)]
                    ],
                    0.95m,
                    "Detected from TOILET1 block insert.",
                    "#FF7F7F7F")
            ]),
            new FakeProtectedDetailAssemblyExtractor(
            [
                new DetectedProtectedDetailAssembly(
                    "DETAIL:MISC:1",
                    "MISC",
                    "WetAreaDetail",
                    "DETAIL-GROUP",
                    [
                        [new GeometryPoint(416m, 585m), new GeometryPoint(468m, 585m)],
                        [new GeometryPoint(468m, 606m), new GeometryPoint(472m, 606m)]
                    ],
                    0.90m,
                    "Detected from MISC protected detail geometry.",
                    "#FF00FF00")
            ]),
            runs,
            candidates,
            roomLabels,
            openings,
            openingLabels,
            fixedComponents,
            protectedDetails,
            unitOfWork,
            new FakeClock(new DateTime(2026, 4, 30, 20, 0, 0, DateTimeKind.Utc)));

        var result = await handler.HandleAsync(versionId, @"C:\managed\SANTA-BARBARA.dxf", CancellationToken.None);

        Assert.Equal("Extracted", result.Status);
        var run = Assert.Single(runs.Items);
        var candidate = Assert.Single(candidates.Items);
        Assert.Equal(versionId, run.FloorPlanVersionId);
        Assert.Equal(run.Id, candidate.WallExtractionRunId);
        Assert.Equal(ExtractedWallCandidateStatus.Accepted, candidate.Status);
        var roomLabel = Assert.Single(roomLabels.Items);
        Assert.Equal(run.Id, roomLabel.WallExtractionRunId);
        Assert.Equal("KITCHEN", roomLabel.Text);
        var opening = Assert.Single(openings.Items);
        Assert.Equal(run.Id, opening.WallExtractionRunId);
        Assert.Equal("Door", opening.Kind);
        var openingLabel = Assert.Single(openingLabels.Items);
        Assert.Equal(run.Id, openingLabel.WallExtractionRunId);
        Assert.Equal("2668", openingLabel.Text);
        var fixedComponent = Assert.Single(fixedComponents.Items);
        Assert.Equal(run.Id, fixedComponent.WallExtractionRunId);
        Assert.Equal("Toilet", fixedComponent.Kind);
        Assert.Equal("TOILET1", fixedComponent.SourceBlockName);
        Assert.Equal("#FF7F7F7F", fixedComponent.ColorArgb);
        var protectedDetail = Assert.Single(protectedDetails.Items);
        Assert.Equal(run.Id, protectedDetail.WallExtractionRunId);
        Assert.Equal("WetAreaDetail", protectedDetail.Kind);
        Assert.Equal("DETAIL-GROUP", protectedDetail.SourceEntityKind);
        Assert.Equal("#FF00FF00", protectedDetail.ColorArgb);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    private sealed class FakeWallExtractor : IWallExtractor
    {
        private readonly IReadOnlyList<DetectedWallCandidate> candidates;

        public FakeWallExtractor(IReadOnlyList<DetectedWallCandidate> candidates)
        {
            this.candidates = candidates;
        }

        public Task<IReadOnlyList<DetectedWallCandidate>> ExtractAsync(string managedFilePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(candidates);
        }
    }

    private sealed class FakeRoomLabelExtractor : IRoomLabelExtractor
    {
        private readonly IReadOnlyList<DetectedRoomLabel> labels;

        public FakeRoomLabelExtractor(IReadOnlyList<DetectedRoomLabel> labels)
        {
            this.labels = labels;
        }

        public Task<IReadOnlyList<DetectedRoomLabel>> ExtractAsync(string managedFilePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(labels);
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
        private readonly IReadOnlyList<DetectedFixedPlanComponent> components;

        public FakeFixedPlanComponentExtractor(IReadOnlyList<DetectedFixedPlanComponent> components)
        {
            this.components = components;
        }

        public Task<IReadOnlyList<DetectedFixedPlanComponent>> ExtractAsync(string managedFilePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(components);
        }
    }

    private sealed class FakeProtectedDetailAssemblyExtractor : IProtectedDetailAssemblyExtractor
    {
        private readonly IReadOnlyList<DetectedProtectedDetailAssembly> assemblies;

        public FakeProtectedDetailAssemblyExtractor(IReadOnlyList<DetectedProtectedDetailAssembly> assemblies)
        {
            this.assemblies = assemblies;
        }

        public Task<IReadOnlyList<DetectedProtectedDetailAssembly>> ExtractAsync(string managedFilePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(assemblies);
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
        public List<ExtractedOpeningCandidate> Items { get; } = [];

        public Task AddRangeAsync(
            IReadOnlyList<ExtractedOpeningCandidate> domainCandidates,
            IReadOnlyList<DetectedOpeningCandidate> detectedCandidates,
            CancellationToken cancellationToken)
        {
            Items.AddRange(domainCandidates);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ExtractedOpeningCandidate>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ExtractedOpeningCandidate>>(
                Items.Where(item => item.WallExtractionRunId == wallExtractionRunId).ToArray());
        }

        public Task RemoveAsync(Guid openingCandidateId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.Id == openingCandidateId);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryExtractedOpeningLabelRepository : IExtractedOpeningLabelRepository
    {
        public List<ExtractedOpeningLabel> Items { get; } = [];

        public Task AddRangeAsync(IReadOnlyList<ExtractedOpeningLabel> labels, CancellationToken cancellationToken)
        {
            Items.AddRange(labels);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ExtractedOpeningLabel>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ExtractedOpeningLabel>>(
                Items.Where(item => item.WallExtractionRunId == wallExtractionRunId).ToArray());
        }

        public Task RemoveAsync(Guid openingLabelId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.Id == openingLabelId);
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

    private sealed class InMemoryExtractedWallCandidateRepository : IExtractedWallCandidateRepository
    {
        public List<ExtractedWallCandidate> Items { get; } = [];

        public Task AddRangeAsync(
            IReadOnlyList<ExtractedWallCandidate> domainCandidates,
            IReadOnlyList<DetectedWallCandidate> detectedCandidates,
            CancellationToken cancellationToken)
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
            var index = Items.FindIndex(item => item.Id == candidate.Id);
            if (index >= 0)
            {
                Items[index] = candidate;
            }

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

