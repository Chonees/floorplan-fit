using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Library;

public sealed class GetFloorPlanLibraryHandler
{
    private readonly IFloorPlanLibraryReader libraryReader;

    public GetFloorPlanLibraryHandler(IFloorPlanLibraryReader libraryReader)
    {
        this.libraryReader = libraryReader;
    }

    public Task<IReadOnlyList<FloorPlanLibraryItemDto>> HandleAsync(CancellationToken cancellationToken)
    {
        return libraryReader.ListAsync(cancellationToken);
    }
}
