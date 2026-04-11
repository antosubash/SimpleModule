using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.RateLimiting.Contracts;

namespace SimpleModule.RateLimiting.EntityConfigurations;

public class RateLimitRuleConfiguration : IEntityTypeConfiguration<RateLimitRule>
{
    public void Configure(EntityTypeBuilder<RateLimitRule> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();
        builder.Property(r => r.PolicyName).IsRequired().HasMaxLength(100);
        builder.HasIndex(r => r.PolicyName).IsUnique();
        builder.Property(r => r.EndpointPattern).HasMaxLength(500);
        builder.Property(r => r.PolicyType).HasConversion<string>().HasMaxLength(50);
        builder.Property(r => r.Target).HasConversion<string>().HasMaxLength(50);
        builder.Property(r => r.ConcurrencyStamp).HasMaxLength(64);
    }
}
