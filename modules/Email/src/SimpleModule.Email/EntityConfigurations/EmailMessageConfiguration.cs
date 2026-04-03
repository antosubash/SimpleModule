using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.EntityConfigurations;

public class EmailMessageConfiguration : IEntityTypeConfiguration<EmailMessage>
{
    public void Configure(EntityTypeBuilder<EmailMessage> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.To).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Cc).HasMaxLength(500);
        builder.Property(e => e.Bcc).HasMaxLength(500);
        builder.Property(e => e.Subject).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Body).IsRequired();
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000);
        builder.Property(e => e.TemplateSlug).HasMaxLength(200);
        builder.Property(e => e.Provider).HasMaxLength(100);
        builder.Property(e => e.ReplyTo).HasMaxLength(500);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);

        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.CreatedAt);
    }
}
