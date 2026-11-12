using Orders.Domain.Orders;

namespace Orders.Api;

public sealed record PlaceOrderRequest(Guid CustomerId, IReadOnlyList<PlaceOrderLine> Lines);

public sealed record PlaceOrderLine(Guid ProductId, int Quantity, decimal UnitPrice, string Currency);

public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string Status,
    decimal Total,
    string Currency,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderLineResponse> Lines)
{
    public static OrderResponse From(Order order)
    {
        var total = order.Total();
        return new OrderResponse(
            order.Id,
            order.CustomerId,
            order.Status.ToString(),
            total.Amount,
            total.Currency,
            order.CreatedAtUtc,
            order.Lines.Select(l => new OrderLineResponse(l.ProductId, l.Quantity, l.UnitPrice.Amount, l.UnitPrice.Currency)).ToList());
    }
}

public sealed record OrderLineResponse(Guid ProductId, int Quantity, decimal UnitPrice, string Currency);
