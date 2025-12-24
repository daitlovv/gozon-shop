using System.Text.Json;
using Orders.Application.Dtos;
using Orders.Application.Interfaces;
using Orders.Domain.Entities;
using Orders.Persistence.Entities;

namespace Orders.Application.Services;

public class OrderService
{
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IOrderUnitOfWork unitOfWork, ILogger<OrderService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> CreateAsync(Guid userId, decimal amount, string description)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var recentOrders = await _unitOfWork.QueryRepository.GetByUserAsync(userId);
            
            bool duplicateOrder = recentOrders
                .Where(o => o.CreatedAt > DateTime.UtcNow.AddMinutes(-5))
                .Any(o => o.Amount == amount && o.Description == description);

            if (duplicateOrder)
            {
                throw new InvalidOperationException("Похожий заказ уже создан недавно");
            }

            Order order = new Order(userId, amount, description);
            await _unitOfWork.CommandRepository.AddAsync(order);

            Guid eventId = Guid.NewGuid();
            PaymentRequestDto paymentRequest = new PaymentRequestDto(
                EventId: eventId,
                OrderId: order.Id,
                UserId: userId,
                Amount: amount
            );

            string payload = JsonSerializer.Serialize(paymentRequest);
            OrderOutboxMessage outboxMessage = new OrderOutboxMessage("PaymentRequest", payload);
            await _unitOfWork.CommandRepository.AddOutboxMessageAsync(outboxMessage);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Заказ создан успешно: ID {OrderId}, User {UserId}", order.Id, userId);
            
            return order.Id;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public Task<List<Order>> GetOrdersAsync(Guid userId)
    {
        return _unitOfWork.QueryRepository.GetByUserAsync(userId);
    }

    public Task<Order?> GetOrderAsync(Guid id)
    {
        return _unitOfWork.QueryRepository.GetByIdAsync(id);
    }
}