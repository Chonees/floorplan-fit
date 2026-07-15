namespace FloorplanFit.Domain.PlanSets;

public static class WholePlanRegistrationAcceptancePolicy
{
    public const int CurrentVersion = 1;

    public const decimal MinimumCoverage = 0.75m;

    public const decimal MaximumResidualInches = 0.05m;

    public static bool IsAccepted(
        int version,
        decimal? horizontalCoverage,
        decimal? verticalCoverage,
        decimal? rootMeanSquareResidual,
        decimal? maximumResidual)
        => version == CurrentVersion &&
           horizontalCoverage is >= MinimumCoverage and <= 1m &&
           verticalCoverage is >= MinimumCoverage and <= 1m &&
           rootMeanSquareResidual is >= 0m and <= MaximumResidualInches &&
           maximumResidual is >= 0m and <= MaximumResidualInches;
}
