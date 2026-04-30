using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class UpdateCuratedWallMetadataHandlerTests
{
    [Fact]
    public async Task HandleAsync_updates_the_curated_wall_metadata()
    {
        var wall = new CuratedWall(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "W-001",
            sourceCandidateId: Guid.NewGuid(),
            sourceEntityRef: "LINE:12",
            geometryPathId: Guid.NewGuid(),
            WallRole.Partition,
            WallMobilityLevel.Flexible,
            WallProtectionLevel.None,
            thicknessMm: 101.6m,
            assemblyCode: "2x4",
            heightMm: null,
            isExterior: false,
            isStructuralHint: false,
            wallGroupId: null,
            sortOrder: 1,
            notes: null);
        var repository = new InMemoryCuratedWallRepository(wall);
        var unitOfWork = new FakeUnitOfWork();

        var handler = new UpdateCuratedWallMetadataHandler(repository, unitOfWork);

        await handler.HandleAsync(
            wall.Id,
            WallRole.Exterior,
            WallMobilityLevel.Locked,
            WallProtectionLevel.Critical,
            thicknessMm: 152.4m,
            assemblyCode: "2x6",
            heightMm: 2743.2m,
            isExterior: true,
            isStructuralHint: true,
            notes: "Do not move facade.",
            CancellationToken.None);

        Assert.Equal(WallRole.Exterior, wall.WallRole);
        Assert.Equal(WallMobilityLevel.Locked, wall.MobilityLevel);
        Assert.Equal(WallProtectionLevel.Critical, wall.ProtectionLevel);
        Assert.Equal(152.4m, wall.ThicknessMm);
        Assert.Equal("2x6", wall.AssemblyCode);
        Assert.Equal(2743.2m, wall.HeightMm);
        Assert.True(wall.IsExterior);
        Assert.True(wall.IsStructuralHint);
        Assert.Equal("Do not move facade.", wall.Notes);
        Assert.True(unitOfWork.SaveChangesCalled);
        Assert.True(repository.UpdateCalled);
    }

    private sealed class InMemoryCuratedWallRepository : ICuratedWallRepository
    {
        private readonly CuratedWall wall;

        public InMemoryCuratedWallRepository(CuratedWall wall)
        {
            this.wall = wall;
        }

        public bool UpdateCalled { get; private set; }

        public Task AddAsync(CuratedWall wall, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<CuratedWall?> GetByIdAsync(Guid curatedWallId, CancellationToken cancellationToken)
            => Task.FromResult(wall.Id == curatedWallId ? wall : null);

        public Task<IReadOnlyList<CuratedWall>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<CuratedWall>>([wall]);

        public Task RemoveBySourceCandidateAsync(Guid curationId, Guid sourceCandidateId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task UpdateAsync(CuratedWall wall, CancellationToken cancellationToken)
        {
            UpdateCalled = true;
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
