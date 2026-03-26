namespace SimpleModule.Storage;

public sealed record StorageEntry(
    string Path,
    string Name,
    long Size,
    string ContentType,
    DateTimeOffset LastModified,
    bool IsFolder
);
