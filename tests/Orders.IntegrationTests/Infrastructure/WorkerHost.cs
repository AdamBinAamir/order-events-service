using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orders.Domain.Orders.Events;
using Orders.Worker;

namespace Orders.IntegrationTests.Infrastructure;

/// <summary>Records every event the worker handles, so tests can count them.</summary>
public sealed class RecordingHandler : IOrderEventHandler
{
    public ConcurrentQueue<OrderEvent> Handled { get; } = new();

    public Task HandleAsync(OrderEvent orderEvent, CancellationToken cancellationToken)
    {
        Handled.Enqueue(orderEvent);
        return Task.CompletedTask;
    }
}

/// <summary>Runs the real worker services in-process with a recording handler.</summary>
public static class WorkerHost
{
    public static async Task<(IHost Host, RecordingHandler Recorder)> StartAsync(InfrastructureFixture infrastructure)
    {
        var recorder = new RecordingHandler();
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddOrdersWorker(infrastructure.DbConnectionString, infrastructure.RabbitMqConnectionString);
        builder.Services.AddSingleton<IOrderEventHandler>(recorder);

        var host = builder.Build();
        await host.StartAsync();
        return (host, recorder);
    }
}

public static class Eventually
{
    public static async Task<bool> TrueAsync(Func<Task<bool>> condition, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(20));
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
                return true;
            await Task.Delay(200);
        }

        return await condition();
    }
}
