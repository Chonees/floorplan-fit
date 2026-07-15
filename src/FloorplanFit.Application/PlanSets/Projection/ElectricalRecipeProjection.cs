using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.PlanSets.Projection;

public static class ElectricalRecipeProjection
{
    private const decimal CoordinateTolerance = 0.01m;

    public static (decimal X, decimal Y) ProjectPoint(
        decimal electricalX,
        decimal electricalY,
        SheetRegistrationTransform registration,
        AdjustmentRecipeSummaryDto recipe,
        ProjectedPlanSheetOutlineNormalization? outlineNormalization = null)
    {
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentNullException.ThrowIfNull(recipe);

        var floor = NormalizeRegisteredPoint(
            RegisterElectricalPoint(electricalX, electricalY, registration),
            outlineNormalization);
        var adjusted = ApplyRecipe(floor.X, floor.Y, recipe);

        return (
            (adjusted.X * recipe.FloorToSiteScale) + recipe.SiteOffsetX,
            (adjusted.Y * recipe.FloorToSiteScale) + recipe.SiteOffsetY);
    }

    private static (decimal X, decimal Y) NormalizeRegisteredPoint(
        (decimal X, decimal Y) point,
        ProjectedPlanSheetOutlineNormalization? normalization)
    {
        if (normalization is null)
        {
            return point;
        }

        return (
            NormalizeAxis(
                point.X,
                normalization.SourceMinX,
                normalization.SourceMaxX,
                normalization.ScaleX,
                normalization.AnchorX),
            NormalizeAxis(
                point.Y,
                normalization.SourceMinY,
                normalization.SourceMaxY,
                normalization.ScaleY,
                normalization.AnchorY));
    }

    private static decimal NormalizeAxis(
        decimal value,
        decimal min,
        decimal max,
        decimal scale,
        string anchor)
        => anchor switch
        {
            "Min" => min + ((value - min) * scale),
            "Max" => max - ((max - value) * scale),
            _ => ((min + max) / 2m) + ((value - ((min + max) / 2m)) * scale)
        };

    private static (decimal X, decimal Y) RegisterElectricalPoint(
        decimal x,
        decimal y,
        SheetRegistrationTransform registration)
    {
        var radians = (double)registration.RotationDegrees * Math.PI / 180d;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        var sourceX = (double)x;
        var sourceY = (double)y;
        var scale = (double)registration.Scale;

        return (
            (decimal)NormalizeTiny(((sourceX * cos) - (sourceY * sin)) * scale) + registration.TranslateX,
            (decimal)NormalizeTiny(((sourceX * sin) + (sourceY * cos)) * scale) + registration.TranslateY);
    }

    private static (decimal X, decimal Y) ApplyRecipe(
        decimal x,
        decimal y,
        AdjustmentRecipeSummaryDto recipe)
    {
        foreach (var operation in recipe.Operations)
        {
            if (IsHorizontal(operation) &&
                string.Equals(operation.Edge, "Right", StringComparison.OrdinalIgnoreCase) &&
                x >= operation.Coordinate - CoordinateTolerance)
            {
                x -= operation.DeltaSourceUnits;
            }
            else if (IsHorizontal(operation) &&
                     string.Equals(operation.Edge, "Left", StringComparison.OrdinalIgnoreCase) &&
                     x <= operation.Coordinate + CoordinateTolerance)
            {
                x += operation.DeltaSourceUnits;
            }
            else if (IsVertical(operation) &&
                     string.Equals(operation.Edge, "Top", StringComparison.OrdinalIgnoreCase) &&
                     y >= operation.Coordinate - CoordinateTolerance)
            {
                y -= operation.DeltaSourceUnits;
            }
            else if (IsVertical(operation) &&
                     string.Equals(operation.Edge, "Bottom", StringComparison.OrdinalIgnoreCase) &&
                     y <= operation.Coordinate + CoordinateTolerance)
            {
                y += operation.DeltaSourceUnits;
            }
        }

        return (x, y);
    }

    private static bool IsHorizontal(AdjustmentRecipeOperationDto operation)
        => string.Equals(operation.AxisTag, "Width", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(operation.Kind, "HorizontalCompression", StringComparison.OrdinalIgnoreCase);

    private static bool IsVertical(AdjustmentRecipeOperationDto operation)
        => string.Equals(operation.AxisTag, "Height", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(operation.Kind, "VerticalCompression", StringComparison.OrdinalIgnoreCase);

    private static double NormalizeTiny(double value)
        => Math.Abs(value) < 0.000000001d ? 0d : value;
}
