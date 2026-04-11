using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.EntityConfigurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedOnAdd();
        builder.Property(o => o.Total).HasColumnType("decimal(18,2)");
        builder.Property(o => o.ConcurrencyStamp).HasMaxLength(64);
        builder.HasMany(o => o.Items).WithOne().HasForeignKey("OrderId");
    }
}
