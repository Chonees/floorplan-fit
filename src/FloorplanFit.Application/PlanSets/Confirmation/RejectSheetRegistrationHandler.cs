using System.Text.Json;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.PlanSets.Confirmation;

public sealed class RejectSheetRegistrationHandler
{
    private readonly ISheetRegistrationRepository sheetRegistrationRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;
    private readonly IPlanSetAuditEventRepository? planSetAuditEventRepository;

    public RejectSheetRegistrationHandler(
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
        RejectSheetRegistrationRequest request,
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

        if (registration.Status is SheetRegistrationStatus.Confirmed)
        {
            throw new InvalidOperationException("Confirmed sheet registration cannot be rejected here.");
        }

        if (registration.Status is SheetRegistrationStatus.Rejected)
        {
            return ToDto(registration);
        }

        var rejected = new SheetRegistration(
            registration.Id,
            registration.PlanSetVersionId,
            registration.DependentSheetId,
            registration.CanonicalFloorPlanVersionId,
            registration.Method,
            registration.Transform,
            registration.Confidence,
            SheetRegistrationStatus.Rejected,
            registration.CreatedAtUtc,
            confirmedAtUtc: null,
            registration.Warning,
            registration.RuleSummary);

        await sheetRegistrationRepository.UpdateAsync(rejected, cancellationToken);
        await TryRecordRegistrationQualityEventAsync(rejected, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(rejected);
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
            // ponytail: rejection telemetry is best-effort; add durable retries only if analytics becomes business-critical.
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
