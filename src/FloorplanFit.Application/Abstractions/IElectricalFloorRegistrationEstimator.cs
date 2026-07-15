using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Abstractions;

public interface IElectricalFloorRegistrationEstimator
{
    Task<ElectricalFloorRegistrationEstimate> EstimateAsync(
        string canonicalFloorSourcePath,
        string electricalSourcePath,
        CancellationToken cancellationToken);
}

public enum ElectricalFloorRegistrationEstimateStatus
{
    Estimated = 1,
    Ambiguous = 2,
    InsufficientEvidence = 3
}

public sealed record ElectricalFloorRegistrationCandidateEvidence(
    decimal RotationDegrees,
    bool Accepted,
    decimal? Scale,
    decimal? TranslateX,
    decimal? TranslateY,
    decimal? HorizontalCoverage,
    decimal? VerticalCoverage,
    decimal? RootMeanSquareResidual,
    decimal? MaximumResidual,
    decimal? LeftEdgeResidual,
    decimal? RightEdgeResidual,
    decimal? BottomEdgeResidual,
    decimal? TopEdgeResidual,
    string Reason);

public sealed record ElectricalFloorRegistrationEstimate(
    ElectricalFloorRegistrationEstimateStatus Status,
    SheetRegistrationTransform? Transform,
    decimal Confidence,
    decimal? ObservedScaleX,
    decimal? ObservedScaleY,
    decimal? HorizontalCoverage,
    decimal? VerticalCoverage,
    decimal? RootMeanSquareResidual,
    decimal? MaximumResidual,
    string EvidenceSummary,
    IReadOnlyList<ElectricalFloorRegistrationCandidateEvidence> Candidates,
    string? CanonicalSourceSha256 = null,
    string? DependentSourceSha256 = null)
{
    public bool IsConclusive =>
        Status is ElectricalFloorRegistrationEstimateStatus.Estimated && Transform is not null;
}
