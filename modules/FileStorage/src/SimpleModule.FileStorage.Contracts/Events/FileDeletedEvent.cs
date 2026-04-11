using SimpleModule.Core.Events;

namespace SimpleModule.FileStorage.Contracts.Events;

public sealed record FileDeletedEvent(FileStorageId FileId, string FileName) : IEvent;
