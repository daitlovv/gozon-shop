using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy => policy
        .WithOrigins("http://localhost:3000", "http://localhost:8080")
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
});

builder.Services.AddOcelot(builder.Configuration)
    .AddPolly();

var app = builder.Build();

app.UseCors("AllowFrontend");

app.UseOcelot().Wait();

app.Run();