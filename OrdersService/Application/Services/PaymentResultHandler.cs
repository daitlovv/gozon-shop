using Orders.Application.Dtos;
using Orders.Application.Interfaces;
using Orders.Domain.Entities;
using Orders.Persistence;

namespace Orders.Application.Services;

public class PaymentResultHandler
{
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly OrdersDbContext _context;
    private readonly ILogger<PaymentResultHandler> _logger;

    public PaymentResultHandler(
        IOrderUnitOfWork unitOfWork,
        OrdersDbContext context,
        ILogger<PaymentResultHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _logger = logger;
    }

    public async Task HandleAsync(PaymentResultDto dto)
    {
        _logger.LogInformation("Обработка результата оплаты для заказа {OrderId}, статус: {Status}", 
            dto.OrderId, dto.Status);

        Order? order = await _unitOfWork.QueryRepository.GetByIdAsync(dto.OrderId);
        
        if (order == null)
        {
            _logger.LogWarning("Заказ {OrderId} не найден при обработке результата оплаты", dto.OrderId);
            return;
        }

        if (dto.Status == "success")
        {
            order.MarkFinished();
            _logger.LogInformation("Заказ {OrderId} помечен как оплаченный", dto.OrderId);
        }
        else
        {
            order.MarkCancelled();
            _logger.LogInformation("Заказ {OrderId} помечен как отмененный. Причина: {Reason}", 
                dto.OrderId, dto.Reason);
        }

        await _context.SaveChangesAsync();
    }
}