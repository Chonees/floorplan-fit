using System.Text.Json;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.PlanSets.Confirmation;

public sealed class ConfirmSheetRegistrationHandler
{
    private readonly ISheetRegistrationRepository sheetRegistrationRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;
    private readonly IPlanSetAuditEventRepository? planSetAuditEventRepository;

    public ConfirmSheetRegistrationHandler(
        ISheetRegistrationRepository sheetRegistrationRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPlanSetAuditEventRepository? planSetAuditEventRepository = null)
    {
        this.sheetRegistrationRepository = sheetRegistrationRepository;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
        this.planSetAuditEventRepository = planSetAuditEventRepository;
    }

    public async Task<SheetRegistrationDto> HandleAsync(
        ConfirmSheetRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.RegistrationId == Guid.Empty)
        {
            throw new ArgumentException("Registration is required.", nameof(request));
        }

        var registration = await sheetRegistrationRepository.GetByIdAsync(request.RegistrationId, cancellationToken);
        if (registration is null)
        {
            throw new InvalidOperationException("Sheet registration was not found.");
        }

        if (registration.Status is SheetRegistrationStatus.Rejected)
        {
            throw new InvalidOperationException("Rejected sheet registration cannot be confirmed.");
        }

        if (registration.Status is SheetRegistrationStatus.Confirmed)
        {
            return ToDto(registration);
        }

        var confirmed = new SheetRegistration(
            registration.Id,
            registration.PlanSetVersionId,
            registration.DependentSheetId,
            registration.CanonicalFloorPlanVersionId,
            registration.Method,
            registration.Transform,
            registration.Confidence,
            SheetRegistrationStatus.Confirmed,
            registration.CreatedAtUtc,
            clock.UtcNow,
            registration.Warning,
            registration.RuleSummary,
            registration.WholePlanRegistrationProof);

        await sheetRegistrationRepository.UpdateAsync(confirmed, cancellationToken);
        await TryRecordRegistrationQualityEventAsync(confirmed, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(confirmed);
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
                    clock.UtcNow),
                cancellationToken);
        }
        catch (Exception)
        {
            // ponytail: confirmation telemetry is best-effort; add durable retries only if analytics becomes business-critical.
        }
    }

    private static SheetRegistrationDto ToDto(SheetRegistration registration)
        => new(
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
