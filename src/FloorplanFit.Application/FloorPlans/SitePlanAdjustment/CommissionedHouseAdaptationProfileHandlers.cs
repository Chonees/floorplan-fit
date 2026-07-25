using FloorplanFit.Application.Abstractions;

namespace FloorplanFit.Application.FloorPlans.SitePlanAdjustment;

public sealed class SaveCommissionedHouseAdaptationProfileHandler
{
    private readonly ICommissionedHouseAdaptationProfileRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public SaveCommissionedHouseAdaptationProfileHandler(
        ICommissionedHouseAdaptationProfileRepository repository,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task<CommissionedHouseAdaptationReadinessResult> HandleAsync(
        CommissionedHouseAdaptationProfile profile,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var readiness = CommissionedHouseAdaptationProfileReadiness.Evaluate(profile);
        if (!readiness.IsReady)
        {
            throw new InvalidOperationException(
                $"House adaptation profile is not Auto-fit ready: {string.Join("; ", readiness.Reasons)}");
        }

        await repository.UpsertAsync(profile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return readiness;
    }
}

public sealed class GetCommissionedHouseAdaptationReadinessHandler
{
    private readonly ICommissionedHouseAdaptationProfileRepository repository;

    public GetCommissionedHouseAdaptationReadinessHandler(
        ICommissionedHouseAdaptationProfileRepository repository)
    {
        this.repository = repository;
    }

    public async Task<CommissionedHouseAdaptationReadinessResult> HandleAsync(
        Guid floorPlanVersionId,
        Guid expectedPublishedCurationId,
        CancellationToken cancellationToken)
    {
        if (floorPlanVersionId == Guid.Empty)
        {
            throw new ArgumentException("Floor-plan version id is required.", nameof(floorPlanVersionId));
        }

        var profile = await repository.GetByFloorPlanVersionIdAsync(
            floorPlanVersionId,
            expectedPublishedCurationId,
            cancellationToken);
        if (profile is not null && profile.PublishedCurationId != expectedPublishedCurationId)
        {
            profile = null;
        }

        return CommissionedHouseAdaptationProfileReadiness.Evaluate(profile);
    }
}

public sealed record GetCommissionedHouseAdaptationProfileResult(
    CommissionedHouseAdaptationProfile? Profile,
    CommissionedHouseAdaptationReadinessResult Readiness);

public sealed class GetCommissionedHouseAdaptationProfileHandler
{
    private readonly ICommissionedHouseAdaptationProfileRepository repository;

    public GetCommissionedHouseAdaptationProfileHandler(
        ICommissionedHouseAdaptationProfileRepository repository)
    {
        this.repository = repository;
    }

    public async Task<GetCommissionedHouseAdaptationProfileResult> HandleAsync(
        Guid floorPlanVersionId,
        Guid expectedPublishedCurationId,
        CancellationToken cancellationToken)
    {
        if (floorPlanVersionId == Guid.Empty)
        {
            throw new ArgumentException("Floor-plan version id is required.", nameof(floorPlanVersionId));
        }

        var profile = await repository.GetByFloorPlanVersionIdAsync(
            floorPlanVersionId,
            expectedPublishedCurationId,
            cancellationToken);
        if (profile is not null && profile.PublishedCurationId != expectedPublishedCurationId)
        {
            profile = null;
        }

        return new(
            profile,
            CommissionedHouseAdaptationProfileReadiness.Evaluate(profile));
    }
}
