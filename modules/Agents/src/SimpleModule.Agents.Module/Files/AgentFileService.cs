using SimpleModule.Storage;

namespace SimpleModule.Agents.Module;

public sealed class AgentFileService(IStorageProvider storageProvider)
{
    private const string AgentFilesPrefix = "agents/";

    public async Task<StorageResult> SaveFileAsync(
        string agentName,
        string fileName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        var path = $"{AgentFilesPrefix}{agentName}/{fileName}";
        return await storageProvider.SaveAsync(path, content, contentType, cancellationToken);
    }

    public async Task<Stream?> GetFileAsync(
        string agentName,
        string fileName,
        CancellationToken cancellationToken = default
    )
    {
        var path = $"{AgentFilesPrefix}{agentName}/{fileName}";
        return await storageProvider.GetAsync(path, cancellationToken);
    }

    public async Task<IReadOnlyList<StorageEntry>> ListFilesAsync(
        string agentName,
        CancellationToken cancellationToken = default
    )
    {
        var prefix = $"{AgentFilesPrefix}{agentName}/";
        return await storageProvider.ListAsync(prefix, cancellationToken);
    }

    public async Task<bool> DeleteFileAsync(
        string agentName,
        string fileName,
        CancellationToken cancellationToken = default
    )
    {
        var path = $"{AgentFilesPrefix}{agentName}/{fileName}";
        return await storageProvider.DeleteAsync(path, cancellationToken);
    }
}
