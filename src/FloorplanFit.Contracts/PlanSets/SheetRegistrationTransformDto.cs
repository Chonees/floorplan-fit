namespace FloorplanFit.Contracts.PlanSets;

public sealed record SheetRegistrationTransformDto(
    decimal Scale,
    decimal RotationDegrees,
    decimal TranslateX,
    decimal TranslateY);
