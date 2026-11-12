using FluentAssertions;
using Orders.Domain.Common;
using Orders.Domain.Orders;
using Orders.Domain.Orders.Events;

namespace Orders.Domain.Tests;

public class OrderTests
{
    private static readonly Guid Customer = Guid.NewGuid();

    private static OrderLine Line(int quantity = 1, decimal price = 10m, string currency = "EUR") =>
        new(Guid.NewGuid(), quantity, new Money(price, currency));

    [Fact]
    public void Place_creates_pending_order_and_raises_OrderPlaced()
    {
        var order = Order.Place(Customer, [Line(2, 10m), Line(1, 5m)]);

        order.Status.Should().Be(OrderStatus.Pending);
        order.Lines.Should().HaveCount(2);
        order.Total().Should().Be(new Money(25m, "EUR"));

        var placed = order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderPlaced>().Subject;
        placed.OrderId.Should().Be(order.Id);
        placed.CustomerId.Should().Be(Customer);
        placed.TotalAmount.Should().Be(25m);
        placed.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Place_without_lines_throws()
    {
        var act = () => Order.Place(Customer, []);

        act.Should().Throw<DomainException>().WithMessage("*at least one line*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Line_with_non_positive_quantity_throws(int quantity)
    {
        var act = () => Line(quantity);

        act.Should().Throw<DomainException>().WithMessage("*Quantity must be positive*");
    }

    [Fact]
    public void Place_with_mixed_currencies_throws()
    {
        var act = () => Order.Place(Customer, [Line(currency: "EUR"), Line(currency: "USD")]);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Confirm_sets_status_and_raises_OrderConfirmed()
    {
        var order = Order.Place(Customer, [Line()]);
        order.ClearDomainEvents();

        order.Confirm();

        order.Status.Should().Be(OrderStatus.Confirmed);
        order.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<OrderConfirmed>();
    }

    [Fact]
    public void Cancel_sets_status_and_raises_OrderCancelled()
    {
        var order = Order.Place(Customer, [Line()]);
        order.ClearDomainEvents();

        order.Cancel();

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<OrderCancelled>();
    }

    [Fact]
    public void Cancelled_order_cannot_be_confirmed()
    {
        var order = Order.Place(Customer, [Line()]);
        order.Cancel();
        order.ClearDomainEvents();

        var act = () => order.Confirm();

        act.Should().Throw<DomainException>().WithMessage("*cancelled order cannot be confirmed*");
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Confirming_twice_throws()
    {
        var order = Order.Place(Customer, [Line()]);
        order.Confirm();

        var act = () => order.Confirm();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancelling_twice_throws()
    {
        var order = Order.Place(Customer, [Line()]);
        order.Cancel();

        var act = () => order.Cancel();

        act.Should().Throw<DomainException>();
    }
}
