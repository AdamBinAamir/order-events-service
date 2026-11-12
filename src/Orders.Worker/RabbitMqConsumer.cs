using Orders.Infrastructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Orders.Worker;

/// <summary>Binds the worker queue to the orders exchange and hands each delivery to OrderEventProcessor.</summary>
internal sealed class RabbitMqConsumer(
    RabbitMqConnectionProvider connections,
    IServiceScopeFactory scopeFactory,
    ILogger<RabbitMqConsumer> logger) : BackgroundService
{
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = await connections.GetConnectionAsync(stoppingToken);
        _channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await RabbitMqConnectionProvider.DeclareExchangeAsync(_channel, stoppingToken);
        await _channel.QueueDeclareAsync(RabbitMqTopology.WorkerQueue, durable: true, exclusive: false,
            autoDelete: false, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(RabbitMqTopology.WorkerQueue, RabbitMqTopology.Exchange,
            RabbitMqTopology.WorkerBinding, cancellationToken: stoppingToken);
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += (_, delivery) => HandleDeliveryAsync(_channel, delivery, stoppingToken);

        await _channel.BasicConsumeAsync(RabbitMqTopology.WorkerQueue, autoAck: false, consumer, stoppingToken);
        logger.LogInformation("Consuming from queue {Queue}", RabbitMqTopology.WorkerQueue);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
    }

    private async Task HandleDeliveryAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken stoppingToken)
    {
        var type = delivery.BasicProperties.Type ?? "unknown";

        if (!Guid.TryParse(delivery.BasicProperties.MessageId, out var messageId))
        {
            logger.LogWarning("Rejecting message without a valid MessageId (type {Type})", type);
            await channel.BasicRejectAsync(delivery.DeliveryTag, requeue: false, stoppingToken);
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<OrderEventProcessor>();
            await processor.ProcessAsync(messageId, type, delivery.Body, stoppingToken);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            // No dead-letter queue yet (see README "next"): wait a moment and requeue.
            logger.LogError(ex, "Failed to process message {MessageId}; requeueing", messageId);
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (_channel is not null)
            await _channel.DisposeAsync();
    }
}
