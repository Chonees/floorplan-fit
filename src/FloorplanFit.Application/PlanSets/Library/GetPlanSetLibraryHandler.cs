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
    private readonly IPlanSheetReader planSheetReader;

    public GetPlanSetLibraryHandler(
        IFloorPlanLibraryReader floorPlanLibraryReader,
        IPlanSheetReader planSheetReader)
    {
        this.floorPlanLibraryReader = floorPlanLibraryReader;
        this.planSheetReader = planSheetReader;
    }

    public async Task<IReadOnlyList<PlanSetLibraryItemDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var floorPlans = await floorPlanLibraryReader.ListAsync(cancellationToken);
        var versionIds = floorPlans
            .Select(item => item.CurrentVersionId ?? item.CurrentVersion?.VersionId)
            .Where(item => item.HasValue)
            .Select(item => item!.Value)
            .ToArray();
        var dependentSheets = await planSheetReader.ListByPlanSetVersionIdsAsync(versionIds, cancellationToken);
        return floorPlans.Select(item => Project(item, dependentSheets)).ToArray();
    }

    private static PlanSetLibraryItemDto Project(
        FloorPlanLibraryItemDto floorPlan,
        IReadOnlyDictionary<Guid, IReadOnlyList<PlanSetSheetDto>> dependentSheetsByVersion)
    {
        var currentVersion = floorPlan.CurrentVersion;
        var canonicalVersionId = floorPlan.CurrentVersionId ?? currentVersion?.VersionId;
        var sheets = new List<PlanSetSheetDto>();

        if (canonicalVersionId.HasValue)
        {
            sheets.Add(new PlanSetSheetDto(
                canonicalVersionId.Value,
                FloorPlanSheetType,
                $"{floorPlan.Name} Floor Plan",
                Guid.Empty,
                canonicalVersionId.Value,
                IsCanonical: true,
                CanonicalRegistrationStatus,
                CanonicalProjectionStatus));

            if (dependentSheetsByVersion.TryGetValue(canonicalVersionId.Value, out var dependentSheets))
            {
                sheets.AddRange(dependentSheets);
            }
        }

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
