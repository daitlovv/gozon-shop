namespace Payments.Domain.Entities;

public class Account
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Balance { get; private set; }
    public int Version { get; private set; }

    public Account(Guid userId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Balance = 0;
        Version = 0;
    }

    public void Deposit(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Сумма должна быть положительной");
        }

        Balance += amount;
        Version++;
    }

    public void Withdraw(decimal amount, int expectedVersion)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Сумма должна быть положительной");
        }
        
        if (Balance < amount)
        {
            throw new InvalidOperationException("NOT_ENOUGH_MONEY");
        }

        if (Version != expectedVersion)
        {
            throw new InvalidOperationException("CONCURRENCY_CONFLICT");
        }

        Balance -= amount;
        Version++;
    }
}