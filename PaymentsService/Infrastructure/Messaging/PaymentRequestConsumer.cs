using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Payments.Application.Dtos;
using Payments.Application.Services;
using Payments.Persistence;
using Payments.Persistence.Entities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Payments.Infrastructure.Messaging;

public class PaymentRequestConsumer : BackgroundService
{
    private readonly PaymentOrchestrator _orchestrator;
    private readonly IModel _channel;
    private readonly IConnection _connection;
    private readonly PaymentsDbContext _context;
    private readonly ILogger<PaymentRequestConsumer> _logger;

    public PaymentRequestConsumer(
        PaymentOrchestrator orchestrator,
        IConnection conn,
        PaymentsDbContext context,
        ILogger<PaymentRequestConsumer> logger)
    {
        _orchestrator = orchestrator;
        _connection = conn;
        _context = context;
        _logger = logger;
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare("orders.exchange", ExchangeType.Direct, durable: true);
        _channel.QueueDeclare("payments.payments", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind("payments.payments", "orders.exchange", "payment.request");
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Запуск PaymentRequestConsumer");

        AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (object sender, BasicDeliverEventArgs ea) =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                string json = Encoding.UTF8.GetString(ea.Body.ToArray());
                PaymentRequestDto? dto = JsonSerializer.Deserialize<PaymentRequestDto>(json);

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

                    _context.Inbox.Add(new PaymentInboxMessage(dto.EventId));
                    await _context.SaveChangesAsync();
                    
                    await _orchestrator.ProcessAsync(dto);
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
                _logger.LogError(jsonException, "Ошибка десериализации JSON в запросе оплаты");
                _channel.BasicNack(ea.DeliveryTag, false, true);
                await transaction.RollbackAsync();
            }
            catch (InvalidOperationException invalidOpException)
            {
                _logger.LogError(invalidOpException, "Ошибка операции обработки платежа");
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

        _channel.BasicConsume("payments.payments", false, consumer);
        _logger.LogInformation("PaymentRequestConsumer начал прослушивание");

        await Task.Delay(Timeout.Infinite, ct);
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        base.Dispose();
    }
}