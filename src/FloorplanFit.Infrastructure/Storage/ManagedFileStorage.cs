using FloorplanFit.Application.Abstractions;
using FloorplanFit.Infrastructure.Runtime;

namespace FloorplanFit.Infrastructure.Storage;

public sealed class ManagedFileStorage : IManagedFileStorage
{
    private readonly AppWorkspace workspace;

    public ManagedFileStorage(AppWorkspace workspace)
    {
        this.workspace = workspace;
    }

    public Task<string> CopyIntoLibraryAsync(string sourceFilePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            throw new ArgumentException("A source file path is required.", nameof(sourceFilePath));
        }

        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("The source DXF file could not be found.", sourceFilePath);
        }

        workspace.EnsureCreated();

        var destinationPath = GetAvailableDestinationPath(Path.GetFileName(sourceFilePath));
        File.Copy(sourceFilePath, destinationPath, overwrite: false);

        return Task.FromResult(destinationPath);
    }

    private string GetAvailableDestinationPath(string fileName)
    {
        var destinationPath = Path.Combine(workspace.LibraryRawDxfDirectory, fileName);

        if (!File.Exists(destinationPath))
        {
            return destinationPath;
        }

        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var counter = 2;

        while (true)
        {
            var candidate = Path.Combine(workspace.LibraryRawDxfDirectory, $"{baseName}-{counter}{extension}");

            if (!File.Exists(candidate))
            {
                return candidate;
            }

            counter++;
        }
    }
}
