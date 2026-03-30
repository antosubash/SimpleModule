using BenchmarkDotNet.Attributes;
using SimpleModule.FileStorage;
using SimpleModule.Tests.Shared.Fixtures;

namespace SimpleModule.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public sealed class FileStorageBenchmarks : IDisposable
{
    private SimpleModuleWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [GlobalSetup]
    public void Setup()
    {
        _factory = new SimpleModuleWebApplicationFactory();
        _client = _factory.CreateAuthenticatedClient([
            FileStoragePermissions.View,
            FileStoragePermissions.Upload,
            FileStoragePermissions.Delete,
        ]);
    }

    [GlobalCleanup]
    public void Cleanup() => Dispose();

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Benchmark]
    public async Task<HttpResponseMessage> GetAllFiles() =>
        await _client.GetAsync("/api/files");

    [Benchmark]
    public async Task<HttpResponseMessage> ListFolders() =>
        await _client.GetAsync("/api/files/folders");
}
