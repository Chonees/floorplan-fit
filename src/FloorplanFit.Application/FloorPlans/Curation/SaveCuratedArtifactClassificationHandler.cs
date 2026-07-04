using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class SaveCuratedArtifactClassificationHandler
{
    private readonly IFloorPlanCurationRepository curationRepository;
    private readonly IFloorPlanArtifactClassificationRepository classificationRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;

    public SaveCuratedArtifactClassificationHandler(
        IFloorPlanCurationRepository curationRepository,
        IFloorPlanArtifactClassificationRepository classificationRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        this.curationRepository = curationRepository;
        this.classificationRepository = classificationRepository;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
    }

    public async Task HandleAsync(
        Guid curationId,
        string sourceArtifactKind,
        Guid sourceArtifactId,
        string resolvedFamily,
        string resolvedCategory,
        string resolvedType,
        CancellationToken cancellationToken)
    {
        var curation = await curationRepository.GetByIdAsync(curationId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan curation was not found.");
        if (curation.Status != FloorPlanCurationStatus.Draft)
        {
            throw new InvalidOperationException("Only draft curations can be edited.");
        }

        if (!FloorPlanArtifactTaxonomy.IsValidClassification(resolvedFamily, resolvedCategory, resolvedType))
        {
            throw new InvalidOperationException("Resolved classification is not part of the supported taxonomy.");
        }

        await classificationRepository.UpsertAsync(
            new FloorPlanArtifactClassification(
                curationId,
                sourceArtifactKind,
                sourceArtifactId,
                resolvedFamily,
                resolvedCategory,
                resolvedType,
                FloorPlanArtifactDecisionState.Reclassified,
                clock.UtcNow),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
