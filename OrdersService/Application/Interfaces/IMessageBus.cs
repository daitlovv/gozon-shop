namespace Orders.Application.Interfaces;

public interface IMessageBus
{
    Task PublishAsync(string exchange, string routingKey, string message);
}