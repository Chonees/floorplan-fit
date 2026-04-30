using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IFloorPlanVersionRepository
{
    Task<int> GetNextVersionNumberAsync(Guid floorPlanTemplateId, CancellationToken cancellationToken);

    Task AddAsync(FloorPlanVersion version, CancellationToken cancellationToken);
}
