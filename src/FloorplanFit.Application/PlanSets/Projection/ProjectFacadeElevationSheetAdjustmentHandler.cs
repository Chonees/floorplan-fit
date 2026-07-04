using System.Text.Json;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.PlanSets.Projection;

public sealed class ProjectFacadeElevationSheetAdjustmentHandler
{
    private const decimal AutoExportConfidenceThreshold = 0.8m;
    private const string RequiredVerticalRule = "PreserveVertical=true";

    private readonly ISheetRegistrationRepository sheetRegistrationRepository;
    private readonly ISheetAdjustmentProjectionRepository sheetAdjustmentProjectionRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;
    private readonly IPlanSetAuditEventRepository? planSetAuditEventRepository;

    public ProjectFacadeElevationSheetAdjustmentHandler(
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
        ProjectFacadeElevationSheetAdjustmentRequest request,
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

        if (registration.Method is not SheetRegistrationMethod.FacadeHorizontalReference)
        {
            throw new ArgumentException("Only facade/elevation horizontal-reference registrations can use facade/elevation projection.", nameof(request));
        }

        var canonicalRecipe = request.CanonicalRecipe ?? AdjustmentRecipeSummaryDto.FromPlacement(request.CanonicalPlacement);
        var compressionStepCount = canonicalRecipe.Operations.Count;
        var status = ResolveStatus(registration, compressionStepCount);
        var warning = ResolveWarning(registration, compressionStepCount, status);
        var projection = new SheetAdjustmentProjection(
            Guid.NewGuid(),
            registration.PlanSetVersionId,
            registration.DependentSheetId,
            registration.Id,
            request.CanonicalAdjustmentId,
            SheetAdjustmentProjectionMethod.FacadeHorizontalPreservingVerticals,
            ComposeTransform(registration.Transform, canonicalRecipe),
            registration.Confidence,
            status,
            warning,
            compressionStepCount,
            clock.UtcNow,
            registration.RuleSummary,
            BuildRecipeHandlingSummary("FacadeElevation", canonicalRecipe));

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
            scale: registrationTransform.Scale * canonicalRecipe.FloorToSiteScale,
            rotationDegrees: 0m,
            translateX: (registrationTransform.TranslateX * canonicalRecipe.FloorToSiteScale) + canonicalRecipe.SiteOffsetX,
            translateY: 0m);
    }

    private static SheetAdjustmentProjectionStatus ResolveStatus(
        SheetRegistration registration,
        int compressionStepCount)
    {
        return registration.Status is SheetRegistrationStatus.Confirmed &&
               registration.Confidence >= AutoExportConfidenceThreshold &&
               compressionStepCount == 0 &&
               HasVerticalPreservationRule(registration)
            ? SheetAdjustmentProjectionStatus.ReadyForExport
            : SheetAdjustmentProjectionStatus.RequiresManualConfirmation;
    }

    private static string? ResolveWarning(
        SheetRegistration registration,
        int compressionStepCount,
        SheetAdjustmentProjectionStatus status)
    {
        if (status is SheetAdjustmentProjectionStatus.ReadyForExport)
        {
            return registration.Warning;
        }

        if (!HasVerticalPreservationRule(registration))
        {
            return "Facade/elevation projection requires a vertical-preservation rule before export.";
        }

        if (registration.Status is not SheetRegistrationStatus.Confirmed)
        {
            return "Facade/elevation projection requires a confirmed registration before export.";
        }

        if (registration.Confidence < AutoExportConfidenceThreshold)
        {
            return "Facade/elevation projection confidence is below the automatic export threshold.";
        }

        if (compressionStepCount > 0)
        {
            return "Canonical compression steps require facade/elevation review before export.";
        }

        return registration.Warning;
    }

    private static bool HasVerticalPreservationRule(SheetRegistration registration)
    {
        return registration.RuleSummary?.Contains(RequiredVerticalRule, StringComparison.OrdinalIgnoreCase) == true;
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
