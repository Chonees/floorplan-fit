using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IFloorPlanArtifactPositionRepository
{
    Task<IReadOnlyList<FloorPlanArtifactPosition>> ListByCurationAsync(Guid floorPlanCurationId, CancellationToken cancellationToken);

    Task UpsertAsync(FloorPlanArtifactPosition position, CancellationToken cancellationToken);
}
