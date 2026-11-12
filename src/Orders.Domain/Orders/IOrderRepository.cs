namespace Orders.Domain.Orders;

public interface IOrderRepository
{
    Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>Saves changed orders and their pending domain events in one transaction.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
