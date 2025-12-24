using Payments.Application.Interfaces;
using Payments.Domain.Entities;

namespace Payments.Application.Services;

public class AccountService
{
    private readonly IAccountCommandRepository _repository;
    private readonly ILogger<AccountService> _logger;

    public AccountService(IAccountCommandRepository repository, ILogger<AccountService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task CreateAccountAsync(Guid userId)
    {
        Account? existing = await _repository.GetByUserIdAsync(userId);
        
        if (existing != null)
        {
            _logger.LogWarning("Счет уже существует для пользователя {UserId}", userId);
            throw new InvalidOperationException("Счет уже существует");
        }

        Account account = new Account(userId);
        await _repository.AddAsync(account);
        await _repository.SaveChangesAsync();
        
        _logger.LogInformation("Счет создан для пользователя {UserId} с ID {AccountId}", 
            userId, account.Id);
    }

    public async Task TopUpAsync(Guid userId, decimal amount)
    {
        Account? account = await _repository.GetByUserIdAsync(userId);
        
        if (account == null)
        {
            _logger.LogWarning("Счет не найден для пользователя {UserId}", userId);
            throw new InvalidOperationException("Счет не найден");
        }

        account.Deposit(amount);
        await _repository.SaveChangesAsync();
        
        _logger.LogInformation("Счет пополнен для пользователя {UserId}. Новый баланс: {Balance}", 
            userId, account.Balance);
    }

    public async Task<decimal> GetBalanceAsync(Guid userId)
    {
        Account? account = await _repository.GetByUserIdAsync(userId);
        
        if (account == null)
        {
            _logger.LogWarning("Счет не найден для пользователя {UserId}", userId);
            throw new InvalidOperationException("Счет не найден");
        }

        return account.Balance;
    }
}