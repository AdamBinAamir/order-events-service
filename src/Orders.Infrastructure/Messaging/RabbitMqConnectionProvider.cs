using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Orders.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public string ConnectionString { get; set; } = "amqp://guest:guest@localhost:5672";
}

/// <summary>One shared connection per process. Waits for the broker if it is not up yet.</summary>
public sealed class RabbitMqConnectionProvider(RabbitMqOptions options, ILogger<RabbitMqConnectionProvider> logger)
    : IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
                return _connection;

            var factory = new ConnectionFactory
            {
                Uri = new Uri(options.ConnectionString),
                ClientProvidedName = AppDomain.CurrentDomain.FriendlyName
            };

            while (true)
            {
                try
                {
                    _connection = await factory.CreateConnectionAsync(cancellationToken);
                    return _connection;
                }
                catch (BrokerUnreachableException ex)
                {
                    logger.LogWarning(ex, "RabbitMQ not reachable yet; retrying in 2s");
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                }
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public static async Task DeclareExchangeAsync(IChannel channel, CancellationToken cancellationToken) =>
        await channel.ExchangeDeclareAsync(RabbitMqTopology.Exchange, ExchangeType.Topic, durable: true,
            autoDelete: false, cancellationToken: cancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
        _lock.Dispose();
    }
}
