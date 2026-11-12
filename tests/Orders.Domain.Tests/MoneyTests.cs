using FluentAssertions;
using Orders.Domain.Common;
using Orders.Domain.Orders;

namespace Orders.Domain.Tests;

public class MoneyTests
{
    [Fact]
    public void Add_same_currency_sums_amounts()
    {
        var sum = new Money(10m, "EUR").Add(new Money(2.5m, "eur"));

        sum.Should().Be(new Money(12.5m, "EUR"));
    }

    [Fact]
    public void Add_different_currencies_throws()
    {
        var act = () => new Money(10m, "EUR").Add(new Money(1m, "USD"));

        act.Should().Throw<DomainException>().WithMessage("*EUR*USD*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("EURO")]
    public void Invalid_currency_throws(string currency)
    {
        var act = () => new Money(1m, currency);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Negative_amount_throws()
    {
        var act = () => new Money(-1m, "EUR");

        act.Should().Throw<DomainException>();
    }
}
