namespace FloorplanFit.Application.Abstractions;

public interface IDimensionExtractor
{
    Task<IReadOnlyList<DetectedDimension>> ExtractAsync(string managedFilePath, CancellationToken cancellationToken);
}
