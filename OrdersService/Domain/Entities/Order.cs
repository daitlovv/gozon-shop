using Orders.Domain.Enums;

namespace Orders.Domain.Entities;

public class Order
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = "";
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Order(Guid userId, decimal amount, string description)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Amount = amount;
        Description = description;
        Status = OrderStatus.New;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkFinished()
    {
        if (Status == OrderStatus.New)
        {
            Status = OrderStatus.Finished;
        }
    }

    public void MarkCancelled()
    {
        if (Status == OrderStatus.New)
        {
            Status = OrderStatus.Cancelled;
        }
    }
}