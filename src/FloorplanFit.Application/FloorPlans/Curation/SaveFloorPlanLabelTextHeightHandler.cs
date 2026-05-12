using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class SaveFloorPlanLabelTextHeightHandler
{
    private readonly IFloorPlanCurationRepository curationRepository;
    private readonly IFloorPlanLabelOverrideRepository labelOverrideRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;

    public SaveFloorPlanLabelTextHeightHandler(
        IFloorPlanCurationRepository curationRepository,
        IFloorPlanLabelOverrideRepository labelOverrideRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        this.curationRepository = curationRepository;
        this.labelOverrideRepository = labelOverrideRepository;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
    }

    public async Task HandleAsync(
        Guid curationId,
        string sourceArtifactKind,
        Guid sourceArtifactId,
        decimal resolvedTextHeight,
        CancellationToken cancellationToken)
    {
        var curation = await curationRepository.GetByIdAsync(curationId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan curation was not found.");
        if (curation.Status != FloorPlanCurationStatus.Draft)
        {
            throw new InvalidOperationException("Only draft curations can be edited.");
        }

        await labelOverrideRepository.UpsertAsync(
            FloorPlanLabelOverride.CreateResolvedTextHeight(
                curationId,
                sourceArtifactKind,
                sourceArtifactId,
                resolvedTextHeight,
                clock.UtcNow),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
