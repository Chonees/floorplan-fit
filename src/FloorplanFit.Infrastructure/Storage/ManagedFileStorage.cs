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

    public Task<string> ReserveAdjustedDxfPathAsync(string sourceFileName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sourceFileName))
        {
            throw new ArgumentException("A source file name is required.", nameof(sourceFileName));
        }

        workspace.EnsureCreated();

        var baseName = Path.GetFileNameWithoutExtension(sourceFileName);
        var extension = Path.GetExtension(sourceFileName);
        var adjustedName = $"{baseName}-adjusted{extension}";
        return Task.FromResult(GetAvailableDestinationPath(workspace.LibraryAdjustedDxfDirectory, adjustedName));
    }

    private string GetAvailableDestinationPath(string fileName)
    {
        return GetAvailableDestinationPath(workspace.LibraryRawDxfDirectory, fileName);
    }

    private static string GetAvailableDestinationPath(string directory, string fileName)
    {
        var destinationPath = Path.Combine(directory, fileName);

        if (!File.Exists(destinationPath))
        {
            return destinationPath;
        }

        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var counter = 2;

        while (true)
        {
            var candidate = Path.Combine(directory, $"{baseName}-{counter}{extension}");

            if (!File.Exists(candidate))
            {
                return candidate;
            }

            counter++;
        }
    }
}
