using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Orders.IntegrationTests.Infrastructure;

/// <summary>Runs the real API (including the outbox processor) against the test containers.</summary>
public sealed class ApiFactory(InfrastructureFixture infrastructure) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Orders", infrastructure.DbConnectionString);
        builder.UseSetting("ConnectionStrings:RabbitMq", infrastructure.RabbitMqConnectionString);
        builder.UseSetting("Outbox:PollInterval", "00:00:00.200");
    }
}
