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

    public ProjectFacadeElevationSheetAdjustmentHandler(
        ISheetRegistrationRepository sheetRegistrationRepository,
        ISheetAdjustmentProjectionRepository sheetAdjustmentProjectionRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        this.sheetRegistrationRepository = sheetRegistrationRepository;
        this.sheetAdjustmentProjectionRepository = sheetAdjustmentProjectionRepository;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
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

        var compressionStepCount = request.CanonicalPlacement.CompressionSteps.Count;
        var status = ResolveStatus(registration, compressionStepCount);
        var warning = ResolveWarning(registration, compressionStepCount, status);
        var projection = new SheetAdjustmentProjection(
            Guid.NewGuid(),
            registration.PlanSetVersionId,
            registration.DependentSheetId,
            registration.Id,
            request.CanonicalAdjustmentId,
            SheetAdjustmentProjectionMethod.FacadeHorizontalPreservingVerticals,
            ComposeTransform(registration.Transform, request.CanonicalPlacement),
            registration.Confidence,
            status,
            warning,
            compressionStepCount,
            clock.UtcNow,
            registration.RuleSummary);

        await sheetAdjustmentProjectionRepository.AddAsync(projection, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(projection);
    }

    private static SheetAdjustmentProjectionTransform ComposeTransform(
        SheetRegistrationTransform registrationTransform,
        AdjustedSitePlanPlacementDto canonicalPlacement)
    {
        return new SheetAdjustmentProjectionTransform(
            scale: registrationTransform.Scale * canonicalPlacement.FloorToSiteScale,
            rotationDegrees: 0m,
            translateX: (registrationTransform.TranslateX * canonicalPlacement.FloorToSiteScale) + canonicalPlacement.SiteOffsetX,
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
            projection.RuleSummary);
    }
}
