using System.Text.Json;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.PlanSets.Registration;

public sealed class RegisterFacadeElevationSheetHandler
{
    private const string DefaultHorizontalReferenceName = "GeneralFacadeDatum";

    private readonly IPlanSheetRepository planSheetRepository;
    private readonly ISheetRegistrationRepository sheetRegistrationRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;
    private readonly IPlanSetAuditEventRepository? planSetAuditEventRepository;

    public RegisterFacadeElevationSheetHandler(
        IPlanSheetRepository planSheetRepository,
        ISheetRegistrationRepository sheetRegistrationRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPlanSetAuditEventRepository? planSetAuditEventRepository = null)
    {
        this.planSheetRepository = planSheetRepository;
        this.sheetRegistrationRepository = sheetRegistrationRepository;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
        this.planSetAuditEventRepository = planSetAuditEventRepository;
    }

    public async Task<SheetRegistrationDto> HandleAsync(
        RegisterFacadeElevationSheetRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PlanSetVersionId == Guid.Empty)
        {
            throw new ArgumentException("Plan set version is required.", nameof(request));
        }

        if (request.FacadeElevationSheetId == Guid.Empty)
        {
            throw new ArgumentException("Facade/elevation sheet is required.", nameof(request));
        }

        var sheet = await planSheetRepository.GetByIdAsync(request.FacadeElevationSheetId, cancellationToken);
        if (sheet is null)
        {
            throw new InvalidOperationException("Dependent sheet was not found.");
        }

        if (sheet.PlanSetVersionId != request.PlanSetVersionId)
        {
            throw new ArgumentException("Facade/elevation sheet does not belong to the requested plan set version.", nameof(request));
        }

        if (sheet.SheetType is not PlanSheetType.FacadeElevation)
        {
            throw new ArgumentException("Only facade/elevation sheets can use facade/elevation registration.", nameof(request));
        }

        var createdAtUtc = clock.UtcNow;
        DateTime? confirmedAtUtc = request.ConfirmRegistration ? createdAtUtc : null;
        var registration = new SheetRegistration(
            Guid.NewGuid(),
            request.PlanSetVersionId,
            request.FacadeElevationSheetId,
            request.PlanSetVersionId,
            SheetRegistrationMethod.FacadeHorizontalReference,
            new SheetRegistrationTransform(
                scale: request.HorizontalScale,
                rotationDegrees: 0m,
                translateX: request.HorizontalOffset,
                translateY: 0m),
            request.Confidence,
            request.ConfirmRegistration
                ? SheetRegistrationStatus.Confirmed
                : SheetRegistrationStatus.PendingConfirmation,
            createdAtUtc,
            confirmedAtUtc,
            request.Warning,
            BuildRuleSummary(request.HorizontalReferenceName));

        await sheetRegistrationRepository.AddAsync(registration, cancellationToken);
        await TryRecordRegistrationQualityEventAsync(registration, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(registration);
    }

    private async Task TryRecordRegistrationQualityEventAsync(
        SheetRegistration registration,
        CancellationToken cancellationToken)
    {
        if (planSetAuditEventRepository is null)
        {
            return;
        }

        try
        {
            await planSetAuditEventRepository.AddAsync(
                new PlanSetAuditEvent(
                    Guid.NewGuid(),
                    "SheetRegistration",
                    registration.Id,
                    "SheetRegistrationQualityMeasured",
                    JsonSerializer.Serialize(new
                    {
                        planSetVersionId = registration.PlanSetVersionId,
                        dependentSheetId = registration.DependentSheetId,
                        canonicalFloorPlanVersionId = registration.CanonicalFloorPlanVersionId,
                        method = registration.Method.ToString(),
                        confidence = registration.Confidence,
                        status = registration.Status.ToString(),
                        warning = registration.Warning,
                        ruleSummary = registration.RuleSummary
                    }),
                    registration.CreatedAtUtc),
                cancellationToken);
        }
        catch (Exception)
        {
            // ponytail: registration telemetry is best-effort; add durable retries only if analytics becomes business-critical.
        }
    }

    private static string BuildRuleSummary(string? horizontalReferenceName)
    {
        var referenceName = string.IsNullOrWhiteSpace(horizontalReferenceName)
            ? DefaultHorizontalReferenceName
            : horizontalReferenceName.Trim();

        return $"PreserveVertical=true;HorizontalReference={referenceName}";
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
            registration.ConfirmedAtUtc,
            registration.RuleSummary);
    }
}
