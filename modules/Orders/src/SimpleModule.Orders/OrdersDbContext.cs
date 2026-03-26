using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.Orders.Contracts;
using SimpleModule.Orders.EntityConfigurations;
using SimpleModule.Products.Contracts;
using SimpleModule.Users.Contracts;

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
        configurationBuilder
            .Properties<UserId>()
            .HaveConversion<UserId.EfCoreValueConverter, UserId.EfCoreValueComparer>();
        configurationBuilder
            .Properties<ProductId>()
            .HaveConversion<ProductId.EfCoreValueConverter, ProductId.EfCoreValueComparer>();
    }
}
