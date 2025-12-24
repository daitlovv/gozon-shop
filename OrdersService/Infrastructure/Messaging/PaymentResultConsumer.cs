using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Orders.Application.Dtos;
using Orders.Application.Services;
using Orders.Persistence;
using Orders.Persistence.Entities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Orders.Infrastructure.Messaging;

public class PaymentResultConsumer : BackgroundService
{
    private readonly PaymentResultHandler _handler;
    private readonly IModel _channel;
    private readonly IConnection _connection;
    private readonly OrdersDbContext _context;
    private readonly ILogger<PaymentResultConsumer> _logger;

    public PaymentResultConsumer(
        PaymentResultHandler handler, 
        IConnection conn,
        OrdersDbContext context,
        ILogger<PaymentResultConsumer> logger)
    {
        _handler = handler;
        _connection = conn;
        _context = context;
        _logger = logger;
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare("payments.exchange", ExchangeType.Direct, durable: true);
        _channel.QueueDeclare("orders.results", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind("orders.results", "payments.exchange", "payment.result");
    }

    protected override Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Запуск PaymentResultConsumer");
        
        AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(_channel);
        
        consumer.Received += async (object sender, BasicDeliverEventArgs ea) =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                string json = Encoding.UTF8.GetString(ea.Body.ToArray());
                PaymentResultDto? dto = JsonSerializer.Deserialize<PaymentResultDto>(json);
                
                if (dto != null)
                {
                    bool isDuplicate = await _context.Inbox
                        .AnyAsync(x => x.EventId == dto.EventId);

                    if (isDuplicate)
                    {
                        _channel.BasicAck(ea.DeliveryTag, false);
                        await transaction.RollbackAsync();
                        return;
                    }

                    _context.Inbox.Add(new OrderInboxMessage(dto.EventId));
                    await _context.SaveChangesAsync();
                    
                    await _handler.HandleAsync(dto);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                else
                {
                    _channel.BasicAck(ea.DeliveryTag, false);
                    await transaction.RollbackAsync();
                }
            }
            catch (JsonException jsonException)
            {
                _logger.LogError(jsonException, "Ошибка десериализации JSON в сообщении оплаты");
                _channel.BasicNack(ea.DeliveryTag, false, true);
                await transaction.RollbackAsync();
            }
            catch (InvalidOperationException invalidOpException)
            {
                _logger.LogError(invalidOpException, "Ошибка операции обработки результата оплаты");
                _channel.BasicAck(ea.DeliveryTag, false);
                await transaction.RollbackAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обработки запроса на оплату");
                _channel.BasicNack(ea.DeliveryTag, false, true);
                await transaction.RollbackAsync();
            }
        };

        _channel.BasicConsume("orders.results", false, consumer);
        _logger.LogInformation("PaymentResultConsumer начал прослушивание");
        
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        base.Dispose();
    }
}