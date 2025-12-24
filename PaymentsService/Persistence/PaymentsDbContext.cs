using Microsoft.EntityFrameworkCore;
using Payments.Domain.Entities;
using Payments.Persistence.Entities;

namespace Payments.Persistence;

public class PaymentsDbContext : DbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options)
        : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<PaymentInboxMessage> Inbox => Set<PaymentInboxMessage>();
    public DbSet<PaymentOutboxMessage> Outbox => Set<PaymentOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Version).IsConcurrencyToken(); // Важно для оптимистичной блокировки!
        });

        builder.Entity<PaymentInboxMessage>(entity =>
        {
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventId).ValueGeneratedNever();
        });

        builder.Entity<PaymentOutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Payload).IsRequired();
        });
    }
}