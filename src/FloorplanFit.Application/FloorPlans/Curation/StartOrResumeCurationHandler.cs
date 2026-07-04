using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class StartOrResumeCurationHandler
{
    private readonly IFloorPlanCurationRepository floorPlanCurationRepository;
    private readonly IClock clock;
    private readonly IUnitOfWork unitOfWork;

    public StartOrResumeCurationHandler(
        IFloorPlanCurationRepository floorPlanCurationRepository,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        this.floorPlanCurationRepository = floorPlanCurationRepository;
        this.clock = clock;
        this.unitOfWork = unitOfWork;
    }

    public async Task<FloorPlanCuration> HandleAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
    {
        var existingDraft = await floorPlanCurationRepository.GetDraftAsync(floorPlanVersionId, cancellationToken);
        if (existingDraft is not null)
        {
            return existingDraft;
        }

        var published = await floorPlanCurationRepository.GetPublishedAsync(floorPlanVersionId, cancellationToken);
        var nextVersion = await floorPlanCurationRepository.GetNextCurationVersionAsync(floorPlanVersionId, cancellationToken);

        var draft = new FloorPlanCuration(
            Guid.NewGuid(),
            floorPlanVersionId,
            nextVersion,
            FloorPlanCurationStatus.Draft,
            published?.Id,
            null,
            clock.UtcNow,
            null);

        await floorPlanCurationRepository.AddAsync(draft, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return draft;
    }
}
