using System.Text.Json.Serialization;

namespace FloorplanFit.Contracts.PlanSets;

[JsonConverter(typeof(JsonStringEnumConverter<PlanSetVerificationDecision>))]
public enum PlanSetVerificationDecision
{
    ReadyForExport = 1,
    Blocked = 2
}

[JsonConverter(typeof(JsonStringEnumConverter<PlanSetVerificationCheckStatus>))]
public enum PlanSetVerificationCheckStatus
{
    Passed = 1,
    Failed = 2,
    InsufficientData = 3,
    Unsupported = 4
}

[JsonConverter(typeof(JsonStringEnumConverter<PlanSetVerificationReasonCode>))]
public enum PlanSetVerificationReasonCode
{
    MissingExpectedOutput = 1,
    UnsafeDxf = 2,
    CanonicalOperationMismatch = 3,
    OutlineCongruenceMismatch = 4,
    SegmentCongruenceMismatch = 5,
    FinalOutputCongruenceMismatch = 6,
    UnsupportedCapability = 7,
    MissingRequiredEvidence = 8
}

public sealed record PlanSetVerificationReasonDto(
    PlanSetVerificationReasonCode Code,
    string Check,
    Guid? SheetId,
    string Detail);

public sealed record PlanSetVerificationOutputDto(
    int ExpectedOutputCount,
    int PresentOutputCount,
    IReadOnlyList<Guid> MissingSheetIds);

public sealed record PlanSetVerificationOperationDto(
    int ExpectedOperationCount,
    int AppliedOperationCount,
    int FailedOperationCount,
    int MissingOperationCount);

public sealed record PlanSetVerificationCheckDto(
    PlanSetVerificationCheckStatus Status,
    int ExpectedEvidenceCount,
    int PassedEvidenceCount,
    int FailedEvidenceCount,
    int MissingEvidenceCount);

public sealed record PlanSetVerificationReportDto(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    PlanSetVerificationOutputDto Outputs,
    PlanSetVerificationCheckDto DxfSafety,
    PlanSetVerificationOperationDto FloorPlanOperations,
    PlanSetVerificationOperationDto DependentOperations,
    PlanSetVerificationCheckDto OutlineCongruence,
    PlanSetVerificationCheckDto SegmentCongruence,
    PlanSetVerificationCheckDto FinalOutputCongruence,
    PlanSetVerificationCheckDto Capabilities,
    IReadOnlyList<PlanSetVerificationReasonDto> Reasons)
{
    public const int CurrentSchemaVersion = 1;

    public PlanSetVerificationDecision Decision => IsGreen
        ? PlanSetVerificationDecision.ReadyForExport
        : PlanSetVerificationDecision.Blocked;

    [JsonIgnore]
    public bool IsGreen =>
        SchemaVersion == CurrentSchemaVersion &&
        Outputs.ExpectedOutputCount == Outputs.PresentOutputCount &&
        Outputs.MissingSheetIds.Count == 0 &&
        DxfSafety.Status == PlanSetVerificationCheckStatus.Passed &&
        FloorPlanOperations.FailedOperationCount == 0 &&
        FloorPlanOperations.MissingOperationCount == 0 &&
        DependentOperations.FailedOperationCount == 0 &&
        DependentOperations.MissingOperationCount == 0 &&
        OutlineCongruence.Status == PlanSetVerificationCheckStatus.Passed &&
        SegmentCongruence.Status == PlanSetVerificationCheckStatus.Passed &&
        FinalOutputCongruence.Status == PlanSetVerificationCheckStatus.Passed &&
        Capabilities.Status == PlanSetVerificationCheckStatus.Passed &&
        Reasons.Count == 0;
}
