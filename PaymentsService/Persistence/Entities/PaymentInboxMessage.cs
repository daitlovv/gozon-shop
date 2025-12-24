namespace Payments.Persistence.Entities;

public class PaymentInboxMessage
{
    public Guid EventId { get; private set; }
    public DateTime ProcessedAt { get; private set; }

    public PaymentInboxMessage(Guid eventId)
    {
        EventId = eventId;
        ProcessedAt = DateTime.UtcNow;
    }
}