namespace Non.Application.Interfaces.Repositories;

public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default);
}