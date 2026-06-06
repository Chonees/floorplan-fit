namespace FloorplanFit.Application.Abstractions;

public interface IFloorPlanCurationDataCloneService
{
    Task EnsureClonedAsync(
        Guid sourceCurationId,
        Guid destinationCurationId,
        CancellationToken cancellationToken);
}
