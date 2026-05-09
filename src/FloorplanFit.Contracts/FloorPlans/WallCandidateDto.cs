namespace FloorplanFit.Contracts.FloorPlans;

public sealed record WallCandidateDto(
    Guid CandidateId,
    string SourceEntityRef,
    string SourceLayer,
    string Status,
    decimal Confidence,
    decimal? ThicknessMm,
    string? DetectionNotes,
    Guid? GeometryPathId,
    int SortOrder)
{
    public string AssemblyHint => ThicknessMm switch
    {
        null => "Thickness unknown",
        >= 95m and <= 110m => "Likely 2x4 wall (4\")",
        >= 145m and <= 160m => "Likely 2x6 wall (6\")",
        _ => $"Thickness: {ThicknessMm:0.#} mm"
    };
}
