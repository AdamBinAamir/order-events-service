using Microsoft.EntityFrameworkCore;
using Orders.Api;
using Orders.Infrastructure;
using Orders.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Orders")
    ?? throw new InvalidOperationException("Connection string 'Orders' is missing.");
var rabbitMq = builder.Configuration.GetConnectionString("RabbitMq")
    ?? throw new InvalidOperationException("Connection string 'RabbitMq' is missing.");

builder.Services.AddOrdersPersistence(connectionString);
builder.Services.AddRabbitMq(rabbitMq);
builder.Services.AddOutboxProcessor(options => builder.Configuration.GetSection("Outbox").Bind(options));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync();
}

app.MapOrderEndpoints();

app.Run();

// Lets WebApplicationFactory<Program> find the entry point in integration tests.
public partial class Program;
