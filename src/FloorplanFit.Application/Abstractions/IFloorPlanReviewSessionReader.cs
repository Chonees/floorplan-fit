using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IFloorPlanReviewSessionReader
{
    Task<FloorPlanReviewSessionDto?> GetByTemplateAsync(Guid templateId, CancellationToken cancellationToken);

    Task<FloorPlanReviewSessionDto?> GetByVersionAsync(
        Guid templateId,
        Guid floorPlanVersionId,
        CancellationToken cancellationToken);

    Task<FloorPlanReviewSessionDto?> GetByCurationAsync(
        Guid templateId,
        Guid curationId,
        CancellationToken cancellationToken)
    {
        return GetByTemplateAsync(templateId, cancellationToken);
    }

    Task<FloorPlanReviewSessionDto?> GetByCurationAsync(
        Guid templateId,
        Guid floorPlanVersionId,
        Guid curationId,
        CancellationToken cancellationToken)
    {
        return GetByVersionAsync(templateId, floorPlanVersionId, cancellationToken);
    }
}
