using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Orders.Domain.Orders;
using Orders.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Orders.IntegrationTests;

public sealed class PersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    private OrdersDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<OrdersDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options);

    [Fact]
    public async Task Order_is_saved_and_reloaded_with_lines_and_money()
    {
        await using (var setup = CreateContext())
            await setup.Database.MigrateAsync();

        var productId = Guid.NewGuid();
        var order = Order.Place(Guid.NewGuid(), [new OrderLine(productId, 3, new Money(9.99m, "EUR"))]);

        await using (var write = CreateContext())
        {
            write.Orders.Add(order);
            await write.SaveChangesAsync();
        }

        await using var read = CreateContext();
        var loaded = await read.Orders.SingleAsync(o => o.Id == order.Id);

        loaded.CustomerId.Should().Be(order.CustomerId);
        loaded.Status.Should().Be(OrderStatus.Pending);
        loaded.Lines.Should().ContainSingle();
        loaded.Lines[0].ProductId.Should().Be(productId);
        loaded.Lines[0].Quantity.Should().Be(3);
        loaded.Lines[0].UnitPrice.Should().Be(new Money(9.99m, "EUR"));
        loaded.Total().Should().Be(new Money(29.97m, "EUR"));
    }
}
