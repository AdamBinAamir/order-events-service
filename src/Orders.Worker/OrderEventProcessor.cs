using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Orders.Domain.Orders.Events;
using Orders.Infrastructure.Inbox;
using Orders.Infrastructure.Persistence;

namespace Orders.Worker;

/// <summary>
/// Idempotent consumer: the handler runs and the message id is recorded in one transaction.
/// A message id that is already recorded is skipped, so redeliveries have no effect.
/// </summary>
public sealed class OrderEventProcessor(
    OrdersDbContext db,
    IOrderEventHandler handler,
    ILogger<OrderEventProcessor> logger)
{
    private static readonly Dictionary<string, Type> EventTypes = new()
    {
        [nameof(OrderPlaced)] = typeof(OrderPlaced),
        [nameof(OrderConfirmed)] = typeof(OrderConfirmed),
        [nameof(OrderCancelled)] = typeof(OrderCancelled)
    };

    /// <returns>true if the message was handled now; false if it was a duplicate or unknown.</returns>
    public async Task<bool> ProcessAsync(Guid messageId, string type, ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        if (await db.ProcessedMessages.AnyAsync(m => m.MessageId == messageId, cancellationToken))
        {
            logger.LogInformation("Skipping duplicate message {MessageId} ({Type})", messageId, type);
            return false;
        }

        if (EventTypes.TryGetValue(type, out var eventType))
        {
            var orderEvent = (OrderEvent)JsonSerializer.Deserialize(body.Span, eventType, OrdersDbContext.JsonOptions)!;
            await handler.HandleAsync(orderEvent, cancellationToken);
        }
        else
        {
            logger.LogWarning("Ignoring message {MessageId} with unknown type {Type}", messageId, type);
        }

        db.ProcessedMessages.Add(new ProcessedMessage { MessageId = messageId, Type = type, ProcessedOnUtc = DateTime.UtcNow });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Another consumer recorded the same id first; our transaction rolls back.
            logger.LogInformation("Message {MessageId} was handled concurrently; skipping", messageId);
            return false;
        }

        await transaction.CommitAsync(cancellationToken);
        return EventTypes.ContainsKey(type);
    }
}
