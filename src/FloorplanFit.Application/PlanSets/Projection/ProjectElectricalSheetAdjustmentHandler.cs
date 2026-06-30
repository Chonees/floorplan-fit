using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.PlanSets.Projection;

public sealed class ProjectElectricalSheetAdjustmentHandler
{
    private const decimal AutoExportConfidenceThreshold = 0.8m;

    private readonly ISheetRegistrationRepository sheetRegistrationRepository;
    private readonly ISheetAdjustmentProjectionRepository sheetAdjustmentProjectionRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;

    public ProjectElectricalSheetAdjustmentHandler(
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
        ProjectElectricalSheetAdjustmentRequest request,
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

        if (registration.Method is not SheetRegistrationMethod.WholeSheetSimilarity)
        {
            throw new ArgumentException("Only electrical whole-sheet registrations can use electrical projection.", nameof(request));
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
            SheetAdjustmentProjectionMethod.ElectricalWholeSheetSimilarity,
            ComposeTransform(registration.Transform, request.CanonicalPlacement),
            registration.Confidence,
            status,
            warning,
            compressionStepCount,
            clock.UtcNow);

        await sheetAdjustmentProjectionRepository.AddAsync(projection, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(projection);
    }

    private static SheetAdjustmentProjectionTransform ComposeTransform(
        SheetRegistrationTransform registrationTransform,
        AdjustedSitePlanPlacementDto canonicalPlacement)
    {
        return new SheetAdjustmentProjectionTransform(
            registrationTransform.Scale * canonicalPlacement.FloorToSiteScale,
            registrationTransform.RotationDegrees,
            (registrationTransform.TranslateX * canonicalPlacement.FloorToSiteScale) + canonicalPlacement.SiteOffsetX,
            (registrationTransform.TranslateY * canonicalPlacement.FloorToSiteScale) + canonicalPlacement.SiteOffsetY);
    }

    private static SheetAdjustmentProjectionStatus ResolveStatus(
        SheetRegistration registration,
        int compressionStepCount)
    {
        return registration.Status is SheetRegistrationStatus.Confirmed &&
               registration.Confidence >= AutoExportConfidenceThreshold &&
               compressionStepCount == 0
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

        if (registration.Status is not SheetRegistrationStatus.Confirmed)
        {
            return "Electrical projection requires a confirmed registration before export.";
        }

        if (registration.Confidence < AutoExportConfidenceThreshold)
        {
            return "Electrical projection confidence is below the automatic export threshold.";
        }

        if (compressionStepCount > 0)
        {
            return "Canonical compression steps require electrical review before export.";
        }

        return registration.Warning;
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
            projection.CreatedAtUtc);
    }
}
