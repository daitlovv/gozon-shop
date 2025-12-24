using System.Text;
using Orders.Application.Interfaces;
using RabbitMQ.Client;

namespace Orders.Infrastructure.Messaging
{
    public class RabbitMqConnection : IMessageBus
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMqConnection> _logger;

        public RabbitMqConnection(IConnection connection, ILogger<RabbitMqConnection> logger)
        {
            _connection = connection;
            _logger = logger;
            _channel = _connection.CreateModel();
            
            // Декларация для отправки запросов на оплату
            _channel.ExchangeDeclare("orders.exchange", ExchangeType.Direct, durable: true);
            _channel.QueueDeclare("payments.payments", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind("payments.payments", "orders.exchange", "payment.request");
            
            // Декларация для получения результатов оплаты
            _channel.ExchangeDeclare("payments.exchange", ExchangeType.Direct, durable: true);
            _channel.QueueDeclare("orders.results", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind("orders.results", "payments.exchange", "payment.result");
            
            _logger.LogInformation("RabbitMQ соединение настроено: orders.exchange → payments.payments, payments.exchange → orders.results");
        }

        public static IConnection CreateConnection(string host)
        {
            ConnectionFactory factory = new ConnectionFactory
            {
                HostName = host
            };
            
            return factory.CreateConnection();
        }

        public Task PublishAsync(string exchange, string routingKey, string message)
        {
            byte[] body = Encoding.UTF8.GetBytes(message);
            
            IBasicProperties properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            
            _channel.BasicPublish(
                exchange: exchange,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body
            );
            
            _logger.LogDebug("Сообщение опубликовано в {Exchange} с ключом {RoutingKey}", exchange, routingKey);
            
            return Task.CompletedTask;
        }

        public Task<string?> ConsumeAsync(string queue, CancellationToken cancellationToken)
        {
            return Task.FromResult<string?>(null);
        }

        public void Acknowledge(ulong deliveryTag)
        {
            _channel.BasicAck(deliveryTag, false);
        }

        public void NegativeAcknowledge(ulong deliveryTag)
        {
            _channel.BasicNack(deliveryTag, false, true);
        }

        public void Dispose()
        {
            _channel?.Close();
            _channel?.Dispose();
        }
    }
}