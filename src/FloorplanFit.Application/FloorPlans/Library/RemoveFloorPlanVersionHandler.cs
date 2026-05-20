using FloorplanFit.Application.Abstractions;

namespace FloorplanFit.Application.FloorPlans.Library;

public sealed class RemoveFloorPlanVersionHandler
{
    private readonly IFloorPlanVersionRepository floorPlanVersionRepository;
    private readonly IUnitOfWork unitOfWork;

    public RemoveFloorPlanVersionHandler(
        IFloorPlanVersionRepository floorPlanVersionRepository,
        IUnitOfWork unitOfWork)
    {
        this.floorPlanVersionRepository = floorPlanVersionRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
    {
        var version = await floorPlanVersionRepository.GetByIdAsync(floorPlanVersionId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan version was not found.");

        await floorPlanVersionRepository.RemoveAsync(version.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
