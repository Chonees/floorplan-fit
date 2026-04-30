using FloorplanFit.Application.Abstractions;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteUnitOfWork : IUnitOfWork
{
    private readonly SqliteSession session;

    public SqliteUnitOfWork(SqliteSession session)
    {
        this.session = session;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return session.CommitAsync(cancellationToken);
    }
}
