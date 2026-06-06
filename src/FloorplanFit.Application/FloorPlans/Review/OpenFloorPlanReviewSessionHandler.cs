using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Review;

public sealed class OpenFloorPlanReviewSessionHandler
{
    private readonly IFloorPlanTemplateRepository floorPlanTemplateRepository;
    private readonly IFloorPlanVersionRepository? floorPlanVersionRepository;
    private readonly IFloorPlanReviewSessionReader reviewSessionReader;
    private readonly StartOrResumeCurationHandler startOrResumeCurationHandler;

    public OpenFloorPlanReviewSessionHandler(
        IFloorPlanTemplateRepository floorPlanTemplateRepository,
        IFloorPlanReviewSessionReader reviewSessionReader,
        StartOrResumeCurationHandler startOrResumeCurationHandler)
        : this(
            floorPlanTemplateRepository,
            floorPlanVersionRepository: null,
            reviewSessionReader,
            startOrResumeCurationHandler)
    {
    }

    public OpenFloorPlanReviewSessionHandler(
        IFloorPlanTemplateRepository floorPlanTemplateRepository,
        IFloorPlanVersionRepository? floorPlanVersionRepository,
        IFloorPlanReviewSessionReader reviewSessionReader,
        StartOrResumeCurationHandler startOrResumeCurationHandler)
    {
        this.floorPlanTemplateRepository = floorPlanTemplateRepository;
        this.floorPlanVersionRepository = floorPlanVersionRepository;
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

        var publishedSession = await reviewSessionReader.GetByTemplateAsync(templateId, cancellationToken);
        if (publishedSession?.ActivePublishedCurationId is not null)
        {
            return new OpenFloorPlanReviewSessionResponse(Guid.Empty, publishedSession);
        }

        var draft = await startOrResumeCurationHandler.HandleAsync(template.CurrentVersionId.Value, cancellationToken);
        var session = await reviewSessionReader.GetByTemplateAsync(templateId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan review session was not found.");

        return new OpenFloorPlanReviewSessionResponse(draft.Id, session);
    }

    public async Task<OpenFloorPlanReviewSessionResponse> HandleAsync(
        Guid templateId,
        Guid floorPlanVersionId,
        CancellationToken cancellationToken)
    {
        var template = await floorPlanTemplateRepository.GetByIdAsync(templateId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan template was not found.");
        if (floorPlanVersionRepository is null)
        {
            throw new InvalidOperationException("Floor plan version repository was not configured.");
        }

        var version = await floorPlanVersionRepository.GetByIdAsync(floorPlanVersionId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan version was not found.");

        if (version.FloorPlanTemplateId != template.Id)
        {
            throw new InvalidOperationException("Floor plan version does not belong to the selected template.");
        }

        var publishedSession = await reviewSessionReader.GetByVersionAsync(templateId, floorPlanVersionId, cancellationToken);
        if (publishedSession?.ActivePublishedCurationId is not null)
        {
            return new OpenFloorPlanReviewSessionResponse(Guid.Empty, publishedSession);
        }

        var draft = await startOrResumeCurationHandler.HandleAsync(floorPlanVersionId, cancellationToken);
        var session = await reviewSessionReader.GetByVersionAsync(templateId, floorPlanVersionId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan review session was not found.");

        return new OpenFloorPlanReviewSessionResponse(draft.Id, session);
    }
}
