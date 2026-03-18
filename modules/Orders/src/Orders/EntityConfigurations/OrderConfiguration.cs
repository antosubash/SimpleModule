using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.Core.Ids;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.EntityConfigurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedOnAdd();
        builder.Property(o => o.Total).HasColumnType("decimal(18,2)");
        builder.HasMany(o => o.Items).WithOne().HasForeignKey("OrderId");

        builder.HasData(GenerateSeedOrders());
    }

    internal static List<object> GenerateSeedOrderItems()
    {
        var orderFaker = new Faker { Random = new Randomizer(99999) };
        var orderItems = new List<object>();

        for (var i = 1; i <= 5; i++)
        {
            var itemCount = orderFaker.Random.Int(1, 3);
            var usedProductIds = new HashSet<int>();
            for (var j = 0; j < itemCount; j++)
            {
                int productId;
                do
                {
                    productId = orderFaker.Random.Int(1, 10);
                } while (!usedProductIds.Add(productId));

                var quantity = orderFaker.Random.Int(1, 5);
                // consume price random to keep sequence consistent
                orderFaker.Random.Decimal(10, 1000);

                orderItems.Add(
                    new
                    {
                        OrderId = OrderId.From(i),
                        ProductId = ProductId.From(productId),
                        Quantity = quantity,
                    }
                );
            }
        }

        return orderItems;
    }

    private static Order[] GenerateSeedOrders()
    {
        var orderFaker = new Faker { Random = new Randomizer(99999) };
        var orders = new List<Order>();

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
            }

            orders.Add(
                new Order
                {
                    Id = OrderId.From(i),
                    UserId = UserId.From(
                        orderFaker
                            .Random.Int(1, 10)
                            .ToString(System.Globalization.CultureInfo.InvariantCulture)
                    ),
                    Total = Math.Round(total, 2),
                    CreatedAt = orderFaker
                        .Date.Between(
                            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            new DateTime(2026, 1, 31, 23, 59, 59, DateTimeKind.Utc)
                        ),
                }
            );
        }

        return orders.ToArray();
    }
}
