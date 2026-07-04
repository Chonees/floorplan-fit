namespace FloorplanFit.Application.Abstractions;

public interface IProtectedDetailAssemblyExtractor
{
    Task<IReadOnlyList<DetectedProtectedDetailAssembly>> ExtractAsync(string managedFilePath, CancellationToken cancellationToken);
}
