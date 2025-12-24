using Microsoft.EntityFrameworkCore;
using Orders.Infrastructure.Messaging;
using Orders.Persistence;
using Orders.Persistence.Entities;

namespace Orders.Infrastructure.BackgroundWorkers;

public class OutboxPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisher> _logger;

    public OutboxPublisher(IServiceScopeFactory scopeFactory, ILogger<OutboxPublisher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), ct);
        
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                OrdersDbContext db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
                PaymentRequestPublisher publisher = scope.ServiceProvider.GetRequiredService<PaymentRequestPublisher>();

                List<OrderOutboxMessage> messages = await db.Outbox
                    .Where(x => !x.Sent)
                    .OrderBy(x => x.CreatedAt)
                    .Take(10)
                    .ToListAsync(ct);

                foreach (OrderOutboxMessage msg in messages)
                {
                    try
                    {
                        publisher.Publish(msg.Payload);
                        msg.MarkSent();
                        _logger.LogInformation("Отправлено исходящее сообщение {MessageId}", msg.Id);
                    }
                    catch (InvalidOperationException invalidOpException)
                    {
                        _logger.LogError(invalidOpException, "Ошибка операции при отправке сообщения {MessageId}", msg.Id);
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Не удалось отправить сообщение {MessageId}", msg.Id);
                        break;
                    }
                }

                if (messages.Any())
                {
                    await db.SaveChangesAsync(ct);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Задача OutboxPublisher отменена");
                break;
            }
            catch (DbUpdateException dbUpdateException)
            {
                _logger.LogError(dbUpdateException, "Ошибка обновления базы данных в OutboxPublisher");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в OutboxPublisher");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }
}