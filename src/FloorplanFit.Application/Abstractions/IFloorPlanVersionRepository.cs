using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IFloorPlanVersionRepository
{
    Task<FloorPlanVersion?> GetByIdAsync(Guid floorPlanVersionId, CancellationToken cancellationToken);

    Task<int> GetNextVersionNumberAsync(Guid floorPlanTemplateId, CancellationToken cancellationToken);

    Task AddAsync(FloorPlanVersion version, CancellationToken cancellationToken);

    Task RemoveAsync(Guid floorPlanVersionId, CancellationToken cancellationToken);
}
