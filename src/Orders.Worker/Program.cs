using Orders.Worker;

var builder = Host.CreateApplicationBuilder(args);

var ordersDb = builder.Configuration.GetConnectionString("Orders")
    ?? throw new InvalidOperationException("Connection string 'Orders' is missing.");
var rabbitMq = builder.Configuration.GetConnectionString("RabbitMq")
    ?? throw new InvalidOperationException("Connection string 'RabbitMq' is missing.");

builder.Services.AddOrdersWorker(ordersDb, rabbitMq);

var host = builder.Build();
host.Run();
