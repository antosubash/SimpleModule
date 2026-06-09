using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.FileStorage.Contracts;
using SimpleModule.FileStorage.Contracts.Events;
using SimpleModule.Storage;
using Wolverine.EntityFrameworkCore;

namespace SimpleModule.FileStorage;

public sealed partial class FileStorageService(
    FileStorageDbContext db,
    IStorageProvider storageProvider,
    IDbContextOutbox<FileStorageDbContext> outbox,
    ILogger<FileStorageService> logger
) : IFileStorageContracts
{
    public async Task<IEnumerable<StoredFile>> GetFilesAsync(
        string? folder = null,
        string? userId = null
    )
    {
        var query = db.StoredFiles.AsNoTracking();

        if (folder is not null)
        {
            var normalizedFolder = StoragePathHelper.Normalize(folder);
            query = query.Where(f => f.Folder == normalizedFolder);
        }
        else
        {
            query = query.Where(f => f.Folder == null);
        }

        if (userId is not null)
        {
            query = query.Where(f => f.CreatedByUserId == userId);
        }

        return await query.OrderBy(f => f.FileName).ToListAsync();
    }

    public async Task<StoredFile?> GetFileByIdAsync(FileStorageId id)
    {
        var file = await db.StoredFiles.FindAsync(id);
        if (file is null)
        {
            LogFileNotFound(logger, id);
        }

        return file;
    }

    public async Task<StoredFile> UploadFileAsync(
        Stream content,
        string fileName,
        string contentType,
        string? folder = null,
        string? userId = null
    )
    {
        var normalizedFolder = folder is not null ? StoragePathHelper.Normalize(folder) : null;
        var storagePath = StoragePathHelper.Combine(normalizedFolder, fileName);

        var result = await storageProvider.SaveAsync(storagePath, content, contentType);

        var storedFile = new StoredFile
        {
            FileName = fileName,
            StoragePath = result.Path,
            ContentType = contentType,
            Size = result.Size,
            Folder = normalizedFolder,
            CreatedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.StoredFiles.Add(storedFile);

        // FileStorageId is database-generated, so the row must be saved before
        // the event can carry it. The explicit transaction keeps the row and the
        // outbox envelope atomic.
        await using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            await db.SaveChangesAsync();
            await outbox.PublishAsync(
                new FileUploadedEvent(
                    storedFile.Id,
                    storedFile.FileName,
                    storedFile.Size,
                    storedFile.ContentType
                )
            );
        }
        catch
        {
            // The row is not yet committed — roll back the orphaned blob. The blob
            // cleanup MUST stay scoped to the pre-commit window: once the row is
            // committed below, deleting the blob would dangle a committed StoredFile
            // against a missing file.
            await storageProvider.DeleteAsync(result.Path);
            throw;
        }

        // Commits the transaction (row + envelope), then flushes messages. A
        // failure here is post-persist: the row may already be committed, so the
        // blob must be left in place. The previous SaveChanges-then-PublishAsync
        // pattern lost the event when the process died between the two calls.
        await outbox.SaveChangesAndFlushMessagesAsync();

        LogFileUploaded(logger, storedFile.Id, storedFile.FileName);

        return storedFile;
    }

    public async Task DeleteFileAsync(FileStorageId id)
    {
        var file =
            await db.StoredFiles.FindAsync(id)
            ?? throw new InvalidOperationException($"File with ID {id} not found.");

        await DeleteFileAsync(file);
    }

    public async Task DeleteFileAsync(StoredFile file)
    {
        var storagePath = file.StoragePath;

        db.StoredFiles.Remove(file);

        // The DB row is the system of record: once it is gone, the file is deleted
        // as far as the application is concerned. Publish through the outbox so the
        // row removal and FileDeletedEvent commit atomically — previously the event
        // fired only after blob deletion succeeded, so a crash (or a failed blob
        // delete) after the DB commit silently dropped the event. Blob deletion
        // below is best-effort cleanup; a failure there leaves an orphaned blob (a
        // storage-sweep concern), not a lost event or a dangling row.
        await outbox.PublishAsync(new FileDeletedEvent(file.Id, file.FileName));
        await outbox.SaveChangesAndFlushMessagesAsync();

        try
        {
            await storageProvider.DeleteAsync(storagePath);
        }
#pragma warning disable CA1031 // Storage deletion is best-effort after successful DB commit
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogStorageDeletionFailed(logger, file.Id, storagePath, ex);
            return;
        }

        LogFileDeleted(logger, file.Id, file.FileName);
    }

    public async Task<Stream?> DownloadFileAsync(FileStorageId id)
    {
        var file = await db.StoredFiles.FindAsync(id);
        if (file is null)
        {
            LogFileNotFound(logger, id);
            return null;
        }

        return await DownloadFileAsync(file);
    }

    public Task<Stream?> DownloadFileAsync(StoredFile file) =>
        storageProvider.GetAsync(file.StoragePath);

    public async Task<IEnumerable<string>> GetFoldersAsync(
        string? parentFolder = null,
        string? userId = null
    )
    {
        var query = db.StoredFiles.AsNoTracking().Where(f => f.Folder != null);

        if (userId is not null)
        {
            query = query.Where(f => f.CreatedByUserId == userId);
        }

        string? normalizedParent = null;
        if (parentFolder is not null)
        {
            normalizedParent = StoragePathHelper.Normalize(parentFolder);
            query = query.Where(f => f.Folder!.StartsWith(normalizedParent + "/"));
        }

        var allFolders = await query.Select(f => f.Folder!).Distinct().ToListAsync();

        return allFolders
            .Select(f => normalizedParent is not null ? f[(normalizedParent.Length + 1)..] : f)
            .Select(GetTopSegment)
            .Distinct()
            .Select(f => normalizedParent is not null ? $"{normalizedParent}/{f}" : f)
            .Order();
    }

    private static string GetTopSegment(string path)
    {
        var slashIndex = path.IndexOf('/', StringComparison.Ordinal);
        return slashIndex < 0 ? path : path[..slashIndex];
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "File with ID {Id} not found")]
    private static partial void LogFileNotFound(ILogger logger, FileStorageId id);

    [LoggerMessage(Level = LogLevel.Information, Message = "File uploaded: {Id} ({FileName})")]
    private static partial void LogFileUploaded(ILogger logger, FileStorageId id, string fileName);

    [LoggerMessage(Level = LogLevel.Information, Message = "File deleted: {Id} ({FileName})")]
    private static partial void LogFileDeleted(ILogger logger, FileStorageId id, string fileName);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to delete storage for file {Id} at path {Path}. Storage may contain orphaned data"
    )]
    private static partial void LogStorageDeletionFailed(
        ILogger logger,
        FileStorageId id,
        string path,
        Exception exception
    );
}
