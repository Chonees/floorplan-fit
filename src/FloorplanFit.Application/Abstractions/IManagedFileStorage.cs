namespace FloorplanFit.Application.Abstractions;

public interface IManagedFileStorage
{
    Task<string> CopyIntoLibraryAsync(string sourceFilePath, CancellationToken cancellationToken);
}
