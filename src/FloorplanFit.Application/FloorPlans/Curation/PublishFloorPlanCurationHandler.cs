using FloorplanFit.Application.Abstractions;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class PublishFloorPlanCurationHandler
{
    private readonly IFloorPlanCurationRepository floorPlanCurationRepository;
    private readonly ICuratedWallRepository curatedWallRepository;
    private readonly IFloorPlanTemplateRepository floorPlanTemplateRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;

    public PublishFloorPlanCurationHandler(
        IFloorPlanCurationRepository floorPlanCurationRepository,
        ICuratedWallRepository curatedWallRepository,
        IFloorPlanTemplateRepository floorPlanTemplateRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        this.floorPlanCurationRepository = floorPlanCurationRepository;
        this.curatedWallRepository = curatedWallRepository;
        this.floorPlanTemplateRepository = floorPlanTemplateRepository;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
    }

    public async Task HandleAsync(Guid templateId, Guid curationId, CancellationToken cancellationToken)
    {
        var template = await floorPlanTemplateRepository.GetByIdAsync(templateId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan template was not found.");

        if (template.CurrentVersionId is null)
        {
            throw new InvalidOperationException("Floor plan template does not have an active version.");
        }

        var curation = await floorPlanCurationRepository.GetDraftAsync(template.CurrentVersionId.Value, cancellationToken)
            ?? throw new InvalidOperationException("Draft curation was not found.");
        var walls = await curatedWallRepository.ListByCurationAsync(curationId, cancellationToken);

        if (walls.Count == 0 || walls.Any(item => string.IsNullOrWhiteSpace(item.StableWallId)))
        {
            throw new InvalidOperationException("A curation must have at least one fully identified curated wall before publish.");
        }

        curation.Publish(clock.UtcNow);
        template.SetActivePublishedCuration(curation.Id);

        await floorPlanCurationRepository.UpdateAsync(curation, cancellationToken);
        await floorPlanTemplateRepository.UpdateAsync(template, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
