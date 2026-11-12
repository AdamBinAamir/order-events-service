using Microsoft.Extensions.Logging;

namespace Orders.Infrastructure.Outbox;

/// <summary>Placeholder publisher that only logs. Replaced by the RabbitMQ publisher.</summary>
internal sealed class LoggingMessagePublisher(ILogger<LoggingMessagePublisher> logger) : IMessagePublisher
{
    public Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Publishing {Type} {MessageId}: {Payload}", message.Type, message.Id, message.Payload);
        return Task.CompletedTask;
    }
}
