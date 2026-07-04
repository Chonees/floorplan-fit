using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Abstractions;

public interface IPlanSetAuditEventReader
{
    Task<IReadOnlyList<PlanSetAuditEvent>> ListQualityEventsByPlanSetVersionAsync(
        Guid planSetVersionId,
        CancellationToken cancellationToken);
}
