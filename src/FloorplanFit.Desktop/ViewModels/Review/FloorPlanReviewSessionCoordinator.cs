using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Extensions.DependencyInjection;

namespace FloorplanFit.Desktop.ViewModels;

internal sealed class FloorPlanReviewSessionCoordinator
{
    private readonly IServiceScopeFactory scopeFactory;

    public FloorPlanReviewSessionCoordinator(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;
    }

    public async Task<ReviewSessionLoadResult> OpenAsync(
        Guid templateId,
        Guid? floorPlanVersionId,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<OpenFloorPlanReviewSessionHandler>();
        var response = floorPlanVersionId is null
            ? await handler.HandleAsync(templateId, cancellationToken)
            : await handler.HandleAsync(templateId, floorPlanVersionId.Value, cancellationToken);

        return new ReviewSessionLoadResult(
            response.DraftCurationId,
            BuildProjection(response.Session));
    }

    public async Task<ReviewSessionLoadResult> RefreshAsync(
        Guid templateId,
        Guid? floorPlanVersionId,
        Guid currentDraftCurationId,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetFloorPlanReviewSessionHandler>();
        FloorPlanReviewSessionDto? session = currentDraftCurationId != Guid.Empty
            ? await handler.HandleByCurationAsync(templateId, currentDraftCurationId, cancellationToken)
            : floorPlanVersionId is null
                ? await handler.HandleAsync(templateId, cancellationToken)
                : await handler.HandleAsync(templateId, floorPlanVersionId.Value, cancellationToken);

        if (session is null)
        {
            throw new InvalidOperationException("Floor plan review session was not found.");
        }

        var nextDraftCurationId = string.Equals(session.Status, "Published", StringComparison.OrdinalIgnoreCase)
            ? Guid.Empty
            : currentDraftCurationId;

        return new ReviewSessionLoadResult(
            nextDraftCurationId,
            BuildProjection(session));
    }

    public async Task<ReviewSessionLoadResult> StartEditingPublishedAsync(
        Guid templateId,
        Guid? floorPlanVersionId,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<EditPublishedFloorPlanCurationHandler>();
        var response = floorPlanVersionId is null
            ? await handler.HandleAsync(templateId, cancellationToken)
            : await handler.HandleAsync(templateId, floorPlanVersionId.Value, cancellationToken);

        return new ReviewSessionLoadResult(
            response.DraftCurationId,
            BuildProjection(response.Session));
    }

    private static ReviewSessionProjection BuildProjection(FloorPlanReviewSessionDto session)
    {
        var curatedPlanArtifacts = ResolveCuratedArtifacts(session);
        var visibleCuratedPlanArtifacts = curatedPlanArtifacts
            .Where(item => !string.Equals(item.DecisionState, FloorPlanArtifactDecisionState.Excluded.ToString(), StringComparison.Ordinal))
            .ToArray();
        var dimensionAssociationsById = session.DimensionAssociations.ToDictionary(item => item.DimensionId);

        return new ReviewSessionProjection(
            session.Code,
            session.Name,
            session.Status,
            session.ActiveVersionNumber,
            session.ActivePublishedCurationId,
            session.GeometryPaths,
            session.RoomLabels,
            session.OpeningCandidates,
            session.OpeningLabels,
            session.Dimensions,
            session.MeasurementContext,
            session.DimensionBindings,
            session.DimensionAssociations,
            session.FixedPlanComponents,
            session.ProtectedDetailAssemblies,
            session.WallCandidates,
            session.PinchGroups,
            session.PinchMarkers,
            session.MeasurementCorridors,
            session.MeasurementNodes,
            session.DimensionIntervalBindings,
            session.ArticulationBands,
            curatedPlanArtifacts,
            visibleCuratedPlanArtifacts,
            dimensionAssociationsById);
    }

    private static IReadOnlyList<CuratedPlanArtifactDto> ResolveCuratedArtifacts(FloorPlanReviewSessionDto session)
    {
        if (session.CuratedPlanArtifacts.Count > 0)
        {
            return SortCuratedArtifacts(session.CuratedPlanArtifacts);
        }

        var items = new List<CuratedPlanArtifactDto>();
        items.AddRange(session.OpeningCandidates.Select(CreateDetectedCuratedArtifact));
        items.AddRange(session.FixedPlanComponents.Select(CreateDetectedCuratedArtifact));
        items.AddRange(session.ProtectedDetailAssemblies.Select(CreateDetectedCuratedArtifact));
        return SortCuratedArtifacts(items);
    }

    private static CuratedPlanArtifactDto CreateDetectedCuratedArtifact(OpeningCandidateDto artifact)
    {
        var detected = FloorPlanArtifactTaxonomy.ResolveDetectedOpeningClassification(artifact.Kind);
        return new CuratedPlanArtifactDto(
            artifact.OpeningCandidateId,
            FloorPlanArtifactSourceKinds.OpeningCandidate,
            artifact.SourceEntityRef,
            artifact.SourceLayer,
            artifact.SourceEntityKind,
            null,
            artifact.GeometryPathId is null ? [] : [artifact.GeometryPathId.Value],
            artifact.Confidence,
            artifact.DetectionNotes,
            artifact.SortOrder,
            detected.Family,
            detected.Category,
            detected.Type,
            detected.Family,
            detected.Category,
            detected.Type,
            FloorPlanArtifactDecisionState.DetectedDefault.ToString(),
            FloorPlanArtifactTaxonomy.ResolveColorArgb(detected.Family, detected.Category, detected.Type));
    }

    private static CuratedPlanArtifactDto CreateDetectedCuratedArtifact(FixedPlanComponentDto artifact)
    {
        var detected = FloorPlanArtifactTaxonomy.ResolveDetectedFixedClassification(artifact.Kind);
        return new CuratedPlanArtifactDto(
            artifact.FixedPlanComponentId,
            FloorPlanArtifactSourceKinds.FixedPlanComponent,
            artifact.SourceEntityRef,
            artifact.SourceLayer,
            artifact.SourceEntityKind,
            artifact.SourceBlockName,
            artifact.GeometryPathIds,
            artifact.Confidence,
            artifact.DetectionNotes,
            artifact.SortOrder,
            detected.Family,
            detected.Category,
            detected.Type,
            detected.Family,
            detected.Category,
            detected.Type,
            FloorPlanArtifactDecisionState.DetectedDefault.ToString(),
            FloorPlanArtifactTaxonomy.ResolveColorArgb(detected.Family, detected.Category, detected.Type));
    }

    private static CuratedPlanArtifactDto CreateDetectedCuratedArtifact(ProtectedDetailAssemblyDto artifact)
    {
        var detected = FloorPlanArtifactTaxonomy.ResolveDetectedProtectedClassification(artifact.Kind);
        return new CuratedPlanArtifactDto(
            artifact.ProtectedDetailAssemblyId,
            FloorPlanArtifactSourceKinds.ProtectedDetailAssembly,
            artifact.SourceEntityRef,
            artifact.SourceLayer,
            artifact.SourceEntityKind,
            null,
            artifact.GeometryPathIds,
            artifact.Confidence,
            artifact.DetectionNotes,
            artifact.SortOrder,
            detected.Family,
            detected.Category,
            detected.Type,
            detected.Family,
            detected.Category,
            detected.Type,
            FloorPlanArtifactDecisionState.DetectedDefault.ToString(),
            FloorPlanArtifactTaxonomy.ResolveColorArgb(detected.Family, detected.Category, detected.Type));
    }

    private static IReadOnlyList<CuratedPlanArtifactDto> SortCuratedArtifacts(IEnumerable<CuratedPlanArtifactDto> artifacts)
    {
        return artifacts
            .OrderBy(item => FloorPlanArtifactTaxonomy.ResolveFamilySortOrder(item.ResolvedFamily))
            .ThenBy(item => FloorPlanArtifactTaxonomy.ResolveCategorySortOrder(item.ResolvedFamily, item.ResolvedCategory))
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.SourceEntityRef, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

internal readonly record struct ReviewSessionLoadResult(
    Guid DraftCurationId,
    ReviewSessionProjection Projection);

internal readonly record struct ReviewSessionProjection(
    string Code,
    string Name,
    string Status,
    int ActiveVersionNumber,
    Guid? ActivePublishedCurationId,
    IReadOnlyList<GeometryPathDto> GeometryPaths,
    IReadOnlyList<RoomLabelDto> RoomLabels,
    IReadOnlyList<OpeningCandidateDto> OpeningCandidates,
    IReadOnlyList<OpeningLabelDto> OpeningLabels,
    IReadOnlyList<DimensionDto> Dimensions,
    MeasurementContextDto? MeasurementContext,
    IReadOnlyList<DimensionBindingDto> DimensionBindings,
    IReadOnlyList<DimensionAssociationDto> DimensionAssociations,
    IReadOnlyList<FixedPlanComponentDto> FixedPlanComponents,
    IReadOnlyList<ProtectedDetailAssemblyDto> ProtectedDetailAssemblies,
    IReadOnlyList<WallCandidateDto> WallCandidates,
    IReadOnlyList<PinchGroupDto> PinchGroups,
    IReadOnlyList<PinchMarkerDto> PinchMarkers,
    IReadOnlyList<MeasurementCorridorDto> MeasurementCorridors,
    IReadOnlyList<MeasurementNodeDto> MeasurementNodes,
    IReadOnlyList<DimensionIntervalBindingDto> DimensionIntervalBindings,
    IReadOnlyList<ArticulationBandDto> ArticulationBands,
    IReadOnlyList<CuratedPlanArtifactDto> CuratedPlanArtifacts,
    IReadOnlyList<CuratedPlanArtifactDto> VisibleCuratedPlanArtifacts,
    IReadOnlyDictionary<Guid, DimensionAssociationDto> DimensionAssociationsById);
