namespace FloorplanFit.Application.Abstractions;

public sealed record DetectedOpeningExtraction(
    IReadOnlyList<DetectedOpeningCandidate> Candidates,
    IReadOnlyList<DetectedOpeningLabel> Labels);
