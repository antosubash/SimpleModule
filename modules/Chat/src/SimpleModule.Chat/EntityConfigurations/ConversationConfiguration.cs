using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SimpleModule.Chat.Contracts;

namespace SimpleModule.Chat.EntityConfigurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.UserId).IsRequired().HasMaxLength(256);
        builder.Property(c => c.Title).IsRequired().HasMaxLength(512);
        builder.Property(c => c.AgentName).IsRequired().HasMaxLength(128);
        // SQLite cannot ORDER BY DateTimeOffset natively; convert to binary long.
        builder.Property(c => c.CreatedAt).HasConversion<DateTimeOffsetToBinaryConverter>();
        builder.Property(c => c.UpdatedAt).HasConversion<DateTimeOffsetToBinaryConverter>();
        builder.Property(c => c.ConcurrencyStamp).HasMaxLength(64);
        builder.HasIndex(c => new { c.UserId, c.UpdatedAt });

        builder
            .HasMany(c => c.Messages)
            .WithOne()
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Content).IsRequired();
        builder.Property(m => m.Role).HasConversion<int>();
        builder.Property(m => m.CreatedAt).HasConversion<DateTimeOffsetToBinaryConverter>();
        builder.Property(m => m.UpdatedAt).HasConversion<DateTimeOffsetToBinaryConverter>();
        builder.Property(m => m.ConcurrencyStamp).HasMaxLength(64);
        builder.HasIndex(m => new { m.ConversationId, m.CreatedAt });
    }
}
