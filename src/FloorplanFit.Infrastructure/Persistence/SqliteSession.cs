using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteSession : IDisposable, IAsyncDisposable
{
    private SqliteTransaction? transaction;

    private SqliteSession(SqliteConnection connection)
    {
        Connection = connection;
    }

    public SqliteConnection Connection { get; }

    public SqliteTransaction Transaction => transaction ??= Connection.BeginTransaction();

    public static Task<SqliteSession> OpenAsync(string databasePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var connection = SqliteConnectionPolicy.Open(databasePath);

        return Task.FromResult(new SqliteSession(connection));
    }

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (transaction is not null)
        {
            transaction.Commit();
            transaction.Dispose();
            transaction = null;
        }

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
        if (transaction is not null)
        {
            try
            {
                transaction.Rollback();
            }
            catch (InvalidOperationException)
            {
            }

            transaction.Dispose();
            transaction = null;
        }

        Connection.Dispose();
    }
}
