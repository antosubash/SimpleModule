using System.Net.Http.Json;
using BenchmarkDotNet.Attributes;
using SimpleModule.PageBuilder;
using SimpleModule.Tests.Shared.Fixtures;

namespace SimpleModule.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public sealed class PageBuilderBenchmarks : IDisposable
{
    private SimpleModuleWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [GlobalSetup]
    public void Setup()
    {
        _factory = new SimpleModuleWebApplicationFactory();
        _client = _factory.CreateAuthenticatedClient([
            PageBuilderPermissions.View,
            PageBuilderPermissions.Create,
            PageBuilderPermissions.Update,
            PageBuilderPermissions.Delete,
            PageBuilderPermissions.Publish,
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
    public async Task<HttpResponseMessage> GetAllPages() =>
        await _client.GetAsync("/api/pagebuilder");

    [Benchmark]
    public async Task<HttpResponseMessage> GetAllTags() =>
        await _client.GetAsync("/api/pagebuilder/tags");

    [Benchmark]
    public async Task<HttpResponseMessage> GetAllTemplates() =>
        await _client.GetAsync("/api/pagebuilder/templates");

    [Benchmark]
    public async Task<HttpResponseMessage> GetTrash() =>
        await _client.GetAsync("/api/pagebuilder/trash");
}
