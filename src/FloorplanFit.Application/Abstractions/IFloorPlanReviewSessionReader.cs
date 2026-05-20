using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IFloorPlanReviewSessionReader
{
    Task<FloorPlanReviewSessionDto?> GetByTemplateAsync(Guid templateId, CancellationToken cancellationToken);

    Task<FloorPlanReviewSessionDto?> GetByVersionAsync(
        Guid templateId,
        Guid floorPlanVersionId,
        CancellationToken cancellationToken);
}
