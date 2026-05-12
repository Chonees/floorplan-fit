using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class RestoreFloorPlanLabelTextHeightHandler
{
    private readonly IFloorPlanCurationRepository curationRepository;
    private readonly IFloorPlanLabelOverrideRepository labelOverrideRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;

    public RestoreFloorPlanLabelTextHeightHandler(
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
        CancellationToken cancellationToken)
    {
        var curation = await curationRepository.GetByIdAsync(curationId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan curation was not found.");
        if (curation.Status != FloorPlanCurationStatus.Draft)
        {
            throw new InvalidOperationException("Only draft curations can be edited.");
        }

        await labelOverrideRepository.UpsertAsync(
            FloorPlanLabelOverride.CreateDetectedDefault(
                curationId,
                sourceArtifactKind,
                sourceArtifactId,
                clock.UtcNow),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
