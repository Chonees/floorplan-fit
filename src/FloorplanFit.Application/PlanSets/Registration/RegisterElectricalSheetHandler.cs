using System.Text.Json;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.PlanSets.Registration;

public sealed class RegisterElectricalSheetHandler
{
    private readonly IPlanSheetRepository planSheetRepository;
    private readonly IPlanSetVersionRepository planSetVersionRepository;
    private readonly IFloorPlanExtractionSourceReader floorPlanExtractionSourceReader;
    private readonly IPlanSheetSourceReader planSheetSourceReader;
    private readonly IElectricalFloorRegistrationEstimator estimator;
    private readonly ISheetRegistrationRepository sheetRegistrationRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;
    private readonly IPlanSetAuditEventRepository? planSetAuditEventRepository;

    public RegisterElectricalSheetHandler(
        IPlanSheetRepository planSheetRepository,
        IPlanSetVersionRepository planSetVersionRepository,
        IFloorPlanExtractionSourceReader floorPlanExtractionSourceReader,
        IPlanSheetSourceReader planSheetSourceReader,
        IElectricalFloorRegistrationEstimator estimator,
        ISheetRegistrationRepository sheetRegistrationRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPlanSetAuditEventRepository? planSetAuditEventRepository = null)
    {
        this.planSheetRepository = planSheetRepository;
        this.planSetVersionRepository = planSetVersionRepository;
        this.floorPlanExtractionSourceReader = floorPlanExtractionSourceReader;
        this.planSheetSourceReader = planSheetSourceReader;
        this.estimator = estimator;
        this.sheetRegistrationRepository = sheetRegistrationRepository;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
        this.planSetAuditEventRepository = planSetAuditEventRepository;
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

        var planSetVersion = await planSetVersionRepository.GetByIdAsync(
            request.PlanSetVersionId,
            cancellationToken) ?? throw new InvalidOperationException("Plan set version was not found.");

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

        var floorSource = await floorPlanExtractionSourceReader.GetByVersionAsync(
            planSetVersion.CanonicalFloorPlanVersionId,
            cancellationToken) ?? throw new InvalidOperationException("Canonical floor-plan source was not found.");

        var electricalSource = await planSheetSourceReader.GetBySheetIdAsync(
            sheet.Id,
            cancellationToken) ?? throw new InvalidOperationException("Electrical sheet source was not found.");

        var estimate = await estimator.EstimateAsync(
            floorSource.ManagedFilePath,
            electricalSource.SourceFilePath,
            cancellationToken);

        if (!estimate.IsConclusive)
        {
            throw new ElectricalFloorRegistrationManualReviewRequiredException(estimate);
        }

        var wholePlanProof = CreateWholePlanProof(
            estimate,
            planSetVersion.CanonicalFloorPlanVersionId,
            request.ElectricalSheetId);

        var createdAtUtc = clock.UtcNow;
        var registration = new SheetRegistration(
            Guid.NewGuid(),
            request.PlanSetVersionId,
            request.ElectricalSheetId,
            planSetVersion.CanonicalFloorPlanVersionId,
            SheetRegistrationMethod.WholeSheetSimilarity,
            estimate.Transform!,
            estimate.Confidence,
            SheetRegistrationStatus.PendingConfirmation,
            createdAtUtc,
            confirmedAtUtc: null,
            warning: null,
            ruleSummary: estimate.EvidenceSummary,
            wholePlanRegistrationProof: wholePlanProof);

        await sheetRegistrationRepository.AddAsync(registration, cancellationToken);
        await TryRecordRegistrationQualityEventAsync(registration, estimate, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(registration);
    }

    private async Task TryRecordRegistrationQualityEventAsync(
        SheetRegistration registration,
        ElectricalFloorRegistrationEstimate estimate,
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
                        ruleSummary = registration.RuleSummary,
                        transform = new
                        {
                            scale = registration.Transform.Scale,
                            rotationDegrees = registration.Transform.RotationDegrees,
                            translateX = registration.Transform.TranslateX,
                            translateY = registration.Transform.TranslateY
                        },
                        observedScaleX = estimate.ObservedScaleX,
                        observedScaleY = estimate.ObservedScaleY,
                        horizontalCoverage = estimate.HorizontalCoverage,
                        verticalCoverage = estimate.VerticalCoverage,
                        rootMeanSquareResidual = estimate.RootMeanSquareResidual,
                        maximumResidual = estimate.MaximumResidual,
                        candidates = estimate.Candidates.Select(candidate => new
                        {
                            rotationDegrees = candidate.RotationDegrees,
                            accepted = candidate.Accepted,
                            scale = candidate.Scale,
                            translateX = candidate.TranslateX,
                            translateY = candidate.TranslateY,
                            horizontalCoverage = candidate.HorizontalCoverage,
                            verticalCoverage = candidate.VerticalCoverage,
                            rootMeanSquareResidual = candidate.RootMeanSquareResidual,
                            maximumResidual = candidate.MaximumResidual,
                            leftEdgeResidual = candidate.LeftEdgeResidual,
                            rightEdgeResidual = candidate.RightEdgeResidual,
                            bottomEdgeResidual = candidate.BottomEdgeResidual,
                            topEdgeResidual = candidate.TopEdgeResidual,
                            reason = candidate.Reason
                        })
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

    private static WholePlanRegistrationProof CreateWholePlanProof(
        ElectricalFloorRegistrationEstimate estimate,
        Guid canonicalFloorPlanVersionId,
        Guid dependentSheetId)
    {
        if (!estimate.HorizontalCoverage.HasValue ||
            !estimate.VerticalCoverage.HasValue ||
            !estimate.RootMeanSquareResidual.HasValue ||
            !estimate.MaximumResidual.HasValue ||
            string.IsNullOrWhiteSpace(estimate.CanonicalSourceSha256) ||
            string.IsNullOrWhiteSpace(estimate.DependentSourceSha256))
        {
            throw new ElectricalFloorRegistrationManualReviewRequiredException(estimate);
        }

        try
        {
            var proof = new WholePlanRegistrationProof(
                WholePlanRegistrationProof.CurrentVersion,
                Passed: estimate.Status is ElectricalFloorRegistrationEstimateStatus.Estimated,
                canonicalFloorPlanVersionId,
                dependentSheetId,
                estimate.CanonicalSourceSha256,
                estimate.DependentSourceSha256,
                estimate.HorizontalCoverage.Value,
                estimate.VerticalCoverage.Value,
                estimate.RootMeanSquareResidual.Value,
                estimate.MaximumResidual.Value);
            return proof.IsAuthoritative
                ? proof
                : throw new ElectricalFloorRegistrationManualReviewRequiredException(estimate);
        }
        catch (ArgumentException)
        {
            throw new ElectricalFloorRegistrationManualReviewRequiredException(estimate);
        }
    }
}

public sealed class ElectricalFloorRegistrationManualReviewRequiredException : InvalidOperationException
{
    public ElectricalFloorRegistrationManualReviewRequiredException(
        ElectricalFloorRegistrationEstimate estimate)
        : base("Electrical floor registration requires manual review.")
    {
        Estimate = estimate ?? throw new ArgumentNullException(nameof(estimate));
    }

    public ElectricalFloorRegistrationEstimate Estimate { get; }
}
