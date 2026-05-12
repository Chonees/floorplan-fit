namespace FloorplanFit.Domain.FloorPlans;

public static class FloorPlanArtifactSourceKinds
{
    public const string WallCandidate = "WallCandidate";
    public const string OpeningCandidate = "OpeningCandidate";
    public const string FixedPlanComponent = "FixedPlanComponent";
    public const string ProtectedDetailAssembly = "ProtectedDetailAssembly";

    public static bool IsSupported(string sourceArtifactKind)
    {
        return string.Equals(sourceArtifactKind, WallCandidate, StringComparison.Ordinal) ||
               string.Equals(sourceArtifactKind, OpeningCandidate, StringComparison.Ordinal) ||
               string.Equals(sourceArtifactKind, FixedPlanComponent, StringComparison.Ordinal) ||
               string.Equals(sourceArtifactKind, ProtectedDetailAssembly, StringComparison.Ordinal);
    }
}
