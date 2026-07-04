using FloorplanFit.Contracts.PlanSets;

namespace FloorplanFit.Application.Abstractions;

public interface IPlanSheetReader
{
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<PlanSetSheetDto>>> ListByPlanSetVersionIdsAsync(
        IReadOnlyCollection<Guid> planSetVersionIds,
        CancellationToken cancellationToken);
}
