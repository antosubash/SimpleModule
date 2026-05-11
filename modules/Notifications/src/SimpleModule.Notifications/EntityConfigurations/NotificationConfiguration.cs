using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.Notifications.Contracts;

namespace SimpleModule.Notifications.EntityConfigurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.UserId).IsRequired().HasMaxLength(450);
        builder.Property(n => n.Type).IsRequired().HasMaxLength(200);
        builder.Property(n => n.Channel).IsRequired().HasMaxLength(50);
        builder.Property(n => n.Title).HasMaxLength(500);
        builder.Property(n => n.Body).HasMaxLength(4000);
        builder.Property(n => n.DataJson).IsRequired();
        builder.Property(n => n.ConcurrencyStamp).HasMaxLength(64);
        builder.Ignore(n => n.IsRead);

        // Covers the inbox list query (filter UserId + order CreatedAt DESC, Id as tiebreaker)
        // and the unread-count query (predicate on UserId + ReadAt).
        builder.HasIndex(n => new { n.UserId, n.CreatedAt, n.Id });
        builder.HasIndex(n => new { n.UserId, n.ReadAt });
    }
}
