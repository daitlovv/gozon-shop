using Microsoft.EntityFrameworkCore.Storage;
using Orders.Application.Interfaces;
using Orders.Persistence.Repositories;

namespace Orders.Persistence.UnitOfWork;

public class OrderUnitOfWork : IOrderUnitOfWork
{
    private readonly OrdersDbContext _context;
    private IDbContextTransaction? _transaction;

    public IOrderQueryRepository QueryRepository { get; }
    public IOrderCommandRepository CommandRepository { get; }

    public OrderUnitOfWork(OrdersDbContext context)
    {
        _context = context;
        QueryRepository = new OrderQueryRepository(context);
        CommandRepository = new OrderCommandRepository(context);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync()
    {
        if (_transaction == null)
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context?.Dispose();
    }
}