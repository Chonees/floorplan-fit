using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Abstractions;

public interface IPlanSetExportRepository
{
    Task AddAsync(PlanSetExport export, CancellationToken cancellationToken);
}
