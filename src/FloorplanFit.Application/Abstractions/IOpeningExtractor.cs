namespace FloorplanFit.Application.Abstractions;

public interface IOpeningExtractor
{
    Task<DetectedOpeningExtraction> ExtractAsync(string managedFilePath, CancellationToken cancellationToken);
}
