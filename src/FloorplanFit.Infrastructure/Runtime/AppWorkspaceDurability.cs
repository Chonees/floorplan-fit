using FloorplanFit.Infrastructure.Persistence;

namespace FloorplanFit.Infrastructure.Runtime;

public static class AppWorkspaceDurability
{
    private const string ProductDirectoryName = "FloorplanFit";

    public static string GetStableWorkspaceRoot(string localApplicationDataRoot)
        => Path.Combine(RequireRoot(localApplicationDataRoot), ProductDirectoryName, "workspace");

    public static string GetBackupRoot(string localApplicationDataRoot)
        => Path.Combine(RequireRoot(localApplicationDataRoot), ProductDirectoryName, "backups");

    public static bool CopyLegacyWorkspaceIfNeeded(
        string legacyWorkspaceRoot,
        string stableWorkspaceRoot,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var legacyRoot = RequireFullPath(legacyWorkspaceRoot, nameof(legacyWorkspaceRoot));
        var stableRoot = RequireFullPath(stableWorkspaceRoot, nameof(stableWorkspaceRoot));
        if (PathsEqual(legacyRoot, stableRoot) || !Directory.Exists(legacyRoot))
        {
            return false;
        }

        if (Directory.Exists(stableRoot) && Directory.EnumerateFileSystemEntries(stableRoot).Any())
        {
            return false;
        }

        EnsureDestinationIsOutsideSource(legacyRoot, stableRoot);

        var stableParent = Directory.GetParent(stableRoot)?.FullName
            ?? throw new InvalidOperationException("Stable workspace needs a parent directory.");
        Directory.CreateDirectory(stableParent);

        var stagingRoot = Path.Combine(
            stableParent,
            $".{Path.GetFileName(stableRoot)}-migration-{Guid.NewGuid():N}");

        try
        {
            CopyDirectory(legacyRoot, stagingRoot, cancellationToken);

            if (Directory.Exists(stableRoot))
            {
                Directory.Delete(stableRoot, recursive: false);
            }

            Directory.Move(stagingRoot, stableRoot);
            return true;
        }
        catch
        {
            DeleteStagingDirectory(stagingRoot, stableParent);
            throw;
        }
    }

    public static string? CreatePreMigrationBackupIfNeeded(
        AppWorkspace workspace,
        string backupRoot,
        int targetSchemaVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        cancellationToken.ThrowIfCancellationRequested();

        if (targetSchemaVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetSchemaVersion),
                "Target schema version must be positive.");
        }

        if (!File.Exists(workspace.DatabasePath))
        {
            return null;
        }

        using var sourceConnection = SqliteConnectionPolicy.Open(workspace.DatabasePath);
        if (ReadUserVersion(sourceConnection) >= targetSchemaVersion)
        {
            return null;
        }

        var fullBackupRoot = RequireFullPath(backupRoot, nameof(backupRoot));
        var finalBackupPath = Path.Combine(fullBackupRoot, $"pre-schema-v{targetSchemaVersion}");
        if (Directory.Exists(finalBackupPath))
        {
            return finalBackupPath;
        }

        EnsureDestinationIsOutsideSource(workspace.RootPath, fullBackupRoot);
        Directory.CreateDirectory(fullBackupRoot);

        var stagingRoot = Path.Combine(
            fullBackupRoot,
            $".pre-schema-v{targetSchemaVersion}-{Guid.NewGuid():N}");

        try
        {
            CopyDirectory(
                workspace.RootPath,
                stagingRoot,
                cancellationToken,
                path => IsDatabaseOrSidecar(path, workspace.DatabasePath));

            var databaseRelativePath = Path.GetRelativePath(workspace.RootPath, workspace.DatabasePath);
            var backupDatabasePath = Path.Combine(stagingRoot, databaseRelativePath);
            using (var backupConnection = SqliteConnectionPolicy.Open(backupDatabasePath, pooling: false))
            {
                sourceConnection.BackupDatabase(backupConnection);
            }

            cancellationToken.ThrowIfCancellationRequested();
            Directory.Move(stagingRoot, finalBackupPath);
            return finalBackupPath;
        }
        catch
        {
            DeleteStagingDirectory(stagingRoot, fullBackupRoot);
            throw;
        }
    }

    private static int ReadUserVersion(Microsoft.Data.Sqlite.SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void CopyDirectory(
        string sourceRoot,
        string destinationRoot,
        CancellationToken cancellationToken,
        Func<string, bool>? skipFile = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(destinationRoot);

        foreach (var entry in Directory.EnumerateFileSystemEntries(sourceRoot))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var attributes = File.GetAttributes(entry);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new IOException($"Workspace contains an unsupported reparse point: {entry}");
            }

            var destination = Path.Combine(destinationRoot, Path.GetFileName(entry));
            if ((attributes & FileAttributes.Directory) != 0)
            {
                CopyDirectory(entry, destination, cancellationToken, skipFile);
            }
            else if (skipFile is null || !skipFile(entry))
            {
                File.Copy(entry, destination, overwrite: false);
            }
        }
    }

    private static bool IsDatabaseOrSidecar(string path, string databasePath)
    {
        var fullPath = Path.GetFullPath(path);
        var fullDatabasePath = Path.GetFullPath(databasePath);
        return PathsEqual(fullPath, fullDatabasePath) ||
               PathsEqual(fullPath, fullDatabasePath + "-wal") ||
               PathsEqual(fullPath, fullDatabasePath + "-shm");
    }

    private static void EnsureDestinationIsOutsideSource(string sourceRoot, string destinationRoot)
    {
        var relativePath = Path.GetRelativePath(sourceRoot, destinationRoot);
        if (!relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
            !string.Equals(relativePath, "..", StringComparison.Ordinal) &&
            !Path.IsPathRooted(relativePath))
        {
            throw new InvalidOperationException("Workspace destination must be outside the source directory.");
        }
    }

    private static void DeleteStagingDirectory(string stagingRoot, string expectedParent)
    {
        if (!Directory.Exists(stagingRoot))
        {
            return;
        }

        var actualParent = Directory.GetParent(Path.GetFullPath(stagingRoot))?.FullName;
        if (!PathsEqual(actualParent, Path.GetFullPath(expectedParent)))
        {
            throw new InvalidOperationException("Refusing to delete a staging directory outside its expected parent.");
        }

        Directory.Delete(stagingRoot, recursive: true);
    }

    private static string RequireRoot(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Application data root is required.", nameof(rootPath));
        }

        return rootPath;
    }

    private static string RequireFullPath(string path, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", parameterName);
        }

        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    }

    private static bool PathsEqual(string? left, string? right)
        => string.Equals(
            left,
            right,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
}
