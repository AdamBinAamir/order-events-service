using Microsoft.EntityFrameworkCore;
using Orders.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Orders.IntegrationTests.Infrastructure;

/// <summary>Starts real Postgres and RabbitMQ containers once for all tests in the collection.</summary>
public sealed class InfrastructureFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.13-management-alpine")
        .Build();

    public string DbConnectionString => _postgres.GetConnectionString();
    public string RabbitMqConnectionString => _rabbitMq.GetConnectionString();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbitMq.StartAsync());

        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }

    public OrdersDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<OrdersDbContext>().UseNpgsql(DbConnectionString).Options);
}

[CollectionDefinition(Name)]
public sealed class InfrastructureCollection : ICollectionFixture<InfrastructureFixture>
{
    public const string Name = "infrastructure";
}
