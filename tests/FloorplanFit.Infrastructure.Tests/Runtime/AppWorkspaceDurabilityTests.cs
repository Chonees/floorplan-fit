using FloorplanFit.Infrastructure.Persistence;
using FloorplanFit.Infrastructure.Runtime;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Tests.Runtime;

public sealed class AppWorkspaceDurabilityTests
{
    [Fact]
    public void Stable_paths_are_rooted_under_local_application_data()
    {
        var localApplicationData = Path.Combine("root", "local-app-data");

        Assert.Equal(
            Path.Combine(localApplicationData, "FloorplanFit", "workspace"),
            AppWorkspaceDurability.GetStableWorkspaceRoot(localApplicationData));
        Assert.Equal(
            Path.Combine(localApplicationData, "FloorplanFit", "backups"),
            AppWorkspaceDurability.GetBackupRoot(localApplicationData));
    }

    [Fact]
    public void CopyLegacyWorkspaceIfNeeded_copies_nested_state_without_deleting_legacy()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-workspace-copy-{Guid.NewGuid():N}");
        var legacyRoot = Path.Combine(tempRoot, "legacy", "workspace");
        var stableRoot = Path.Combine(tempRoot, "stable", "workspace");
        var legacyFile = Path.Combine(legacyRoot, "library", "raw-dxf", "plan.dxf");

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(legacyFile)!);
            File.WriteAllText(legacyFile, "legacy-plan");
            File.WriteAllText(Path.Combine(legacyRoot, "app.db"), "legacy-database");

            var copied = AppWorkspaceDurability.CopyLegacyWorkspaceIfNeeded(
                legacyRoot,
                stableRoot,
                CancellationToken.None);

            Assert.True(copied);
            Assert.Equal("legacy-plan", File.ReadAllText(Path.Combine(stableRoot, "library", "raw-dxf", "plan.dxf")));
            Assert.Equal("legacy-database", File.ReadAllText(Path.Combine(stableRoot, "app.db")));
            Assert.True(File.Exists(legacyFile));
            Assert.True(File.Exists(Path.Combine(legacyRoot, "app.db")));
        }
        finally
        {
            DeleteTempRoot(tempRoot);
        }
    }

    [Fact]
    public void CopyLegacyWorkspaceIfNeeded_does_not_merge_into_non_empty_target()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-workspace-existing-{Guid.NewGuid():N}");
        var legacyRoot = Path.Combine(tempRoot, "legacy", "workspace");
        var stableRoot = Path.Combine(tempRoot, "stable", "workspace");

        try
        {
            Directory.CreateDirectory(legacyRoot);
            Directory.CreateDirectory(stableRoot);
            File.WriteAllText(Path.Combine(legacyRoot, "legacy.txt"), "legacy");
            File.WriteAllText(Path.Combine(stableRoot, "current.txt"), "current");

            var copied = AppWorkspaceDurability.CopyLegacyWorkspaceIfNeeded(
                legacyRoot,
                stableRoot,
                CancellationToken.None);

            Assert.False(copied);
            Assert.False(File.Exists(Path.Combine(stableRoot, "legacy.txt")));
            Assert.Equal("current", File.ReadAllText(Path.Combine(stableRoot, "current.txt")));
            Assert.True(File.Exists(Path.Combine(legacyRoot, "legacy.txt")));
        }
        finally
        {
            DeleteTempRoot(tempRoot);
        }
    }

    [Fact]
    public void CreatePreMigrationBackupIfNeeded_snapshots_database_and_managed_files_once()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-workspace-backup-{Guid.NewGuid():N}");
        var workspaceRoot = Path.Combine(tempRoot, "FloorplanFit", "workspace");
        var backupRoot = Path.Combine(tempRoot, "FloorplanFit", "backups");
        var workspace = new AppWorkspace(workspaceRoot);
        var managedFile = Path.Combine(workspace.LibraryRawDxfDirectory, "plan.dxf");

        try
        {
            workspace.EnsureCreated();
            File.WriteAllText(managedFile, "managed-plan");

            using (var connection = new SqliteConnection($"Data Source={workspace.DatabasePath}"))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText =
                    """
                    CREATE TABLE sentinel (value TEXT NOT NULL);
                    INSERT INTO sentinel (value) VALUES ('database-state');
                    PRAGMA user_version = 0;
                    """;
                command.ExecuteNonQuery();
            }

            var backupPath = AppWorkspaceDurability.CreatePreMigrationBackupIfNeeded(
                workspace,
                backupRoot,
                SqliteSchemaInitializer.CurrentSchemaVersion,
                CancellationToken.None);
            var repeatedBackupPath = AppWorkspaceDurability.CreatePreMigrationBackupIfNeeded(
                workspace,
                backupRoot,
                SqliteSchemaInitializer.CurrentSchemaVersion,
                CancellationToken.None);

            Assert.Equal(
                Path.Combine(backupRoot, $"pre-schema-v{SqliteSchemaInitializer.CurrentSchemaVersion}"),
                backupPath);
            Assert.Equal(backupPath, repeatedBackupPath);
            Assert.Equal("managed-plan", File.ReadAllText(Path.Combine(backupPath!, "library", "raw-dxf", "plan.dxf")));
            Assert.Equal("managed-plan", File.ReadAllText(managedFile));

            using var backupConnection = new SqliteConnection($"Data Source={Path.Combine(backupPath!, "app.db")}");
            backupConnection.Open();
            using var verificationCommand = backupConnection.CreateCommand();
            verificationCommand.CommandText = "SELECT value FROM sentinel";
            Assert.Equal("database-state", Convert.ToString(verificationCommand.ExecuteScalar()));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            DeleteTempRoot(tempRoot);
        }
    }

    private static void DeleteTempRoot(string tempRoot)
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}
