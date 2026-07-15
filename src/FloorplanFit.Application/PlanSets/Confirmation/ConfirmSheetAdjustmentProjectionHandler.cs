using System.Text.Json;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.PlanSets.Confirmation;

public sealed class ConfirmSheetAdjustmentProjectionHandler
{
    private readonly ISheetAdjustmentProjectionRepository sheetAdjustmentProjectionRepository;
    private readonly ISheetRegistrationRepository sheetRegistrationRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;
    private readonly IPlanSetAuditEventRepository? planSetAuditEventRepository;

    public ConfirmSheetAdjustmentProjectionHandler(
        ISheetAdjustmentProjectionRepository sheetAdjustmentProjectionRepository,
        ISheetRegistrationRepository sheetRegistrationRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPlanSetAuditEventRepository? planSetAuditEventRepository = null)
    {
        this.sheetAdjustmentProjectionRepository = sheetAdjustmentProjectionRepository;
        this.sheetRegistrationRepository = sheetRegistrationRepository;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
        this.planSetAuditEventRepository = planSetAuditEventRepository;
    }

    public async Task<SheetAdjustmentProjectionDto> HandleAsync(
        ConfirmSheetAdjustmentProjectionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ProjectionId == Guid.Empty)
        {
            throw new ArgumentException("Projection is required.", nameof(request));
        }

        var projection = await sheetAdjustmentProjectionRepository.GetByIdAsync(request.ProjectionId, cancellationToken);
        if (projection is null)
        {
            throw new InvalidOperationException("Sheet adjustment projection was not found.");
        }

        SheetAdjustmentProjectionCapabilities.EnsureSupported(
            projection.Method,
            projection.CanonicalCompressionStepCount);

        if (projection.Status is SheetAdjustmentProjectionStatus.ReadyForExport)
        {
            return ToDto(projection);
        }

        var registration = await sheetRegistrationRepository.GetByIdAsync(projection.SheetRegistrationId, cancellationToken);
        if (registration?.Status is not SheetRegistrationStatus.Confirmed)
        {
            throw new InvalidOperationException("Projection requires a confirmed sheet registration before export.");
        }

        var confirmed = new SheetAdjustmentProjection(
            projection.Id,
            projection.PlanSetVersionId,
            projection.DependentSheetId,
            projection.SheetRegistrationId,
            projection.CanonicalAdjustmentId,
            projection.Method,
            projection.Transform,
            projection.Confidence,
            SheetAdjustmentProjectionStatus.ReadyForExport,
            projection.Warning,
            projection.CanonicalCompressionStepCount,
            projection.CreatedAtUtc,
            projection.RuleSummary,
            BuildConfirmedRecipeHandlingSummary(projection));

        await sheetAdjustmentProjectionRepository.UpdateAsync(confirmed, cancellationToken);
        await TryRecordProjectionQualityEventAsync(confirmed, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(confirmed);
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
                    clock.UtcNow),
                cancellationToken);
        }
        catch (Exception)
        {
            // ponytail: confirmation telemetry is best-effort; add durable retries only if analytics becomes business-critical.
        }
    }

    private static string? BuildConfirmedRecipeHandlingSummary(SheetAdjustmentProjection projection)
    {
        if (projection.CanonicalCompressionStepCount == 0 ||
            string.IsNullOrWhiteSpace(projection.RecipeHandlingSummary))
        {
            return projection.RecipeHandlingSummary;
        }

        return projection.RecipeHandlingSummary
            .Replace(
            "local recipe requires review before DXF deformation",
            "local recipe manually confirmed; recipe-aware DXF export will apply canonical operations",
            StringComparison.OrdinalIgnoreCase)
            .Replace(
                "; manual review required:",
                "; manual review completed:",
                StringComparison.OrdinalIgnoreCase);
    }

    private static SheetAdjustmentProjectionDto ToDto(SheetAdjustmentProjection projection)
        => new(
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
