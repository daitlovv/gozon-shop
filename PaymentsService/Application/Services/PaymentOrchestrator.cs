using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Payments.Application.Dtos;
using Payments.Application.Interfaces;
using Payments.Persistence;
using Payments.Persistence.Entities;

namespace Payments.Application.Services;

public class PaymentOrchestrator
{
    private readonly IAccountWithdrawalService _withdrawalService;
    private readonly PaymentsDbContext _context;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<PaymentOrchestrator> _logger;

    public PaymentOrchestrator(
        IAccountWithdrawalService withdrawalService,
        PaymentsDbContext context,
        IMessageBus messageBus,
        ILogger<PaymentOrchestrator> logger)
    {
        _withdrawalService = withdrawalService;
        _context = context;
        _messageBus = messageBus;
        _logger = logger;
    }

    public async Task ProcessAsync(PaymentRequestDto request)
    {
        try
        {
            var withdrawalResult = await _withdrawalService.TryWithdrawAsync(request);
            
            Guid eventId = Guid.NewGuid();
            PaymentResultDto result = new PaymentResultDto(
                EventId: eventId,
                OrderId: withdrawalResult.OrderId,
                Status: withdrawalResult.Status,
                Reason: withdrawalResult.Reason
            );

            string payload = JsonSerializer.Serialize(result);
            _context.Outbox.Add(new PaymentOutboxMessage("PaymentResult", payload));

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Платеж обработан для заказа {OrderId}, результат: {Status}", 
                request.OrderId, result.Status);
        }
        catch (JsonException jsonException)
        {
            _logger.LogError(jsonException, "Ошибка сериализации JSON для заказа {OrderId}", request.OrderId);
            throw new InvalidOperationException("Ошибка формирования результата платежа");
        }
        catch (DbUpdateException dbUpdateException)
        {
            _logger.LogError(dbUpdateException, "Ошибка обновления базы данных для заказа {OrderId}", request.OrderId);
            throw new InvalidOperationException("Ошибка сохранения данных платежа");
        }
        catch (Exception generalException)
        {
            _logger.LogError(generalException, "Непредвиденная ошибка обработки платежа для заказа {OrderId}", request.OrderId);
            throw new InvalidOperationException("Ошибка обработки платежа");
        }
    }
}