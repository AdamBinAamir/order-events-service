using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Orders.Domain.Orders;
using Orders.IntegrationTests.Infrastructure;

namespace Orders.IntegrationTests;

[Collection(InfrastructureCollection.Name)]
public sealed class PersistenceTests(InfrastructureFixture infrastructure)
{
    [Fact]
    public async Task Order_is_saved_and_reloaded_with_lines_and_money()
    {
        var productId = Guid.NewGuid();
        var order = Order.Place(Guid.NewGuid(), [new OrderLine(productId, 3, new Money(9.99m, "EUR"))]);

        await using (var write = infrastructure.CreateDbContext())
        {
            write.Orders.Add(order);
            await write.SaveChangesAsync();
        }

        await using var read = infrastructure.CreateDbContext();
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
