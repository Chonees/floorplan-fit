using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.PlanSets.Library;

public sealed class GetPlanSetLibraryHandler
{
    private const string FloorPlanSheetType = "FloorPlan";
    private const string CanonicalRegistrationStatus = "Canonical";
    private const string CanonicalProjectionStatus = "CanonicalSource";

    private readonly IFloorPlanLibraryReader floorPlanLibraryReader;
    private readonly IPlanSheetReader planSheetReader;
    private readonly IPlanSetVersionRepository? planSetVersionRepository;
    private readonly IHousePlanSetRepository? housePlanSetRepository;
    private readonly ISheetRegistrationRepository? sheetRegistrationRepository;
    private readonly ISheetAdjustmentProjectionRepository? sheetAdjustmentProjectionRepository;

    public GetPlanSetLibraryHandler(
        IFloorPlanLibraryReader floorPlanLibraryReader,
        IPlanSheetReader planSheetReader,
        IPlanSetVersionRepository? planSetVersionRepository = null,
        IHousePlanSetRepository? housePlanSetRepository = null,
        ISheetRegistrationRepository? sheetRegistrationRepository = null,
        ISheetAdjustmentProjectionRepository? sheetAdjustmentProjectionRepository = null)
    {
        this.floorPlanLibraryReader = floorPlanLibraryReader;
        this.planSheetReader = planSheetReader;
        this.planSetVersionRepository = planSetVersionRepository;
        this.housePlanSetRepository = housePlanSetRepository;
        this.sheetRegistrationRepository = sheetRegistrationRepository;
        this.sheetAdjustmentProjectionRepository = sheetAdjustmentProjectionRepository;
    }

    public async Task<IReadOnlyList<PlanSetLibraryItemDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var floorPlans = await floorPlanLibraryReader.ListAsync(cancellationToken);
        var canonicalVersionIds = floorPlans
            .Select(item => item.CurrentVersionId ?? item.CurrentVersion?.VersionId)
            .Where(item => item.HasValue)
            .Select(item => item!.Value)
            .ToArray();
        var planSetVersions = await LoadPlanSetVersionsAsync(canonicalVersionIds, cancellationToken);
        var housePlanSets = await LoadHousePlanSetsAsync(floorPlans, cancellationToken);
        var activePlanSetVersionIds = canonicalVersionIds
            .Select(id => FindPlanSetVersion(planSetVersions, id)?.Id ?? id)
            .ToArray();
        var dependentSheets = await planSheetReader.ListByPlanSetVersionIdsAsync(
            activePlanSetVersionIds,
            cancellationToken);
        var latestRegistrationStatuses = await LoadLatestRegistrationStatusesAsync(
            activePlanSetVersionIds,
            cancellationToken);
        var latestProjectionStatuses = await LoadLatestProjectionStatusesAsync(
            activePlanSetVersionIds,
            cancellationToken);

        return floorPlans
            .Select(item => Project(
                item,
                dependentSheets,
                planSetVersions,
                housePlanSets,
                latestRegistrationStatuses,
                latestProjectionStatuses))
            .ToArray();
    }

    private async Task<IReadOnlyDictionary<Guid, PlanSetVersion>> LoadPlanSetVersionsAsync(
        IReadOnlyList<Guid> canonicalFloorPlanVersionIds,
        CancellationToken cancellationToken)
    {
        if (planSetVersionRepository is null)
        {
            return new Dictionary<Guid, PlanSetVersion>();
        }

        var versions = new Dictionary<Guid, PlanSetVersion>();
        foreach (var canonicalFloorPlanVersionId in canonicalFloorPlanVersionIds)
        {
            var version = await planSetVersionRepository.GetByCanonicalFloorPlanVersionAsync(
                canonicalFloorPlanVersionId,
                cancellationToken);
            if (version is not null)
            {
                versions[canonicalFloorPlanVersionId] = version;
            }
        }

        return versions;
    }

    private async Task<IReadOnlyDictionary<Guid, HousePlanSet>> LoadHousePlanSetsAsync(
        IReadOnlyList<FloorPlanLibraryItemDto> floorPlans,
        CancellationToken cancellationToken)
    {
        if (housePlanSetRepository is null)
        {
            return new Dictionary<Guid, HousePlanSet>();
        }

        var sets = new Dictionary<Guid, HousePlanSet>();
        foreach (var floorPlan in floorPlans)
        {
            var housePlanSet = await housePlanSetRepository.GetBySourceFloorPlanTemplateAsync(
                floorPlan.TemplateId,
                cancellationToken);
            if (housePlanSet is not null)
            {
                sets[floorPlan.TemplateId] = housePlanSet;
            }
        }

        return sets;
    }

    private async Task<IReadOnlyDictionary<Guid, RegistrationState>> LoadLatestRegistrationStatusesAsync(
        IReadOnlyList<Guid> planSetVersionIds,
        CancellationToken cancellationToken)
    {
        if (sheetRegistrationRepository is null)
        {
            return new Dictionary<Guid, RegistrationState>();
        }

        var registrations = new List<SheetRegistration>();
        foreach (var planSetVersionId in planSetVersionIds)
        {
            registrations.AddRange(await sheetRegistrationRepository.ListByPlanSetVersionAsync(
                planSetVersionId,
                cancellationToken));
        }

        return registrations
            .OrderBy(item => item.CreatedAtUtc)
            .ThenBy(item => item.Id)
            .GroupBy(item => item.DependentSheetId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var registration = group.Last();
                    return new RegistrationState(
                        registration.Id,
                        registration.Status.ToString(),
                        registration.Method.ToString(),
                        registration.Confidence,
                        registration.Warning,
                        registration.RuleSummary);
                });
    }

    private async Task<IReadOnlyDictionary<Guid, ProjectionState>> LoadLatestProjectionStatusesAsync(
        IReadOnlyList<Guid> planSetVersionIds,
        CancellationToken cancellationToken)
    {
        if (sheetAdjustmentProjectionRepository is null)
        {
            return new Dictionary<Guid, ProjectionState>();
        }

        var projections = new List<SheetAdjustmentProjection>();
        foreach (var planSetVersionId in planSetVersionIds)
        {
            projections.AddRange(await sheetAdjustmentProjectionRepository.ListByPlanSetVersionAsync(
                planSetVersionId,
                cancellationToken));
        }

        return projections
            .OrderBy(item => item.CreatedAtUtc)
            .ThenBy(item => item.Id)
            .GroupBy(item => item.DependentSheetId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var projection = group.Last();
                    return new ProjectionState(
                        projection.Id,
                        projection.Status.ToString(),
                        projection.Method.ToString(),
                        projection.Confidence,
                        projection.Warning,
                        projection.RuleSummary);
                });
    }

    private static PlanSetLibraryItemDto Project(
        FloorPlanLibraryItemDto floorPlan,
        IReadOnlyDictionary<Guid, IReadOnlyList<PlanSetSheetDto>> dependentSheetsByVersion,
        IReadOnlyDictionary<Guid, PlanSetVersion> planSetVersions,
        IReadOnlyDictionary<Guid, HousePlanSet> housePlanSets,
        IReadOnlyDictionary<Guid, RegistrationState> latestRegistrationStatuses,
        IReadOnlyDictionary<Guid, ProjectionState> latestProjectionStatuses)
    {
        var currentVersion = floorPlan.CurrentVersion;
        var canonicalVersionId = floorPlan.CurrentVersionId ?? currentVersion?.VersionId;
        var activePlanSetVersionId = canonicalVersionId.HasValue
            ? FindPlanSetVersion(planSetVersions, canonicalVersionId.Value)?.Id ?? canonicalVersionId.Value
            : (Guid?)null;
        var housePlanSetId = FindHousePlanSet(housePlanSets, floorPlan.TemplateId)?.Id ?? floorPlan.TemplateId;
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

            if (activePlanSetVersionId.HasValue &&
                dependentSheetsByVersion.TryGetValue(activePlanSetVersionId.Value, out var dependentSheets))
            {
                sheets.AddRange(dependentSheets.Select(sheet =>
                {
                    var projectedSheet = sheet;
                    if (latestRegistrationStatuses.TryGetValue(sheet.SheetId, out var registration))
                    {
                        projectedSheet = projectedSheet with
                        {
                            RegistrationStatus = registration.Status,
                            SheetRegistrationId = registration.RegistrationId,
                            RegistrationMethod = registration.Method,
                            RegistrationConfidence = registration.Confidence,
                            RegistrationWarning = registration.Warning,
                            RegistrationRuleSummary = registration.RuleSummary
                        };
                    }

                    if (latestProjectionStatuses.TryGetValue(sheet.SheetId, out var projection))
                    {
                        projectedSheet = projectedSheet with
                        {
                            ProjectionStatus = projection.Status,
                            SheetProjectionId = projection.ProjectionId,
                            ProjectionMethod = projection.Method,
                            ProjectionConfidence = projection.Confidence,
                            ProjectionWarning = projection.Warning,
                            ProjectionRuleSummary = projection.RuleSummary
                        };
                    }

                    return projectedSheet;
                }));
            }
        }

        return new PlanSetLibraryItemDto(
            housePlanSetId,
            floorPlan.Code,
            floorPlan.Name,
            activePlanSetVersionId,
            canonicalVersionId,
            floorPlan.ActivePublishedCurationId,
            sheets);
    }

    private static PlanSetVersion? FindPlanSetVersion(
        IReadOnlyDictionary<Guid, PlanSetVersion> versions,
        Guid canonicalFloorPlanVersionId)
        => versions.TryGetValue(canonicalFloorPlanVersionId, out var version) ? version : null;

    private static HousePlanSet? FindHousePlanSet(
        IReadOnlyDictionary<Guid, HousePlanSet> sets,
        Guid sourceFloorPlanTemplateId)
        => sets.TryGetValue(sourceFloorPlanTemplateId, out var set) ? set : null;

    private sealed record RegistrationState(
        Guid RegistrationId,
        string Status,
        string Method,
        decimal Confidence,
        string? Warning,
        string? RuleSummary);

    private sealed record ProjectionState(
        Guid ProjectionId,
        string Status,
        string Method,
        decimal Confidence,
        string? Warning,
        string? RuleSummary);
}
