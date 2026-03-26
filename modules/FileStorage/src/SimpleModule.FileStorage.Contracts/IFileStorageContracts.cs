namespace SimpleModule.FileStorage.Contracts;

public interface IFileStorageContracts
{
    Task<IEnumerable<StoredFile>> GetFilesAsync(string? folder = null);
    Task<StoredFile?> GetFileByIdAsync(FileStorageId id);
    Task<StoredFile> UploadFileAsync(Stream content, string fileName, string contentType, string? folder = null);
    Task DeleteFileAsync(FileStorageId id);
    Task<Stream?> DownloadFileAsync(FileStorageId id);
    Task<IEnumerable<string>> GetFoldersAsync(string? parentFolder = null);
}
