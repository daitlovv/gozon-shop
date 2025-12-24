using Microsoft.EntityFrameworkCore;
using Orders.Application.Interfaces;
using Orders.Application.Services;
using Orders.Infrastructure.BackgroundWorkers;
using Orders.Infrastructure.Messaging;
using Orders.Persistence;
using Orders.Persistence.UnitOfWork;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

string connectionString = builder.Configuration.GetConnectionString("db") 
    ?? "Host=orders-db;Database=orders;Username=postgres;Password=postgres";

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IOrderUnitOfWork, OrderUnitOfWork>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<PaymentResultHandler>();

builder.Services.AddSingleton<IConnection>(sp =>
{
    IConfiguration configuration = sp.GetRequiredService<IConfiguration>();
    
    ConnectionFactory factory = new ConnectionFactory
    {
        HostName = configuration["RabbitMq:Host"] ?? "rabbitmq",
        Port = int.Parse(configuration["RabbitMq:Port"] ?? "5672"),
        UserName = configuration["RabbitMq:Username"] ?? "guest",
        Password = configuration["RabbitMq:Password"] ?? "guest",
        VirtualHost = "/",
        DispatchConsumersAsync = true,
        AutomaticRecoveryEnabled = true
    };
    
    int maxRetries = 30;
    TimeSpan retryDelay = TimeSpan.FromSeconds(2);
    
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            Console.WriteLine($"Подключение к RabbitMQ (попытка {i+1}/{maxRetries})");
            IConnection connection = factory.CreateConnection();
            Console.WriteLine($"Успешное подключение к RabbitMQ по адресу {connection.Endpoint.HostName}:{connection.Endpoint.Port}");
            return connection;
        }
        catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException brokerException)
        {
            Console.WriteLine($"Не удалось подключиться к RabbitMQ (попытка {i+1}/{maxRetries}): {brokerException.Message}");
            
            if (i == maxRetries - 1)
            {
                Console.WriteLine("Достигнуто максимальное количество попыток. Завершение работы");
                throw;
            }
            
            Task.Delay(retryDelay).Wait();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка подключения к RabbitMQ (попытка {i+1}/{maxRetries}): {ex.Message}");
            
            if (i == maxRetries - 1)
            {
                Console.WriteLine("Достигнуто max количество попыток. Завершение работы");
                throw;
            }
            
            Task.Delay(retryDelay).Wait();
        }
    }
    
    throw new InvalidOperationException("Не удалось подключиться к RabbitMQ после всех попыток");
});

builder.Services.AddScoped<IMessageBus, RabbitMqConnection>();
builder.Services.AddSingleton<PaymentRequestPublisher>();
builder.Services.AddHostedService<OutboxPublisher>();
builder.Services.AddHostedService<PaymentResultConsumer>();

WebApplication app = builder.Build();

int maxDbRetries = 30;
TimeSpan dbRetryDelay = TimeSpan.FromSeconds(2);

for (int i = 0; i < maxDbRetries; i++)
{
    try
    {
        using IServiceScope scope = app.Services.CreateScope();
        OrdersDbContext db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        Console.WriteLine($"Подключение к базе данных заказов (попытка {i + 1}/{maxDbRetries})");
        await db.Database.EnsureCreatedAsync();
        Console.WriteLine("База данных заказов успешно подключена!");
        break;
    }
    catch (Npgsql.NpgsqlException npgsqlException)
    {
        Console.WriteLine($"Ошибка подключения к базе данных (попытка {i + 1}/{maxDbRetries}): {npgsqlException.Message}");
        
        if (i == maxDbRetries - 1)
        {
            Console.WriteLine("Достигнуто максимальное количество попыток. Завершение работы");
            throw;
        }
        
        await Task.Delay(dbRetryDelay);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка подключения к базе данных (попытка {i + 1}/{maxDbRetries}): {ex.Message}");
        
        if (i == maxDbRetries - 1)
        {
            Console.WriteLine("Достигнуто максимальное количество попыток. Завершение работы");
            throw;
        }
        
        await Task.Delay(dbRetryDelay);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();