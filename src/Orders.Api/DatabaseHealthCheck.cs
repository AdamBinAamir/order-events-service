using Microsoft.Extensions.Diagnostics.HealthChecks;
using Orders.Infrastructure.Persistence;

namespace Orders.Api;

/// <summary>Ready only when the database answers.</summary>
internal sealed class DatabaseHealthCheck(OrdersDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await db.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Cannot reach the orders database.");
}
