namespace FloorplanFit.Contracts.PlanSets;

public enum PlanSetExportFailureStage
{
    DependentSheetGeneration = 1,
    UserPackagePublication = 2,
    VerificationAndWorkspacePublication = 3
}

public sealed record PlanSetExportFailureDto(
    int SchemaVersion,
    PlanSetExportFailureStage Stage,
    bool Canceled,
    string ErrorType,
    string Message)
{
    public const int CurrentSchemaVersion = 1;
}
