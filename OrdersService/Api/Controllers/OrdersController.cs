using Microsoft.AspNetCore.Mvc;
using Orders.Application.Services;

namespace Orders.Api.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _service;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(OrderService service, ILogger<OrdersController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromHeader(Name = "user_id")] Guid userId,
        [FromBody] CreateOrderRequest request)
    {
        _logger.LogInformation("Создание заказа для пользователя {UserId} с суммой {Amount}", 
            userId, request.Amount);
            
        try
        {
            Guid id = await _service.CreateAsync(userId, request.Amount, request.Description);
            _logger.LogInformation("Заказ создан с ID {OrderId}", id);
            
            return Ok(new { order_id = id });
        }
        catch (InvalidOperationException invalidOpException)
        {
            _logger.LogError(invalidOpException, "Ошибка операции создания заказа для пользователя {UserId}", userId);
            
            return StatusCode(500, new { 
                error = "ОШИБКА_СОЗДАНИЯ_ЗАКАЗА", 
                message = invalidOpException.Message 
            });
        }
        catch (System.Text.Json.JsonException jsonException)
        {
            _logger.LogError(jsonException, "Ошибка формата данных при создании заказа для пользователя {UserId}", userId);
            
            return StatusCode(500, new { 
                error = "ОШИБКА_ФОРМАТА_ДАННЫХ", 
                message = "Неверный формат данных запроса" 
            });
        }
        catch (Exception generalException)
        {
            _logger.LogError(generalException, "Непредвиденная ошибка создания заказа для пользователя {UserId}", userId);
            
            return StatusCode(500, new { 
                error = "ВНУТРЕННЯЯ_ОШИБКА_СЕРВЕРА", 
                message = "Произошла внутренняя ошибка сервера" 
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromHeader(Name = "user_id")] Guid userId)
    {
        try
        {
            var orders = await _service.GetOrdersAsync(userId);
            
            return Ok(orders.Select(o => new
            {
                o.Id,
                o.UserId,
                o.Amount,
                o.Description,
                Status = o.Status.ToString(),
                o.CreatedAt
            }));
        }
        catch (InvalidOperationException invalidOpException)
        {
            _logger.LogError(invalidOpException, "Ошибка операции получения заказов для пользователя {UserId}", userId);
            
            return StatusCode(500, new { 
                error = "ОШИБКА_ПОЛУЧЕНИЯ_ЗАКАЗОВ", 
                message = invalidOpException.Message 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения заказов для пользователя {UserId}", userId);
            
            return StatusCode(500, new { 
                error = "ВНУТРЕННЯЯ_ОШИБКА_СЕРВЕРА", 
                message = "Ошибка получения списка заказов" 
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        try
        {
            var order = await _service.GetOrderAsync(id);
            
            if (order == null)
            {
                return NotFound(new { error = "ЗАКАЗ_НЕ_НАЙДЕН" });
            }
                
            return Ok(new
            {
                order.Id,
                order.UserId,
                order.Amount,
                order.Description,
                Status = order.Status.ToString(),
                order.CreatedAt
            });
        }
        catch (InvalidOperationException invalidOpException)
        {
            _logger.LogError(invalidOpException, "Ошибка операции получения заказа {OrderId}", id);
            
            return StatusCode(500, new { 
                error = "ОШИБКА_ПОЛУЧЕНИЯ_ЗАКАЗА", 
                message = invalidOpException.Message 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения заказа {OrderId}", id);
            
            return StatusCode(500, new { 
                error = "ВНУТРЕННЯЯ_ОШИБКА_СЕРВЕРА", 
                message = "Ошибка получения информации о заказе" 
            });
        }
    }

    public record CreateOrderRequest(decimal Amount, string Description);
}