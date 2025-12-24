namespace Orders.Persistence.Entities;

public class OrderInboxMessage
{
    public Guid EventId { get; private set; }
    public DateTime ProcessedAt { get; private set; }

    public OrderInboxMessage(Guid eventId)
    {
        EventId = eventId;
        ProcessedAt = DateTime.UtcNow;
    }
}