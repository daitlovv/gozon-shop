using Microsoft.EntityFrameworkCore;
using Payments.Application.Dtos;
using Payments.Application.Interfaces;
using Payments.Persistence;

namespace Payments.Application.Services;

public class AccountWithdrawalService : IAccountWithdrawalService
{
    private readonly PaymentsDbContext _context;
    private readonly ILogger<AccountWithdrawalService> _logger;

    public AccountWithdrawalService(PaymentsDbContext context, ILogger<AccountWithdrawalService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<WithdrawalResultDto> TryWithdrawAsync(PaymentRequestDto request)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(x => x.UserId == request.UserId);

        if (account == null)
        {
            _logger.LogWarning("Счет не найден для пользователя {UserId} при списании", request.UserId);
            return new WithdrawalResultDto(request.OrderId, "fail", "NO_ACCOUNT");
        }

        int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await _context.Entry(account).ReloadAsync();
                
                int currentVersion = account.Version;
                
                if (account.Balance < request.Amount)
                {
                    _logger.LogWarning("Недостаточно средств для списания {Amount} для заказа {OrderId} (попытка {Attempt}, баланс: {Balance})", 
                        request.Amount, request.OrderId, attempt, account.Balance);
                    return new WithdrawalResultDto(request.OrderId, "fail", "NOT_ENOUGH_MONEY");
                }

                account.Withdraw(request.Amount, currentVersion);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Успешное списание {Amount} для заказа {OrderId} (попытка {Attempt}, новый баланс: {Balance})", 
                    request.Amount, request.OrderId, attempt, account.Balance);
                return new WithdrawalResultDto(request.OrderId, "success", "OK");
            }
            catch (InvalidOperationException ex) when (ex.Message == "CONCURRENCY_CONFLICT")
            {
                if (attempt == maxRetries)
                {
                    _logger.LogWarning("Конфликт параллельного доступа при списании для заказа {OrderId} после {MaxRetries} попыток", 
                        request.OrderId, maxRetries);
                    return new WithdrawalResultDto(request.OrderId, "fail", "CONCURRENCY_CONFLICT");
                }
                
                _logger.LogDebug("Конфликт версий для заказа {OrderId}, повторная попытка {NextAttempt}/{MaxRetries}", 
                    request.OrderId, attempt + 1, maxRetries);
            }
            catch (InvalidOperationException ex) when (ex.Message == "NOT_ENOUGH_MONEY")
            {
                _logger.LogWarning("Недостаточно средств для списания {Amount} для заказа {OrderId} (попытка {Attempt})", 
                    request.Amount, request.OrderId, attempt);
                return new WithdrawalResultDto(request.OrderId, "fail", "NOT_ENOUGH_MONEY");
            }
            catch (ArgumentException argumentException)
            {
                _logger.LogError(argumentException, "Неверная сумма для списания {Amount} для заказа {OrderId}", request.Amount, request.OrderId);
                return new WithdrawalResultDto(request.OrderId, "fail", "INVALID_AMOUNT");
            }
            catch (DbUpdateConcurrencyException concurrencyException)
            {
                if (attempt == maxRetries)
                {
                    _logger.LogError(concurrencyException, "Конфликт версий при списании для заказа {OrderId} после {MaxRetries} попыток", 
                        request.OrderId, maxRetries);
                    return new WithdrawalResultDto(request.OrderId, "fail", "VERSION_CONFLICT");
                }
                
                _logger.LogDebug("Конфликт версий (DbUpdate) для заказа {OrderId}, повторная попытка {NextAttempt}/{MaxRetries}", 
                    request.OrderId, attempt + 1, maxRetries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка списания для заказа {OrderId}", request.OrderId);
                return new WithdrawalResultDto(request.OrderId, "fail", ex.Message);
            }
        }

        return new WithdrawalResultDto(request.OrderId, "fail", "MAX_RETRIES_EXCEEDED");
    }
}