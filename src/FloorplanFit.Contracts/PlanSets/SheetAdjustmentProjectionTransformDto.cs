namespace FloorplanFit.Contracts.PlanSets;

public sealed record SheetAdjustmentProjectionTransformDto(
    decimal Scale,
    decimal RotationDegrees,
    decimal TranslateX,
    decimal TranslateY);
