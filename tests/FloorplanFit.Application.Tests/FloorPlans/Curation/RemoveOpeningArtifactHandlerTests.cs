using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class RemoveOpeningArtifactHandlerTests
{
    [Fact]
    public async Task RemoveFixedPlanComponentHandler_removes_component_and_saves_changes()
    {
        var component = new ExtractedFixedPlanComponent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "INSERT:7",
            "FIXTURES",
            "Toilet",
            "INSERT",
            "TOILET1",
            0.95m,
            null,
            1);
        var repository = new InMemoryFixedPlanComponentRepository([component]);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RemoveFixedPlanComponentHandler(repository, unitOfWork);

        await handler.HandleAsync(component.Id, CancellationToken.None);

        Assert.Empty(repository.Items);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    [Fact]
    public async Task RemoveProtectedDetailAssemblyHandler_removes_assembly_and_saves_changes()
    {
        var assembly = new ExtractedProtectedDetailAssembly(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "DETAIL:MISC:1",
            "MISC",
            "WetAreaDetail",
            "DETAIL-GROUP",
            0.90m,
            null,
            1);
        var repository = new InMemoryProtectedDetailAssemblyRepository([assembly]);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RemoveProtectedDetailAssemblyHandler(repository, unitOfWork);

        await handler.HandleAsync(assembly.Id, CancellationToken.None);

        Assert.Empty(repository.Items);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    [Fact]
    public async Task RemoveOpeningCandidateHandler_removes_candidate_and_saves_changes()
    {
        var candidate = new ExtractedOpeningCandidate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ARC:12",
            "DOORS",
            "Door",
            "ARC",
            Guid.NewGuid(),
            0.95m,
            null,
            1);
        var repository = new InMemoryOpeningCandidateRepository([candidate]);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RemoveOpeningCandidateHandler(repository, unitOfWork);

        await handler.HandleAsync(candidate.Id, CancellationToken.None);

        Assert.Empty(repository.Items);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    [Fact]
    public async Task RemoveOpeningLabelHandler_removes_label_and_saves_changes()
    {
        var label = new ExtractedOpeningLabel(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "TEXT:9",
            "DOORTEXT",
            "Door",
            "24\"DR.",
            100m,
            200m,
            0.95m,
            null,
            1);
        var repository = new InMemoryOpeningLabelRepository([label]);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RemoveOpeningLabelHandler(repository, unitOfWork);

        await handler.HandleAsync(label.Id, CancellationToken.None);

        Assert.Empty(repository.Items);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    [Fact]
    public async Task RemoveRoomLabelHandler_removes_room_label_and_saves_changes()
    {
        var label = new ExtractedRoomLabel(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "TEXT:1",
            "ROOM LBLS",
            "KITCHEN",
            125m,
            784m,
            0.95m,
            null,
            1);
        var repository = new InMemoryRoomLabelRepository([label]);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RemoveRoomLabelHandler(repository, unitOfWork);

        await handler.HandleAsync(label.Id, CancellationToken.None);

        Assert.Empty(repository.Items);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    private sealed class InMemoryOpeningCandidateRepository : IExtractedOpeningCandidateRepository
    {
        public InMemoryOpeningCandidateRepository(IReadOnlyList<ExtractedOpeningCandidate> seed)
        {
            Items = [.. seed];
        }

        public List<ExtractedOpeningCandidate> Items { get; }

        public Task AddRangeAsync(IReadOnlyList<ExtractedOpeningCandidate> domainCandidates, IReadOnlyList<DetectedOpeningCandidate> detectedCandidates, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<ExtractedOpeningCandidate>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ExtractedOpeningCandidate>>(Items.Where(item => item.WallExtractionRunId == wallExtractionRunId).ToArray());

        public Task RemoveAsync(Guid openingCandidateId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.Id == openingCandidateId);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryFixedPlanComponentRepository : IExtractedFixedPlanComponentRepository
    {
        public InMemoryFixedPlanComponentRepository(IReadOnlyList<ExtractedFixedPlanComponent> seed)
        {
            Items = [.. seed];
        }

        public List<ExtractedFixedPlanComponent> Items { get; }

        public Task AddRangeAsync(IReadOnlyList<ExtractedFixedPlanComponent> domainComponents, IReadOnlyList<DetectedFixedPlanComponent> detectedComponents, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<ExtractedFixedPlanComponent>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ExtractedFixedPlanComponent>>(Items.Where(item => item.WallExtractionRunId == wallExtractionRunId).ToArray());

        public Task RemoveAsync(Guid fixedPlanComponentId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.Id == fixedPlanComponentId);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryProtectedDetailAssemblyRepository : IExtractedProtectedDetailAssemblyRepository
    {
        public InMemoryProtectedDetailAssemblyRepository(IReadOnlyList<ExtractedProtectedDetailAssembly> seed)
        {
            Items = [.. seed];
        }

        public List<ExtractedProtectedDetailAssembly> Items { get; }

        public Task AddRangeAsync(
            IReadOnlyList<ExtractedProtectedDetailAssembly> domainAssemblies,
            IReadOnlyList<DetectedProtectedDetailAssembly> detectedAssemblies,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<ExtractedProtectedDetailAssembly>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ExtractedProtectedDetailAssembly>>(Items.Where(item => item.WallExtractionRunId == wallExtractionRunId).ToArray());

        public Task RemoveAsync(Guid protectedDetailAssemblyId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.Id == protectedDetailAssemblyId);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryOpeningLabelRepository : IExtractedOpeningLabelRepository
    {
        public InMemoryOpeningLabelRepository(IReadOnlyList<ExtractedOpeningLabel> seed)
        {
            Items = [.. seed];
        }

        public List<ExtractedOpeningLabel> Items { get; }

        public Task AddRangeAsync(IReadOnlyList<ExtractedOpeningLabel> labels, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<ExtractedOpeningLabel>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ExtractedOpeningLabel>>(Items.Where(item => item.WallExtractionRunId == wallExtractionRunId).ToArray());

        public Task RemoveAsync(Guid openingLabelId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.Id == openingLabelId);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryRoomLabelRepository : IExtractedRoomLabelRepository
    {
        public InMemoryRoomLabelRepository(IReadOnlyList<ExtractedRoomLabel> seed)
        {
            Items = [.. seed];
        }

        public List<ExtractedRoomLabel> Items { get; }

        public Task AddRangeAsync(IReadOnlyList<ExtractedRoomLabel> labels, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<ExtractedRoomLabel>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ExtractedRoomLabel>>(Items.Where(item => item.WallExtractionRunId == wallExtractionRunId).ToArray());

        public Task RemoveAsync(Guid roomLabelId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.Id == roomLabelId);
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
}
