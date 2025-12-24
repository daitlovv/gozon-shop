using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orders.Persistence;

namespace Orders.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly OrdersDbContext _db;

    public HealthController(OrdersDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> CheckHealth()
    {
        try
        {
            await _db.Database.ExecuteSqlRawAsync("SELECT 1");
            
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "orders-service"
            });
        }
        catch (Npgsql.NpgsqlException npgsqlException)
        {
            return StatusCode(500, new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                service = "orders-service",
                error = "Ошибка подключения к базе данных",
                details = npgsqlException.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                service = "orders-service",
                error = "Внутренняя ошибка сервиса",
                details = ex.Message
            });
        }
    }
}