using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.PlanSets.Registration;

public sealed class RegisterElectricalSheetHandler
{
    private readonly IPlanSheetRepository planSheetRepository;
    private readonly ISheetRegistrationRepository sheetRegistrationRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;

    public RegisterElectricalSheetHandler(
        IPlanSheetRepository planSheetRepository,
        ISheetRegistrationRepository sheetRegistrationRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        this.planSheetRepository = planSheetRepository;
        this.sheetRegistrationRepository = sheetRegistrationRepository;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
    }

    public async Task<SheetRegistrationDto> HandleAsync(
        RegisterElectricalSheetRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PlanSetVersionId == Guid.Empty)
        {
            throw new ArgumentException("Plan set version is required.", nameof(request));
        }

        if (request.ElectricalSheetId == Guid.Empty)
        {
            throw new ArgumentException("Electrical sheet is required.", nameof(request));
        }

        var sheet = await planSheetRepository.GetByIdAsync(request.ElectricalSheetId, cancellationToken);
        if (sheet is null)
        {
            throw new InvalidOperationException("Dependent sheet was not found.");
        }

        if (sheet.PlanSetVersionId != request.PlanSetVersionId)
        {
            throw new ArgumentException("Electrical sheet does not belong to the requested plan set version.", nameof(request));
        }

        if (sheet.SheetType is not PlanSheetType.ElectricalPlan)
        {
            throw new ArgumentException("Only electrical sheets can use electrical registration.", nameof(request));
        }

        var createdAtUtc = clock.UtcNow;
        DateTime? confirmedAtUtc = request.ConfirmRegistration ? createdAtUtc : null;
        var registration = new SheetRegistration(
            Guid.NewGuid(),
            request.PlanSetVersionId,
            request.ElectricalSheetId,
            request.PlanSetVersionId,
            SheetRegistrationMethod.WholeSheetSimilarity,
            new SheetRegistrationTransform(
                request.Scale,
                request.RotationDegrees,
                request.TranslateX,
                request.TranslateY),
            request.Confidence,
            request.ConfirmRegistration
                ? SheetRegistrationStatus.Confirmed
                : SheetRegistrationStatus.PendingConfirmation,
            createdAtUtc,
            confirmedAtUtc,
            request.Warning);

        await sheetRegistrationRepository.AddAsync(registration, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(registration);
    }

    private static SheetRegistrationDto ToDto(SheetRegistration registration)
    {
        return new SheetRegistrationDto(
            registration.Id,
            registration.PlanSetVersionId,
            registration.DependentSheetId,
            registration.CanonicalFloorPlanVersionId,
            registration.Method.ToString(),
            new SheetRegistrationTransformDto(
                registration.Transform.Scale,
                registration.Transform.RotationDegrees,
                registration.Transform.TranslateX,
                registration.Transform.TranslateY),
            registration.Confidence,
            registration.Status.ToString(),
            registration.Warning,
            registration.CreatedAtUtc,
            registration.ConfirmedAtUtc);
    }
}
