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
        builder.Property(f => f.CreatedByUserId).HasMaxLength(450);
        builder.Property(f => f.ConcurrencyStamp).HasMaxLength(64);
        builder.HasIndex(f => f.Folder);
        builder.HasIndex(f => f.CreatedByUserId);
        builder.HasIndex(f => new { f.Folder, f.FileName }).IsUnique();
        // The default listing orders by FileName (GetFilesAsync). The composite
        // (Folder, FileName) index above cannot satisfy that ordering when filtering
        // by "Folder IS NULL" (root folder), so without this the query falls back to a
        // bitmap scan of every root file + a top-N sort on each request. A standalone
        // FileName index turns it into an ordered index scan (≈15ms → <0.1ms at 80k rows).
        builder.HasIndex(f => f.FileName);
    }
}
