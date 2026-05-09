using FloorplanFit.Application.Abstractions;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class RemoveProtectedDetailAssemblyHandler
{
    private readonly IExtractedProtectedDetailAssemblyRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public RemoveProtectedDetailAssemblyHandler(
        IExtractedProtectedDetailAssemblyRepository repository,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(Guid protectedDetailAssemblyId, CancellationToken cancellationToken)
    {
        await repository.RemoveAsync(protectedDetailAssemblyId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
