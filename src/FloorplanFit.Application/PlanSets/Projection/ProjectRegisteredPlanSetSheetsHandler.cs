using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.PlanSets.Projection;

public sealed class ProjectRegisteredPlanSetSheetsHandler
{
    private readonly ISheetRegistrationRepository sheetRegistrationRepository;
    private readonly ProjectElectricalSheetAdjustmentHandler electricalProjector;
    private readonly ProjectRoofSheetAdjustmentHandler roofProjector;
    private readonly ProjectFacadeElevationSheetAdjustmentHandler facadeProjector;

    public ProjectRegisteredPlanSetSheetsHandler(
        ISheetRegistrationRepository sheetRegistrationRepository,
        ProjectElectricalSheetAdjustmentHandler electricalProjector,
        ProjectRoofSheetAdjustmentHandler roofProjector,
        ProjectFacadeElevationSheetAdjustmentHandler facadeProjector)
    {
        this.sheetRegistrationRepository = sheetRegistrationRepository;
        this.electricalProjector = electricalProjector;
        this.roofProjector = roofProjector;
        this.facadeProjector = facadeProjector;
    }

    public async Task<ProjectRegisteredPlanSetSheetsResponse> HandleAsync(
        ProjectRegisteredPlanSetSheetsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.CanonicalPlacement);

        if (request.PlanSetVersionId == Guid.Empty)
        {
            throw new ArgumentException("Plan set version is required.", nameof(request));
        }

        if (request.CanonicalAdjustmentId == Guid.Empty)
        {
            throw new ArgumentException("Canonical adjustment is required.", nameof(request));
        }

        var registrations = await sheetRegistrationRepository.ListByPlanSetVersionAsync(
            request.PlanSetVersionId,
            cancellationToken);
        var latestRegistrations = registrations
            .GroupBy(registration => registration.DependentSheetId)
            .Select(group => group
                .OrderBy(registration => registration.CreatedAtUtc)
                .ThenBy(registration => registration.Id)
                .Last())
            .OrderBy(registration => registration.CreatedAtUtc)
            .ThenBy(registration => registration.Id)
            .ToArray();
        var projections = new List<SheetAdjustmentProjectionDto>(latestRegistrations.Length);

        foreach (var registration in latestRegistrations)
        {
            projections.Add(await ProjectAsync(registration, request, cancellationToken));
        }

        return new ProjectRegisteredPlanSetSheetsResponse(projections.Count, projections);
    }

    private Task<SheetAdjustmentProjectionDto> ProjectAsync(
        SheetRegistration registration,
        ProjectRegisteredPlanSetSheetsRequest request,
        CancellationToken cancellationToken)
    {
        return registration.Method switch
        {
            SheetRegistrationMethod.WholeSheetSimilarity => electricalProjector.HandleAsync(
                new ProjectElectricalSheetAdjustmentRequest(
                    registration.Id,
                    request.CanonicalAdjustmentId,
                    request.CanonicalPlacement,
                    request.CanonicalRecipe),
                cancellationToken),
            SheetRegistrationMethod.RoofFootprintWithOverhang => roofProjector.HandleAsync(
                new ProjectRoofSheetAdjustmentRequest(
                    registration.Id,
                    request.CanonicalAdjustmentId,
                    request.CanonicalPlacement,
                    request.CanonicalRecipe),
                cancellationToken),
            SheetRegistrationMethod.FacadeHorizontalReference => facadeProjector.HandleAsync(
                new ProjectFacadeElevationSheetAdjustmentRequest(
                    registration.Id,
                    request.CanonicalAdjustmentId,
                    request.CanonicalPlacement,
                    request.CanonicalRecipe),
                cancellationToken),
            _ => throw new InvalidOperationException("Unsupported sheet registration method.")
        };
    }
}
