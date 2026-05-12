using Avalonia;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class DimensionPreviewProjector
{
    public static IReadOnlyList<DimensionDto> BuildRenderedDimensions(
        IReadOnlyList<DimensionDto>? dimensions,
        DimensionPreviewEditRequest? activeEdit)
    {
        if (dimensions is not { Count: > 0 })
        {
            return dimensions ?? [];
        }

        if (activeEdit is null)
        {
            return dimensions;
        }

        var baseDimension = dimensions.FirstOrDefault(item => item.DimensionId == activeEdit.Value.DimensionId)
                            ?? activeEdit.Value.BaseDimension;
        var editedDimension = BuildEditedDimensionPreview(activeEdit.Value, baseDimension);
        if (editedDimension is null)
        {
            return dimensions;
        }

        return dimensions
            .Select(item => item.DimensionId == editedDimension.DimensionId ? editedDimension : item)
            .ToArray();
    }

    private static DimensionDto? BuildEditedDimensionPreview(
        DimensionPreviewEditRequest edit,
        DimensionDto baseDimension)
    {
        var baseHandlePoint = NativeDimensionEditor.ResolveHandlePoint(baseDimension, edit.HandleKind);
        var currentWorldPoint = edit.CurrentWorldPoint ?? baseHandlePoint;
        var deltaX = RoundModelValue((decimal)(currentWorldPoint.X - baseHandlePoint.X));
        var deltaY = RoundModelValue((decimal)(currentWorldPoint.Y - baseHandlePoint.Y));
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
        Point? CurrentWorldPoint);
}
