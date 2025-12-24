using Orders.Application.Interfaces;

namespace Orders.Infrastructure.Messaging;

public class PaymentRequestPublisher
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<PaymentRequestPublisher> _logger;

    public PaymentRequestPublisher(IMessageBus messageBus, ILogger<PaymentRequestPublisher> logger)
    {
        _messageBus = messageBus;
        _logger = logger;
    }

    public void Publish(string payload)
    {
        try
        {
            _messageBus.PublishAsync("orders.exchange", "payment.request", payload).Wait();
            _logger.LogInformation("Запрос на оплату опубликован в orders.exchange");
        }
        catch (AggregateException aggregateException)
        {
            if (aggregateException.InnerException is InvalidOperationException invalidOpException)
            {
                _logger.LogError(invalidOpException, "Ошибка операции публикации сообщения");
                throw new InvalidOperationException("Ошибка отправки запроса на оплату");
            }
            
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось опубликовать запрос на оплату");
            throw;
        }
    }
}