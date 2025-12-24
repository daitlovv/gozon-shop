namespace Payments.Application.Interfaces;

public interface IMessageBus : IDisposable
{
    Task PublishAsync(string exchange, string routingKey, string message);
}