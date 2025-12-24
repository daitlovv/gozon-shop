namespace Payments.Persistence.Entities;

public class PaymentOutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; }
    public string Payload { get; private set; }
    public bool Sent { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    public PaymentOutboxMessage(string type, string payload)
    {
        Id = Guid.NewGuid();
        Type = type;
        Payload = payload;
        CreatedAt = DateTime.UtcNow;
        Sent = false;
    }

    public void MarkSent() => Sent = true;
}