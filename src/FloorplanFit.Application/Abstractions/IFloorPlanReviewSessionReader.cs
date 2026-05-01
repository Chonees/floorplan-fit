using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IFloorPlanReviewSessionReader
{
    Task<FloorPlanReviewSessionDto?> GetByTemplateAsync(Guid templateId, CancellationToken cancellationToken);
}
