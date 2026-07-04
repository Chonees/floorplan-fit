namespace FloorplanFit.Domain.FloorPlans;

public static class FloorPlanArtifactPositionSourceKinds
{
    public const string RoomLabel = "RoomLabel";
    public const string OpeningLabel = "OpeningLabel";
    public const string OpeningCandidate = FloorPlanArtifactSourceKinds.OpeningCandidate;
    public const string FixedPlanComponent = FloorPlanArtifactSourceKinds.FixedPlanComponent;
    public const string ProtectedDetailAssembly = FloorPlanArtifactSourceKinds.ProtectedDetailAssembly;

    public static bool IsSupported(string sourceArtifactKind)
    {
        return string.Equals(sourceArtifactKind, RoomLabel, StringComparison.Ordinal) ||
               string.Equals(sourceArtifactKind, OpeningLabel, StringComparison.Ordinal) ||
               string.Equals(sourceArtifactKind, OpeningCandidate, StringComparison.Ordinal) ||
               string.Equals(sourceArtifactKind, FixedPlanComponent, StringComparison.Ordinal) ||
               string.Equals(sourceArtifactKind, ProtectedDetailAssembly, StringComparison.Ordinal);
    }

    public static bool SupportsAbsolutePoint(string sourceArtifactKind)
    {
        return string.Equals(sourceArtifactKind, RoomLabel, StringComparison.Ordinal) ||
               string.Equals(sourceArtifactKind, OpeningLabel, StringComparison.Ordinal);
    }

    public static bool SupportsTranslation(string sourceArtifactKind)
    {
        return string.Equals(sourceArtifactKind, OpeningCandidate, StringComparison.Ordinal) ||
               string.Equals(sourceArtifactKind, FixedPlanComponent, StringComparison.Ordinal) ||
               string.Equals(sourceArtifactKind, ProtectedDetailAssembly, StringComparison.Ordinal);
    }
}
