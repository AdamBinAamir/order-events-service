using System.Text;
using Orders.Infrastructure.Outbox;
using RabbitMQ.Client;

namespace Orders.Infrastructure.Messaging;

/// <summary>
/// Publishes outbox messages to the topic exchange with publisher confirms,
/// so PublishAsync only returns once the broker has stored the message.
/// </summary>
internal sealed class RabbitMqPublisher(RabbitMqConnectionProvider connections) : IMessagePublisher, IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IChannel? _channel;

    public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var channel = await GetChannelAsync(cancellationToken);

            var properties = new BasicProperties
            {
                MessageId = message.Id.ToString(),
                Type = message.Type,
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                Timestamp = new AmqpTimestamp(new DateTimeOffset(message.OccurredOnUtc).ToUnixTimeSeconds())
            };

            await channel.BasicPublishAsync(
                RabbitMqTopology.Exchange,
                RabbitMqTopology.RoutingKeyFor(message.Type),
                mandatory: false,
                basicProperties: properties,
                body: Encoding.UTF8.GetBytes(message.Payload),
                cancellationToken: cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        var connection = await connections.GetConnectionAsync(cancellationToken);
        _channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            cancellationToken);
        await RabbitMqConnectionProvider.DeclareExchangeAsync(_channel, cancellationToken);
        return _channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();
        _lock.Dispose();
    }
}
