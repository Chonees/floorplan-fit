using System.Text.Json;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.PlanSets.Projection;

public sealed class ProjectRoofSheetAdjustmentHandler
{
    private const decimal AutoExportConfidenceThreshold = 0.8m;

    private readonly ISheetRegistrationRepository sheetRegistrationRepository;
    private readonly ISheetAdjustmentProjectionRepository sheetAdjustmentProjectionRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;
    private readonly IPlanSetAuditEventRepository? planSetAuditEventRepository;

    public ProjectRoofSheetAdjustmentHandler(
        ISheetRegistrationRepository sheetRegistrationRepository,
        ISheetAdjustmentProjectionRepository sheetAdjustmentProjectionRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPlanSetAuditEventRepository? planSetAuditEventRepository = null)
    {
        this.sheetRegistrationRepository = sheetRegistrationRepository;
        this.sheetAdjustmentProjectionRepository = sheetAdjustmentProjectionRepository;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
        this.planSetAuditEventRepository = planSetAuditEventRepository;
    }

    public async Task<SheetAdjustmentProjectionDto> HandleAsync(
        ProjectRoofSheetAdjustmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.CanonicalPlacement);

        if (request.SheetRegistrationId == Guid.Empty)
        {
            throw new ArgumentException("Sheet registration is required.", nameof(request));
        }

        if (request.CanonicalAdjustmentId == Guid.Empty)
        {
            throw new ArgumentException("Canonical adjustment is required.", nameof(request));
        }

        var registration = await sheetRegistrationRepository.GetByIdAsync(request.SheetRegistrationId, cancellationToken);
        if (registration is null)
        {
            throw new InvalidOperationException("Sheet registration was not found.");
        }

        if (registration.Method is not SheetRegistrationMethod.RoofFootprintWithOverhang)
        {
            throw new ArgumentException("Only roof-overhang registrations can use roof projection.", nameof(request));
        }

        var canonicalRecipe = request.CanonicalRecipe ?? AdjustmentRecipeSummaryDto.FromPlacement(request.CanonicalPlacement);
        var compressionStepCount = canonicalRecipe.Operations.Count;
        var hasUnsupportedCapability = SheetAdjustmentProjectionCapabilities.TryGetUnsupportedReason(
            SheetAdjustmentProjectionMethod.RoofOverhangPreserving,
            compressionStepCount,
            out var unsupportedReason);
        var status = ResolveStatus(registration, hasUnsupportedCapability);
        var warning = ResolveWarning(registration, status, unsupportedReason);
        var projection = new SheetAdjustmentProjection(
            Guid.NewGuid(),
            registration.PlanSetVersionId,
            registration.DependentSheetId,
            registration.Id,
            request.CanonicalAdjustmentId,
            SheetAdjustmentProjectionMethod.RoofOverhangPreserving,
            ComposeTransform(registration.Transform, canonicalRecipe),
            registration.Confidence,
            status,
            warning,
            compressionStepCount,
            clock.UtcNow,
            registration.RuleSummary,
            BuildRecipeHandlingSummary("RoofPlan", canonicalRecipe));

        await sheetAdjustmentProjectionRepository.AddAsync(projection, cancellationToken);
        await TryRecordProjectionQualityEventAsync(projection, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(projection);
    }

    private async Task TryRecordProjectionQualityEventAsync(
        SheetAdjustmentProjection projection,
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
                    "SheetAdjustmentProjection",
                    projection.Id,
                    "SheetAdjustmentProjectionQualityMeasured",
                    JsonSerializer.Serialize(new
                    {
                        planSetVersionId = projection.PlanSetVersionId,
                        dependentSheetId = projection.DependentSheetId,
                        sheetRegistrationId = projection.SheetRegistrationId,
                        canonicalAdjustmentId = projection.CanonicalAdjustmentId,
                        method = projection.Method.ToString(),
                        confidence = projection.Confidence,
                        status = projection.Status.ToString(),
                        warning = projection.Warning,
                        canonicalCompressionStepCount = projection.CanonicalCompressionStepCount,
                        ruleSummary = projection.RuleSummary,
                        recipeHandlingSummary = projection.RecipeHandlingSummary
                    }),
                    projection.CreatedAtUtc),
                cancellationToken);
        }
        catch (Exception)
        {
            // ponytail: projection telemetry is best-effort; add durable retries only if analytics becomes business-critical.
        }
    }

    private static SheetAdjustmentProjectionTransform ComposeTransform(
        SheetRegistrationTransform registrationTransform,
        AdjustmentRecipeSummaryDto canonicalRecipe)
    {
        return new SheetAdjustmentProjectionTransform(
            registrationTransform.Scale * canonicalRecipe.FloorToSiteScale,
            registrationTransform.RotationDegrees,
            (registrationTransform.TranslateX * canonicalRecipe.FloorToSiteScale) + canonicalRecipe.SiteOffsetX,
            (registrationTransform.TranslateY * canonicalRecipe.FloorToSiteScale) + canonicalRecipe.SiteOffsetY);
    }

    private static SheetAdjustmentProjectionStatus ResolveStatus(
        SheetRegistration registration,
        bool hasUnsupportedCapability)
    {
        if (hasUnsupportedCapability)
        {
            return SheetAdjustmentProjectionStatus.Unsupported;
        }

        return registration.Status is SheetRegistrationStatus.Confirmed &&
               registration.Confidence >= AutoExportConfidenceThreshold &&
               !string.IsNullOrWhiteSpace(registration.RuleSummary)
            ? SheetAdjustmentProjectionStatus.ReadyForExport
            : SheetAdjustmentProjectionStatus.RequiresManualConfirmation;
    }

    private static string? ResolveWarning(
        SheetRegistration registration,
        SheetAdjustmentProjectionStatus status,
        string? unsupportedReason)
    {
        if (status is SheetAdjustmentProjectionStatus.Unsupported)
        {
            return unsupportedReason;
        }

        if (status is SheetAdjustmentProjectionStatus.ReadyForExport)
        {
            return registration.Warning;
        }

        if (string.IsNullOrWhiteSpace(registration.RuleSummary))
        {
            return "Roof projection requires an overhang preservation rule before export.";
        }

        if (registration.Status is not SheetRegistrationStatus.Confirmed)
        {
            return "Roof projection requires a confirmed registration before export.";
        }

        if (registration.Confidence < AutoExportConfidenceThreshold)
        {
            return "Roof projection confidence is below the automatic export threshold.";
        }

        return registration.Warning;
    }

    private static string BuildRecipeHandlingSummary(
        string sheetKind,
        AdjustmentRecipeSummaryDto canonicalRecipe)
        => canonicalRecipe.ToSheetReviewSummary(sheetKind);

    private static SheetAdjustmentProjectionDto ToDto(SheetAdjustmentProjection projection)
    {
        return new SheetAdjustmentProjectionDto(
            projection.Id,
            projection.PlanSetVersionId,
            projection.DependentSheetId,
            projection.SheetRegistrationId,
            projection.CanonicalAdjustmentId,
            projection.Method.ToString(),
            new SheetAdjustmentProjectionTransformDto(
                projection.Transform.Scale,
                projection.Transform.RotationDegrees,
                projection.Transform.TranslateX,
                projection.Transform.TranslateY),
            projection.Confidence,
            projection.Status.ToString(),
            projection.Warning,
            projection.CanonicalCompressionStepCount,
            projection.CreatedAtUtc,
            projection.RuleSummary,
            projection.RecipeHandlingSummary);
    }
}
