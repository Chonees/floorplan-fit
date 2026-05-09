using FloorplanFit.Application.Abstractions;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class PublishFloorPlanCurationHandler
{
    private readonly IFloorPlanCurationRepository floorPlanCurationRepository;
    private readonly IPinchMarkerRepository pinchMarkerRepository;
    private readonly IFloorPlanTemplateRepository floorPlanTemplateRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;

    public PublishFloorPlanCurationHandler(
        IFloorPlanCurationRepository floorPlanCurationRepository,
        IPinchMarkerRepository pinchMarkerRepository,
        IFloorPlanTemplateRepository floorPlanTemplateRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        this.floorPlanCurationRepository = floorPlanCurationRepository;
        this.pinchMarkerRepository = pinchMarkerRepository;
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
        var pinchMarkers = await pinchMarkerRepository.ListByCurationAsync(curationId, cancellationToken);

        if (pinchMarkers.Count == 0)
        {
            throw new InvalidOperationException("A curation must contain at least one pinch marker before publish.");
        }

        curation.Publish(clock.UtcNow);
        template.SetActivePublishedCuration(curation.Id);

        await floorPlanCurationRepository.UpdateAsync(curation, cancellationToken);
        await floorPlanTemplateRepository.UpdateAsync(template, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
