using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Abstractions;

public interface IPlanSheetRepository
{
    Task AddAsync(PlanSheet sheet, CancellationToken cancellationToken);

    Task<PlanSheet?> GetByIdAsync(Guid sheetId, CancellationToken cancellationToken);
}
