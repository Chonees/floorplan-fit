using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.PlanSets.Library;

public sealed class ResolvePlanSetVersionHandler
{
    private readonly IPlanSetVersionRepository repository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;

    public ResolvePlanSetVersionHandler(
        IPlanSetVersionRepository repository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
    }

    public async Task<ResolvePlanSetVersionResponse> HandleAsync(
        ResolvePlanSetVersionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.HousePlanSetId == Guid.Empty)
        {
            throw new ArgumentException("House plan set is required.", nameof(request));
        }

        if (request.CanonicalFloorPlanVersionId == Guid.Empty)
        {
            throw new ArgumentException("Canonical floor-plan version is required.", nameof(request));
        }

        var existing = await repository.GetByCanonicalFloorPlanVersionAsync(
            request.CanonicalFloorPlanVersionId,
            cancellationToken);
        if (existing is not null)
        {
            return ToResponse(existing, created: false);
        }

        var version = new PlanSetVersion(
            Guid.NewGuid(),
            request.HousePlanSetId,
            request.CanonicalFloorPlanVersionId,
            versionNumber: 1,
            clock.UtcNow);

        await repository.AddAsync(version, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(version, created: true);
    }

    private static ResolvePlanSetVersionResponse ToResponse(
        PlanSetVersion version,
        bool created)
        => new(
            version.Id,
            version.HousePlanSetId,
            version.CanonicalFloorPlanVersionId,
            version.VersionNumber,
            created);
}
