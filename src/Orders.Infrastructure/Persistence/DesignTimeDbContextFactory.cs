using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Orders.Infrastructure.Persistence;

/// <summary>Used by `dotnet ef`. Reads ORDERS_DB or falls back to the local docker-compose database.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<OrdersDbContext>
{
    private const string LocalDev = "Host=localhost;Port=5432;Database=orders;Username=orders;Password=orders";

    public OrdersDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ORDERS_DB") ?? LocalDev;
        var options = new DbContextOptionsBuilder<OrdersDbContext>().UseNpgsql(connectionString).Options;
        return new OrdersDbContext(options);
    }
}
