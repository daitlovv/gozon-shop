using Orders.Domain.Entities;
using Orders.Persistence.Entities;

namespace Orders.Application.Interfaces;

public interface IOrderCommandRepository
{
    Task AddAsync(Order order);
    Task AddOutboxMessageAsync(OrderOutboxMessage message);
}