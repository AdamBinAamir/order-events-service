using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orders.Domain.Orders;
using Orders.Infrastructure.Outbox;
using Orders.Infrastructure.Persistence;

namespace Orders.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrdersPersistence(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<OrdersDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IOrderRepository, OrderRepository>();
        return services;
    }

    /// <summary>Runs the background service that publishes outbox rows.</summary>
    public static IServiceCollection AddOutboxProcessor(this IServiceCollection services, Action<OutboxOptions>? configure = null)
    {
        var options = new OutboxOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddHostedService<OutboxProcessor>();
        services.AddScoped<IMessagePublisher, LoggingMessagePublisher>();
        return services;
    }
}
