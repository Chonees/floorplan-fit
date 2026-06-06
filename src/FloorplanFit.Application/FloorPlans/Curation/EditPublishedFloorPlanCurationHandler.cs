using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class EditPublishedFloorPlanCurationHandler
{
    private readonly IFloorPlanTemplateRepository floorPlanTemplateRepository;
    private readonly IFloorPlanVersionRepository? floorPlanVersionRepository;
    private readonly IFloorPlanCurationRepository floorPlanCurationRepository;
    private readonly IFloorPlanCurationDataCloneService curationDataCloneService;
    private readonly IFloorPlanReviewSessionReader reviewSessionReader;
    private readonly IClock clock;
    private readonly IUnitOfWork unitOfWork;

    public EditPublishedFloorPlanCurationHandler(
        IFloorPlanTemplateRepository floorPlanTemplateRepository,
        IFloorPlanCurationRepository floorPlanCurationRepository,
        IFloorPlanCurationDataCloneService curationDataCloneService,
        IFloorPlanReviewSessionReader reviewSessionReader,
        IClock clock,
        IUnitOfWork unitOfWork)
        : this(
            floorPlanTemplateRepository,
            floorPlanVersionRepository: null,
            floorPlanCurationRepository,
            curationDataCloneService,
            reviewSessionReader,
            clock,
            unitOfWork)
    {
    }

    public EditPublishedFloorPlanCurationHandler(
        IFloorPlanTemplateRepository floorPlanTemplateRepository,
        IFloorPlanVersionRepository? floorPlanVersionRepository,
        IFloorPlanCurationRepository floorPlanCurationRepository,
        IFloorPlanCurationDataCloneService curationDataCloneService,
        IFloorPlanReviewSessionReader reviewSessionReader,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        this.floorPlanTemplateRepository = floorPlanTemplateRepository;
        this.floorPlanVersionRepository = floorPlanVersionRepository;
        this.floorPlanCurationRepository = floorPlanCurationRepository;
        this.curationDataCloneService = curationDataCloneService;
        this.reviewSessionReader = reviewSessionReader;
        this.clock = clock;
        this.unitOfWork = unitOfWork;
    }

    public async Task<OpenFloorPlanReviewSessionResponse> HandleAsync(
        Guid templateId,
        CancellationToken cancellationToken)
    {
        var template = await floorPlanTemplateRepository.GetByIdAsync(templateId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan template was not found.");

        if (template.CurrentVersionId is null)
        {
            throw new InvalidOperationException("Floor plan template does not have an active version.");
        }

        return await OpenDraftForPublishedCurationAsync(
            templateId,
            template.CurrentVersionId.Value,
            template.ActivePublishedCurationId,
            cancellationToken);
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

        return await OpenDraftForPublishedCurationAsync(
            templateId,
            floorPlanVersionId,
            template.ActivePublishedCurationId,
            cancellationToken);
    }

    private async Task<OpenFloorPlanReviewSessionResponse> OpenDraftForPublishedCurationAsync(
        Guid templateId,
        Guid floorPlanVersionId,
        Guid? activePublishedCurationId,
        CancellationToken cancellationToken)
    {
        if (activePublishedCurationId is null)
        {
            throw new InvalidOperationException("No published curation is available to edit.");
        }

        var published = await floorPlanCurationRepository.GetByIdAsync(activePublishedCurationId.Value, cancellationToken)
            ?? throw new InvalidOperationException("Published curation was not found.");
        if (published.FloorPlanVersionId != floorPlanVersionId || published.Status != FloorPlanCurationStatus.Published)
        {
            throw new InvalidOperationException("Active published curation does not belong to the selected floor plan version.");
        }

        var draft = await GetOrCreateDraftAsync(floorPlanVersionId, published.Id, cancellationToken);
        await curationDataCloneService.EnsureClonedAsync(published.Id, draft.Id, cancellationToken);

        var session = await reviewSessionReader.GetByCurationAsync(templateId, floorPlanVersionId, draft.Id, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan review session was not found.");

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new OpenFloorPlanReviewSessionResponse(draft.Id, session);
    }

    private async Task<FloorPlanCuration> GetOrCreateDraftAsync(
        Guid floorPlanVersionId,
        Guid publishedCurationId,
        CancellationToken cancellationToken)
    {
        var existingDraft = await floorPlanCurationRepository.GetDraftAsync(floorPlanVersionId, cancellationToken);
        if (existingDraft is not null)
        {
            return existingDraft;
        }

        var nextVersion = await floorPlanCurationRepository.GetNextCurationVersionAsync(floorPlanVersionId, cancellationToken);
        var draft = new FloorPlanCuration(
            Guid.NewGuid(),
            floorPlanVersionId,
            nextVersion,
            FloorPlanCurationStatus.Draft,
            publishedCurationId,
            null,
            clock.UtcNow,
            null);

        await floorPlanCurationRepository.AddAsync(draft, cancellationToken);
        return draft;
    }
}
