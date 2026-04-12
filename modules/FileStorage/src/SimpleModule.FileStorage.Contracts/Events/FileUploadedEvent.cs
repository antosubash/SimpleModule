using SimpleModule.Core.Events;

namespace SimpleModule.FileStorage.Contracts.Events;

public sealed record FileUploadedEvent(
    FileStorageId FileId,
    string FileName,
    long FileSize,
    string ContentType
) : IEvent;
