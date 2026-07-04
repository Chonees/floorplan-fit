using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Abstractions;

public interface IPlanSetAuditEventRepository
{
    Task AddAsync(PlanSetAuditEvent auditEvent, CancellationToken cancellationToken);
}
