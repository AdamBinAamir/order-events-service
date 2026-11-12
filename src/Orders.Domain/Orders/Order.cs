using Orders.Domain.Common;
using Orders.Domain.Orders.Events;

namespace Orders.Domain.Orders;

public sealed class Order : AggregateRoot
{
    private readonly List<OrderLine> _lines = [];

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();

    private Order() { } // for EF Core

    public static Order Place(Guid customerId, IEnumerable<OrderLine> lines)
    {
        if (customerId == Guid.Empty)
            throw new DomainException("Customer id is required.");

        var lineList = lines?.ToList() ?? [];
        if (lineList.Count == 0)
            throw new DomainException("An order needs at least one line.");

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
        order._lines.AddRange(lineList);

        // Computing the total also checks that all lines use one currency.
        var total = order.Total();

        order.Raise(new OrderPlaced(order.Id, customerId, total.Amount, total.Currency));
        return order;
    }

    public Money Total() =>
        _lines.Skip(1).Aggregate(_lines[0].Total, (sum, line) => sum.Add(line.Total));

    public void Confirm()
    {
        if (Status == OrderStatus.Cancelled)
            throw new DomainException("A cancelled order cannot be confirmed.");
        if (Status == OrderStatus.Confirmed)
            throw new DomainException("The order is already confirmed.");

        Status = OrderStatus.Confirmed;
        Raise(new OrderConfirmed(Id));
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Cancelled)
            throw new DomainException("The order is already cancelled.");

        Status = OrderStatus.Cancelled;
        Raise(new OrderCancelled(Id));
    }
}
