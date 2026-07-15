using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

internal static class SqliteConnectionPolicy
{
    private const int BusyTimeoutMilliseconds = 5000;

    public static SqliteConnection Open(string databasePath, bool pooling = true)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path is required.", nameof(databasePath));
        }

        var databaseDirectory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(databaseDirectory))
        {
            Directory.CreateDirectory(databaseDirectory);
        }

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = pooling,
            ForeignKeys = true,
            DefaultTimeout = BusyTimeoutMilliseconds / 1000
        }.ToString();

        var connection = new SqliteConnection(connectionString);

        try
        {
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText =
                $"""
                PRAGMA foreign_keys = ON;
                PRAGMA busy_timeout = {BusyTimeoutMilliseconds};
                """;
            command.ExecuteNonQuery();

            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }
}
