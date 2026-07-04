namespace FloorplanFit.Domain.FloorPlans;

public static class FloorPlanLabelOverrideSourceKinds
{
    public const string RoomLabel = FloorPlanArtifactPositionSourceKinds.RoomLabel;
    public const string OpeningLabel = FloorPlanArtifactPositionSourceKinds.OpeningLabel;

    public static bool IsSupported(string sourceArtifactKind)
    {
        return string.Equals(sourceArtifactKind, RoomLabel, StringComparison.Ordinal) ||
               string.Equals(sourceArtifactKind, OpeningLabel, StringComparison.Ordinal);
    }
}
