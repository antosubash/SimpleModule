using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.Orders.Contracts;
using SimpleModule.Orders.EntityConfigurations;

namespace SimpleModule.Orders;

public class OrdersDbContext(
    DbContextOptions<OrdersDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
        modelBuilder.ApplyConfiguration(new OrderItemConfiguration());
        modelBuilder.ApplyModuleSchema("Orders", dbOptions.Value);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<OrderId>()
            .HaveConversion<OrderId.EfCoreValueConverter, OrderId.EfCoreValueComparer>();

        // SQLite cannot ORDER BY or compare DateTimeOffset expressions natively.
        // Store as long (binary ticks) only when running against SQLite.
        if (dbOptions.Value.DetectProvider("Orders") == DatabaseProvider.Sqlite)
        {
            configurationBuilder
                .Properties<DateTimeOffset>()
                .HaveConversion<DateTimeOffsetToBinaryConverter>();
            configurationBuilder
                .Properties<DateTimeOffset?>()
                .HaveConversion<DateTimeOffsetToBinaryConverter>();
        }
    }
}
