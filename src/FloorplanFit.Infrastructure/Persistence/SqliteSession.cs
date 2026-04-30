using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteSession : IDisposable, IAsyncDisposable
{
    private bool committed;

    private SqliteSession(SqliteConnection connection, SqliteTransaction transaction)
    {
        Connection = connection;
        Transaction = transaction;
    }

    public SqliteConnection Connection { get; }

    public SqliteTransaction Transaction { get; }

    public static Task<SqliteSession> OpenAsync(string databasePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var databaseDirectory = Path.GetDirectoryName(databasePath);

        if (!string.IsNullOrWhiteSpace(databaseDirectory))
        {
            Directory.CreateDirectory(databaseDirectory);
        }

        var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();
        var transaction = connection.BeginTransaction();

        return Task.FromResult(new SqliteSession(connection, transaction));
    }

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Transaction.Commit();
        committed = true;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        DisposeCore();
        GC.SuppressFinalize(this);
    }

    public ValueTask DisposeAsync()
    {
        DisposeCore();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    private void DisposeCore()
    {
        if (!committed)
        {
            try
            {
                Transaction.Rollback();
            }
            catch (InvalidOperationException)
            {
            }
        }

        Transaction.Dispose();
        Connection.Dispose();
    }
}
