using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage.EntityConfigurations;

public class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedOnAdd();
        builder.Property(f => f.FileName).IsRequired().HasMaxLength(512);
        builder.Property(f => f.StoragePath).IsRequired().HasMaxLength(1024);
        builder.Property(f => f.ContentType).IsRequired().HasMaxLength(256);
        builder.Property(f => f.Folder).HasMaxLength(1024);
        builder.HasIndex(f => f.Folder);
        builder.HasIndex(f => new { f.Folder, f.FileName }).IsUnique();
    }
}
