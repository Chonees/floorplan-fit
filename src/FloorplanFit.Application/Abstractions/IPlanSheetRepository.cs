using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Abstractions;

public interface IPlanSheetRepository
{
    Task AddAsync(PlanSheet sheet, CancellationToken cancellationToken);

    Task<PlanSheet?> GetByIdAsync(Guid sheetId, CancellationToken cancellationToken);

    Task UpdateAsync(PlanSheet sheet, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    Task RemoveAsync(Guid sheetId, CancellationToken cancellationToken);
}
