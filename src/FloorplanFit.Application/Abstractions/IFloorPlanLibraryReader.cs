using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IFloorPlanLibraryReader
{
    Task<IReadOnlyList<FloorPlanLibraryItemDto>> ListAsync(CancellationToken cancellationToken);
}
