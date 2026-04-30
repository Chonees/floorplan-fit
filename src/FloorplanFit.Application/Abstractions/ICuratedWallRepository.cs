using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface ICuratedWallRepository
{
    Task AddAsync(CuratedWall wall, CancellationToken cancellationToken);

    Task<CuratedWall?> GetByIdAsync(Guid curatedWallId, CancellationToken cancellationToken);

    Task<IReadOnlyList<CuratedWall>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken);

    Task UpdateAsync(CuratedWall wall, CancellationToken cancellationToken);
}
