using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls;
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
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<SaveFloorPlanDimensionOverrideHandler>();
        await handler.HandleAsync(draftCurationId, dimension, cancellationToken);
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

    internal readonly record struct RestoreArtifactPositionRequest(
        string SourceArtifactKind,
        Guid SourceArtifactId,
        FloorPlanArtifactPositionMode PositionMode,
        decimal? ResolvedX,
        decimal? ResolvedY,
        decimal? TranslationDx,
        decimal? TranslationDy);
}
