namespace SimpleModule.Storage;

public static class StoragePathHelper
{
    public static string Normalize(string path)
    {
        var normalized = path.Replace('\\', '/').Trim('/').Trim();
        return normalized;
    }

    public static string Combine(string? folder, string fileName)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return fileName;
        }

        return $"{Normalize(folder)}/{fileName}";
    }

    public static string GetFileName(string path)
    {
        var normalized = Normalize(path);
        var lastSlash = normalized.LastIndexOf('/');
        return lastSlash < 0 ? normalized : normalized[(lastSlash + 1)..];
    }

    public static string? GetFolder(string path)
    {
        var normalized = Normalize(path);
        var lastSlash = normalized.LastIndexOf('/');
        return lastSlash < 0 ? null : normalized[..lastSlash];
    }
}
