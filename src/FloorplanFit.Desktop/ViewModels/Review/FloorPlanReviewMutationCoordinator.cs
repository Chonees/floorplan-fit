using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls;
using FloorplanFit.Domain.Documents;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Extensions.DependencyInjection;

namespace FloorplanFit.Desktop.ViewModels;

internal sealed class FloorPlanReviewMutationCoordinator
{
    private readonly IServiceScopeFactory scopeFactory;

    public FloorPlanReviewMutationCoordinator(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;
    }

    public async Task SaveArtifactPositionAsync(
        Guid draftCurationId,
        FloorPlanPreviewControl.MovableArtifactMovedEventArgs movement,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<SaveFloorPlanArtifactPositionHandler>();
        await handler.HandleAsync(
            draftCurationId,
            movement.SourceArtifactKind,
            movement.SourceArtifactId,
            movement.PositionMode,
            movement.ResolvedX,
            movement.ResolvedY,
            movement.TranslationDx,
            movement.TranslationDy,
            cancellationToken);
    }

    public async Task RejectWallCandidateAsync(
        Guid draftCurationId,
        Guid candidateId,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<RejectWallCandidateHandler>();
        await handler.HandleAsync(draftCurationId, candidateId, cancellationToken);
    }

    public async Task PublishCurationAsync(
        Guid templateId,
        Guid draftCurationId,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<PublishFloorPlanCurationHandler>();
        await handler.HandleAsync(templateId, draftCurationId, cancellationToken);
    }

    // closingEdge trails the cancellation token on purpose: it is additive, and every pre-existing
    // positional call site keeps binding its 4th argument to the token.
    public async Task<Guid> AddPinchGroupAsync(
        Guid draftCurationId,
        string groupName,
        PinchAxisTag axisTag,
        CancellationToken cancellationToken,
        string? closingEdge = null)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<AddPinchGroupHandler>();
        return await handler.HandleAsync(draftCurationId, groupName, axisTag, cancellationToken, closingEdge);
    }

    public async Task<Guid> AddManualWallCandidateAsync(
        Guid templateId,
        Guid? floorPlanVersionId,
        decimal startX,
        decimal startY,
        decimal endX,
        decimal endY,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<AddManualWallCandidateHandler>();
        return await handler.HandleAsync(
            templateId,
            floorPlanVersionId,
            startX,
            startY,
            endX,
            endY,
            cancellationToken);
    }

    public async Task RemovePinchGroupAsync(
        Guid draftCurationId,
        Guid pinchGroupId,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<RemovePinchGroupHandler>();
        await handler.HandleAsync(draftCurationId, pinchGroupId, cancellationToken);
    }

    public async Task RenamePinchGroupAsync(
        Guid draftCurationId,
        Guid pinchGroupId,
        string groupName,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<RenamePinchGroupHandler>();
        await handler.HandleAsync(draftCurationId, pinchGroupId, groupName, cancellationToken);
    }

    public async Task SetPinchGroupClosingEdgeAsync(
        Guid draftCurationId,
        Guid pinchGroupId,
        string closingEdge,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<SetPinchGroupClosingEdgeHandler>();
        await handler.HandleAsync(draftCurationId, pinchGroupId, closingEdge, cancellationToken);
    }

    public async Task<Guid> AddMeasurementCorridorAsync(
        Guid draftCurationId,
        string name,
        PinchAxisTag axisTag,
        Guid guideGeometryPathId,
        decimal bandMinCoordinate,
        decimal bandMaxCoordinate,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<AddMeasurementCorridorHandler>();
        return await handler.HandleAsync(
            draftCurationId,
            name,
            axisTag,
            guideGeometryPathId,
            bandMinCoordinate,
            bandMaxCoordinate,
            cancellationToken);
    }

    public async Task RemoveMeasurementCorridorAsync(
        Guid draftCurationId,
        Guid corridorId,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<RemoveMeasurementCorridorHandler>();
        await handler.HandleAsync(draftCurationId, corridorId, cancellationToken);
    }

    public async Task RemoveMeasurementNodeAsync(
        Guid draftCurationId,
        Guid nodeId,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<RemoveMeasurementNodeHandler>();
        await handler.HandleAsync(draftCurationId, nodeId, cancellationToken);
    }

    public async Task ChangeMeasurementCorridorAxisAsync(
        Guid draftCurationId,
        Guid corridorId,
        PinchAxisTag axisTag,
        decimal bandMinCoordinate,
        decimal bandMaxCoordinate,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ChangeMeasurementCorridorAxisHandler>();
        await handler.HandleAsync(
            draftCurationId,
            corridorId,
            axisTag,
            bandMinCoordinate,
            bandMaxCoordinate,
            cancellationToken);
    }

    public async Task<Guid> AddMeasurementNodeAsync(
        Guid draftCurationId,
        Guid corridorId,
        string referenceKind,
        string sourceArtifactKind,
        Guid sourceArtifactId,
        Guid geometryPathId,
        string snapKind,
        decimal anchorX,
        decimal anchorY,
        decimal axisCoordinate,
        decimal offsetAlongAxis,
        decimal offsetNormal,
        decimal positionRatio,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<AddMeasurementNodeHandler>();
        return await handler.HandleAsync(
            draftCurationId,
            corridorId,
            referenceKind,
            sourceArtifactKind,
            sourceArtifactId,
            geometryPathId,
            snapKind,
            anchorX,
            anchorY,
            axisCoordinate,
            offsetAlongAxis,
            offsetNormal,
            positionRatio,
            cancellationToken);
    }

    public async Task SaveDimensionIntervalBindingAsync(
        Guid draftCurationId,
        Guid dimensionId,
        Guid corridorId,
        Guid startNodeId,
        Guid endNodeId,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<SaveDimensionIntervalBindingHandler>();
        await handler.HandleAsync(
            draftCurationId,
            dimensionId,
            corridorId,
            startNodeId,
            endNodeId,
            cancellationToken);
    }

    public async Task RestoreDimensionIntervalBindingAsync(
        Guid draftCurationId,
        Guid dimensionId,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<RestoreDimensionIntervalBindingHandler>();
        await handler.HandleAsync(
            draftCurationId,
            dimensionId,
            cancellationToken);
    }

    public async Task AddPinchMarkerAsync(
        Guid draftCurationId,
        Guid sourceCandidateId,
        Guid pinchGroupId,
        decimal positionRatio,
        decimal maxTrimMm,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<AddPinchMarkerHandler>();
        await handler.HandleAsync(
            draftCurationId,
            sourceCandidateId,
            pinchGroupId,
            positionRatio,
            maxTrimMm,
            cancellationToken);
    }

    public async Task UpdatePinchMarkerMaxTrimAsync(
        Guid draftCurationId,
        Guid pinchMarkerId,
        decimal maxTrimMm,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<UpdatePinchMarkerMaxTrimHandler>();
        await handler.HandleAsync(
            draftCurationId,
            pinchMarkerId,
            maxTrimMm,
            cancellationToken);
    }

    public async Task SaveLabelTextHeightAsync(
        Guid draftCurationId,
        string sourceArtifactKind,
        Guid sourceArtifactId,
        decimal resolvedTextHeight,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<SaveFloorPlanLabelTextHeightHandler>();
        await handler.HandleAsync(
            draftCurationId,
            sourceArtifactKind,
            sourceArtifactId,
            resolvedTextHeight,
            cancellationToken);
    }

    public async Task RestoreLabelTextHeightAsync(
        Guid draftCurationId,
        string sourceArtifactKind,
        Guid sourceArtifactId,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<RestoreFloorPlanLabelTextHeightHandler>();
        await handler.HandleAsync(
            draftCurationId,
            sourceArtifactKind,
            sourceArtifactId,
            cancellationToken);
    }

    public async Task SaveDimensionOverrideAsync(
        Guid draftCurationId,
        DimensionDto dimension,
        DimensionBindingDto? manualBindingOverride,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<SaveFloorPlanDimensionOverrideHandler>();
        await handler.HandleAsync(draftCurationId, dimension, manualBindingOverride, cancellationToken);
    }

    public async Task RestoreArtifactPositionAsync(
        Guid draftCurationId,
        RestoreArtifactPositionRequest request,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<RestoreFloorPlanArtifactPositionHandler>();
        await handler.HandleAsync(
            draftCurationId,
            request.SourceArtifactKind,
            request.SourceArtifactId,
            request.PositionMode,
            request.ResolvedX,
            request.ResolvedY,
            request.TranslationDx,
            request.TranslationDy,
            cancellationToken);
    }

    public async Task RestoreDimensionOverrideAsync(
        Guid draftCurationId,
        string sourceDimensionKey,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<RestoreFloorPlanDimensionOverrideHandler>();
        await handler.HandleAsync(draftCurationId, sourceDimensionKey, cancellationToken);
    }

    public async Task SaveCuratedArtifactClassificationAsync(
        Guid draftCurationId,
        string sourceArtifactKind,
        Guid sourceArtifactId,
        string resolvedFamily,
        string resolvedCategory,
        string resolvedType,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<SaveCuratedArtifactClassificationHandler>();
        await handler.HandleAsync(
            draftCurationId,
            sourceArtifactKind,
            sourceArtifactId,
            resolvedFamily,
            resolvedCategory,
            resolvedType,
            cancellationToken);
    }

    public async Task RestoreCuratedArtifactClassificationAsync(
        Guid draftCurationId,
        string sourceArtifactKind,
        Guid sourceArtifactId,
        string detectedFamily,
        string detectedCategory,
        string detectedType,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<RestoreCuratedArtifactClassificationHandler>();
        await handler.HandleAsync(
            draftCurationId,
            sourceArtifactKind,
            sourceArtifactId,
            detectedFamily,
            detectedCategory,
            detectedType,
            cancellationToken);
    }

    public async Task ExcludeCuratedArtifactAsync(
        Guid draftCurationId,
        string sourceArtifactKind,
        Guid sourceArtifactId,
        string resolvedFamily,
        string resolvedCategory,
        string resolvedType,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ExcludeCuratedArtifactHandler>();
        await handler.HandleAsync(
            draftCurationId,
            sourceArtifactKind,
            sourceArtifactId,
            resolvedFamily,
            resolvedCategory,
            resolvedType,
            cancellationToken);
    }

    public async Task RemoveRoomLabelAsync(Guid roomLabelId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<RemoveRoomLabelHandler>();
        await handler.HandleAsync(roomLabelId, cancellationToken);
    }

    public async Task RemoveOpeningLabelAsync(Guid openingLabelId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<RemoveOpeningLabelHandler>();
        await handler.HandleAsync(openingLabelId, cancellationToken);
    }

    public async Task RemovePinchMarkerAsync(Guid pinchMarkerId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<RemovePinchMarkerHandler>();
        await handler.HandleAsync(pinchMarkerId, cancellationToken);
    }

    public async Task<ExportAdjustedDxfResponse> ExportAdjustedDxfAsync(
        Guid templateId,
        Guid? floorPlanVersionId,
        Guid draftCurationId,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ExportAdjustedDxfHandler>();
        return await handler.HandleAsync(templateId, floorPlanVersionId, draftCurationId, cancellationToken);
    }

    internal readonly record struct RestoreArtifactPositionRequest(
        string SourceArtifactKind,
        Guid SourceArtifactId,
        FloorPlanArtifactPositionMode PositionMode,
        decimal? ResolvedX,
        decimal? ResolvedY,
        decimal? TranslationDx,
        decimal? TranslationDy);
}
