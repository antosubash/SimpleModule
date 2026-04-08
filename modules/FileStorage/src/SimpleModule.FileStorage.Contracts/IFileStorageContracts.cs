namespace SimpleModule.FileStorage.Contracts;

public interface IFileStorageContracts
{
    Task<IEnumerable<StoredFile>> GetFilesAsync(string? folder = null, string? userId = null);
    Task<StoredFile?> GetFileByIdAsync(FileStorageId id);
    Task<StoredFile> UploadFileAsync(
        Stream content,
        string fileName,
        string contentType,
        string? folder = null,
        string? userId = null
    );
    Task DeleteFileAsync(FileStorageId id);
    Task DeleteFileAsync(StoredFile file);
    Task<Stream?> DownloadFileAsync(FileStorageId id);
    Task<Stream?> DownloadFileAsync(StoredFile file);
    Task<IEnumerable<string>> GetFoldersAsync(string? parentFolder = null, string? userId = null);
}
