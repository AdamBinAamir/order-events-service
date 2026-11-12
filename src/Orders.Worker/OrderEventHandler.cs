using Orders.Domain.Orders.Events;

namespace Orders.Worker;

/// <summary>What the worker does with an event. Today it only logs; a real one might send an email or reserve stock.</summary>
public interface IOrderEventHandler
{
    Task HandleAsync(OrderEvent orderEvent, CancellationToken cancellationToken);
}

internal sealed class LoggingOrderEventHandler(ILogger<LoggingOrderEventHandler> logger) : IOrderEventHandler
{
    public Task HandleAsync(OrderEvent orderEvent, CancellationToken cancellationToken)
    {
        switch (orderEvent)
        {
            case OrderPlaced placed:
                logger.LogInformation("Order {OrderId} placed by {CustomerId}, total {Total} {Currency}",
                    placed.OrderId, placed.CustomerId, placed.TotalAmount, placed.Currency);
                break;
            case OrderConfirmed confirmed:
                logger.LogInformation("Order {OrderId} confirmed", confirmed.OrderId);
                break;
            case OrderCancelled cancelled:
                logger.LogInformation("Order {OrderId} cancelled", cancelled.OrderId);
                break;
        }

        return Task.CompletedTask;
    }
}
