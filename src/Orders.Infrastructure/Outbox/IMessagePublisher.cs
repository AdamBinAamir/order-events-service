namespace Orders.Infrastructure.Outbox;

/// <summary>Sends an outbox message to the broker. Must only return once the broker has accepted it.</summary>
public interface IMessagePublisher
{
    Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken);
}
