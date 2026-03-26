using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.FileStorage.Contracts;
using SimpleModule.Storage;

namespace SimpleModule.FileStorage;

public sealed partial class FileStorageService(
    FileStorageDbContext db,
    IStorageProvider storageProvider,
    ILogger<FileStorageService> logger
) : IFileStorageContracts
{
    public async Task<IEnumerable<StoredFile>> GetFilesAsync(string? folder = null)
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
        string? folder = null
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
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.StoredFiles.Add(storedFile);
        await db.SaveChangesAsync();

        LogFileUploaded(logger, storedFile.Id, storedFile.FileName);
        return storedFile;
    }

    public async Task DeleteFileAsync(FileStorageId id)
    {
        var file = await db.StoredFiles.FindAsync(id)
            ?? throw new InvalidOperationException($"File with ID {id} not found.");

        await storageProvider.DeleteAsync(file.StoragePath);
        db.StoredFiles.Remove(file);
        await db.SaveChangesAsync();

        LogFileDeleted(logger, id, file.FileName);
    }

    public async Task<Stream?> DownloadFileAsync(FileStorageId id)
    {
        var file = await db.StoredFiles.FindAsync(id);
        if (file is null)
        {
            LogFileNotFound(logger, id);
            return null;
        }

        return await storageProvider.GetAsync(file.StoragePath);
    }

    public async Task<IEnumerable<string>> GetFoldersAsync(string? parentFolder = null)
    {
        var query = db.StoredFiles.AsNoTracking().Where(f => f.Folder != null);

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
}
