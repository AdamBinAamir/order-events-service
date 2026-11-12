using Orders.Infrastructure;

namespace Orders.Worker;

public static class WorkerServiceCollectionExtensions
{
    public static IServiceCollection AddOrdersWorker(this IServiceCollection services, string ordersDb, string rabbitMq)
    {
        services.AddOrdersPersistence(ordersDb);
        services.AddRabbitMq(rabbitMq);
        services.AddScoped<IOrderEventHandler, LoggingOrderEventHandler>();
        services.AddScoped<OrderEventProcessor>();
        services.AddHostedService<RabbitMqConsumer>();
        return services;
    }
}
