using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.SitePlanAdjustment;

public sealed record CommissionedHouseFitRequestBuildResult(
    bool Succeeded,
    CommissionedHouseFitRequest? Request,
    string RejectionReason);

public static class CommissionedHouseFitRequestFactory
{
    private const decimal MillimetersPerInch = 25.4m;

    public static CommissionedHouseFitRequestBuildResult Create(
        IReadOnlyList<GeometryPathDto> projectedFloorPlanGeometryPaths,
        SitePlanBuildableAreaDto buildableArea,
        decimal sitePlanToMillimetersFactor,
        IReadOnlyCollection<Guid>? placementGeometryPathIds = null)
    {
        ArgumentNullException.ThrowIfNull(projectedFloorPlanGeometryPaths);
        ArgumentNullException.ThrowIfNull(buildableArea);

        if (sitePlanToMillimetersFactor <= 0m)
        {
            return Reject("Site-plan measurement factor must be positive.");
        }

        try
        {
            var buildableWidth = checked(buildableArea.MaxX - buildableArea.MinX);
            var buildableDepth = checked(buildableArea.MaxY - buildableArea.MinY);
            if (buildableWidth <= 0m || buildableDepth <= 0m)
            {
                return Reject("Site-plan buildable area must have positive Width and Depth.");
            }

            var selectedIds = placementGeometryPathIds is { Count: > 0 }
                ? placementGeometryPathIds.ToHashSet()
                : null;
            IReadOnlyList<GeometryPathDto> placementGeometry = selectedIds is null
                ? projectedFloorPlanGeometryPaths
                : projectedFloorPlanGeometryPaths
                    .Where(path => selectedIds.Contains(path.Id))
                    .ToArray();
            if (placementGeometry.Count == 0)
            {
                return Reject("No commissioned structural placement geometry is available.");
            }

            var footprint = StructuralFootprint.Resolve(placementGeometry);
            if (footprint is null || footprint.Value.Width <= 0m || footprint.Value.Height <= 0m)
            {
                return Reject("A positive structural FloorPlan footprint could not be resolved.");
            }

            return new CommissionedHouseFitRequestBuildResult(
                true,
                new CommissionedHouseFitRequest(
                    ToInches(footprint.Value.Width, sitePlanToMillimetersFactor),
                    ToInches(footprint.Value.Height, sitePlanToMillimetersFactor),
                    ToInches(buildableWidth, sitePlanToMillimetersFactor),
                    ToInches(buildableDepth, sitePlanToMillimetersFactor)),
                string.Empty);
        }
        catch (OverflowException)
        {
            return Reject("FloorPlan or buildable dimensions cannot be represented safely in inches.");
        }
    }

    private static decimal ToInches(decimal value, decimal toMillimetersFactor)
        => checked(value * toMillimetersFactor / MillimetersPerInch);

    private static CommissionedHouseFitRequestBuildResult Reject(string reason)
        => new(false, null, reason);
}
