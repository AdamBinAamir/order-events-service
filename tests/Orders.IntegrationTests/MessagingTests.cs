using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Orders.Api;
using Orders.Domain.Orders.Events;
using Orders.Infrastructure.Messaging;
using Orders.Infrastructure.Persistence;
using Orders.IntegrationTests.Infrastructure;
using RabbitMQ.Client;

namespace Orders.IntegrationTests;

[Collection(InfrastructureCollection.Name)]
public sealed class MessagingTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private IHost _worker = null!;
    private RecordingHandler _recorder = null!;

    public async Task InitializeAsync() => (_worker, _recorder) = await WorkerHost.StartAsync(infrastructure);

    public async Task DisposeAsync()
    {
        await _worker.StopAsync();
        _worker.Dispose();
    }

    [Fact]
    public async Task The_worker_handles_an_event_once_even_if_it_is_delivered_twice()
    {
        var placed = new OrderPlaced(Guid.NewGuid(), Guid.NewGuid(), 10m, "EUR");
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(placed, OrdersDbContext.JsonOptions));

        var factory = new ConnectionFactory { Uri = new Uri(infrastructure.RabbitMqConnectionString) };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        // Declare the same topology the worker uses, so nothing is dropped if it is still starting.
        await channel.ExchangeDeclareAsync(RabbitMqTopology.Exchange, ExchangeType.Topic, durable: true);
        await channel.QueueDeclareAsync(RabbitMqTopology.WorkerQueue, durable: true, exclusive: false, autoDelete: false);
        await channel.QueueBindAsync(RabbitMqTopology.WorkerQueue, RabbitMqTopology.Exchange, RabbitMqTopology.WorkerBinding);

        var properties = new BasicProperties { MessageId = placed.EventId.ToString(), Type = nameof(OrderPlaced) };
        for (var i = 0; i < 2; i++)
            await channel.BasicPublishAsync(RabbitMqTopology.Exchange, "orders.placed", false, properties, body);

        var handled = await Eventually.TrueAsync(async () =>
        {
            await using var db = infrastructure.CreateDbContext();
            return await db.ProcessedMessages.AnyAsync(m => m.MessageId == placed.EventId);
        });
        handled.Should().BeTrue("the worker should process the message");

        // Give the second delivery time to arrive and be skipped.
        await Task.Delay(TimeSpan.FromSeconds(2));

        _recorder.Handled.Count(e => e.EventId == placed.EventId).Should().Be(1);
        await using var check = infrastructure.CreateDbContext();
        (await check.ProcessedMessages.CountAsync(m => m.MessageId == placed.EventId)).Should().Be(1);
    }

    [Fact]
    public async Task An_order_placed_through_the_api_reaches_the_worker()
    {
        await using var api = new ApiFactory(infrastructure);
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/orders",
            new PlaceOrderRequest(Guid.NewGuid(), [new PlaceOrderLine(Guid.NewGuid(), 1, 5m, "USD")]));
        var order = (await response.Content.ReadFromJsonAsync<OrderResponse>())!;

        var received = await Eventually.TrueAsync(() =>
            Task.FromResult(_recorder.Handled.OfType<OrderPlaced>().Any(e => e.OrderId == order.Id)));

        received.Should().BeTrue("the event should flow API -> outbox -> RabbitMQ -> worker");

        await using var db = infrastructure.CreateDbContext();
        var placedMessages = await db.OutboxMessages.Where(m => m.Type == "OrderPlaced").ToListAsync();
        placedMessages.Single(m => m.Payload.Contains(order.Id.ToString())).ProcessedOnUtc.Should().NotBeNull();
    }
}
