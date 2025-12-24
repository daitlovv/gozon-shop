using Microsoft.EntityFrameworkCore;
using Payments.Application.Interfaces;
using Payments.Persistence;

namespace Payments.Infrastructure.BackgroundWorkers;

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
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
                var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

                var messages = await db.Outbox
                    .Where(x => !x.Sent)
                    .OrderBy(x => x.CreatedAt)
                    .Take(10)
                    .ToListAsync(ct);

                foreach (var msg in messages)
                {
                    try
                    {
                        await messageBus.PublishAsync("payments.exchange", "payment.result", msg.Payload);
                        msg.MarkSent();
                        _logger.LogInformation("Sent outbox message {MessageId}", msg.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send message {MessageId}", msg.Id);
                        break;
                    }
                }

                if (messages.Any())
                {
                    await db.SaveChangesAsync(ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OutboxPublisher");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }
}