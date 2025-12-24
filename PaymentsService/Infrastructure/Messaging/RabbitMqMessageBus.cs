using System.Text;
using Payments.Application.Interfaces;
using RabbitMQ.Client;

namespace Payments.Infrastructure.Messaging;

public class RabbitMqMessageBus : IMessageBus
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqMessageBus> _logger;

    public RabbitMqMessageBus(IConnection connection, ILogger<RabbitMqMessageBus> logger)
    {
        _connection = connection;
        _logger = logger;
        _channel = _connection.CreateModel();
        
        _channel.ExchangeDeclare("payments.exchange", ExchangeType.Direct, durable: true);
        _channel.QueueDeclare("orders.results", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind("orders.results", "payments.exchange", "payment.result");
    }

    public Task PublishAsync(string exchange, string routingKey, string message)
    {
        var body = Encoding.UTF8.GetBytes(message);
        
        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = Guid.NewGuid().ToString();

        _channel.BasicPublish(
            exchange: exchange,
            routingKey: routingKey,
            mandatory: true,
            basicProperties: properties,
            body: body
        );

        _logger.LogDebug("Message published to {Exchange} with routing key {RoutingKey}", 
            exchange, routingKey);
            
        return Task.CompletedTask;
    }

    public void Acknowledge(ulong deliveryTag)
    {
        _channel.BasicAck(deliveryTag, false);
    }

    public void NegativeAcknowledge(ulong deliveryTag)
    {
        _channel.BasicNack(deliveryTag, false, true);
    }

    public Task<string?> ConsumeAsync(string queue, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(null);
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
    }
}