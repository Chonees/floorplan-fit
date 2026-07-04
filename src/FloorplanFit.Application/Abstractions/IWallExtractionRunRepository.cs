using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IWallExtractionRunRepository
{
    Task AddAsync(WallExtractionRun run, CancellationToken cancellationToken);

    Task<WallExtractionRun?> GetLatestByVersionAsync(Guid floorPlanVersionId, CancellationToken cancellationToken);
}
