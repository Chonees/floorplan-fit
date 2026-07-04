using System.Text.Json;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.PlanSets.Classification;

public sealed class CorrectPlanSheetTypeHandler
{
    private readonly IPlanSheetRepository planSheetRepository;
    private readonly ISheetRegistrationRepository sheetRegistrationRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;
    private readonly IPlanSetAuditEventRepository? planSetAuditEventRepository;

    public CorrectPlanSheetTypeHandler(
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

    public async Task<CorrectPlanSheetTypeResponse> HandleAsync(
        CorrectPlanSheetTypeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PlanSetVersionId == Guid.Empty)
        {
            throw new ArgumentException("Plan-set version is required.", nameof(request));
        }

        if (request.SheetId == Guid.Empty)
        {
            throw new ArgumentException("Plan sheet is required.", nameof(request));
        }

        var correctedType = ResolveDependentSheetType(request);
        var sheet = await planSheetRepository.GetByIdAsync(request.SheetId, cancellationToken)
            ?? throw new InvalidOperationException("Plan sheet was not found.");
        if (sheet.PlanSetVersionId != request.PlanSetVersionId)
        {
            throw new ArgumentException("Plan sheet does not belong to the requested plan-set version.", nameof(request));
        }

        var registrations = await sheetRegistrationRepository.ListByPlanSetVersionAsync(
            request.PlanSetVersionId,
            cancellationToken);
        if (registrations.Any(registration =>
                registration.DependentSheetId == sheet.Id &&
                registration.Status is not SheetRegistrationStatus.Rejected))
        {
            throw new InvalidOperationException("Registered sheets must be unregistered before changing sheet type.");
        }

        var previousType = sheet.SheetType;
        var corrected = new PlanSheet(
            sheet.Id,
            sheet.PlanSetVersionId,
            correctedType,
            sheet.ImportedDocumentId,
            sheet.MeasurementContextId,
            sheet.Name,
            sheet.Status,
            sheet.CreatedAtUtc);

        await planSheetRepository.UpdateAsync(corrected, cancellationToken);
        await TryRecordCorrectionEventAsync(corrected, previousType, request.Reason, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CorrectPlanSheetTypeResponse(
            corrected.Id,
            corrected.PlanSetVersionId,
            corrected.SheetType.ToString(),
            previousType.ToString(),
            corrected.Status.ToString(),
            clock.UtcNow);
    }

    private static PlanSheetType ResolveDependentSheetType(CorrectPlanSheetTypeRequest request)
    {
        if (!Enum.TryParse<PlanSheetType>(request.SheetType, ignoreCase: true, out var sheetType) ||
            sheetType is PlanSheetType.Unknown)
        {
            throw new ArgumentException("A known dependent sheet type is required.", nameof(request));
        }

        if (sheetType is PlanSheetType.FloorPlan)
        {
            throw new ArgumentException("Floor plans must use the canonical floor-plan import flow.", nameof(request));
        }

        return sheetType;
    }

    private async Task TryRecordCorrectionEventAsync(
        PlanSheet sheet,
        PlanSheetType previousSheetType,
        string? reason,
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
                    "PlanSheet",
                    sheet.Id,
                    "SheetClassificationQualityMeasured",
                    JsonSerializer.Serialize(new
                    {
                        planSetVersionId = sheet.PlanSetVersionId,
                        previousSheetType = previousSheetType.ToString(),
                        sheetType = sheet.SheetType.ToString(),
                        source = "UserCorrection",
                        confidence = 1m,
                        status = "Corrected",
                        warning = (string?)null,
                        ruleSummary = string.IsNullOrWhiteSpace(reason)
                            ? "Sheet type corrected by user."
                            : reason.Trim()
                    }),
                    clock.UtcNow),
                cancellationToken);
        }
        catch (Exception)
        {
            // ponytail: classification correction telemetry is best-effort; add retries only if analytics becomes business-critical.
        }
    }
}
