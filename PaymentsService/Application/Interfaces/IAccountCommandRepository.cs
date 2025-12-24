using Payments.Domain.Entities;

namespace Payments.Application.Interfaces;

public interface IAccountCommandRepository
{
    Task<Account?> GetByUserIdAsync(Guid userId);
    Task AddAsync(Account account);
    Task SaveChangesAsync();
}