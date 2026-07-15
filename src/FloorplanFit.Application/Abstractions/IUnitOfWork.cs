namespace FloorplanFit.Application.Abstractions;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task RollbackAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
