using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orders.Domain.Orders;
using Orders.Infrastructure.Messaging;
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

    public static IServiceCollection AddRabbitMq(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton(new RabbitMqOptions { ConnectionString = connectionString });
        services.AddSingleton<RabbitMqConnectionProvider>();
        return services;
    }

    /// <summary>Runs the background service that publishes outbox rows to RabbitMQ. Needs AddRabbitMq.</summary>
    public static IServiceCollection AddOutboxProcessor(this IServiceCollection services, Action<OutboxOptions>? configure = null)
    {
        var options = new OutboxOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
        services.AddHostedService<OutboxProcessor>();
        return services;
    }
}
