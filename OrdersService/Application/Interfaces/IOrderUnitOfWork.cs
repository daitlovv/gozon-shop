namespace Orders.Application.Interfaces;

public interface IOrderUnitOfWork : IDisposable
{
    IOrderQueryRepository QueryRepository { get; }
    IOrderCommandRepository CommandRepository { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}