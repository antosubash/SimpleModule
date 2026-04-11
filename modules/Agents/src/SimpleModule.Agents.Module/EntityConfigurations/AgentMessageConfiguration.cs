using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.Agents.Contracts;

namespace SimpleModule.Agents.Module.EntityConfigurations;

public sealed class AgentMessageConfiguration : IEntityTypeConfiguration<AgentMessage>
{
    public void Configure(EntityTypeBuilder<AgentMessage> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasMaxLength(36);
        builder.Property(e => e.SessionId).IsRequired().HasMaxLength(36);
        builder.Property(e => e.Role).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Content).IsRequired();
        builder.Property(e => e.Timestamp).IsRequired();

        builder.HasIndex(e => e.SessionId);
        builder.HasIndex(e => new { e.SessionId, e.Timestamp });
    }
}
