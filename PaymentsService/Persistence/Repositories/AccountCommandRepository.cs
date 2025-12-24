using Microsoft.EntityFrameworkCore;
using Payments.Application.Interfaces;
using Payments.Domain.Entities;

namespace Payments.Persistence.Repositories;

public class AccountCommandRepository : IAccountCommandRepository
{
    private readonly PaymentsDbContext _db;

    public AccountCommandRepository(PaymentsDbContext db)
    {
        _db = db;
    }

    public Task<Account?> GetByUserIdAsync(Guid userId)
    {
        return _db.Accounts.FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public async Task AddAsync(Account account)
    {
        await _db.Accounts.AddAsync(account);
    }

    public Task SaveChangesAsync()
    {
        return _db.SaveChangesAsync();
    }
}