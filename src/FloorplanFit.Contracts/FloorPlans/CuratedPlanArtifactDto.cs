namespace FloorplanFit.Contracts.FloorPlans;

public sealed record CuratedPlanArtifactDto(
    Guid SourceArtifactId,
    string SourceArtifactKind,
    string SourceEntityRef,
    string SourceLayer,
    string SourceEntityKind,
    string? SourceBlockName,
    IReadOnlyList<Guid> GeometryPathIds,
    decimal Confidence,
    string? DetectionNotes,
    int SortOrder,
    string DetectedFamily,
    string DetectedCategory,
    string DetectedType,
    string ResolvedFamily,
    string ResolvedCategory,
    string ResolvedType,
    string DecisionState,
    string ResolvedColorArgb,
    bool HasManualPosition = false,
    decimal TranslationDx = 0m,
    decimal TranslationDy = 0m);
