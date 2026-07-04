using FloorplanFit.Contracts.PlanSets;

namespace FloorplanFit.Application.Abstractions;

public interface IPlanSheetSourceReader
{
    Task<PlanSheetSourceDto?> GetBySheetIdAsync(Guid sheetId, CancellationToken cancellationToken);
}
