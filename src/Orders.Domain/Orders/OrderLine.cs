using Orders.Domain.Common;

namespace Orders.Domain.Orders;

public sealed class OrderLine
{
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = null!;

    public Money Total => UnitPrice.Multiply(Quantity);

    private OrderLine() { } // for EF Core

    public OrderLine(Guid productId, int quantity, Money unitPrice)
    {
        if (productId == Guid.Empty)
            throw new DomainException("Product id is required.");
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive.");

        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice ?? throw new DomainException("Unit price is required.");
    }
}
