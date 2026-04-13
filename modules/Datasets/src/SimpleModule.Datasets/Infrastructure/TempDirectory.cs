namespace SimpleModule.Datasets.Infrastructure;

/// <summary>
/// Creates a uniquely-named temporary directory that is deleted on disposal.
/// </summary>
internal sealed class TempDirectory : IDisposable
{
    public string Path { get; }

    public TempDirectory(string prefix)
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

#pragma warning disable CA1031
    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch
        { /* best-effort cleanup */
        }
    }
#pragma warning restore CA1031
}
