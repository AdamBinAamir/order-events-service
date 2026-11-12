using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orders.Infrastructure.Persistence;

namespace Orders.Infrastructure.Outbox;

public sealed class OutboxOptions
{
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);
    public int BatchSize { get; set; } = 50;
}

/// <summary>
/// Publishes unprocessed outbox rows and marks them processed.
/// Rows are locked with FOR UPDATE SKIP LOCKED, so several API replicas can run this safely.
/// Delivery is at-least-once: if the process dies after publishing but before the commit,
/// the rows are published again on restart. Consumers de-duplicate on the message id.
/// </summary>
internal sealed class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    OutboxOptions options,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var published = await ProcessBatchAsync(stoppingToken);
                if (published < options.BatchSize)
                    await Task.Delay(options.PollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox batch failed; retrying after {Delay}", options.PollInterval);
                await Task.Delay(options.PollInterval, stoppingToken);
            }
        }
    }

    internal async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var batch = await db.OutboxMessages
            .FromSql($"""
                SELECT * FROM "OutboxMessages"
                WHERE "ProcessedOnUtc" IS NULL
                ORDER BY "OccurredOnUtc"
                LIMIT {options.BatchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        foreach (var message in batch)
        {
            await publisher.PublishAsync(message, cancellationToken);
            message.ProcessedOnUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        if (batch.Count > 0)
            logger.LogInformation("Published {Count} outbox message(s)", batch.Count);

        return batch.Count;
    }
}
