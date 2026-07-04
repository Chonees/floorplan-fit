using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.PlanSets.Library;

public sealed class ResolveHousePlanSetHandler
{
    private readonly IHousePlanSetRepository repository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;

    public ResolveHousePlanSetHandler(
        IHousePlanSetRepository repository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
    }

    public async Task<ResolveHousePlanSetResponse> HandleAsync(
        ResolveHousePlanSetRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.SourceFloorPlanTemplateId == Guid.Empty)
        {
            throw new ArgumentException("Source floor-plan template is required.", nameof(request));
        }

        var existing = await repository.GetBySourceFloorPlanTemplateAsync(
            request.SourceFloorPlanTemplateId,
            cancellationToken);
        if (existing is not null)
        {
            return ToResponse(existing, created: false);
        }

        var housePlanSet = new HousePlanSet(
            Guid.NewGuid(),
            request.SourceFloorPlanTemplateId,
            request.Code,
            request.Name,
            clock.UtcNow);

        await repository.AddAsync(housePlanSet, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(housePlanSet, created: true);
    }

    private static ResolveHousePlanSetResponse ToResponse(
        HousePlanSet housePlanSet,
        bool created)
        => new(
            housePlanSet.Id,
            housePlanSet.SourceFloorPlanTemplateId,
            housePlanSet.Code,
            housePlanSet.Name,
            created);
}
