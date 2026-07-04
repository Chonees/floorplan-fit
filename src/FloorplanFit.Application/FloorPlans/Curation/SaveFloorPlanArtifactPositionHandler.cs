using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class SaveFloorPlanArtifactPositionHandler
{
    private readonly IFloorPlanCurationRepository curationRepository;
    private readonly IFloorPlanArtifactPositionRepository positionRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;

    public SaveFloorPlanArtifactPositionHandler(
        IFloorPlanCurationRepository curationRepository,
        IFloorPlanArtifactPositionRepository positionRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        this.curationRepository = curationRepository;
        this.positionRepository = positionRepository;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
    }

    public async Task HandleAsync(
        Guid curationId,
        string sourceArtifactKind,
        Guid sourceArtifactId,
        FloorPlanArtifactPositionMode positionMode,
        decimal? resolvedX,
        decimal? resolvedY,
        decimal? translationDx,
        decimal? translationDy,
        CancellationToken cancellationToken)
    {
        var curation = await curationRepository.GetByIdAsync(curationId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan curation was not found.");
        if (curation.Status != FloorPlanCurationStatus.Draft)
        {
            throw new InvalidOperationException("Only draft curations can be edited.");
        }

        var position = positionMode switch
        {
            FloorPlanArtifactPositionMode.AbsolutePoint => FloorPlanArtifactPosition.CreateAbsolutePoint(
                curationId,
                sourceArtifactKind,
                sourceArtifactId,
                resolvedX ?? throw new InvalidOperationException("Resolved X is required for AbsolutePoint mode."),
                resolvedY ?? throw new InvalidOperationException("Resolved Y is required for AbsolutePoint mode."),
                clock.UtcNow),
            FloorPlanArtifactPositionMode.Translation => FloorPlanArtifactPosition.CreateTranslation(
                curationId,
                sourceArtifactKind,
                sourceArtifactId,
                translationDx ?? throw new InvalidOperationException("Translation dx is required for Translation mode."),
                translationDy ?? throw new InvalidOperationException("Translation dy is required for Translation mode."),
                clock.UtcNow),
            _ => throw new InvalidOperationException("Unsupported artifact position mode.")
        };

        await positionRepository.UpsertAsync(position, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
