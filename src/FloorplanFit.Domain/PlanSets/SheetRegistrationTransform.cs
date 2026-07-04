namespace FloorplanFit.Domain.PlanSets;

public sealed class SheetRegistrationTransform
{
    public SheetRegistrationTransform(
        decimal scale,
        decimal rotationDegrees,
        decimal translateX,
        decimal translateY)
    {
        if (scale <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), "Registration scale must be greater than zero.");
        }

        Scale = scale;
        RotationDegrees = rotationDegrees;
        TranslateX = translateX;
        TranslateY = translateY;
    }

    public decimal Scale { get; }

    public decimal RotationDegrees { get; }

    public decimal TranslateX { get; }

    public decimal TranslateY { get; }
}
