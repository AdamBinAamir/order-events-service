namespace Orders.Infrastructure.Messaging;

public static class RabbitMqTopology
{
    public const string Exchange = "orders";
    public const string WorkerQueue = "orders.worker";
    public const string WorkerBinding = "orders.*";

    /// <summary>OrderPlaced -> orders.placed, OrderConfirmed -> orders.confirmed, ...</summary>
    public static string RoutingKeyFor(string eventType)
    {
        var name = eventType.StartsWith("Order", StringComparison.Ordinal) ? eventType["Order".Length..] : eventType;
        return $"orders.{name.ToLowerInvariant()}";
    }
}
