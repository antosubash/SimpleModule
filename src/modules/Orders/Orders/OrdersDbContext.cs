using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.Orders.Contracts;

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
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Total).HasColumnType("decimal(18,2)");
            entity.HasMany(o => o.Items).WithOne().HasForeignKey("OrderId");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey("OrderId", nameof(OrderItem.ProductId));
        });

        SeedOrders(modelBuilder);

        modelBuilder.ApplyModuleSchema("Orders", dbOptions.Value);
    }

    private static void SeedOrders(ModelBuilder modelBuilder)
    {
        var orderFaker = new Faker { Random = new Randomizer(99999) };

        var orders = new List<Order>();
        var orderItems = new List<object>();

        for (var i = 1; i <= 5; i++)
        {
            var itemCount = orderFaker.Random.Int(1, 3);
            var usedProductIds = new HashSet<int>();
            decimal total = 0;
            for (var j = 0; j < itemCount; j++)
            {
                int productId;
                do
                {
                    productId = orderFaker.Random.Int(1, 10);
                } while (!usedProductIds.Add(productId));

                var quantity = orderFaker.Random.Int(1, 5);
                var price = orderFaker.Random.Decimal(10, 1000);
                total += price * quantity;

                orderItems.Add(
                    new
                    {
                        OrderId = i,
                        ProductId = productId,
                        Quantity = quantity,
                    }
                );
            }

            orders.Add(
                new Order
                {
                    Id = i,
                    UserId = orderFaker.Random.Int(1, 10),
                    Total = Math.Round(total, 2),
                    CreatedAt = orderFaker.Date.Recent(30).ToUniversalTime(),
                }
            );
        }

        modelBuilder.Entity<Order>().HasData(orders);
        modelBuilder.Entity<OrderItem>().HasData(orderItems);
    }
}
