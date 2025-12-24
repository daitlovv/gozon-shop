using Microsoft.EntityFrameworkCore;
using Orders.Domain.Entities;
using Orders.Persistence.Entities;

namespace Orders.Persistence;

public class OrdersDbContext : DbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options)
        : base(options) 
    { 
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderOutboxMessage> Outbox => Set<OrderOutboxMessage>();
    public DbSet<OrderInboxMessage> Inbox => Set<OrderInboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Status).IsRequired();
        });

        builder.Entity<OrderOutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Payload).IsRequired();
        });

        builder.Entity<OrderInboxMessage>(entity =>
        {
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventId).ValueGeneratedNever();
        });
    }
}