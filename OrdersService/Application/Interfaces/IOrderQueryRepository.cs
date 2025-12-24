using Orders.Domain.Entities;

namespace Orders.Application.Interfaces;

public interface IOrderQueryRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<List<Order>> GetByUserAsync(Guid userId);
}