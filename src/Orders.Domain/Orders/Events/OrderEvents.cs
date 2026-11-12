using Orders.Domain.Common;

namespace Orders.Domain.Orders.Events;

public abstract record OrderEvent(Guid OrderId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}

public sealed record OrderPlaced(Guid OrderId, Guid CustomerId, decimal TotalAmount, string Currency)
    : OrderEvent(OrderId);

public sealed record OrderConfirmed(Guid OrderId) : OrderEvent(OrderId);

public sealed record OrderCancelled(Guid OrderId) : OrderEvent(OrderId);
