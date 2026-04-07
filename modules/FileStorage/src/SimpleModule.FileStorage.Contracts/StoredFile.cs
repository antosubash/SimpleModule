using SimpleModule.Core;

namespace SimpleModule.FileStorage.Contracts;

[Dto]
public class StoredFile
{
    public FileStorageId Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? Folder { get; set; }
    public string? CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
