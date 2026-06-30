using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Contracts.PlanSets;

namespace FloorplanFit.Application.PlanSets.Library;

public sealed class GetPlanSetLibraryHandler
{
    private const string FloorPlanSheetType = "FloorPlan";
    private const string CanonicalRegistrationStatus = "Canonical";
    private const string CanonicalProjectionStatus = "CanonicalSource";

    private readonly IFloorPlanLibraryReader floorPlanLibraryReader;

    public GetPlanSetLibraryHandler(IFloorPlanLibraryReader floorPlanLibraryReader)
    {
        this.floorPlanLibraryReader = floorPlanLibraryReader;
    }

    public async Task<IReadOnlyList<PlanSetLibraryItemDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var floorPlans = await floorPlanLibraryReader.ListAsync(cancellationToken);
        return floorPlans.Select(Project).ToArray();
    }

    private static PlanSetLibraryItemDto Project(FloorPlanLibraryItemDto floorPlan)
    {
        var currentVersion = floorPlan.CurrentVersion;
        var canonicalVersionId = floorPlan.CurrentVersionId ?? currentVersion?.VersionId;
        IReadOnlyList<PlanSetSheetDto> sheets = canonicalVersionId.HasValue
            ?
            [
                new PlanSetSheetDto(
                    canonicalVersionId.Value,
                    FloorPlanSheetType,
                    $"{floorPlan.Name} Floor Plan",
                    Guid.Empty,
                    canonicalVersionId.Value,
                    IsCanonical: true,
                    CanonicalRegistrationStatus,
                    CanonicalProjectionStatus)
            ]
            : [];

        return new PlanSetLibraryItemDto(
            floorPlan.TemplateId,
            floorPlan.Code,
            floorPlan.Name,
            canonicalVersionId,
            canonicalVersionId,
            floorPlan.ActivePublishedCurationId,
            sheets);
    }
}
