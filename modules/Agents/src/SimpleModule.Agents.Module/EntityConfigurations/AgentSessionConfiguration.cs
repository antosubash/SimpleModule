using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.Agents.Sessions;

namespace SimpleModule.Agents.Module.EntityConfigurations;

public sealed class AgentSessionConfiguration : IEntityTypeConfiguration<AgentSession>
{
    public void Configure(EntityTypeBuilder<AgentSession> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasMaxLength(36);
        builder.Property(e => e.AgentName).IsRequired().HasMaxLength(256);
        builder.Property(e => e.UserId).HasMaxLength(256);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.LastMessageAt).IsRequired();

        builder.HasIndex(e => e.AgentName);
        builder.HasIndex(e => e.UserId);
    }
}
