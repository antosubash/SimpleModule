using SimpleModule.Core;
using SimpleModule.Core.Entities;

namespace SimpleModule.FileStorage.Contracts;

[Dto]
public class StoredFile : Entity<FileStorageId>
{
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? Folder { get; set; }
    public string? CreatedByUserId { get; set; }
}
