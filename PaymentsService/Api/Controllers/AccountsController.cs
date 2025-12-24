using Microsoft.AspNetCore.Mvc;
using Payments.Application.Services;

namespace Payments.Api.Controllers;

[ApiController]
[Route("accounts")]
public class AccountsController : ControllerBase
{
    private readonly AccountService _accountService;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(AccountService accountService, ILogger<AccountsController> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccount([FromHeader(Name = "user_id")] Guid userId)
    {
        try
        {
            await _accountService.CreateAccountAsync(userId);
            return Ok(new { message = "Счет создан успешно" });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Счет уже существует")
        {
            return BadRequest(new { error = "ACCOUNT_ALREADY_EXISTS" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка создания счета");
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = ex.Message });
        }
    }

    [HttpPost("{userId}/topup")]
    public async Task<IActionResult> TopUpAccount(Guid userId, [FromBody] TopUpRequest request)
    {
        try
        {
            await _accountService.TopUpAsync(userId, request.Amount);
            decimal balance = await _accountService.GetBalanceAsync(userId);
            return Ok(new { new_balance = balance });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Счет не найден")
        {
            return NotFound(new { error = "ACCOUNT_NOT_FOUND" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "INVALID_AMOUNT", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка пополнения счета");
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = ex.Message });
        }
    }

    [HttpGet("{userId}/balance")]
    public async Task<IActionResult> GetBalance(Guid userId)
    {
        try
        {
            decimal balance = await _accountService.GetBalanceAsync(userId);
            return Ok(new { balance = balance });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Счет не найден")
        {
            return NotFound(new { error = "ACCOUNT_NOT_FOUND" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения баланса");
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = ex.Message });
        }
    }

    public record TopUpRequest(decimal Amount);
}