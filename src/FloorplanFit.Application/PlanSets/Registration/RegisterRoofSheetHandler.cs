using System.Text.Json;
using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.PlanSets.Registration;

public sealed class RegisterRoofSheetHandler
{
    private readonly IPlanSheetRepository planSheetRepository;
    private readonly ISheetRegistrationRepository sheetRegistrationRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;
    private readonly IPlanSetAuditEventRepository? planSetAuditEventRepository;

    public RegisterRoofSheetHandler(
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
        RegisterRoofSheetRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PlanSetVersionId == Guid.Empty)
        {
            throw new ArgumentException("Plan set version is required.", nameof(request));
        }

        if (request.RoofSheetId == Guid.Empty)
        {
            throw new ArgumentException("Roof sheet is required.", nameof(request));
        }

        if (request.OverhangInches < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(request.OverhangInches), "Roof overhang cannot be negative.");
        }

        var sheet = await planSheetRepository.GetByIdAsync(request.RoofSheetId, cancellationToken);
        if (sheet is null)
        {
            throw new InvalidOperationException("Dependent sheet was not found.");
        }

        if (sheet.PlanSetVersionId != request.PlanSetVersionId)
        {
            throw new ArgumentException("Roof sheet does not belong to the requested plan set version.", nameof(request));
        }

        if (sheet.SheetType is not PlanSheetType.RoofPlan)
        {
            throw new ArgumentException("Only roof sheets can use roof registration.", nameof(request));
        }

        var createdAtUtc = clock.UtcNow;
        DateTime? confirmedAtUtc = request.ConfirmRegistration ? createdAtUtc : null;
        var registration = new SheetRegistration(
            Guid.NewGuid(),
            request.PlanSetVersionId,
            request.RoofSheetId,
            request.PlanSetVersionId,
            SheetRegistrationMethod.RoofFootprintWithOverhang,
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
            request.Warning,
            $"PreserveOverhangInches={request.OverhangInches.ToString("0.####", CultureInfo.InvariantCulture)}");

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

