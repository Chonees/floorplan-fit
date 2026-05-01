using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Review;

public sealed class OpenFloorPlanReviewSessionHandler
{
    private readonly IFloorPlanTemplateRepository floorPlanTemplateRepository;
    private readonly IFloorPlanReviewSessionReader reviewSessionReader;
    private readonly StartOrResumeCurationHandler startOrResumeCurationHandler;

    public OpenFloorPlanReviewSessionHandler(
        IFloorPlanTemplateRepository floorPlanTemplateRepository,
        IFloorPlanReviewSessionReader reviewSessionReader,
        StartOrResumeCurationHandler startOrResumeCurationHandler)
    {
        this.floorPlanTemplateRepository = floorPlanTemplateRepository;
        this.reviewSessionReader = reviewSessionReader;
        this.startOrResumeCurationHandler = startOrResumeCurationHandler;
    }

    public async Task<OpenFloorPlanReviewSessionResponse> HandleAsync(Guid templateId, CancellationToken cancellationToken)
    {
        var template = await floorPlanTemplateRepository.GetByIdAsync(templateId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan template was not found.");

        if (template.CurrentVersionId is null)
        {
            throw new InvalidOperationException("Floor plan template does not have an active version.");
        }

        var draft = await startOrResumeCurationHandler.HandleAsync(template.CurrentVersionId.Value, cancellationToken);
        var session = await reviewSessionReader.GetByTemplateAsync(templateId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan review session was not found.");

        return new OpenFloorPlanReviewSessionResponse(draft.Id, session);
    }
}
