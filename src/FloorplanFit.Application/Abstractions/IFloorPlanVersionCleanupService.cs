namespace FloorplanFit.Application.Abstractions;

public interface IFloorPlanVersionCleanupService
{
    Task<FloorPlanVersionCleanupResult> CleanupAsync(CancellationToken cancellationToken);
}

public sealed record FloorPlanVersionCleanupResult(int VersionsRemoved);
