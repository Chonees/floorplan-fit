using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Review;

public sealed class GetFloorPlanReviewSessionHandler
{
    private readonly IFloorPlanReviewSessionReader reviewSessionReader;

    public GetFloorPlanReviewSessionHandler(IFloorPlanReviewSessionReader reviewSessionReader)
    {
        this.reviewSessionReader = reviewSessionReader;
    }

    public Task<FloorPlanReviewSessionDto?> HandleAsync(Guid templateId, CancellationToken cancellationToken)
    {
        return reviewSessionReader.GetByTemplateAsync(templateId, cancellationToken);
    }

    public Task<FloorPlanReviewSessionDto?> HandleAsync(
        Guid templateId,
        Guid floorPlanVersionId,
        CancellationToken cancellationToken)
    {
        return reviewSessionReader.GetByVersionAsync(templateId, floorPlanVersionId, cancellationToken);
    }
}
