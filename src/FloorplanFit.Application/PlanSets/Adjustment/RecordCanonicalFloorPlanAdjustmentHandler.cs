using System.Text.Json;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.PlanSets.Adjustment;

public sealed class RecordCanonicalFloorPlanAdjustmentHandler
{
    private readonly ICanonicalFloorPlanAdjustmentRepository canonicalFloorPlanAdjustmentRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;

    public RecordCanonicalFloorPlanAdjustmentHandler(
        ICanonicalFloorPlanAdjustmentRepository canonicalFloorPlanAdjustmentRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        this.canonicalFloorPlanAdjustmentRepository = canonicalFloorPlanAdjustmentRepository;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
    }

    public async Task<RecordCanonicalFloorPlanAdjustmentResponse> HandleAsync(
        RecordCanonicalFloorPlanAdjustmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Placement);

        var recipe = AdjustmentRecipeSummaryDto.FromPlacement(request.Placement);
        var adjustment = new CanonicalFloorPlanAdjustment(
            Guid.NewGuid(),
            request.PlanSetVersionId,
            request.CanonicalFloorPlanVersionId,
            request.SitePlanSourcePath,
            request.CanonicalFloorPlanExportPath,
            JsonSerializer.Serialize(request.Placement),
            JsonSerializer.Serialize(recipe),
            clock.UtcNow);

        await canonicalFloorPlanAdjustmentRepository.AddAsync(adjustment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RecordCanonicalFloorPlanAdjustmentResponse(
            adjustment.Id,
            adjustment.PlanSetVersionId,
            adjustment.CanonicalFloorPlanVersionId,
            adjustment.CanonicalFloorPlanExportPath,
            recipe,
            adjustment.CreatedAtUtc);
    }
}
