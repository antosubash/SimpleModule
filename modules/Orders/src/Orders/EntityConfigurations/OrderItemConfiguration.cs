using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.EntityConfigurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey("OrderId", nameof(OrderItem.ProductId));

        builder.HasData(OrderConfiguration.GenerateSeedOrderItems());
    }
}
