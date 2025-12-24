using Microsoft.EntityFrameworkCore;
using Orders.Application.Interfaces;
using Orders.Domain.Entities;

namespace Orders.Persistence.Repositories;

public class OrderQueryRepository : IOrderQueryRepository
{
    private readonly OrdersDbContext _context;

    public OrderQueryRepository(OrdersDbContext context)
    {
        _context = context;
    }

    public Task<Order?> GetByIdAsync(Guid id)
    {
        return _context.Orders.FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<List<Order>> GetByUserAsync(Guid userId)
    {
        return _context.Orders.Where(x => x.UserId == userId).ToListAsync();
    }
}