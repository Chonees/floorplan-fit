using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.PlanSets.Library;

public sealed record UnlinkPlanSheetRequest(Guid SheetId);

public sealed class UnlinkPlanSheetHandler
{
    private readonly IPlanSheetRepository planSheetRepository;
    private readonly ISheetRegistrationRepository registrationRepository;
    private readonly ISheetAdjustmentProjectionRepository projectionRepository;
    private readonly IUnitOfWork unitOfWork;

    public UnlinkPlanSheetHandler(
        IPlanSheetRepository planSheetRepository,
        ISheetRegistrationRepository registrationRepository,
        ISheetAdjustmentProjectionRepository projectionRepository,
        IUnitOfWork unitOfWork)
    {
        this.planSheetRepository = planSheetRepository;
        this.registrationRepository = registrationRepository;
        this.projectionRepository = projectionRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(
        UnlinkPlanSheetRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sheet = await planSheetRepository.GetByIdAsync(request.SheetId, cancellationToken)
            ?? throw new InvalidOperationException("Plan sheet was not found.");

        if (sheet.SheetType == PlanSheetType.FloorPlan)
        {
            throw new InvalidOperationException(
                "The canonical floor-plan sheet cannot be unlinked.");
        }

        await projectionRepository.RemoveByDependentSheetIdAsync(sheet.Id, cancellationToken);
        await registrationRepository.RemoveByDependentSheetIdAsync(sheet.Id, cancellationToken);
        await planSheetRepository.RemoveAsync(sheet.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
