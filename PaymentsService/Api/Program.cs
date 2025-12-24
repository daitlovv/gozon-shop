using Microsoft.EntityFrameworkCore;
using Payments.Application.Interfaces;
using Payments.Application.Services;
using Payments.Infrastructure.BackgroundWorkers;
using Payments.Infrastructure.Messaging;
using Payments.Persistence;
using Payments.Persistence.Repositories;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

string connectionString = builder.Configuration.GetConnectionString("db") 
    ?? "Host=payments-db;Database=payments;Username=postgres;Password=postgres";

builder.Services.AddDbContext<PaymentsDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IAccountCommandRepository, AccountCommandRepository>();
builder.Services.AddScoped<IAccountWithdrawalService, AccountWithdrawalService>();
builder.Services.AddScoped<PaymentOrchestrator>();
builder.Services.AddScoped<AccountService>();

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
            Console.WriteLine($"Подключение к RabbitMQ (попытка {i+1}/{maxRetries})...");
            IConnection connection = factory.CreateConnection();
            Console.WriteLine($"Успешное подключение к RabbitMQ по адресу {connection.Endpoint.HostName}:{connection.Endpoint.Port}");
            return connection;
        }
        catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException brokerException)
        {
            Console.WriteLine($"Не удалось подключиться к RabbitMQ (попытка {i+1}/{maxRetries}): {brokerException.Message}");
            
            if (i == maxRetries - 1)
            {
                Console.WriteLine("Достигнуто максимальное количество попыток. Завершение работы...");
                throw;
            }
            
            Task.Delay(retryDelay).Wait();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка подключения к RabbitMQ (попытка {i+1}/{maxRetries}): {ex.Message}");
            
            if (i == maxRetries - 1)
            {
                Console.WriteLine("Достигнуто максимальное количество попыток. Завершение работы...");
                throw;
            }
            
            Task.Delay(retryDelay).Wait();
        }
    }
    
    throw new InvalidOperationException("Не удалось подключиться к RabbitMQ после всех попыток");
});

builder.Services.AddScoped<IMessageBus, RabbitMqMessageBus>();
builder.Services.AddHostedService<OutboxPublisher>();
builder.Services.AddHostedService<PaymentRequestConsumer>();

var app = builder.Build();

int maxDbRetries = 30;
TimeSpan dbRetryDelay = TimeSpan.FromSeconds(2);

for (int i = 0; i < maxDbRetries; i++)
{
    try
    {
        using var scope = app.Services.CreateScope();
        PaymentsDbContext db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        Console.WriteLine($"Подключение к базе данных платежей (попытка {i + 1}/{maxDbRetries})");
        await db.Database.EnsureCreatedAsync();
        Console.WriteLine("База данных платежей успешно подключена!");
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