namespace FloorplanFit.Application.Abstractions;

public interface IWallExtractor
{
    Task<IReadOnlyList<DetectedWallCandidate>> ExtractAsync(string managedFilePath, CancellationToken cancellationToken);
}
