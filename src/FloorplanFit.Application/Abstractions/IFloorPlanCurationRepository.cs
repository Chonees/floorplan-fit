using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IFloorPlanCurationRepository
{
    Task<FloorPlanCuration?> GetByIdAsync(Guid curationId, CancellationToken cancellationToken);

    Task<FloorPlanCuration?> GetDraftAsync(Guid floorPlanVersionId, CancellationToken cancellationToken);

    Task<FloorPlanCuration?> GetPublishedAsync(Guid floorPlanVersionId, CancellationToken cancellationToken);

    Task<int> GetNextCurationVersionAsync(Guid floorPlanVersionId, CancellationToken cancellationToken);

    Task AddAsync(FloorPlanCuration curation, CancellationToken cancellationToken);

    Task UpdateAsync(FloorPlanCuration curation, CancellationToken cancellationToken);
}
