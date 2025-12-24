using Microsoft.EntityFrameworkCore;
using Orders.Application.Interfaces;
using Orders.Domain.Entities;
using Orders.Persistence.Entities;

namespace Orders.Persistence.Repositories;

public class OrderCommandRepository : IOrderCommandRepository
{
    private readonly OrdersDbContext _context;

    public OrderCommandRepository(OrdersDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(Order order)
    {
        _context.Orders.Add(order);
        return Task.CompletedTask;
    }

    public Task AddOutboxMessageAsync(OrderOutboxMessage message)
    {
        _context.Outbox.Add(message);
        return Task.CompletedTask;
    }

    public Task<List<OrderOutboxMessage>> GetPendingOutboxMessagesAsync(int limit)
    {
        return _context.Outbox
            .Where(x => !x.Sent)
            .OrderBy(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }
}