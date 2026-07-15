using FloorplanFit.Application.Abstractions;

namespace FloorplanFit.Application.FloorPlans.Library;

public sealed class RemoveFloorPlanVersionHandler
{
    private readonly IFloorPlanVersionRepository floorPlanVersionRepository;
    private readonly IPlanSetVersionRepository planSetVersionRepository;
    private readonly IUnitOfWork unitOfWork;

    public RemoveFloorPlanVersionHandler(
        IFloorPlanVersionRepository floorPlanVersionRepository,
        IPlanSetVersionRepository planSetVersionRepository,
        IUnitOfWork unitOfWork)
    {
        this.floorPlanVersionRepository = floorPlanVersionRepository;
        this.planSetVersionRepository = planSetVersionRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
    {
        var version = await floorPlanVersionRepository.GetByIdAsync(floorPlanVersionId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan version was not found.");

        var planSetVersion = await planSetVersionRepository.GetByCanonicalFloorPlanVersionAsync(
            version.Id,
            cancellationToken);
        if (planSetVersion is not null)
        {
            throw new InvalidOperationException(
                "Floor plan version is the canonical source of a HousePlanSet and cannot be removed.");
        }

        await floorPlanVersionRepository.RemoveAsync(version.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
