using Avalonia;
using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class DimensionPreviewProjector
{
    public static IReadOnlyList<DimensionDto> BuildRenderedDimensions(
        IReadOnlyList<DimensionDto>? dimensions,
        DimensionPreviewEditRequest? activeEdit,
        DimensionPreviewReactiveInputs? reactiveInputs = null)
    {
        if (dimensions is not { Count: > 0 })
        {
            return dimensions ?? [];
        }

        var renderedDimensions = ApplyReactivePreview(dimensions, reactiveInputs);

        if (activeEdit is null)
        {
            return renderedDimensions;
        }

        var baseDimension = renderedDimensions.FirstOrDefault(item => item.DimensionId == activeEdit.Value.DimensionId)
                            ?? activeEdit.Value.BaseDimension;
        var editedDimension = BuildEditedDimensionPreview(activeEdit.Value, baseDimension);
        if (editedDimension is null)
        {
            return renderedDimensions;
        }

        return renderedDimensions
            .Select(item => item.DimensionId == editedDimension.DimensionId ? editedDimension : item)
            .ToArray();
    }

    private static IReadOnlyList<DimensionDto> ApplyReactivePreview(
        IReadOnlyList<DimensionDto> dimensions,
        DimensionPreviewReactiveInputs? reactiveInputs)
    {
        if (reactiveInputs is null ||
            reactiveInputs.Value.GeometryPaths.Count == 0 ||
            reactiveInputs.Value.DimensionAssociations.Count == 0)
        {
            return dimensions;
        }

        var liveEdges = MeasurableEdgeProjector.Build(
            reactiveInputs.Value.GeometryPaths,
            reactiveInputs.Value.WallCandidates,
            reactiveInputs.Value.OpeningCandidates,
            reactiveInputs.Value.MeasurementContext);

        if (liveEdges.Count == 0)
        {
            return dimensions;
        }

        return ReactiveDimensionProjector.Project(
            dimensions,
            reactiveInputs.Value.DimensionAssociations,
            liveEdges,
            reactiveInputs.Value.DimensionBindings);
    }

    private static DimensionDto? BuildEditedDimensionPreview(
        DimensionPreviewEditRequest edit,
        DimensionDto baseDimension)
    {
        var referenceWorldPoint = edit.ReferenceWorldPoint;
        var currentWorldPoint = edit.CurrentWorldPoint ?? referenceWorldPoint;
        var deltaX = RoundModelValue((decimal)(currentWorldPoint.X - referenceWorldPoint.X));
        var deltaY = RoundModelValue((decimal)(currentWorldPoint.Y - referenceWorldPoint.Y));
        if (!HasMeaningfulDifference(0m, deltaX) && !HasMeaningfulDifference(0m, deltaY))
        {
            return baseDimension;
        }

        return NativeDimensionEditor.ApplyHandleDelta(baseDimension, edit.HandleKind, deltaX, deltaY);
    }

    private static bool HasMeaningfulDifference(decimal original, decimal current)
        => Math.Abs(current - original) >= FloorPlanPreviewControl.MovementPersistenceEpsilon;

    private static decimal RoundModelValue(decimal value)
        => decimal.Round(value, 3, MidpointRounding.AwayFromZero);

    internal readonly record struct DimensionPreviewEditRequest(
        Guid DimensionId,
        DimensionDto BaseDimension,
        FloorPlanPreviewControl.DimensionHandleKind HandleKind,
        Point ReferenceWorldPoint,
        Point? CurrentWorldPoint);

    internal readonly record struct DimensionPreviewReactiveInputs(
        IReadOnlyList<GeometryPathDto> GeometryPaths,
        IReadOnlyList<WallCandidateDto> WallCandidates,
        IReadOnlyList<OpeningCandidateDto> OpeningCandidates,
        IReadOnlyList<DimensionBindingDto> DimensionBindings,
        IReadOnlyList<DimensionAssociationDto> DimensionAssociations,
        MeasurementContextDto? MeasurementContext);
}
