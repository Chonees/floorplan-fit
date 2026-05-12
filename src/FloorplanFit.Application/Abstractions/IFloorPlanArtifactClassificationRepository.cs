using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IFloorPlanArtifactClassificationRepository
{
    Task<IReadOnlyList<FloorPlanArtifactClassification>> ListByCurationAsync(Guid floorPlanCurationId, CancellationToken cancellationToken);

    Task UpsertAsync(FloorPlanArtifactClassification classification, CancellationToken cancellationToken);
}
