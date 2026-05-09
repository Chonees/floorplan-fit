using FloorplanFit.Application.Abstractions;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class RemoveFixedPlanComponentHandler
{
    private readonly IExtractedFixedPlanComponentRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public RemoveFixedPlanComponentHandler(IExtractedFixedPlanComponentRepository repository, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(Guid fixedPlanComponentId, CancellationToken cancellationToken)
    {
        await repository.RemoveAsync(fixedPlanComponentId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
